using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations.Shopee;

namespace WebHoanTien.Admin;

public class ShopeeSettlementStagingService : ITransientDependency
{
    private readonly ShopeeCanonicalSettlementReportParser _canonicalParser;
    private readonly ShopeeSettlementReportParser _legacyParser;
    private readonly IRepository<ShopeeSettlementBatch, Guid> _batches;
    private readonly IRepository<ShopeeSettlementBill, Guid> _bills;
    private readonly IRepository<ShopeeSettlementRecord, Guid> _records;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;

    public ShopeeSettlementStagingService(ShopeeCanonicalSettlementReportParser canonicalParser,
        ShopeeSettlementReportParser legacyParser, IRepository<ShopeeSettlementBatch, Guid> batches,
        IRepository<ShopeeSettlementBill, Guid> bills, IRepository<ShopeeSettlementRecord, Guid> records,
        IRepository<AffiliateOrder, Guid> orders, IRepository<AffiliateConversion, Guid> conversions,
        IGuidGenerator guidGenerator, IClock clock)
    {
        _canonicalParser = canonicalParser;
        _legacyParser = legacyParser;
        _batches = batches;
        _bills = bills;
        _records = records;
        _orders = orders;
        _conversions = conversions;
        _guidGenerator = guidGenerator;
        _clock = clock;
    }

    public async Task<ShopeeSettlementImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        ShopeeSettlementImportSource source, CancellationToken cancellationToken = default)
    {
        if (reportStream is null || !reportStream.CanRead)
            throw Invalid("Không đọc được file đối soát Shopee.");
        var extension = Path.GetExtension(reportFileName).ToLowerInvariant();
        if (extension is not ".csv" and not ".txt")
            throw Invalid("Chỉ hỗ trợ file đối soát CSV hoặc TXT.");

        await using var buffer = new MemoryStream();
        await reportStream.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0) throw Invalid("File đối soát đang trống.");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var existingBatch = await _batches.FindAsync(batch => batch.ContentHash == hash,
            cancellationToken: cancellationToken);
        if (existingBatch is not null)
            return ToResult(existingBatch, isDuplicate: true);

        var normalized = await ParseAsync(bytes, reportFileName, hash, source, cancellationToken);
        var existingBillCount = 0;
        var newBills = new List<NormalizedBill>();
        foreach (var bill in normalized)
        {
            var existingBill = await _bills.FindAsync(value => value.SourceAffiliateId == bill.SourceAffiliateId &&
                value.ValidationId == bill.ValidationId, cancellationToken: cancellationToken);
            if (existingBill is null)
            {
                newBills.Add(bill);
                continue;
            }

            var existingRows = await _records.GetListAsync(record => record.BillId == existingBill.Id,
                cancellationToken: cancellationToken);
            EnsureSameBill(existingBill, existingRows, bill);
            existingBillCount++;
        }

        if (newBills.Count == 0)
        {
            return new ShopeeSettlementImportResultDto
            {
                ImportedRowCount = 0,
                ValidationCount = normalized.Count,
                AlreadyImportedValidationCount = existingBillCount,
                IsDuplicate = true
            };
        }

        var allOrderIds = newBills.SelectMany(bill => bill.Rows).Select(row => row.ExternalOrderId)
            .Distinct(StringComparer.Ordinal).ToList();
        foreach (var chunk in allOrderIds.Chunk(500))
        {
            var conflictingRecords = await _records.GetListAsync(
                record => chunk.Contains(record.ExternalOrderId), cancellationToken: cancellationToken);
            if (conflictingRecords.Count > 0)
                throw Invalid($"Đơn hàng {conflictingRecords[0].ExternalOrderId} đã thuộc một batch đối soát khác.");
        }

        var candidateOrders = new List<AffiliateOrder>();
        foreach (var chunk in allOrderIds.Chunk(500))
            candidateOrders.AddRange(await _orders.GetListAsync(order => chunk.Contains(order.ExternalOrderId),
                cancellationToken: cancellationToken));
        var conversionIds = candidateOrders.Select(order => order.ConversionId).Distinct().ToList();
        var candidateConversions = new List<AffiliateConversion>();
        foreach (var chunk in conversionIds.Chunk(500))
            candidateConversions.AddRange(await _conversions.GetListAsync(conversion => chunk.Contains(conversion.Id),
                cancellationToken: cancellationToken));
        var conversions = candidateConversions.Where(conversion => conversion.Platform == AffiliatePlatform.Shopee)
            .ToDictionary(conversion => conversion.Id);
        var ordersByExternalId = candidateOrders.Where(order => conversions.ContainsKey(order.ConversionId))
            .GroupBy(order => order.ExternalOrderId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        var batch = new ShopeeSettlementBatch(_guidGenerator.Create(), source,
            Path.GetFileName(reportFileName), hash);
        await _batches.InsertAsync(batch, autoSave: false, cancellationToken: cancellationToken);
        var records = new List<ShopeeSettlementRecord>();
        foreach (var inputBill in newBills)
        {
            var bill = new ShopeeSettlementBill(_guidGenerator.Create(), batch.Id, inputBill.SourceAffiliateId,
                inputBill.ValidationId, inputBill.PayoutId, inputBill.PaidAt, inputBill.OrderCompletedFrom,
                inputBill.OrderCompletedTo, inputBill.EligibleCommission, inputBill.AfterServiceFeeCommission,
                inputBill.PaidCommission, inputBill.HasAuthoritativeEligibleCommission, inputBill.Rows.Count);
            await _bills.InsertAsync(bill, autoSave: false, cancellationToken: cancellationToken);

            foreach (var inputRow in inputBill.Rows)
            {
                var record = new ShopeeSettlementRecord(_guidGenerator.Create(), batch.Id, bill.Id,
                    inputRow.ExternalOrderId, inputRow.EligibleCommission, inputRow.AllocatedServiceFee,
                    inputRow.AllocatedTax, inputRow.ActualPaidCommission);
                if (!ordersByExternalId.TryGetValue(inputRow.ExternalOrderId, out var matches) || matches.Count == 0)
                {
                    record.SetUnmatched("Không tìm thấy đơn hàng tương ứng trong CatsBack.");
                }
                else if (matches.Count != 1)
                {
                    record.SetUnmatched("Tìm thấy nhiều đơn hàng có cùng ID; cần kiểm tra dữ liệu nguồn.");
                }
                else
                {
                    var order = matches[0];
                    var conversion = conversions[order.ConversionId];
                    if (!conversion.UserId.HasValue)
                        record.SetUnmatched("Đơn hàng có trong CatsBack nhưng chưa được ghép với người dùng.");
                    else if (inputBill.HasAuthoritativeEligibleCommission &&
                             !CloseMoney(inputRow.EligibleCommission, order.NetCommission))
                        record.SetInvalid(order.Id, conversion.Id, conversion.UserId,
                            "Hoa hồng hợp lệ từ bảng kê lệch với hoa hồng đơn hàng trong CatsBack.");
                    else if (!inputBill.HasAuthoritativeEligibleCommission &&
                             !NotGreaterThan(inputRow.ActualPaidCommission, order.NetCommission))
                        record.SetInvalid(order.Id, conversion.Id, conversion.UserId,
                            "Tiền thực trả trong file lớn hơn hoa hồng đơn hàng trong CatsBack.");
                    else if (order.Status == AffiliateOrderStatus.Completed)
                        record.SetPendingApproval(order.Id, conversion.Id, conversion.UserId);
                    else if (order.Status == AffiliateOrderStatus.Settled)
                        record.SetAlreadySettled(order.Id, conversion.Id, conversion.UserId);
                    else
                        record.SetInvalid(order.Id, conversion.Id, conversion.UserId,
                            $"Đơn hàng đang ở trạng thái {order.Status}, chưa thể duyệt đối soát.");
                }
                records.Add(record);
            }
        }

        await _records.InsertManyAsync(records, autoSave: false, cancellationToken: cancellationToken);
        UpdateBatch(batch, newBills.Count, records);
        await _batches.UpdateAsync(batch, autoSave: true, cancellationToken: cancellationToken);
        var result = ToResult(batch, isDuplicate: false);
        result.ValidationCount = newBills.Count;
        result.AlreadyImportedValidationCount = existingBillCount;
        return result;
    }

    private async Task<List<NormalizedBill>> ParseAsync(byte[] bytes, string fileName, string hash,
        ShopeeSettlementImportSource source, CancellationToken cancellationToken)
    {
        var head = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 4096));
        if (head.Contains("schema_version", StringComparison.OrdinalIgnoreCase))
        {
            await using var stream = new MemoryStream(bytes, writable: false);
            var report = await _canonicalParser.ParseAsync(stream, cancellationToken);
            return report.Rows.GroupBy(row => new { row.SourceAffiliateId, row.ValidationId })
                .Select(group =>
                {
                    var first = group.First();
                    return new NormalizedBill(first.SourceAffiliateId, first.ValidationId, first.PayoutId,
                        first.PaidAt, first.OrderCompletedFrom, first.OrderCompletedTo,
                        first.BillEligibleCommission, first.BillAfterServiceFeeCommission,
                        first.BillPaidCommission, true, group.Select(row => new NormalizedRow(row.ExternalOrderId,
                            row.OrderEligibleCommission, row.AllocatedServiceFee, row.AllocatedTax,
                            row.ActualPaidCommission)).ToList());
                }).ToList();
        }

        if (source != ShopeeSettlementImportSource.Manual)
            throw Invalid("Tool chỉ được import file theo schema catsback-settlement-v1.");
        await using var legacyStream = new MemoryStream(bytes, writable: false);
        var legacy = await _legacyParser.ParseAsync(legacyStream, cancellationToken);
        var paidAt = legacy.Rows.Max(row => row.PaidAt) ?? _clock.Now;
        var payoutId = legacy.Rows.Select(row => row.PaymentReference)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? Path.GetFileNameWithoutExtension(fileName);
        var rows = legacy.Rows.Select(row => new NormalizedRow(row.ExternalOrderId, row.ActualPaidCommission,
            0m, 0m, row.ActualPaidCommission)).ToList();
        var total = rows.Sum(row => row.ActualPaidCommission);
        return new List<NormalizedBill>
        {
            new("manual", $"manual-{hash[..24]}", payoutId, paidAt, null, null, total, total, total, false, rows)
        };
    }

    private static void UpdateBatch(ShopeeSettlementBatch batch, int billCount,
        IReadOnlyCollection<ShopeeSettlementRecord> records)
    {
        batch.UpdateSummary(billCount, records.Count,
            records.Count(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval),
            records.Count(record => record.Status == ShopeeSettlementRecordStatus.Approved),
            records.Count(record => record.Status == ShopeeSettlementRecordStatus.Unmatched),
            records.Count(record => record.Status == ShopeeSettlementRecordStatus.AlreadySettled),
            records.Count(record => record.Status == ShopeeSettlementRecordStatus.Invalid),
            records.Sum(record => record.EligibleCommission),
            records.Sum(record => record.ActualPaidCommission),
            records.Where(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval)
                .Sum(record => record.ActualPaidCommission),
            records.Where(record => record.Status == ShopeeSettlementRecordStatus.Approved)
                .Sum(record => record.ActualPaidCommission));
    }

    private static void EnsureSameBill(ShopeeSettlementBill existingBill,
        IReadOnlyCollection<ShopeeSettlementRecord> existingRows, NormalizedBill inputBill)
    {
        var sameMetadata = string.Equals(existingBill.PayoutId, inputBill.PayoutId, StringComparison.Ordinal) &&
            CloseDate(existingBill.PaidAt, inputBill.PaidAt) &&
            CloseDate(existingBill.OrderCompletedFrom, inputBill.OrderCompletedFrom) &&
            CloseDate(existingBill.OrderCompletedTo, inputBill.OrderCompletedTo) &&
            existingBill.EligibleCommission == inputBill.EligibleCommission &&
            existingBill.AfterServiceFeeCommission == inputBill.AfterServiceFeeCommission &&
            existingBill.PaidCommission == inputBill.PaidCommission &&
            existingBill.HasAuthoritativeEligibleCommission == inputBill.HasAuthoritativeEligibleCommission &&
            existingBill.RecordCount == inputBill.Rows.Count && existingRows.Count == inputBill.Rows.Count;
        if (!sameMetadata)
            throw Invalid($"Bảng kê {inputBill.ValidationId} đã import trước đó nhưng metadata hoặc tổng tiền đã thay đổi.");

        var rowsByOrder = existingRows.ToDictionary(row => row.ExternalOrderId, StringComparer.Ordinal);
        foreach (var inputRow in inputBill.Rows)
        {
            if (!rowsByOrder.TryGetValue(inputRow.ExternalOrderId, out var existingRow) ||
                existingRow.EligibleCommission != inputRow.EligibleCommission ||
                existingRow.AllocatedServiceFee != inputRow.AllocatedServiceFee ||
                existingRow.AllocatedTax != inputRow.AllocatedTax ||
                existingRow.ActualPaidCommission != inputRow.ActualPaidCommission)
                throw Invalid($"Bảng kê {inputBill.ValidationId} đã import trước đó nhưng chi tiết đơn hàng đã thay đổi.");
        }
    }

    private static bool CloseDate(DateTime? left, DateTime? right)
    {
        if (!left.HasValue || !right.HasValue) return left.HasValue == right.HasValue;
        return Math.Abs((left.Value - right.Value).TotalSeconds) <= 1d;
    }

    private static bool CloseMoney(decimal left, decimal right)
    {
        var tolerance = Math.Max(1m, Math.Abs(right) * 0.0001m);
        return Math.Abs(left - right) <= tolerance;
    }

    private static bool NotGreaterThan(decimal value, decimal upperBound)
    {
        var tolerance = Math.Max(1m, Math.Abs(upperBound) * 0.0001m);
        return value <= upperBound + tolerance;
    }

    private static ShopeeSettlementImportResultDto ToResult(ShopeeSettlementBatch batch, bool isDuplicate) => new()
    {
        BatchId = batch.Id,
        ImportedRowCount = batch.RecordCount,
        ValidationCount = batch.BillCount,
        PendingApprovalCount = batch.PendingCount,
        ApprovedCount = batch.ApprovedCount,
        AlreadySettledCount = batch.AlreadySettledCount,
        UnmatchedCount = batch.UnmatchedCount,
        ErrorCount = batch.InvalidCount,
        IsDuplicate = isDuplicate
    };

    private static UserFriendlyException Invalid(string message) =>
        new(message, code: WebHoanTienDomainErrorCodes.InvalidShopeeSettlementReport);

    private sealed record NormalizedBill(string SourceAffiliateId, string ValidationId, string PayoutId,
        DateTime PaidAt, DateTime? OrderCompletedFrom, DateTime? OrderCompletedTo, decimal EligibleCommission,
        decimal AfterServiceFeeCommission, decimal PaidCommission, bool HasAuthoritativeEligibleCommission,
        IReadOnlyList<NormalizedRow> Rows);

    private sealed record NormalizedRow(string ExternalOrderId, decimal EligibleCommission,
        decimal AllocatedServiceFee, decimal AllocatedTax, decimal ActualPaidCommission);
}
