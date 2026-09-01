using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;
using WebHoanTien.Notifications;
using WebHoanTien.Permissions;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Orders)]
[RemoteService(IsEnabled = false)]
public class AdminShopeeSettlementApprovalAppService : WebHoanTienAppService,
    IAdminShopeeSettlementApprovalAppService
{
    private readonly IRepository<ShopeeSettlementBatch, Guid> _batches;
    private readonly IRepository<ShopeeSettlementBill, Guid> _bills;
    private readonly IRepository<ShopeeSettlementRecord, Guid> _records;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly AffiliateCommissionCalculator _calculator;
    private readonly CustomerNotificationManager _notificationManager;

    public AdminShopeeSettlementApprovalAppService(IRepository<ShopeeSettlementBatch, Guid> batches,
        IRepository<ShopeeSettlementBill, Guid> bills, IRepository<ShopeeSettlementRecord, Guid> records,
        IRepository<AffiliateOrder, Guid> orders, IRepository<AffiliateConversion, Guid> conversions,
        AffiliateCommissionCalculator calculator, CustomerNotificationManager notificationManager)
    {
        _batches = batches;
        _bills = bills;
        _records = records;
        _orders = orders;
        _conversions = conversions;
        _calculator = calculator;
        _notificationManager = notificationManager;
    }

    public async Task<AdminShopeeSettlementPageDto> GetListAsync(AdminShopeeSettlementBatchListInput input)
    {
        var query = await _batches.GetQueryableAsync();
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim();
            query = query.Where(batch => batch.OriginalFileName.Contains(filter) ||
                batch.ContentHash.Contains(filter));
        }
        if (input.Status.HasValue) query = query.Where(batch => batch.Status == input.Status);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query.OrderByDescending(batch => batch.CreationTime)
            .Skip(input.SkipCount).Take(input.MaxResultCount));
        var all = await _batches.GetListAsync();
        return new AdminShopeeSettlementPageDto
        {
            Summary = new AdminShopeeSettlementSummaryDto
            {
                PendingCount = all.Sum(batch => batch.PendingCount),
                PendingAmount = all.Sum(batch => batch.PendingPaidCommission),
                ApprovedCount = all.Sum(batch => batch.ApprovedCount),
                ApprovedAmount = all.Sum(batch => batch.ApprovedPaidCommission),
                IssueCount = all.Sum(batch => batch.UnmatchedCount + batch.AlreadySettledCount + batch.InvalidCount)
            },
            Batches = new PagedResultDto<AdminShopeeSettlementBatchDto>(totalCount,
                rows.Select(MapBatch).ToList())
        };
    }

    public async Task<AdminShopeeSettlementBatchDetailsDto> GetAsync(Guid batchId, int skipCount = 0,
        int maxResultCount = 50)
    {
        var batch = await _batches.GetAsync(batchId);
        var query = (await _records.GetQueryableAsync()).Where(record => record.BatchId == batchId);
        var count = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query
            .OrderBy(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval ? 0 : 1)
            .ThenBy(record => record.ExternalOrderId).Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, 200)));
        var bills = (await _bills.GetListAsync(bill => bill.BatchId == batchId)).ToDictionary(bill => bill.Id);
        var conversionIds = rows.Where(row => row.AffiliateConversionId.HasValue)
            .Select(row => row.AffiliateConversionId!.Value).Distinct().ToList();
        var conversions = conversionIds.Count == 0
            ? new Dictionary<Guid, AffiliateConversion>()
            : (await _conversions.GetListAsync(value => conversionIds.Contains(value.Id)))
                .ToDictionary(value => value.Id);
        return new AdminShopeeSettlementBatchDetailsDto
        {
            Batch = MapBatch(batch),
            Records = new PagedResultDto<AdminShopeeSettlementRecordDto>(count,
                rows.Select(row => MapRecord(row, bills[row.BillId], conversions)).ToList())
        };
    }

    [UnitOfWork]
    public async Task<AdminShopeeSettlementApprovalResultDto> ApproveAsync(Guid recordId)
    {
        var record = await _records.GetAsync(recordId);
        var batch = await _batches.GetAsync(record.BatchId);
        if (record.Status != ShopeeSettlementRecordStatus.PendingApproval)
            return await BuildResultAsync(batch, Array.Empty<ApprovalWork>(), skippedCount: 1);
        var bill = await _bills.GetAsync(record.BillId);
        var work = await TryPrepareAsync(record, bill);
        if (work is null)
        {
            await RefreshBatchAsync(batch);
            return await BuildResultAsync(batch, Array.Empty<ApprovalWork>(), skippedCount: 1);
        }
        await ApplyAsync(work);
        await RefreshBatchAsync(batch);
        return await BuildResultAsync(batch, new[] { work }, skippedCount: 0);
    }

    [UnitOfWork]
    public async Task<AdminShopeeSettlementApprovalResultDto> ApproveAllAsync(Guid batchId)
    {
        var batch = await _batches.GetAsync(batchId);
        var pending = await _records.GetListAsync(record => record.BatchId == batchId &&
            record.Status == ShopeeSettlementRecordStatus.PendingApproval);
        if (pending.Count == 0)
            return await BuildResultAsync(batch, Array.Empty<ApprovalWork>(), skippedCount: 0);
        var billIds = pending.Select(record => record.BillId).Distinct().ToList();
        var bills = (await _bills.GetListAsync(bill => billIds.Contains(bill.Id))).ToDictionary(bill => bill.Id);
        var preparation = await PrepareManyAsync(pending, bills);
        var adminId = CurrentUser.Id ??
            throw new AbpAuthorizationException("Không xác định được admin duyệt đối soát.");
        foreach (var item in preparation.Work)
        {
            item.Order.Settle(item.Record.ActualPaidCommission, item.UserCommission,
                item.Bill.PayoutId, item.Bill.PaidAt);
            item.Record.Approve(adminId, Clock.Now, item.UserCommission);
        }
        await _orders.UpdateManyAsync(preparation.Work.Select(item => item.Order).DistinctBy(order => order.Id),
            autoSave: false);
        await _records.UpdateManyAsync(pending, autoSave: false);
        await _notificationManager.NotifySettledOrdersAsync(preparation.Work
            .Where(item => item.Conversion.UserId.HasValue)
            .Select(item => (item.Conversion.UserId!.Value, item.Order)));
        await RefreshBatchAsync(batch);
        return await BuildResultAsync(batch, preparation.Work, preparation.SkippedCount);
    }

    private async Task<BulkApprovalPreparation> PrepareManyAsync(List<ShopeeSettlementRecord> records,
        IReadOnlyDictionary<Guid, ShopeeSettlementBill> bills)
    {
        var duplicatedOrderIds = records.Where(record => record.AffiliateOrderId.HasValue)
            .GroupBy(record => record.AffiliateOrderId!.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
        var orderIds = records.Where(record => record.AffiliateOrderId.HasValue)
            .Select(record => record.AffiliateOrderId!.Value).Distinct().ToList();
        var orderRows = new List<AffiliateOrder>();
        foreach (var chunk in orderIds.Chunk(500))
            orderRows.AddRange(await _orders.GetListAsync(order => chunk.Contains(order.Id)));
        var orders = orderRows.ToDictionary(order => order.Id);
        var conversionIds = records.Where(record => record.AffiliateConversionId.HasValue)
            .Select(record => record.AffiliateConversionId!.Value).Distinct().ToList();
        var conversionRows = new List<AffiliateConversion>();
        foreach (var chunk in conversionIds.Chunk(500))
            conversionRows.AddRange(await _conversions.GetListAsync(conversion => chunk.Contains(conversion.Id)));
        var conversions = conversionRows.ToDictionary(conversion => conversion.Id);

        var work = new List<ApprovalWork>(records.Count);
        foreach (var record in records)
        {
            if (!record.AffiliateOrderId.HasValue || !record.AffiliateConversionId.HasValue)
            {
                record.SetInvalid("Bản ghi không còn liên kết đầy đủ với đơn hàng CatsBack.");
                continue;
            }
            if (duplicatedOrderIds.Contains(record.AffiliateOrderId.Value))
            {
                record.SetInvalid("Đơn hàng xuất hiện trong nhiều bản ghi đối soát chờ duyệt.");
                continue;
            }
            if (!orders.TryGetValue(record.AffiliateOrderId.Value, out var order) ||
                !conversions.TryGetValue(record.AffiliateConversionId.Value, out var conversion))
            {
                record.SetInvalid("Không còn tìm thấy đơn hàng hoặc lượt chuyển đổi tương ứng.");
                continue;
            }
            if (conversion.Platform != AffiliatePlatform.Shopee || order.ConversionId != conversion.Id ||
                !string.Equals(order.ExternalOrderId, record.ExternalOrderId, StringComparison.Ordinal))
            {
                record.SetInvalid("Liên kết đơn hàng Shopee đã thay đổi và không còn hợp lệ.");
                continue;
            }
            if (!conversion.UserId.HasValue)
            {
                record.SetUnmatched("Đơn hàng chưa được ghép với người dùng nên chưa thể cộng ví.");
                continue;
            }
            if (record.UserId != conversion.UserId)
            {
                record.SetInvalid("Người dùng được ghép với đơn hàng đã thay đổi; hãy bấm Đối chiếu lại trước khi duyệt.");
                continue;
            }
            if (order.Status == AffiliateOrderStatus.Settled)
            {
                record.SetAlreadySettled(order.Id, conversion.Id, conversion.UserId);
                continue;
            }
            if (order.Status != AffiliateOrderStatus.Completed)
            {
                record.SetInvalid($"Đơn hàng đang ở trạng thái {order.Status}, không còn đủ điều kiện duyệt.");
                continue;
            }
            var bill = bills[record.BillId];
            if (bill.HasAuthoritativeEligibleCommission &&
                !CloseMoney(record.EligibleCommission, order.NetCommission))
            {
                record.SetInvalid("Hoa hồng hợp lệ từ bảng kê lệch với hoa hồng đơn hàng trong CatsBack.");
                continue;
            }
            if (!bill.HasAuthoritativeEligibleCommission &&
                !NotGreaterThan(record.ActualPaidCommission, order.NetCommission))
            {
                record.SetInvalid("Tiền thực trả trong file lớn hơn hoa hồng đơn hàng trong CatsBack.");
                continue;
            }

            work.Add(new ApprovalWork(record, bill, order, conversion,
                _calculator.CalculateUserCommission(record.ActualPaidCommission, conversion.UserShareRate)));
        }

        return new BulkApprovalPreparation(work, records.Count - work.Count);
    }

    [UnitOfWork]
    public async Task<AdminShopeeSettlementRefreshResultDto> RefreshMatchesAsync(Guid batchId)
    {
        var batch = await _batches.GetAsync(batchId);
        var candidates = await _records.GetListAsync(record => record.BatchId == batchId &&
            (record.Status == ShopeeSettlementRecordStatus.Unmatched ||
             record.Status == ShopeeSettlementRecordStatus.Invalid));
        if (candidates.Count > 0)
        {
            var billIds = candidates.Select(record => record.BillId).Distinct().ToList();
            var bills = (await _bills.GetListAsync(bill => billIds.Contains(bill.Id)))
                .ToDictionary(bill => bill.Id);
            var externalOrderIds = candidates.Select(record => record.ExternalOrderId)
                .Distinct(StringComparer.Ordinal).ToList();
            var orders = new List<AffiliateOrder>();
            foreach (var chunk in externalOrderIds.Chunk(500))
                orders.AddRange(await _orders.GetListAsync(order => chunk.Contains(order.ExternalOrderId)));
            var conversionIds = orders.Select(order => order.ConversionId).Distinct().ToList();
            var conversionRows = new List<AffiliateConversion>();
            foreach (var chunk in conversionIds.Chunk(500))
                conversionRows.AddRange(await _conversions.GetListAsync(conversion => chunk.Contains(conversion.Id)));
            var conversions = conversionRows.Where(conversion => conversion.Platform == AffiliatePlatform.Shopee)
                .ToDictionary(conversion => conversion.Id);
            var ordersByExternalId = orders.Where(order => conversions.ContainsKey(order.ConversionId))
                .GroupBy(order => order.ExternalOrderId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

            foreach (var record in candidates)
            {
                if (!ordersByExternalId.TryGetValue(record.ExternalOrderId, out var matches) || matches.Count == 0)
                {
                    record.SetUnmatched("Không tìm thấy đơn hàng tương ứng trong CatsBack.");
                    continue;
                }
                if (matches.Count != 1)
                {
                    record.SetUnmatched("Tìm thấy nhiều đơn hàng có cùng ID; cần kiểm tra dữ liệu nguồn.");
                    continue;
                }

                var order = matches[0];
                var conversion = conversions[order.ConversionId];
                if (!conversion.UserId.HasValue)
                    record.SetUnmatched("Đơn hàng có trong CatsBack nhưng chưa được ghép với người dùng.");
                else if (bills[record.BillId].HasAuthoritativeEligibleCommission &&
                         !CloseMoney(record.EligibleCommission, order.NetCommission))
                    record.SetInvalid(order.Id, conversion.Id, conversion.UserId,
                        "Hoa hồng hợp lệ từ bảng kê lệch với hoa hồng đơn hàng trong CatsBack.");
                else if (!bills[record.BillId].HasAuthoritativeEligibleCommission &&
                         !NotGreaterThan(record.ActualPaidCommission, order.NetCommission))
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

            await _records.UpdateManyAsync(candidates, autoSave: false);
        }

        await RefreshBatchAsync(batch);
        return new AdminShopeeSettlementRefreshResultDto
        {
            BatchId = batch.Id,
            CheckedCount = candidates.Count,
            ReadyForApprovalCount = batch.PendingCount,
            UnmatchedCount = batch.UnmatchedCount,
            AlreadySettledCount = batch.AlreadySettledCount,
            InvalidCount = batch.InvalidCount,
            Batch = MapBatch(batch)
        };
    }

    private async Task<ApprovalWork?> TryPrepareAsync(ShopeeSettlementRecord record, ShopeeSettlementBill bill)
    {
        if (record.Status != ShopeeSettlementRecordStatus.PendingApproval) return null;
        if (!record.AffiliateOrderId.HasValue || !record.AffiliateConversionId.HasValue)
            return await MarkInvalidAsync(record, "Bản ghi không còn liên kết đầy đủ với đơn hàng CatsBack.");

        var order = await _orders.FindAsync(record.AffiliateOrderId.Value);
        var conversion = await _conversions.FindAsync(record.AffiliateConversionId.Value);
        if (order is null || conversion is null)
            return await MarkInvalidAsync(record, "Không còn tìm thấy đơn hàng hoặc lượt chuyển đổi tương ứng.");
        if (conversion.Platform != AffiliatePlatform.Shopee || order.ConversionId != conversion.Id ||
            !string.Equals(order.ExternalOrderId, record.ExternalOrderId, StringComparison.Ordinal))
            return await MarkInvalidAsync(record, "Liên kết đơn hàng Shopee đã thay đổi và không còn hợp lệ.");
        if (!conversion.UserId.HasValue)
        {
            record.SetUnmatched("Đơn hàng chưa được ghép với người dùng nên chưa thể cộng ví.");
            await _records.UpdateAsync(record, autoSave: false);
            return null;
        }
        if (record.UserId != conversion.UserId)
            return await MarkInvalidAsync(record,
                "Người dùng được ghép với đơn hàng đã thay đổi; hãy bấm Đối chiếu lại trước khi duyệt.");
        if (order.Status == AffiliateOrderStatus.Settled)
        {
            record.SetAlreadySettled(order.Id, conversion.Id, conversion.UserId);
            await _records.UpdateAsync(record, autoSave: false);
            return null;
        }
        if (order.Status != AffiliateOrderStatus.Completed)
            return await MarkInvalidAsync(record,
                $"Đơn hàng đang ở trạng thái {order.Status}, không còn đủ điều kiện duyệt.");
        if (bill.HasAuthoritativeEligibleCommission &&
            !CloseMoney(record.EligibleCommission, order.NetCommission))
            return await MarkInvalidAsync(record,
                "Hoa hồng hợp lệ từ bảng kê lệch với hoa hồng đơn hàng trong CatsBack.");
        if (!bill.HasAuthoritativeEligibleCommission &&
            !NotGreaterThan(record.ActualPaidCommission, order.NetCommission))
            return await MarkInvalidAsync(record,
                "Tiền thực trả trong file lớn hơn hoa hồng đơn hàng trong CatsBack.");

        var competingRecords = await _records.GetListAsync(other => other.Id != record.Id &&
            other.AffiliateOrderId == order.Id &&
            other.Status == ShopeeSettlementRecordStatus.PendingApproval);
        if (competingRecords.Count > 0)
        {
            const string issue = "Đơn hàng xuất hiện trong nhiều bản ghi đối soát chờ duyệt.";
            record.SetInvalid(issue);
            foreach (var competingRecord in competingRecords) competingRecord.SetInvalid(issue);
            competingRecords.Add(record);
            await _records.UpdateManyAsync(competingRecords, autoSave: false);
            return null;
        }

        var userCommission = _calculator.CalculateUserCommission(record.ActualPaidCommission,
            conversion.UserShareRate);
        return new ApprovalWork(record, bill, order, conversion, userCommission);
    }

    private async Task<ApprovalWork?> MarkInvalidAsync(ShopeeSettlementRecord record, string issue)
    {
        record.SetInvalid(issue);
        await _records.UpdateAsync(record, autoSave: false);
        return null;
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

    private async Task ApplyAsync(ApprovalWork work)
    {
        var adminId = CurrentUser.Id ?? throw new AbpAuthorizationException("Không xác định được admin duyệt đối soát.");
        work.Order.Settle(work.Record.ActualPaidCommission, work.UserCommission,
            work.Bill.PayoutId, work.Bill.PaidAt);
        work.Record.Approve(adminId, Clock.Now, work.UserCommission);
        await _orders.UpdateAsync(work.Order, autoSave: false);
        await _records.UpdateAsync(work.Record, autoSave: false);
        if (work.Conversion.UserId.HasValue)
            await _notificationManager.NotifyOrderStatusAsync(work.Conversion.UserId.Value, work.Order);
    }

    private async Task RefreshBatchAsync(ShopeeSettlementBatch batch)
    {
        var rows = await _records.GetListAsync(record => record.BatchId == batch.Id);
        batch.UpdateSummary(batch.BillCount, rows.Count,
            rows.Count(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval),
            rows.Count(record => record.Status == ShopeeSettlementRecordStatus.Approved),
            rows.Count(record => record.Status == ShopeeSettlementRecordStatus.Unmatched),
            rows.Count(record => record.Status == ShopeeSettlementRecordStatus.AlreadySettled),
            rows.Count(record => record.Status == ShopeeSettlementRecordStatus.Invalid),
            rows.Sum(record => record.EligibleCommission), rows.Sum(record => record.ActualPaidCommission),
            rows.Where(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval)
                .Sum(record => record.ActualPaidCommission),
            rows.Where(record => record.Status == ShopeeSettlementRecordStatus.Approved)
                .Sum(record => record.ActualPaidCommission));
        await _batches.UpdateAsync(batch, autoSave: true);
    }

    private Task<AdminShopeeSettlementApprovalResultDto> BuildResultAsync(ShopeeSettlementBatch batch,
        IReadOnlyCollection<ApprovalWork> approved, int skippedCount) => Task.FromResult(new AdminShopeeSettlementApprovalResultDto
    {
        BatchId = batch.Id,
        ApprovedCount = approved.Count,
        SkippedCount = skippedCount,
        ApprovedCommission = approved.Sum(item => item.Record.ActualPaidCommission),
        CreditedUserCommission = approved.Sum(item => item.UserCommission),
        Batch = MapBatch(batch)
    });

    private AdminShopeeSettlementRecordDto MapRecord(ShopeeSettlementRecord record, ShopeeSettlementBill bill,
        IReadOnlyDictionary<Guid, AffiliateConversion> conversions)
    {
        var projected = record.ApprovedUserCommission;
        if (record.Status == ShopeeSettlementRecordStatus.PendingApproval && record.AffiliateConversionId.HasValue &&
            conversions.TryGetValue(record.AffiliateConversionId.Value, out var conversion))
            projected = _calculator.CalculateUserCommission(record.ActualPaidCommission, conversion.UserShareRate);
        return new AdminShopeeSettlementRecordDto
        {
            Id = record.Id,
            CreationTime = record.CreationTime,
            BatchId = record.BatchId,
            BillId = record.BillId,
            SourceAffiliateId = bill.SourceAffiliateId,
            ValidationId = bill.ValidationId,
            PayoutId = bill.PayoutId,
            PaidAt = bill.PaidAt,
            ExternalOrderId = record.ExternalOrderId,
            EligibleCommission = record.EligibleCommission,
            AllocatedServiceFee = record.AllocatedServiceFee,
            AllocatedTax = record.AllocatedTax,
            ActualPaidCommission = record.ActualPaidCommission,
            ProjectedUserCommission = projected,
            ApprovedUserCommission = record.ApprovedUserCommission,
            Status = record.Status,
            AffiliateOrderId = record.AffiliateOrderId,
            UserId = record.UserId,
            ApprovedAt = record.ApprovedAt,
            Issue = record.Issue
        };
    }

    private static AdminShopeeSettlementBatchDto MapBatch(ShopeeSettlementBatch batch) => new()
    {
        Id = batch.Id,
        CreationTime = batch.CreationTime,
        Source = batch.Source,
        OriginalFileName = batch.OriginalFileName,
        ContentHash = batch.ContentHash,
        Status = batch.Status,
        BillCount = batch.BillCount,
        RecordCount = batch.RecordCount,
        PendingCount = batch.PendingCount,
        ApprovedCount = batch.ApprovedCount,
        UnmatchedCount = batch.UnmatchedCount,
        AlreadySettledCount = batch.AlreadySettledCount,
        InvalidCount = batch.InvalidCount,
        TotalEligibleCommission = batch.TotalEligibleCommission,
        TotalPaidCommission = batch.TotalPaidCommission,
        PendingPaidCommission = batch.PendingPaidCommission,
        ApprovedPaidCommission = batch.ApprovedPaidCommission
    };

    private sealed record ApprovalWork(ShopeeSettlementRecord Record, ShopeeSettlementBill Bill,
        AffiliateOrder Order, AffiliateConversion Conversion, decimal UserCommission);

    private sealed record BulkApprovalPreparation(List<ApprovalWork> Work, int SkippedCount);
}
