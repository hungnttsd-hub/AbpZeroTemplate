using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations.Shopee;
using WebHoanTien.Operations;

namespace WebHoanTien.Admin;

public class ShopeeReportImporter
{
    private readonly ShopeeReportParser _parser;
    private readonly AffiliateConversionUpserter _upserter;
    private readonly IRepository<AffiliateSyncRun, Guid> _runs;
    private readonly IRepository<AffiliateRawPayload, Guid> _payloads;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly ILogger<ShopeeReportImporter> _logger;

    public ShopeeReportImporter(ShopeeReportParser parser, AffiliateConversionUpserter upserter,
        IRepository<AffiliateSyncRun, Guid> runs, IRepository<AffiliateRawPayload, Guid> payloads,
        IGuidGenerator guidGenerator, IClock clock, ILogger<ShopeeReportImporter> logger)
    {
        _parser = parser;
        _upserter = upserter;
        _runs = runs;
        _payloads = payloads;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _logger = logger;
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

        foreach (var conversion in parsed.Conversions)
        {
            try
            {
                var upsert = await _upserter.UpsertAsync(AffiliatePlatform.Shopee, conversion);
                if (upsert.Inserted) result.InsertedCount++;
                else result.UpdatedCount++;
                if (!upsert.Matched) result.UnmatchedCount++;
            }
            catch (Exception exception)
            {
                result.ErrorCount++;
                if (result.Errors.Count < 20)
                {
                    result.Errors.Add($"{conversion.ExternalConversionId}: {exception.Message}");
                }
                _logger.LogError(exception, "Không thể import conversion Shopee {ConversionId}",
                    conversion.ExternalConversionId);
            }
        }

        var metadata = JsonSerializer.Serialize(new
        {
            FileName = Path.GetFileName(reportFileName),
            parsed.RowCount,
            Columns = parsed.Headers
        });
        await _payloads.InsertAsync(new AffiliateRawPayload(_guidGenerator.Create(), run.Id, null,
            "ShopeeReportImport", metadata, now.AddDays(WebHoanTienConsts.RetentionDays)), autoSave: true,
            cancellationToken: cancellationToken);
        run.Complete(_clock.Now, parsed.RowCount, result.InsertedCount, result.UpdatedCount, result.UnmatchedCount,
            result.ErrorCount, result.ErrorCount == 0 ? null : $"{result.ErrorCount} conversion lỗi");
        await _runs.UpdateAsync(run, autoSave: true, cancellationToken: cancellationToken);
        return result;
    }
}
