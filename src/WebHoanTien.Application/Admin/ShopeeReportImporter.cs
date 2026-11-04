using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations;
using WebHoanTien.Integrations.Shopee;
using WebHoanTien.Operations;

namespace WebHoanTien.Admin;

public class ShopeeReportImporter
{
    private const int ImportBatchSize = 100;
    private readonly ShopeeReportParser _parser;
    private readonly AffiliateConversionUpserter _upserter;
    private readonly IRepository<AffiliateSyncRun, Guid> _runs;
    private readonly IRepository<AffiliateRawPayload, Guid> _payloads;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly ILogger<ShopeeReportImporter> _logger;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<AffiliateOrder, Guid> _orders;

    public ShopeeReportImporter(ShopeeReportParser parser, AffiliateConversionUpserter upserter,
        IRepository<AffiliateSyncRun, Guid> runs, IRepository<AffiliateRawPayload, Guid> payloads,
        IGuidGenerator guidGenerator, IClock clock, ILogger<ShopeeReportImporter> logger,
        IUnitOfWorkManager unitOfWorkManager, IRepository<AffiliateOrder, Guid> orders)
    {
        _parser = parser;
        _upserter = upserter;
        _runs = runs;
        _payloads = payloads;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _logger = logger;
        _unitOfWorkManager = unitOfWorkManager;
        _orders = orders;
    }

    public async Task<ShopeeReportImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        CancellationToken cancellationToken = default)
    {
        if (reportStream is null || !reportStream.CanRead)
        {
            throw new UserFriendlyException("Không đọc được file báo cáo.", code: WebHoanTienDomainErrorCodes.InvalidShopeeReport);
        }

        var extension = Path.GetExtension(reportFileName).ToLowerInvariant();
        if (extension is not ".csv" and not ".txt")
        {
            throw new UserFriendlyException("Chỉ hỗ trợ báo cáo Shopee định dạng CSV hoặc TXT.",
                code: WebHoanTienDomainErrorCodes.InvalidShopeeReport);
        }

        var timer = Stopwatch.StartNew();
        var parsed = await _parser.ParseAsync(reportStream, cancellationToken);
        var now = _clock.Now;
        var run = new AffiliateSyncRun(_guidGenerator.Create(), AffiliatePlatform.Shopee,
            AffiliateSyncKind.Import, now, now, now);
        await _runs.InsertAsync(run, autoSave: true, cancellationToken: cancellationToken);

        var result = new ShopeeReportImportResultDto
        {
            ImportedRowCount = parsed.RowCount,
            ConversionCount = parsed.Conversions.Count
        };

        foreach (var batch in parsed.Conversions.Chunk(ImportBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                foreach (var upsert in await ImportBatchAsync(batch, cancellationToken))
                    AddResult(result, upsert);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // The failed transaction has been disposed/rolled back before retrying in fresh units of work.
                // Isolate bad orders without losing the rest of a large report.
                _logger.LogWarning(exception, "Import lô Shopee thất bại; thử lại riêng {Count} đơn", batch.Length);
                foreach (var conversion in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        foreach (var upsert in await ImportBatchAsync(new[] { conversion }, cancellationToken))
                            AddResult(result, upsert);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception orderException)
                    {
                        result.ErrorCount++;
                        if (result.Errors.Count < 20)
                            result.Errors.Add($"{conversion.ExternalConversionId}: {orderException.Message}");
                        _logger.LogError(orderException, "Không thể import conversion Shopee {ConversionId}",
                            conversion.ExternalConversionId);
                    }
                }
            }
        }

        var metadata = JsonSerializer.Serialize(new
        {
            FileName = Path.GetFileName(reportFileName),
            parsed.RowCount,
            result.SkippedSettledCount,
            Columns = parsed.Headers
        });
        await _payloads.InsertAsync(new AffiliateRawPayload(_guidGenerator.Create(), run.Id, null,
            "ShopeeReportImport", metadata, now.AddDays(WebHoanTienConsts.RetentionDays)), autoSave: true,
            cancellationToken: cancellationToken);
        var errorSummary = result.ErrorCount == 0
            ? null
            : string.Join(" | ", result.Errors);
        if (errorSummary?.Length > 4000) errorSummary = errorSummary[..4000];
        run.Complete(_clock.Now, parsed.RowCount, result.InsertedCount, result.UpdatedCount, result.UnmatchedCount,
            result.ErrorCount, errorSummary ?? $"{result.ErrorCount} conversion lỗi");
        await _runs.UpdateAsync(run, autoSave: true, cancellationToken: cancellationToken);
        _logger.LogInformation(
            "Import Shopee hoàn tất: {RowCount} dòng, thêm {InsertedCount}, cập nhật {UpdatedCount}, " +
            "bỏ qua đã duyệt {SkippedSettledCount}, lỗi {ErrorCount}, {ElapsedMilliseconds} ms",
            parsed.RowCount, result.InsertedCount, result.UpdatedCount, result.SkippedSettledCount,
            result.ErrorCount, timer.ElapsedMilliseconds);
        return result;
    }

    private async Task<List<AffiliateConversionUpsertResult>> ImportBatchAsync(
        IReadOnlyCollection<NormalizedAffiliateConversion> conversions, CancellationToken cancellationToken)
    {
        using var work = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        var externalIds = conversions.SelectMany(x => x.Orders).Select(x => x.ExternalOrderId)
            .Distinct(StringComparer.Ordinal).ToList();
        var settledIds = (await _orders.GetListAsync(x => x.Platform == AffiliatePlatform.Shopee &&
                externalIds.Contains(x.ExternalOrderId) &&
                (x.Status == AffiliateOrderStatus.Settled || x.SettledAt.HasValue),
                cancellationToken: cancellationToken))
            .Select(x => x.ExternalOrderId).ToHashSet(StringComparer.Ordinal);
        var batch = new AffiliateConversionImportBatch();
        var results = new List<AffiliateConversionUpsertResult>();
        foreach (var conversion in conversions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (conversion.Orders.Count > 0 && conversion.Orders.All(x => settledIds.Contains(x.ExternalOrderId)))
            {
                results.Add(new AffiliateConversionUpsertResult(false, false, SkippedSettled: true));
                continue;
            }

            results.Add(await _upserter.UpsertAsync(AffiliatePlatform.Shopee, conversion, batch));
        }

        await _upserter.CompleteImportBatchAsync(AffiliatePlatform.Shopee, batch);
        await work.CompleteAsync(cancellationToken);
        return results;
    }

    private static void AddResult(ShopeeReportImportResultDto result, AffiliateConversionUpsertResult upsert)
    {
        if (upsert.SkippedSettled)
        {
            result.SkippedSettledCount++;
            return;
        }
        if (upsert.Inserted) result.InsertedCount++;
        else result.UpdatedCount++;
        if (!upsert.Matched) result.UnmatchedCount++;
        result.MatchedItemCount += upsert.MatchedItemCount;
        result.UnmatchedItemCount += upsert.UnmatchedItemCount;
        result.MultiTrackingOrderCount += upsert.MultiTrackingOrderCount;
        if (!string.IsNullOrWhiteSpace(upsert.ConflictMessage))
        {
            result.ErrorCount++;
            if (result.Errors.Count < 20) result.Errors.Add(upsert.ConflictMessage);
        }
    }
}
