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
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly ShopeeSettlementMatcher _matcher;

    public ShopeeSettlementStagingService(ShopeeCanonicalSettlementReportParser canonicalParser,
        ShopeeSettlementReportParser legacyParser, IRepository<ShopeeSettlementBatch, Guid> batches,
        IRepository<ShopeeSettlementBill, Guid> bills, IRepository<ShopeeSettlementRecord, Guid> records,
        IGuidGenerator guidGenerator, IClock clock, ShopeeSettlementMatcher matcher)
    {
        _canonicalParser = canonicalParser;
        _legacyParser = legacyParser;
        _batches = batches;
        _bills = bills;
        _records = records;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _matcher = matcher;
    }

    public async Task<ShopeeSettlementImportResultDto> ImportAsync(Stream reportStream, string reportFileName,
        ShopeeSettlementImportSource source, CancellationToken cancellationToken = default)
    {
        if (reportStream is null || !reportStream.CanRead)
            throw Invalid("Không đọc được file đối soát Shopee.");
        var extension = Path.GetExtension(reportFileName).ToLowerInvariant();
        if (extension is not ".csv" and not ".txt" and not ".json")
            throw Invalid("Chỉ hỗ trợ file đối soát JSON, CSV hoặc TXT.");

        await using var buffer = new MemoryStream();
        await reportStream.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0) throw Invalid("File đối soát đang trống.");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var normalized = await ParseAsync(bytes, reportFileName, hash, source, cancellationToken);
        var existingBillCount = 0;
        Guid? firstExistingBatchId = null;
        var newBills = new List<NormalizedBill>();
        var updatedBills = new List<UpdatedBill>();
        var resultRows = new List<ShopeeSettlementRecord>();
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
            resultRows.AddRange(existingRows);
            firstExistingBatchId ??= existingBill.BatchId;
            if (SameBill(existingBill, existingRows, bill))
            {
                await _matcher.RefreshAsync(existingRows);
                existingBillCount++;
                continue;
            }

            EnsureCanUpdate(existingBill, existingRows, bill);
            existingBill.UpdateFromImport(bill.PayoutId, bill.PaidAt, bill.OrderCompletedFrom,
                bill.OrderCompletedTo, bill.EligibleCommission, bill.AfterServiceFeeCommission,
                bill.PaidCommission, bill.HasAuthoritativeEligibleCommission, bill.Rows.Count,
                bill.PaymentStatus, bill.ValidationPayoutStatus, bill.OverallValidationStatus,
                bill.BillValidationStatus, bill.SettlementCycle, bill.HasAdjustment, bill.HasClawback,
                bill.IsCumulative, bill.HasBonus, bill.HasPpp);
            var existingByOrder = existingRows.ToDictionary(row => row.ExternalOrderId, StringComparer.Ordinal);
            foreach (var inputRow in bill.Rows)
            {
                existingByOrder[inputRow.ExternalOrderId].UpdateAmounts(inputRow.EligibleCommission,
                    inputRow.AllocatedServiceFee, inputRow.AllocatedTax, inputRow.ActualPaidCommission);
            }
            updatedBills.Add(new UpdatedBill(existingBill, existingRows, bill));
        }

        if (newBills.Count == 0 && updatedBills.Count == 0)
        {
            var duplicateResult = ToResult(firstExistingBatchId!.Value, resultRows, normalized.Count,
                isDuplicate: true);
            duplicateResult.AlreadyImportedValidationCount = existingBillCount;
            return duplicateResult;
        }

        var newOrderIds = newBills.SelectMany(bill => bill.Rows).Select(row => row.ExternalOrderId)
            .Distinct(StringComparer.Ordinal).ToList();
        foreach (var chunk in newOrderIds.Chunk(500))
        {
            var chunkIds = chunk.ToList();
            var conflictingRecords = await _records.GetListAsync(
                record => chunkIds.Contains(record.ExternalOrderId), cancellationToken: cancellationToken);
            if (conflictingRecords.Count > 0)
                throw Invalid($"Đơn hàng {conflictingRecords[0].ExternalOrderId} đã thuộc một batch đối soát khác.");
        }

        ShopeeSettlementBatch? batch = null;
        var records = new List<ShopeeSettlementRecord>();
        if (newBills.Count > 0)
        {
            batch = new ShopeeSettlementBatch(_guidGenerator.Create(), source,
                Path.GetFileName(reportFileName), hash);
            await _batches.InsertAsync(batch, autoSave: false, cancellationToken: cancellationToken);
        }
        foreach (var inputBill in newBills)
        {
            var bill = new ShopeeSettlementBill(_guidGenerator.Create(), batch!.Id, inputBill.SourceAffiliateId,
                inputBill.ValidationId, inputBill.PayoutId, inputBill.PaidAt, inputBill.OrderCompletedFrom,
                inputBill.OrderCompletedTo, inputBill.EligibleCommission, inputBill.AfterServiceFeeCommission,
                inputBill.PaidCommission, inputBill.HasAuthoritativeEligibleCommission, inputBill.Rows.Count,
                inputBill.PaymentStatus, inputBill.ValidationPayoutStatus, inputBill.OverallValidationStatus,
                inputBill.BillValidationStatus, inputBill.SettlementCycle, inputBill.HasAdjustment,
                inputBill.HasClawback, inputBill.IsCumulative, inputBill.HasBonus, inputBill.HasPpp);
            await _bills.InsertAsync(bill, autoSave: false, cancellationToken: cancellationToken);

            foreach (var inputRow in inputBill.Rows)
            {
                var record = new ShopeeSettlementRecord(_guidGenerator.Create(), batch.Id, bill.Id,
                    inputRow.ExternalOrderId, inputRow.EligibleCommission, inputRow.AllocatedServiceFee,
                    inputRow.AllocatedTax, inputRow.ActualPaidCommission);

                records.Add(record);
                resultRows.Add(record);
            }
        }

        var updatedRecords = updatedBills.SelectMany(item => item.Records).ToList();

        if (records.Count > 0)
            await _records.InsertManyAsync(records, autoSave: true, cancellationToken: cancellationToken);
        if (updatedBills.Count > 0)
        {
            await _bills.UpdateManyAsync(updatedBills.Select(item => item.Entity), autoSave: false,
                cancellationToken: cancellationToken);
            await _records.UpdateManyAsync(updatedRecords, autoSave: false, cancellationToken: cancellationToken);
            foreach (var batchId in updatedBills.Select(item => item.Entity.BatchId).Distinct())
            {
                var refreshedBatch = await _batches.GetAsync(batchId, cancellationToken: cancellationToken);
                var existingRecords = await _records.GetListAsync(record => record.BatchId == batchId,
                    cancellationToken: cancellationToken);
                UpdateBatch(refreshedBatch, refreshedBatch.BillCount, existingRecords);
                await _batches.UpdateAsync(refreshedBatch, autoSave: false, cancellationToken: cancellationToken);
                batch ??= refreshedBatch;
            }
        }
        if (newBills.Count > 0)
        {
            UpdateBatch(batch!, newBills.Count, records);
            await _batches.UpdateAsync(batch!, autoSave: false, cancellationToken: cancellationToken);
        }
        await _matcher.RefreshAsync(resultRows);
        var result = ToResult(batch!.Id, resultRows, normalized.Count, isDuplicate: false);
        result.AlreadyImportedValidationCount = existingBillCount;
        result.UpdatedValidationCount = updatedBills.Count;
        return result;
    }

    private async Task<List<NormalizedBill>> ParseAsync(byte[] bytes, string fileName, string hash,
        ShopeeSettlementImportSource source, CancellationToken cancellationToken)
    {
        var head = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 4096));
        if (Path.GetExtension(fileName).Equals(".json", StringComparison.OrdinalIgnoreCase) &&
            !head.TrimStart('\ufeff', ' ', '\r', '\n', '\t').StartsWith("{", StringComparison.Ordinal))
            throw Invalid("File JSON phải là báo cáo được xuất từ tool CatsBack.");
        if (Path.GetExtension(fileName).Equals(".json", StringComparison.OrdinalIgnoreCase) ||
            head.TrimStart('\ufeff', ' ', '\r', '\n', '\t').StartsWith("{", StringComparison.Ordinal) ||
            head.Contains("schema_version", StringComparison.OrdinalIgnoreCase))
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
                        first.BillPaidCommission, true, first.PaymentStatus, first.ValidationPayoutStatus,
                        first.OverallValidationStatus, first.BillValidationStatus, first.SettlementCycle,
                        first.HasAdjustment, first.HasClawback, first.IsCumulative, first.HasBonus, first.HasPpp,
                        group.Select(row => new NormalizedRow(row.ExternalOrderId,
                            row.OrderEligibleCommission, row.AllocatedServiceFee, row.AllocatedTax,
                            row.ActualPaidCommission)).ToList());
                }).ToList();
        }

        if (source != ShopeeSettlementImportSource.Manual)
            throw Invalid("Tool chỉ được import file theo schema catsback-settlement-v1 hoặc catsback-settlement-v2.");
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
            new("manual", $"manual-{hash[..24]}", payoutId, paidAt, null, null, total, total, total, false,
                4, 2, null, null, null, false, false, false, false, false, rows)
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
            records.Count(record => record.Status == ShopeeSettlementRecordStatus.AwaitingShopeePayment),
            records.Sum(record => record.EligibleCommission),
            records.Sum(record => record.ActualPaidCommission),
            records.Where(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval)
                .Sum(record => record.ActualPaidCommission),
            records.Where(record => record.Status == ShopeeSettlementRecordStatus.Approved)
                .Sum(record => record.ActualPaidCommission));
    }

    private static bool SameBill(ShopeeSettlementBill existingBill,
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
            existingBill.PaymentStatus == inputBill.PaymentStatus &&
            existingBill.ValidationPayoutStatus == inputBill.ValidationPayoutStatus &&
            existingBill.OverallValidationStatus == inputBill.OverallValidationStatus &&
            existingBill.BillValidationStatus == inputBill.BillValidationStatus &&
            existingBill.SettlementCycle == inputBill.SettlementCycle &&
            existingBill.HasAdjustment == inputBill.HasAdjustment &&
            existingBill.HasClawback == inputBill.HasClawback &&
            existingBill.IsCumulative == inputBill.IsCumulative &&
            existingBill.HasBonus == inputBill.HasBonus && existingBill.HasPpp == inputBill.HasPpp &&
            existingBill.RecordCount == inputBill.Rows.Count && existingRows.Count == inputBill.Rows.Count;
        if (!sameMetadata) return false;
        if (existingRows.Any(row => row.Status == ShopeeSettlementRecordStatus.AwaitingShopeePayment))
            return false;

        var rowsByOrder = existingRows.ToDictionary(row => row.ExternalOrderId, StringComparer.Ordinal);
        return inputBill.Rows.All(inputRow =>
            rowsByOrder.TryGetValue(inputRow.ExternalOrderId, out var existingRow) &&
            (existingRow.Status == ShopeeSettlementRecordStatus.Approved ||
             existingRow.EligibleCommission == inputRow.EligibleCommission &&
             existingRow.AllocatedServiceFee == inputRow.AllocatedServiceFee &&
             existingRow.AllocatedTax == inputRow.AllocatedTax &&
             existingRow.ActualPaidCommission == inputRow.ActualPaidCommission));
    }

    private static void EnsureCanUpdate(ShopeeSettlementBill existingBill,
        IReadOnlyCollection<ShopeeSettlementRecord> existingRows, NormalizedBill inputBill)
    {
        if (existingBill.IsShopeePaid &&
            (!inputBill.PaidAt.HasValue || inputBill.PaidAt.Value <= DateTime.UnixEpoch))
            throw Invalid($"Bảng kê {inputBill.ValidationId} đã có thời gian thanh toán. File mới thiếu thời gian này; hãy tổng hợp lại từ Shopee.");
        var existingOrderIds = existingRows.Select(row => row.ExternalOrderId).ToHashSet(StringComparer.Ordinal);
        var inputOrderIds = inputBill.Rows.Select(row => row.ExternalOrderId).ToHashSet(StringComparer.Ordinal);
        if (existingBill.RecordCount != inputBill.Rows.Count || existingRows.Count != inputBill.Rows.Count ||
            !existingOrderIds.SetEquals(inputOrderIds))
            throw Invalid($"Bảng kê {inputBill.ValidationId} đã import trước đó nhưng danh sách đơn hàng đã thay đổi.");
    }

    private static bool CloseDate(DateTime? left, DateTime? right)
    {
        if (!left.HasValue || !right.HasValue) return left.HasValue == right.HasValue;
        return Math.Abs((left.Value - right.Value).TotalSeconds) <= 1d;
    }

    private static ShopeeSettlementImportResultDto ToResult(Guid batchId,
        IReadOnlyCollection<ShopeeSettlementRecord> records, int validationCount, bool isDuplicate) => new()
    {
        BatchId = batchId,
        ImportedRowCount = records.Count,
        ValidationCount = validationCount,
        PendingApprovalCount = records.Count(record =>
            record.Status == ShopeeSettlementRecordStatus.PendingApproval),
        ApprovedCount = records.Count(record => record.Status == ShopeeSettlementRecordStatus.Approved),
        AlreadySettledCount = records.Count(record =>
            record.Status == ShopeeSettlementRecordStatus.AlreadySettled),
        UnmatchedCount = records.Count(record => record.Status == ShopeeSettlementRecordStatus.Unmatched),
        ErrorCount = records.Count(record => record.Status == ShopeeSettlementRecordStatus.Invalid),
        WaitingPaymentCount = records.Count(record =>
            record.Status == ShopeeSettlementRecordStatus.AwaitingShopeePayment),
        IsDuplicate = isDuplicate
    };

    private static UserFriendlyException Invalid(string message) =>
        new(message, code: WebHoanTienDomainErrorCodes.InvalidShopeeSettlementReport);

    private sealed record NormalizedBill(string SourceAffiliateId, string ValidationId, string PayoutId,
        DateTime? PaidAt, DateTime? OrderCompletedFrom, DateTime? OrderCompletedTo, decimal EligibleCommission,
        decimal AfterServiceFeeCommission, decimal PaidCommission, bool HasAuthoritativeEligibleCommission,
        int PaymentStatus, int ValidationPayoutStatus, int? OverallValidationStatus,
        int? BillValidationStatus, int? SettlementCycle, bool HasAdjustment, bool HasClawback,
        bool IsCumulative, bool HasBonus, bool HasPpp,
        IReadOnlyList<NormalizedRow> Rows);

    private sealed record NormalizedRow(string ExternalOrderId, decimal EligibleCommission,
        decimal AllocatedServiceFee, decimal AllocatedTax, decimal ActualPaidCommission);

    private sealed record UpdatedBill(ShopeeSettlementBill Entity,
        IReadOnlyCollection<ShopeeSettlementRecord> Records, NormalizedBill Input);
}
