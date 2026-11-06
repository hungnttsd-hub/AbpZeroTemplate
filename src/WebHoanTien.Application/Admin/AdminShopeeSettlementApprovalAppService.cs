using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Data;
using Volo.Abp.Identity;
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
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IRepository<AffiliateOrderItemAttribution, Guid> _attributions;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly AffiliateCommissionCalculator _calculator;
    private readonly CustomerNotificationManager _notificationManager;
    private readonly ShopeeSettlementMatcher _matcher;
    private readonly ShopeeSettlementApprovalRunner _approvalRunner;

    public AdminShopeeSettlementApprovalAppService(IRepository<ShopeeSettlementBatch, Guid> batches,
        IRepository<ShopeeSettlementBill, Guid> bills, IRepository<ShopeeSettlementRecord, Guid> records,
        IRepository<AffiliateOrder, Guid> orders, IRepository<AffiliateOrderItem, Guid> items,
        IRepository<AffiliateOrderItemAttribution, Guid> attributions,
        IRepository<AffiliateConversion, Guid> conversions, IRepository<IdentityUser, Guid> users,
        AffiliateCommissionCalculator calculator, CustomerNotificationManager notificationManager,
        ShopeeSettlementMatcher matcher, ShopeeSettlementApprovalRunner approvalRunner)
    {
        _batches = batches;
        _bills = bills;
        _records = records;
        _orders = orders;
        _items = items;
        _attributions = attributions;
        _conversions = conversions;
        _users = users;
        _calculator = calculator;
        _notificationManager = notificationManager;
        _matcher = matcher;
        _approvalRunner = approvalRunner;
    }

    [UnitOfWork]
    public async Task<AdminShopeeSettlementPageDto> GetListAsync(AdminShopeeSettlementBatchListInput input)
    {
        await _matcher.RefreshUnmatchedAsync();
        var filteredRecords = await GetFilteredRecordsAsync(input);
        var ordersWithoutSettlementCount = await AsyncExecuter.CountAsync(
            await GetFilteredOrdersWithoutSettlementAsync(input));
        var directSummary = await BuildDirectApprovalSummaryAsync(input);
        var query = (await _batches.GetQueryableAsync())
            .Where(batch => filteredRecords.Any(record => record.BatchId == batch.Id));

        var totalCount = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query.OrderByDescending(batch => batch.CreationTime)
            .Skip(input.SkipCount).Take(input.MaxResultCount));
        var counts = await AsyncExecuter.ToListAsync(filteredRecords.GroupBy(record => record.Status)
            .Select(group => new { Status = group.Key, Count = group.Count(), Amount = group.Sum(record => record.ActualPaidCommission) }));
        var financial = await BuildFinancialSummaryAsync(filteredRecords);
        var paidBills = (await _bills.GetQueryableAsync())
            .Where(bill => bill.PaidAt.HasValue && bill.PaidAt.Value > DateTime.UnixEpoch);
        var paidUnapprovedCount = await AsyncExecuter.CountAsync(filteredRecords.Where(record =>
            record.Status != ShopeeSettlementRecordStatus.Approved &&
            record.Status != ShopeeSettlementRecordStatus.AlreadySettled &&
            paidBills.Any(bill => bill.Id == record.BillId)));
        var pageIds = rows.Select(batch => batch.Id).ToList();
        var pageRecords = pageIds.Count == 0 ? new List<ShopeeSettlementRecord>() :
            await AsyncExecuter.ToListAsync(filteredRecords.Where(record => pageIds.Contains(record.BatchId)));
        var pageBills = pageIds.Count == 0 ? new Dictionary<Guid, ShopeeSettlementBill>() :
            (await _bills.GetListAsync(bill => pageIds.Contains(bill.BatchId))).ToDictionary(bill => bill.Id);
        var batchDtos = rows.Select(batch =>
        {
            var details = pageRecords.Where(record => record.BatchId == batch.Id).ToList();
            return MapFilteredBatch(batch, details, pageBills);
        }).ToList();
        return new AdminShopeeSettlementPageDto
        {
            Summary = new AdminShopeeSettlementSummaryDto
            {
                TotalCount = counts.Sum(group => group.Count) + ordersWithoutSettlementCount,
                OrdersWithoutSettlementCount = ordersWithoutSettlementCount,
                CommissionOrderCount = financial.OrderCount + directSummary.Financial.OrderCount,
                PendingCount = counts.Where(group => group.Status == ShopeeSettlementRecordStatus.PendingApproval).Sum(group => group.Count) + directSummary.PendingCount,
                PendingAmount = financial.PendingCommission + directSummary.Financial.PendingCommission,
                ApprovedCount = counts.Where(group => group.Status == ShopeeSettlementRecordStatus.Approved).Sum(group => group.Count) + directSummary.ApprovedCount,
                ApprovedAmount = counts.Where(group => group.Status == ShopeeSettlementRecordStatus.Approved).Sum(group => group.Amount) + directSummary.ApprovedCommission,
                IssueCount = counts.Where(group => group.Status != ShopeeSettlementRecordStatus.PendingApproval &&
                    group.Status != ShopeeSettlementRecordStatus.Approved).Sum(group => group.Count) + ordersWithoutSettlementCount - directSummary.PendingCount - directSummary.ApprovedCount,
                TotalActualCommission = financial.ActualCommission + directSummary.Financial.ActualCommission,
                TotalUserCommission = financial.UserCommission + directSummary.Financial.UserCommission,
                TotalAdminCommission = financial.AdminCommission + directSummary.Financial.AdminCommission,
                PaidUnapprovedCount = paidUnapprovedCount,
                UnapprovedCount = counts.Where(group => group.Status != ShopeeSettlementRecordStatus.Approved &&
                    group.Status != ShopeeSettlementRecordStatus.AlreadySettled).Sum(group => group.Count) + directSummary.PendingCount
            },
            Batches = new PagedResultDto<AdminShopeeSettlementBatchDto>(totalCount,
                batchDtos)
        };
    }

    [UnitOfWork]
    public async Task<AdminShopeeSettlementBatchDetailsDto> GetAsync(Guid batchId, int skipCount = 0,
        int maxResultCount = 50, AdminShopeeSettlementBatchListInput? input = null)
    {
        await _matcher.RefreshUnmatchedAsync(batchId);
        var batch = await _batches.GetAsync(batchId);
        var query = (await GetFilteredRecordsAsync(input)).Where(record => record.BatchId == batchId);
        var count = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query
            .OrderBy(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval ? 0 : 1)
            .ThenBy(record => record.ExternalOrderId).Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, 200)));
        var bills = (await _bills.GetListAsync(bill => bill.BatchId == batchId)).ToDictionary(bill => bill.Id);
        var orderIds = rows.Where(row => row.AffiliateOrderId.HasValue)
            .Select(row => row.AffiliateOrderId!.Value).Distinct().ToList();
        var ordersById = orderIds.Count == 0 ? new Dictionary<Guid, AffiliateOrder>() :
            (await _orders.GetListAsync(order => orderIds.Contains(order.Id)))
                .ToDictionary(order => order.Id);
        var detailItems = orderIds.Count == 0 ? new List<AffiliateOrderItem>() :
            await _items.GetListAsync(item => orderIds.Contains(item.OrderId));
        var productNamesByOrder = detailItems
                .Where(item => !string.IsNullOrWhiteSpace(item.ProductName))
                .GroupBy(item => item.OrderId)
                .ToDictionary(group => group.Key, group => group
                    .Select(item => item.ProductName!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList());
        var detailItemIds = detailItems.Select(x => x.Id).ToList();
        var detailAttributions = detailItemIds.Count == 0 ? new List<AffiliateOrderItemAttribution>() :
            await _attributions.GetListAsync(x => detailItemIds.Contains(x.OrderItemId));
        var orderByItem = detailItems.ToDictionary(x => x.Id, x => x.OrderId);
        var attributionsByOrder = detailAttributions.Where(x => orderByItem.ContainsKey(x.OrderItemId))
            .GroupBy(x => orderByItem[x.OrderItemId]).ToDictionary(group => group.Key, group => group.ToList());
        var userIds = detailAttributions.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value)
            .Concat(rows.Where(row => row.UserId.HasValue).Select(row => row.UserId!.Value))
            .Distinct().ToList();
        var users = userIds.Count == 0
            ? new Dictionary<Guid, IdentityUser>()
            : (await _users.GetListAsync(user => userIds.Contains(user.Id))).ToDictionary(user => user.Id);
        var batchRecords = await AsyncExecuter.ToListAsync(query);
        var batchDto = MapFilteredBatch(batch, batchRecords, bills);
        return new AdminShopeeSettlementBatchDetailsDto
        {
            Batch = batchDto,
            Records = new PagedResultDto<AdminShopeeSettlementRecordDto>(count,
                rows.Select(row => MapRecord(row, bills[row.BillId], productNamesByOrder,
                        attributionsByOrder, users, ordersById))
                    .ToList())
        };
    }

    [UnitOfWork]
    public async Task<PagedResultDto<AdminShopeeSettlementRecordDto>> GetRecordsAsync(
        AdminShopeeSettlementBatchListInput input, int skipCount = 0, int maxResultCount = 50)
    {
        await _matcher.RefreshUnmatchedAsync();
        var query = await GetFilteredRecordsAsync(input);
        var ordersWithoutSettlement = await GetFilteredOrdersWithoutSettlementAsync(input);
        // Merge identifiers before paging in SQL; never materialize all conversion orders on this page.
        var entries = query.Select(record => new SettlementListEntry
        {
            Id = record.Id, HasSettlementRecord = true, OrderId = record.AffiliateOrderId,
            CreationTime = record.CreationTime, ExternalOrderId = record.ExternalOrderId
        }).Concat(ordersWithoutSettlement.Select(order => new SettlementListEntry
        {
            Id = order.Id, HasSettlementRecord = false, OrderId = (Guid?)order.Id,
            CreationTime = order.CreationTime, ExternalOrderId = order.ExternalOrderId
        }));
        var count = await AsyncExecuter.CountAsync(entries);
        var page = await AsyncExecuter.ToListAsync(entries.OrderByDescending(entry => entry.CreationTime)
            .ThenBy(entry => entry.ExternalOrderId)
            .ThenBy(entry => entry.Id).Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, 200)));
        var recordIds = page.Where(entry => entry.HasSettlementRecord).Select(entry => entry.Id).ToList();
        var rows = recordIds.Count == 0 ? new List<ShopeeSettlementRecord>() :
            await _records.GetListAsync(record => recordIds.Contains(record.Id));
        var billIds = rows.Select(row => row.BillId).Distinct().ToList();
        var bills = billIds.Count == 0
            ? new Dictionary<Guid, ShopeeSettlementBill>()
            : (await _bills.GetListAsync(bill => billIds.Contains(bill.Id))).ToDictionary(bill => bill.Id);
        var orderIds = page.Where(entry => entry.OrderId.HasValue)
            .Select(entry => entry.OrderId!.Value).Distinct().ToList();
        var ordersById = orderIds.Count == 0 ? new Dictionary<Guid, AffiliateOrder>() :
            (await _orders.GetListAsync(order => orderIds.Contains(order.Id)))
                .ToDictionary(order => order.Id);
        var detailItems = orderIds.Count == 0 ? new List<AffiliateOrderItem>() :
            await _items.GetListAsync(item => orderIds.Contains(item.OrderId));
        var productNamesByOrder = detailItems
            .Where(item => !string.IsNullOrWhiteSpace(item.ProductName))
            .GroupBy(item => item.OrderId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.ProductName!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList());
        var detailItemIds = detailItems.Select(item => item.Id).ToList();
        var detailAttributions = detailItemIds.Count == 0 ? new List<AffiliateOrderItemAttribution>() :
            await _attributions.GetListAsync(attribution => detailItemIds.Contains(attribution.OrderItemId));
        var orderByItem = detailItems.ToDictionary(item => item.Id, item => item.OrderId);
        var attributionsByOrder = detailAttributions.Where(attribution =>
                orderByItem.ContainsKey(attribution.OrderItemId))
            .GroupBy(attribution => orderByItem[attribution.OrderItemId])
            .ToDictionary(group => group.Key, group => group.ToList());
        var userIds = detailAttributions.Where(attribution => attribution.UserId.HasValue)
            .Select(attribution => attribution.UserId!.Value)
            .Concat(rows.Where(row => row.UserId.HasValue).Select(row => row.UserId!.Value))
            .Distinct().ToList();
        var users = userIds.Count == 0
            ? new Dictionary<Guid, IdentityUser>()
            : (await _users.GetListAsync(user => userIds.Contains(user.Id))).ToDictionary(user => user.Id);
        var mappedRecords = rows.ToDictionary(row => row.Id, row => MapRecord(row, bills[row.BillId],
            productNamesByOrder, attributionsByOrder, users, ordersById));
        var conversionIds = page.Where(entry => !entry.HasSettlementRecord)
            .Select(entry => ordersById[entry.Id].ConversionId).Distinct().ToList();
        var conversions = conversionIds.Count == 0 ? new Dictionary<Guid, AffiliateConversion>() :
            (await _conversions.GetListAsync(conversion => conversionIds.Contains(conversion.Id) &&
                conversion.Platform == AffiliatePlatform.Shopee)).ToDictionary(conversion => conversion.Id);
        return new PagedResultDto<AdminShopeeSettlementRecordDto>(count, page.Select(entry =>
            entry.HasSettlementRecord ? mappedRecords[entry.Id] : MapOrderWithoutSettlement(
                ordersById[entry.Id], productNamesByOrder, attributionsByOrder, users,
                conversions.GetValueOrDefault(ordersById[entry.Id].ConversionId)?.PurchaseTime)).ToList());
    }

    [UnitOfWork(IsDisabled = true)]
    public async Task<AdminShopeeSettlementApprovalResultDto> ApproveAsync(Guid recordId, AdminShopeeSettlementManualInput? manual = null)
    {
        return await _approvalRunner.RunAsync(async () =>
        {
            var record = await _records.GetAsync(recordId);
            var batch = await _batches.GetAsync(record.BatchId);
            if (record.Status is ShopeeSettlementRecordStatus.Approved or ShopeeSettlementRecordStatus.AlreadySettled)
                return await BuildResultAsync(batch, Array.Empty<ApprovalWork>(), skippedCount: 1);
            await _matcher.RefreshAsync(new[] { record });
            var bill = await _bills.GetAsync(record.BillId);
            if (bill.IsShopeePaid && manual is not null)
                throw new UserFriendlyException("Đơn Shopee đã thanh toán phải dùng số tiền đối soát.");
            var work = await TryPrepareAsync(record, bill, manual);
            if (work is null)
            {
                await RefreshBatchAsync(batch);
                return await BuildResultAsync(batch, Array.Empty<ApprovalWork>(), skippedCount: 1);
            }
            await ApplyAsync(work);
            await RefreshBatchAsync(batch);
            return await BuildResultAsync(batch, new[] { work }, skippedCount: 0);
        });
    }

    [UnitOfWork(IsDisabled = true)]
    public async Task<AdminShopeeSettlementBulkApprovalResultDto> ApproveOrderAsync(Guid orderId)
    {
        return await _approvalRunner.RunAsync(async () =>
        {
            var order = await _orders.GetAsync(orderId);
            var result = await ApproveOrdersWithoutSettlementAsync(new[] { order }, "CompletedSevenDaysIndividual");
            if (result.ApprovedCount == 0)
                throw new UserFriendlyException("Đơn phải hoàn thành, được đặt hơn 7 ngày, chưa duyệt và có affiliate link hợp lệ. Nếu đã có bảng kê, hãy tải lại trang để duyệt theo dữ liệu đối soát.");
            return result;
        });
    }

    [UnitOfWork(IsDisabled = true)]
    public async Task<AdminShopeeSettlementApprovalResultDto> ApproveAllAsync(Guid batchId,
        AdminShopeeSettlementBatchListInput? input = null)
    {
        return await _approvalRunner.RunAsync(async () =>
        {
            var batch = await _batches.GetAsync(batchId);
            var candidates = await GetPaidApprovalCandidatesAsync(input, batchId);
            var preparation = await ApproveRecordsAsync(candidates);
            return await BuildResultAsync(batch, preparation.Work, preparation.SkippedCount);
        });
    }

    [UnitOfWork(IsDisabled = true)]
    public async Task<AdminShopeeSettlementBulkApprovalResultDto> ApproveAllPaidAsync(
        AdminShopeeSettlementBatchListInput input)
    {
        return await _approvalRunner.RunAsync(async () =>
        {
            // Select across every matching batch/page, then prepare together to detect duplicate orders.
            var candidates = await GetPaidApprovalCandidatesAsync(input);
            var preparation = await ApproveRecordsAsync(candidates);
            return new AdminShopeeSettlementBulkApprovalResultDto
            {
                ApprovedCount = preparation.Work.Count,
                SkippedCount = preparation.SkippedCount,
                ApprovedCommission = preparation.Work.Sum(item => item.Record.ActualPaidCommission),
                CreditedUserCommission = preparation.Work.Sum(item => item.TotalUserCommission)
            };
        });
    }

    [UnitOfWork(IsDisabled = true)]
    public async Task<AdminShopeeSettlementBulkApprovalResultDto> ApproveFilteredAsync(
        AdminShopeeSettlementBatchListInput input, Guid? batchId = null)
    {
        return await _approvalRunner.RunAsync(async () =>
        {
            if (batchId.HasValue) await _batches.GetAsync(batchId.Value);
            // Apply the same filters as the list, without its pagination or a paid-only restriction.
            var records = await GetFilteredRecordsAsync(input);
            var candidates = await AsyncExecuter.ToListAsync(records.Where(record =>
                (!batchId.HasValue || record.BatchId == batchId.Value) &&
                record.Status != ShopeeSettlementRecordStatus.Approved &&
                record.Status != ShopeeSettlementRecordStatus.AlreadySettled));
            var preparation = await ApproveRecordsAsync(candidates);
            var direct = batchId.HasValue
                ? new AdminShopeeSettlementBulkApprovalResultDto()
                : await ApproveOrdersWithoutSettlementAsync(await AsyncExecuter.ToListAsync(
                    await GetEligibleOrdersWithoutSettlementAsync(input)), "CompletedSevenDaysFiltered");
            return new AdminShopeeSettlementBulkApprovalResultDto
            {
                ApprovedCount = preparation.Work.Count + direct.ApprovedCount,
                SkippedCount = preparation.SkippedCount + direct.SkippedCount,
                ApprovedCommission = preparation.Work.Sum(item => item.Record.ActualPaidCommission) + direct.ApprovedCommission,
                CreditedUserCommission = preparation.Work.Sum(item => item.TotalUserCommission) + direct.CreditedUserCommission
            };
        });
    }

    [UnitOfWork(IsDisabled = true)]
    public async Task<AdminShopeeSettlementBulkApprovalResultDto> ApproveCompletedOlderThanSevenDaysAsync(bool isManual)
    {
        return await _approvalRunner.RunAsync(async () =>
        {
            await _matcher.RefreshUnmatchedAsync();
            var cutoff = Clock.Now.AddDays(-7);
            var orders = (await _orders.GetQueryableAsync()).Where(order =>
                order.Platform == AffiliatePlatform.Shopee && order.Status == AffiliateOrderStatus.Completed &&
                !order.SettledAt.HasValue);
            var conversions = (await _conversions.GetQueryableAsync()).Where(conversion =>
                conversion.Platform == AffiliatePlatform.Shopee && conversion.PurchaseTime < cutoff &&
                conversion.PurchaseTime > DateTime.UnixEpoch);
            var query = from record in await _records.GetQueryableAsync()
                        join order in orders on record.AffiliateOrderId equals order.Id
                        join conversion in conversions on order.ConversionId equals conversion.Id
                        where record.Status != ShopeeSettlementRecordStatus.Approved &&
                              record.Status != ShopeeSettlementRecordStatus.AlreadySettled
                        select record;
            var candidates = await AsyncExecuter.ToListAsync(query);
            var preparation = await ApproveRecordsAsync(candidates, cutoff,
                isManual ? "CompletedSevenDaysManual" : "CompletedSevenDaysDaily");
            var direct = await ApproveOrdersWithoutSettlementAsync(await AsyncExecuter.ToListAsync(
                    await GetEligibleOrdersWithoutSettlementAsync(new AdminShopeeSettlementBatchListInput())),
                isManual ? "CompletedSevenDaysManual" : "CompletedSevenDaysDaily");
            return new AdminShopeeSettlementBulkApprovalResultDto
            {
                ApprovedCount = preparation.Work.Count + direct.ApprovedCount,
                SkippedCount = preparation.SkippedCount + direct.SkippedCount,
                ApprovedCommission = preparation.Work.Sum(item => item.Record.ActualPaidCommission) + direct.ApprovedCommission,
                CreditedUserCommission = preparation.Work.Sum(item => item.TotalUserCommission) + direct.CreditedUserCommission
            };
        });
    }

    private async Task<AdminShopeeSettlementBulkApprovalResultDto> ApproveOrdersWithoutSettlementAsync(
        IReadOnlyCollection<AffiliateOrder> candidates, string approvalSource)
    {
        if (candidates.Count == 0) return new AdminShopeeSettlementBulkApprovalResultDto();
        var conversionIds = candidates.Select(order => order.ConversionId).Distinct().ToList();
        var conversions = (await _conversions.GetListAsync(conversion => conversionIds.Contains(conversion.Id) &&
            conversion.Platform == AffiliatePlatform.Shopee)).ToDictionary(conversion => conversion.Id);
        var orderIds = candidates.Select(order => order.Id).ToList();
        var externalIds = candidates.Select(order => order.ExternalOrderId).ToList();
        // Recheck under the same approval lock/transaction used by bill-based approvals.
        var competingRecords = await _records.GetListAsync(record =>
            (record.AffiliateOrderId.HasValue && orderIds.Contains(record.AffiliateOrderId.Value)) ||
            externalIds.Contains(record.ExternalOrderId));
        var blockedIds = competingRecords.Where(record => record.AffiliateOrderId.HasValue)
            .Select(record => record.AffiliateOrderId!.Value).ToHashSet();
        var blockedExternalIds = competingRecords.Select(record => record.ExternalOrderId)
            .ToHashSet(StringComparer.Ordinal);
        var attributions = await LoadAttributionsByOrderAsync(orderIds);
        var now = Clock.Now;
        var work = new List<DirectApprovalWork>();
        foreach (var order in candidates)
        {
            if (blockedIds.Contains(order.Id) || blockedExternalIds.Contains(order.ExternalOrderId)) continue;
            var prepared = PrepareOrderWithoutSettlement(order,
                conversions.GetValueOrDefault(order.ConversionId)?.PurchaseTime,
                attributions.GetValueOrDefault(order.Id, new List<AffiliateOrderItemAttribution>()), now.AddDays(-7));
            if (prepared is not null) work.Add(prepared);
        }
        var adminId = CurrentUser.Id ?? throw new AbpAuthorizationException("Không xác định được admin duyệt đối soát.");
        foreach (var item in work)
        {
            foreach (var allocation in item.Plan.Allocations)
                allocation.Attribution.Settle(allocation.SettledNetCommission, allocation.SettledUserCommission);
            // Keep an auditable approval snapshot on the order; no provider bill/record is fabricated.
            item.Order.ExtraProperties["ApprovalSource"] = approvalSource;
            item.Order.ExtraProperties["ApprovalBasis"] = "ShopeeConversionReport";
            item.Order.ExtraProperties["ApprovedBy"] = adminId;
            item.Order.ExtraProperties["ApprovedAtUtc"] = now;
            item.Order.ExtraProperties["OrderStatusAtApproval"] = (int)item.Order.Status;
            item.Order.ExtraProperties["GrossCommissionAtApproval"] = item.Amounts.Gross;
            item.Order.ExtraProperties["ServiceFeeAtApproval"] = item.Amounts.Fee;
            item.Order.ExtraProperties["TaxAtApproval"] = item.Amounts.Tax;
            item.Order.ExtraProperties["DefaultTaxPercent"] = AdminShopeeSettlementManualInput.DefaultTaxPercent;
            item.Order.ExtraProperties["DefaultServiceFeePercent"] = AdminShopeeSettlementManualInput.DefaultServiceFeePercent;
            item.Order.Settle(item.Amounts.Net, item.Plan.Recipients.Sum(recipient => recipient.UserCommission),
                $"catback:completed-seven-days:{item.Order.Id:N}", now);
        }
        if (work.Count > 0)
        {
            await _orders.UpdateManyAsync(work.Select(item => item.Order), autoSave: false);
            await _attributions.UpdateManyAsync(work.SelectMany(item => item.Plan.Allocations)
                .Select(allocation => allocation.Attribution), autoSave: false);
            await _notificationManager.NotifySettledOrdersAsync(work.SelectMany(item =>
                item.Plan.Recipients.Select(recipient => (recipient.UserId, item.Order, recipient.UserCommission))));
        }
        return new AdminShopeeSettlementBulkApprovalResultDto
        {
            ApprovedCount = work.Count,
            SkippedCount = candidates.Count - work.Count,
            ApprovedCommission = work.Sum(item => item.Amounts.Net),
            CreditedUserCommission = work.Sum(item => item.Plan.Recipients.Sum(recipient => recipient.UserCommission))
        };
    }

    private DirectApprovalWork? PrepareOrderWithoutSettlement(AffiliateOrder order, DateTime? purchaseTime,
        List<AffiliateOrderItemAttribution> attributions, DateTime cutoff)
    {
        if (order.Platform != AffiliatePlatform.Shopee || order.Status != AffiliateOrderStatus.Completed ||
            order.SettledAt.HasValue || !purchaseTime.HasValue || purchaseTime.Value <= DateTime.UnixEpoch ||
            purchaseTime.Value >= cutoff || order.NetCommission < 0m ||
            attributions.Any(row => row.SettledNetCommission.HasValue || row.SettledUserCommission.HasValue)) return null;
        var amounts = ShopeeSettlementAmounts.FromConversion(order.NetCommission);
        var plan = BuildAllocationPlan(amounts.Net, attributions);
        return plan is null ? null : new DirectApprovalWork(order, amounts, plan);
    }

    private async Task<IQueryable<AffiliateOrder>> GetEligibleOrdersWithoutSettlementAsync(
        AdminShopeeSettlementBatchListInput input)
    {
        var cutoff = Clock.Now.AddDays(-7);
        var conversions = (await _conversions.GetQueryableAsync()).Where(conversion =>
            conversion.Platform == AffiliatePlatform.Shopee && conversion.PurchaseTime > DateTime.UnixEpoch &&
            conversion.PurchaseTime < cutoff);
        return (await GetFilteredOrdersWithoutSettlementAsync(input)).Where(order =>
            order.Status == AffiliateOrderStatus.Completed && !order.SettledAt.HasValue &&
            conversions.Any(conversion => conversion.Id == order.ConversionId));
    }

    private sealed record DirectApprovalWork(AffiliateOrder Order, ShopeeSettlementAmounts Amounts,
        SettlementAllocationPlan Plan);

    private async Task<List<ShopeeSettlementRecord>> GetPaidApprovalCandidatesAsync(
        AdminShopeeSettlementBatchListInput? input, Guid? batchId = null)
    {
        var records = await GetFilteredRecordsAsync(input);
        var paidBills = (await _bills.GetQueryableAsync())
            .Where(bill => bill.PaidAt.HasValue && bill.PaidAt.Value > DateTime.UnixEpoch);
        return await AsyncExecuter.ToListAsync(records.Where(record =>
            (!batchId.HasValue || record.BatchId == batchId.Value) &&
            record.Status != ShopeeSettlementRecordStatus.Approved &&
            record.Status != ShopeeSettlementRecordStatus.AlreadySettled &&
            paidBills.Any(bill => bill.Id == record.BillId)));
    }

    private async Task<BulkApprovalPreparation> ApproveRecordsAsync(List<ShopeeSettlementRecord> candidates,
        DateTime? completedBefore = null, string? approvalSource = null)
    {
        if (candidates.Count == 0)
            return new BulkApprovalPreparation(new List<ApprovalWork>(), 0);
        await _matcher.RefreshAsync(candidates);
        var billIds = candidates.Select(record => record.BillId).Distinct().ToList();
        var bills = (await _bills.GetListAsync(bill => billIds.Contains(bill.Id))).ToDictionary(bill => bill.Id);
        var preparation = await PrepareManyAsync(candidates, bills, completedBefore);
        var adminId = CurrentUser.Id ??
            throw new AbpAuthorizationException("Không xác định được admin duyệt đối soát.");
        foreach (var item in preparation.Work)
        {
            if (approvalSource is not null) item.Record.ExtraProperties["ApprovalSource"] = approvalSource;
            foreach (var allocation in item.Allocations)
                allocation.Attribution.Settle(allocation.SettledNetCommission, allocation.SettledUserCommission);
            item.Order.Settle(item.Record.ActualPaidCommission, item.TotalUserCommission,
                SettlementReference(item.Bill), SettlementTime(item.Bill));
            item.Record.Approve(adminId, Clock.Now, item.TotalUserCommission);
        }
        await _orders.UpdateManyAsync(preparation.Work.Select(item => item.Order).DistinctBy(order => order.Id),
            autoSave: false);
        var changedAttributions = preparation.Work.SelectMany(item => item.Allocations)
            .Select(item => item.Attribution).DistinctBy(item => item.Id).ToList();
        if (changedAttributions.Count > 0)
            await _attributions.UpdateManyAsync(changedAttributions, autoSave: false);
        await _records.UpdateManyAsync(candidates, autoSave: false);
        await _notificationManager.NotifySettledOrdersAsync(preparation.Work
            .SelectMany(item => item.Recipients.Select(recipient =>
                (recipient.UserId, item.Order, recipient.UserCommission))));
        foreach (var batchId in candidates.Select(record => record.BatchId).Distinct())
            await _matcher.RefreshBatchAsync(batchId);
        return preparation;
    }

    private async Task<BulkApprovalPreparation> PrepareManyAsync(List<ShopeeSettlementRecord> records,
        IReadOnlyDictionary<Guid, ShopeeSettlementBill> bills, DateTime? completedBefore = null)
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
        {
            var chunkIds = chunk.ToList();
            orderRows.AddRange(await _orders.GetListAsync(order => chunkIds.Contains(order.Id)));
        }
        var orders = orderRows.ToDictionary(order => order.Id);
        var conversionIds = records.Where(record => record.AffiliateConversionId.HasValue)
            .Select(record => record.AffiliateConversionId!.Value).Distinct().ToList();
        var conversionRows = new List<AffiliateConversion>();
        foreach (var chunk in conversionIds.Chunk(500))
        {
            var chunkIds = chunk.ToList();
            conversionRows.AddRange(await _conversions.GetListAsync(conversion => chunkIds.Contains(conversion.Id)));
        }
        var conversions = conversionRows.ToDictionary(conversion => conversion.Id);
        var attributionsByOrder = await LoadAttributionsByOrderAsync(orderIds);

        var work = new List<ApprovalWork>(records.Count);
        foreach (var record in records)
        {
            if (record.Status is ShopeeSettlementRecordStatus.Approved or ShopeeSettlementRecordStatus.AlreadySettled) continue;
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
            if (order.Status == AffiliateOrderStatus.Settled || order.SettledAt.HasValue)
            {
                record.SetAlreadySettled(order.Id, conversion.Id, record.UserId);
                continue;
            }
            if (completedBefore.HasValue && (order.Platform != AffiliatePlatform.Shopee ||
                order.Status != AffiliateOrderStatus.Completed || conversion.PurchaseTime <= DateTime.UnixEpoch ||
                conversion.PurchaseTime >= completedBefore.Value)) continue;
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

            var amounts = ShopeeSettlementAmounts.For(record, bill);
            var plan = BuildAllocationPlan(amounts.Net,
                attributionsByOrder.GetValueOrDefault(order.Id, new List<AffiliateOrderItemAttribution>()));
            if (plan is null)
            {
                record.SetUnmatched("Đơn hàng chưa có affiliate link hợp lệ để cộng ví.");
                continue;
            }
            var soleUserId = plan.Recipients.Count == 1 ? plan.Recipients[0].UserId : (Guid?)null;
            amounts.Apply(record);
            record.ExtraProperties["OrderStatusAtApproval"] = (int)order.Status;
            record.SetPendingApproval(order.Id, conversion.Id, soleUserId,
                plan.UnmatchedCount > 0
                    ? $"Có {plan.UnmatchedCount} affiliate link chưa ghép; phần này sẽ không được cộng cho người dùng."
                    : null);
            work.Add(new ApprovalWork(record, bill, order, conversion, plan.Allocations, plan.Recipients));
        }

        return new BulkApprovalPreparation(work, records.Count - work.Count);
    }

    [UnitOfWork]
    public async Task<AdminShopeeSettlementRefreshResultDto> RefreshMatchesAsync(Guid batchId)
    {
        var batch = await _batches.GetAsync(batchId);
        var checkedCount = await _matcher.RefreshUnmatchedAsync(batchId);

        await RefreshBatchAsync(batch);
        return new AdminShopeeSettlementRefreshResultDto
        {
            BatchId = batch.Id,
            CheckedCount = checkedCount,
            ReadyForApprovalCount = batch.PendingCount,
            UnmatchedCount = batch.UnmatchedCount,
            AlreadySettledCount = batch.AlreadySettledCount,
            InvalidCount = batch.InvalidCount,
            Batch = MapBatch(batch)
        };
    }

    private async Task<ApprovalWork?> TryPrepareAsync(ShopeeSettlementRecord record, ShopeeSettlementBill bill,
        AdminShopeeSettlementManualInput? manual = null)
    {
        if (record.Status is ShopeeSettlementRecordStatus.Approved or ShopeeSettlementRecordStatus.AlreadySettled)
            return null;
        if (!record.AffiliateOrderId.HasValue || !record.AffiliateConversionId.HasValue)
            return await MarkInvalidAsync(record, "Bản ghi không còn liên kết đầy đủ với đơn hàng CatsBack.");

        var order = await _orders.FindAsync(record.AffiliateOrderId.Value);
        var conversion = await _conversions.FindAsync(record.AffiliateConversionId.Value);
        if (order is null || conversion is null)
            return await MarkInvalidAsync(record, "Không còn tìm thấy đơn hàng hoặc lượt chuyển đổi tương ứng.");
        if (conversion.Platform != AffiliatePlatform.Shopee || order.ConversionId != conversion.Id ||
            !string.Equals(order.ExternalOrderId, record.ExternalOrderId, StringComparison.Ordinal))
            return await MarkInvalidAsync(record, "Liên kết đơn hàng Shopee đã thay đổi và không còn hợp lệ.");
        if (order.Status == AffiliateOrderStatus.Settled || order.SettledAt.HasValue)
        {
            record.SetAlreadySettled(order.Id, conversion.Id, record.UserId);
            await _records.UpdateAsync(record, autoSave: false);
            return null;
        }
        if (manual is not null && (manual.GrossCommission is null or < 0m or > 99999999999999m ||
            manual.TaxPercent is null or < 0m or > 100m ||
            manual.ServiceFeePercent is null or < 0m or > 100m ||
            manual.TaxPercent + manual.ServiceFeePercent > 100m))
            throw new UserFriendlyException("Hoa hồng phải không âm; thuế và phí phải từ 0 đến 100%, tổng hai tỷ lệ không vượt quá 100%.");
        if (manual is null && bill.HasAuthoritativeEligibleCommission &&
            !CloseMoney(record.EligibleCommission, order.NetCommission))
            return await MarkInvalidAsync(record,
                "Hoa hồng hợp lệ từ bảng kê lệch với hoa hồng đơn hàng trong CatsBack.");
        if (manual is null && !bill.HasAuthoritativeEligibleCommission &&
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

        var attributionsByOrder = await LoadAttributionsByOrderAsync(new List<Guid> { order.Id });
        var amounts = manual is null ? ShopeeSettlementAmounts.For(record, bill) : ShopeeSettlementAmounts.Manual(manual);
        var plan = BuildAllocationPlan(amounts.Net,
            attributionsByOrder.GetValueOrDefault(order.Id, new List<AffiliateOrderItemAttribution>()));
        if (plan is null)
        {
            record.SetUnmatched("Đơn hàng chưa có affiliate link hợp lệ để cộng ví.");
            await _records.UpdateAsync(record, autoSave: false);
            return null;
        }
        amounts.Apply(record);
        record.ExtraProperties["OrderStatusAtApproval"] = (int)order.Status;
        if (manual is not null)
        {
            record.ExtraProperties["ManualGrossCommission"] = amounts.Gross;
            record.ExtraProperties["ManualTaxPercent"] = manual.TaxPercent!.Value;
            record.ExtraProperties["ManualServiceFeePercent"] = manual.ServiceFeePercent!.Value;
        }
        var soleUserId = plan.Recipients.Count == 1 ? plan.Recipients[0].UserId : (Guid?)null;
        record.SetPendingApproval(order.Id, conversion.Id, soleUserId,
            plan.UnmatchedCount > 0
                ? $"Có {plan.UnmatchedCount} affiliate link chưa ghép; phần này sẽ không được cộng cho người dùng."
                : null);
        return new ApprovalWork(record, bill, order, conversion, plan.Allocations, plan.Recipients);
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
        foreach (var allocation in work.Allocations)
            allocation.Attribution.Settle(allocation.SettledNetCommission, allocation.SettledUserCommission);
        work.Order.Settle(work.Record.ActualPaidCommission, work.TotalUserCommission,
            SettlementReference(work.Bill), SettlementTime(work.Bill));
        work.Record.Approve(adminId, Clock.Now, work.TotalUserCommission);
        await _orders.UpdateAsync(work.Order, autoSave: false);
        if (work.Allocations.Count > 0)
            await _attributions.UpdateManyAsync(work.Allocations.Select(x => x.Attribution), autoSave: false);
        await _records.UpdateAsync(work.Record, autoSave: false);
        foreach (var recipient in work.Recipients)
            await _notificationManager.NotifyOrderStatusAsync(recipient.UserId, work.Order,
                expectedUserCommission: null, settledUserCommission: recipient.UserCommission);
    }

    private Task RefreshBatchAsync(ShopeeSettlementBatch batch) => _matcher.RefreshBatchAsync(batch.Id);

    private Task<AdminShopeeSettlementApprovalResultDto> BuildResultAsync(ShopeeSettlementBatch batch,
        IReadOnlyCollection<ApprovalWork> approved, int skippedCount) => Task.FromResult(new AdminShopeeSettlementApprovalResultDto
    {
        BatchId = batch.Id,
        ApprovedCount = approved.Count,
        SkippedCount = skippedCount,
        ApprovedCommission = approved.Sum(item => item.Record.ActualPaidCommission),
        CreditedUserCommission = approved.Sum(item => item.TotalUserCommission),
        Batch = MapBatch(batch)
    });

    private AdminShopeeSettlementRecordDto MapRecord(ShopeeSettlementRecord record, ShopeeSettlementBill bill,
        IReadOnlyDictionary<Guid, List<string>> productNamesByOrder,
        IReadOnlyDictionary<Guid, List<AffiliateOrderItemAttribution>> attributionsByOrder,
        IReadOnlyDictionary<Guid, IdentityUser> users, IReadOnlyDictionary<Guid, AffiliateOrder> ordersById)
    {
        var order = record.AffiliateOrderId.HasValue
            ? ordersById.GetValueOrDefault(record.AffiliateOrderId.Value) : null;
        var amounts = ShopeeSettlementAmounts.For(record, bill);
        var actualPaidCommission = amounts.Net;
        var orderAttributions = record.AffiliateOrderId.HasValue
            ? attributionsByOrder.GetValueOrDefault(record.AffiliateOrderId.Value,
                new List<AffiliateOrderItemAttribution>())
            : new List<AffiliateOrderItemAttribution>();
        List<SettlementRecipient> recipientRows;
        if (record.Status == ShopeeSettlementRecordStatus.Approved)
        {
            recipientRows = orderAttributions.Where(x => x.UserId.HasValue && x.SettledUserCommission.HasValue)
                .GroupBy(x => x.UserId!.Value)
                .Select(group => new SettlementRecipient(group.Key,
                    group.Sum(x => x.SettledUserCommission ?? 0m))).ToList();
        }
        else
        {
            recipientRows = BuildAllocationPlan(actualPaidCommission, orderAttributions)?.Recipients ??
                new List<SettlementRecipient>();
        }
        var projected = recipientRows.Sum(x => x.UserCommission);
        var soleRecipient = recipientRows.Count == 1 ? recipientRows[0] : null;
        return new AdminShopeeSettlementRecordDto
        {
            Id = record.Id,
            OrderStatus = order?.Status,
            CreationTime = record.CreationTime,
            BatchId = record.BatchId,
            BillId = record.BillId,
            SourceAffiliateId = bill.SourceAffiliateId,
            ValidationId = bill.ValidationId,
            PayoutId = bill.PayoutId,
            PaidAt = bill.PaidAt,
            PaymentStatus = bill.PaymentStatus,
            ValidationPayoutStatus = bill.ValidationPayoutStatus,
            OverallValidationStatus = bill.OverallValidationStatus,
            BillValidationStatus = bill.BillValidationStatus,
            SettlementCycle = bill.SettlementCycle,
            IsShopeePaid = bill.IsShopeePaid,
            HasAdjustment = bill.HasAdjustment,
            HasClawback = bill.HasClawback,
            IsCumulative = bill.IsCumulative,
            HasBonus = bill.HasBonus,
            HasPpp = bill.HasPpp,
            ExternalOrderId = record.ExternalOrderId,
            DefaultGrossCommission = order?.NetCommission ?? record.EligibleCommission,
            EligibleCommission = record.EligibleCommission,
            AllocatedServiceFee = amounts.Fee,
            AllocatedTax = amounts.Tax,
            UsesDefaultServiceFee = amounts.DefaultFee || record.ExtraProperties.ContainsKey("DefaultServiceFeePercent"),
            UsesDefaultTax = amounts.DefaultTax || record.ExtraProperties.ContainsKey("DefaultTaxPercent"),
            ActualPaidCommission = actualPaidCommission,
            ProjectedUserCommission = projected,
            ApprovedUserCommission = record.ApprovedUserCommission,
            Status = record.Status,
            AffiliateOrderId = record.AffiliateOrderId,
            UserId = soleRecipient?.UserId ?? record.UserId,
            ProductNames = record.AffiliateOrderId.HasValue &&
                productNamesByOrder.TryGetValue(record.AffiliateOrderId.Value, out var productNames)
                    ? productNames
                    : new List<string>(),
            UserEmail = (soleRecipient?.UserId ?? record.UserId) is { } displayUserId &&
                users.TryGetValue(displayUserId, out var user)
                ? string.IsNullOrWhiteSpace(user.Email) ? user.UserName : user.Email
                : null,
            Recipients = recipientRows.Select(recipient => new AdminShopeeSettlementRecipientDto
            {
                UserId = recipient.UserId,
                UserEmail = users.TryGetValue(recipient.UserId, out var recipientUser)
                    ? string.IsNullOrWhiteSpace(recipientUser.Email) ? recipientUser.UserName : recipientUser.Email
                    : null,
                ProjectedUserCommission = recipient.UserCommission,
                ApprovedUserCommission = record.Status == ShopeeSettlementRecordStatus.Approved
                    ? recipient.UserCommission
                    : 0m
            }).ToList(),
            ApprovedAt = record.ApprovedAt,
            Issue = record.Issue
        };
    }

    private AdminShopeeSettlementRecordDto MapOrderWithoutSettlement(AffiliateOrder order,
        IReadOnlyDictionary<Guid, List<string>> productNamesByOrder,
        IReadOnlyDictionary<Guid, List<AffiliateOrderItemAttribution>> attributionsByOrder,
        IReadOnlyDictionary<Guid, IdentityUser> users, DateTime? purchaseTime)
    {
        var attributions = attributionsByOrder.GetValueOrDefault(order.Id, new List<AffiliateOrderItemAttribution>());
        var settled = order.Status == AffiliateOrderStatus.Settled || order.SettledAt.HasValue;
        var amounts = settled ? ApprovedDirectAmounts(order) : ShopeeSettlementAmounts.FromConversion(Math.Max(0m, order.NetCommission));
        var prepared = PrepareOrderWithoutSettlement(order, purchaseTime, attributions, Clock.Now.AddDays(-7));
        var plan = settled ? null : BuildAllocationPlan(amounts.Net, attributions);
        var recipients = attributions
            .Where(row => row.Status == AffiliateAttributionStatus.Matched && row.UserId.HasValue)
            .GroupBy(row => row.UserId!.Value)
            .Select(group => new AdminShopeeSettlementRecipientDto
            {
                UserId = group.Key,
                UserEmail = users.TryGetValue(group.Key, out var user)
                    ? string.IsNullOrWhiteSpace(user.Email) ? user.UserName : user.Email : null,
                ProjectedUserCommission = settled ? group.Sum(row => row.SettledUserCommission ?? 0m)
                    : plan?.Recipients.FirstOrDefault(recipient => recipient.UserId == group.Key)?.UserCommission ?? 0m,
                ApprovedUserCommission = settled ? group.Sum(row => row.SettledUserCommission ?? 0m) : 0m
            }).ToList();
        return new AdminShopeeSettlementRecordDto
        {
            Id = order.Id,
            CreationTime = order.CreationTime,
            HasSettlementRecord = false,
            CanApproveWithoutSettlement = prepared is not null,
            OrderStatus = order.Status,
            ExternalOrderId = order.ExternalOrderId,
            AffiliateOrderId = order.Id,
            Status = settled ? ShopeeSettlementRecordStatus.Approved : prepared is not null
                ? ShopeeSettlementRecordStatus.PendingApproval : ShopeeSettlementRecordStatus.Unmatched,
            DefaultGrossCommission = amounts.Gross,
            EligibleCommission = amounts.Gross,
            AllocatedServiceFee = amounts.Fee,
            AllocatedTax = amounts.Tax,
            ActualPaidCommission = amounts.Net,
            ProjectedUserCommission = settled ? order.SettledUserCommission ?? 0m
                : plan?.Recipients.Sum(recipient => recipient.UserCommission) ?? 0m,
            ApprovedUserCommission = settled ? order.SettledUserCommission ?? 0m : 0m,
            ApprovedAt = settled ? order.SettledAt : null,
            UsesDefaultTax = amounts.DefaultTax,
            UsesDefaultServiceFee = amounts.DefaultFee,
            ProductNames = productNamesByOrder.GetValueOrDefault(order.Id, new List<string>()),
            Recipients = recipients,
            UserId = recipients.Count == 1 ? recipients[0].UserId : null,
            UserEmail = recipients.Count == 1 ? recipients[0].UserEmail : null,
            Issue = settled ? null : prepared is not null
                ? "Đơn hoàn thành, đã đặt hơn 7 ngày; có thể duyệt và cộng ví, không cần bảng kê."
                : order.Status != AffiliateOrderStatus.Completed ? "Chưa đủ điều kiện: đơn chưa hoàn thành."
                : !purchaseTime.HasValue || purchaseTime.Value <= DateTime.UnixEpoch ? "Chưa có ngày đặt hàng hợp lệ để xét mốc 7 ngày."
                : purchaseTime.Value >= Clock.Now.AddDays(-7) ? "Chưa đủ điều kiện: thời gian đặt hàng phải hơn 7 ngày."
                : "Cần kiểm tra affiliate link/người nhận hoặc dữ liệu đã chốt tiền của đơn."
        };
    }

    private async Task<IQueryable<AffiliateOrder>> GetFilteredOrdersWithoutSettlementAsync(
        AdminShopeeSettlementBatchListInput input)
    {
        var orders = (await _orders.GetQueryableAsync()).Where(order => order.Platform == AffiliatePlatform.Shopee);
        var records = await _records.GetQueryableAsync();
        // Exclude against all records, regardless of the current filters, to avoid showing the same order twice.
        orders = orders.Where(order => !records.Any(record => record.AffiliateOrderId == order.Id ||
            record.ExternalOrderId == order.ExternalOrderId));
        // These filters identify settlement metadata that conversion-only orders do not have.
        if (!string.IsNullOrWhiteSpace(input.Filter) || input.Status.HasValue ||
            !string.IsNullOrWhiteSpace(input.AffiliateId) || input.IsShopeePaid == true)
            return orders.Where(order => false);
        if (input.OrderReview is AdminShopeeOrderReviewFilter.Completed or AdminShopeeOrderReviewFilter.CompletedUnprocessed)
            orders = orders.Where(order => order.Status == AffiliateOrderStatus.Completed && !order.SettledAt.HasValue);
        if (input.OrderReview is AdminShopeeOrderReviewFilter.Unprocessed or AdminShopeeOrderReviewFilter.CompletedUnprocessed)
            orders = orders.Where(order => order.Status != AffiliateOrderStatus.Settled && !order.SettledAt.HasValue);
        if (input.IsApproved.HasValue)
            orders = input.IsApproved.Value
                ? orders.Where(order => order.Status == AffiliateOrderStatus.Settled || order.SettledAt.HasValue)
                : orders.Where(order => order.Status != AffiliateOrderStatus.Settled && !order.SettledAt.HasValue);
        if (input.FromDate.HasValue || input.ToDate.HasValue)
        {
            var conversions = (await _conversions.GetQueryableAsync())
                .Where(conversion => conversion.Platform == AffiliatePlatform.Shopee);
            if (input.FromDate.HasValue)
            {
                var fromUtc = new DateTimeOffset(DateTime.SpecifyKind(input.FromDate.Value.Date,
                    DateTimeKind.Unspecified), TimeSpan.FromHours(7)).UtcDateTime;
                conversions = conversions.Where(conversion => conversion.PurchaseTime >= fromUtc);
            }
            if (input.ToDate.HasValue)
            {
                var toUtc = new DateTimeOffset(DateTime.SpecifyKind(input.ToDate.Value.Date.AddDays(1),
                    DateTimeKind.Unspecified), TimeSpan.FromHours(7)).UtcDateTime;
                conversions = conversions.Where(conversion => conversion.PurchaseTime < toUtc);
            }
            orders = orders.Where(order => conversions.Any(conversion => conversion.Id == order.ConversionId));
        }
        return orders;
    }

    private sealed class SettlementListEntry
    {
        public Guid Id { get; set; }
        public bool HasSettlementRecord { get; set; }
        public Guid? OrderId { get; set; }
        public DateTime CreationTime { get; set; }
        public string ExternalOrderId { get; set; } = string.Empty;
    }

    private async Task<IQueryable<ShopeeSettlementRecord>> GetFilteredRecordsAsync(
        AdminShopeeSettlementBatchListInput? input)
    {
        var records = await _records.GetQueryableAsync();
        if (input is null) return records;
        var filterError = input.Validate(new ValidationContext(input)).FirstOrDefault();
        if (filterError is not null) throw new UserFriendlyException(filterError.ErrorMessage!);
        if (input.OrderReview.HasValue)
        {
            var orders = (await _orders.GetQueryableAsync()).Where(order => order.Platform == AffiliatePlatform.Shopee);
            if (input.OrderReview is AdminShopeeOrderReviewFilter.Completed or AdminShopeeOrderReviewFilter.CompletedUnprocessed)
                records = records.Where(record => orders.Any(order => order.Id == record.AffiliateOrderId &&
                    order.Status == AffiliateOrderStatus.Completed && !order.SettledAt.HasValue));
            if (input.OrderReview is AdminShopeeOrderReviewFilter.Unprocessed or AdminShopeeOrderReviewFilter.CompletedUnprocessed)
                records = records.Where(record => record.Status != ShopeeSettlementRecordStatus.Approved &&
                    record.Status != ShopeeSettlementRecordStatus.AlreadySettled &&
                    !orders.Any(order => order.Id == record.AffiliateOrderId &&
                        (order.Status == AffiliateOrderStatus.Settled || order.SettledAt.HasValue)));
        }
        if (input.FromDate.HasValue || input.ToDate.HasValue)
        {
            var conversions = (await _conversions.GetQueryableAsync())
                .Where(conversion => conversion.Platform == AffiliatePlatform.Shopee);
            if (input.FromDate.HasValue)
            {
                var fromUtc = new DateTimeOffset(DateTime.SpecifyKind(input.FromDate.Value.Date,
                    DateTimeKind.Unspecified), TimeSpan.FromHours(7)).UtcDateTime;
                conversions = conversions.Where(conversion => conversion.PurchaseTime >= fromUtc);
            }
            if (input.ToDate.HasValue)
            {
                var toUtc = new DateTimeOffset(DateTime.SpecifyKind(input.ToDate.Value.Date.AddDays(1),
                    DateTimeKind.Unspecified), TimeSpan.FromHours(7)).UtcDateTime;
                conversions = conversions.Where(conversion => conversion.PurchaseTime < toUtc);
            }
            records = records.Where(record => conversions.Any(conversion =>
                conversion.Id == record.AffiliateConversionId));
        }
        var batches = await _batches.GetQueryableAsync();
        var bills = await _bills.GetQueryableAsync();
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim();
            batches = batches.Where(batch => batch.OriginalFileName.Contains(filter) || batch.ContentHash.Contains(filter));
        }
        if (input.Status.HasValue) batches = batches.Where(batch => batch.Status == input.Status.Value);
        if (input.IsApproved.HasValue)
            records = input.IsApproved.Value
                ? records.Where(record => record.Status == ShopeeSettlementRecordStatus.Approved)
                : records.Where(record => record.Status != ShopeeSettlementRecordStatus.Approved);
        // Match the persisted fields behind ShopeeSettlementBill.IsShopeePaid for SQL translation.
        if (input.IsShopeePaid.HasValue)
            bills = input.IsShopeePaid.Value
                ? bills.Where(bill => bill.PaidAt.HasValue && bill.PaidAt.Value > DateTime.UnixEpoch)
                : bills.Where(bill => !bill.PaidAt.HasValue || bill.PaidAt.Value <= DateTime.UnixEpoch);
        if (!string.IsNullOrWhiteSpace(input.AffiliateId))
        {
            var affiliateId = input.AffiliateId.Trim();
            bills = bills.Where(bill => bill.SourceAffiliateId == affiliateId);
        }
        return from record in records
            join batch in batches on record.BatchId equals batch.Id
            join bill in bills on record.BillId equals bill.Id
            select record;
    }

    private static AdminShopeeSettlementBatchDto MapFilteredBatch(ShopeeSettlementBatch batch,
        IReadOnlyCollection<ShopeeSettlementRecord> records, IReadOnlyDictionary<Guid, ShopeeSettlementBill> bills)
    {
        var dto = MapBatch(batch);
        dto.BillCount = records.Select(record => record.BillId).Distinct().Count();
        dto.RecordCount = records.Count;
        dto.PendingCount = records.Count(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval);
        dto.ApprovedCount = records.Count(record => record.Status == ShopeeSettlementRecordStatus.Approved);
        dto.UnmatchedCount = records.Count(record => record.Status == ShopeeSettlementRecordStatus.Unmatched);
        dto.AlreadySettledCount = records.Count(record => record.Status == ShopeeSettlementRecordStatus.AlreadySettled);
        dto.InvalidCount = records.Count(record => record.Status == ShopeeSettlementRecordStatus.Invalid);
        dto.WaitingPaymentCount = records.Count(record => record.Status == ShopeeSettlementRecordStatus.AwaitingShopeePayment);
        dto.TotalEligibleCommission = records.Sum(record => record.EligibleCommission);
        dto.TotalPaidCommission = records.Sum(record => EffectivePaidCommission(record, bills[record.BillId]));
        dto.PendingPaidCommission = records.Where(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval)
            .Sum(record => EffectivePaidCommission(record, bills[record.BillId]));
        dto.ApprovedPaidCommission = records.Where(record => record.Status == ShopeeSettlementRecordStatus.Approved)
            .Sum(record => record.ActualPaidCommission);
        return dto;
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
        WaitingPaymentCount = batch.WaitingPaymentCount,
        TotalEligibleCommission = batch.TotalEligibleCommission,
        TotalPaidCommission = batch.TotalPaidCommission,
        PendingPaidCommission = batch.PendingPaidCommission,
        ApprovedPaidCommission = batch.ApprovedPaidCommission
    };

    private sealed record ApprovalWork(ShopeeSettlementRecord Record, ShopeeSettlementBill Bill,
        AffiliateOrder Order, AffiliateConversion Conversion, List<SettlementAllocation> Allocations,
        List<SettlementRecipient> Recipients)
    {
        public decimal TotalUserCommission => Recipients.Sum(x => x.UserCommission);
    }

    private sealed record SettlementAllocation(AffiliateOrderItemAttribution Attribution,
        decimal SettledNetCommission, decimal SettledUserCommission);

    private sealed record SettlementRecipient(Guid UserId, decimal UserCommission);

    private sealed record SettlementFinancialSummary(int OrderCount, decimal ActualCommission,
        decimal UserCommission, decimal AdminCommission, decimal PendingCommission);

    private sealed record SettlementAllocationPlan(List<SettlementAllocation> Allocations,
        List<SettlementRecipient> Recipients, int UnmatchedCount);

    private sealed record BulkApprovalPreparation(List<ApprovalWork> Work, int SkippedCount);

    private async Task<Dictionary<Guid, List<AffiliateOrderItemAttribution>>> LoadAttributionsByOrderAsync(
        IReadOnlyCollection<Guid> orderIds)
    {
        if (orderIds.Count == 0) return new Dictionary<Guid, List<AffiliateOrderItemAttribution>>();
        var items = await _items.GetListAsync(x => orderIds.Contains(x.OrderId));
        var itemIds = items.Select(x => x.Id).ToList();
        if (itemIds.Count == 0) return new Dictionary<Guid, List<AffiliateOrderItemAttribution>>();
        var orderByItem = items.ToDictionary(x => x.Id, x => x.OrderId);
        return (await _attributions.GetListAsync(x => itemIds.Contains(x.OrderItemId)))
            .Where(x => orderByItem.ContainsKey(x.OrderItemId))
            .GroupBy(x => orderByItem[x.OrderItemId])
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    private static ShopeeSettlementAmounts ApprovedDirectAmounts(AffiliateOrder order) => new(
        order.GetProperty<decimal?>("GrossCommissionAtApproval") ?? order.NetCommission,
        order.GetProperty<decimal?>("ServiceFeeAtApproval") ?? 0m,
        order.GetProperty<decimal?>("TaxAtApproval") ?? 0m,
        order.SettledNetCommission ?? 0m,
        order.ExtraProperties.ContainsKey("DefaultServiceFeePercent"),
        order.ExtraProperties.ContainsKey("DefaultTaxPercent"));

    private async Task<DirectApprovalSummary> BuildDirectApprovalSummaryAsync(AdminShopeeSettlementBatchListInput input)
    {
        var orders = await GetFilteredOrdersWithoutSettlementAsync(input);
        var conversions = (await _conversions.GetQueryableAsync())
            .Where(conversion => conversion.Platform == AffiliatePlatform.Shopee);
        var cutoff = Clock.Now.AddDays(-7);
        var candidates = await AsyncExecuter.ToListAsync(orders.Where(order =>
            order.Status == AffiliateOrderStatus.Settled || order.SettledAt.HasValue ||
            (order.Status == AffiliateOrderStatus.Completed && conversions.Any(conversion =>
                conversion.Id == order.ConversionId && conversion.PurchaseTime > DateTime.UnixEpoch &&
                conversion.PurchaseTime < cutoff))));
        if (candidates.Count == 0)
            return new DirectApprovalSummary(0, 0, 0m, new SettlementFinancialSummary(0, 0m, 0m, 0m, 0m));
        var conversionIds = candidates.Select(order => order.ConversionId).Distinct().ToList();
        var conversionRows = (await _conversions.GetListAsync(conversion => conversionIds.Contains(conversion.Id) &&
            conversion.Platform == AffiliatePlatform.Shopee)).ToDictionary(conversion => conversion.Id);
        var attributions = await LoadAttributionsByOrderAsync(candidates.Select(order => order.Id).ToList());
        var pendingCount = 0;
        var approvedCount = 0;
        decimal pendingAmount = 0m, approvedAmount = 0m, userAmount = 0m;
        foreach (var order in candidates)
        {
            if (order.Status == AffiliateOrderStatus.Settled || order.SettledAt.HasValue)
            {
                approvedCount++;
                approvedAmount += order.SettledNetCommission ?? 0m;
                userAmount += order.SettledUserCommission ?? 0m;
                continue;
            }
            var prepared = PrepareOrderWithoutSettlement(order,
                conversionRows.GetValueOrDefault(order.ConversionId)?.PurchaseTime,
                attributions.GetValueOrDefault(order.Id, new List<AffiliateOrderItemAttribution>()), cutoff);
            if (prepared is null) continue;
            pendingCount++;
            pendingAmount += prepared.Amounts.Net;
            userAmount += prepared.Plan.Recipients.Sum(recipient => recipient.UserCommission);
        }
        var total = pendingAmount + approvedAmount;
        return new DirectApprovalSummary(pendingCount, approvedCount, approvedAmount,
            new SettlementFinancialSummary(pendingCount + approvedCount, total, userAmount,
                Math.Max(0m, total - userAmount), pendingAmount));
    }

    private sealed record DirectApprovalSummary(int PendingCount, int ApprovedCount, decimal ApprovedCommission,
        SettlementFinancialSummary Financial);

    private async Task<SettlementFinancialSummary> BuildFinancialSummaryAsync(
        IQueryable<ShopeeSettlementRecord> records)
    {
        var eligibleQuery = records.Where(record => record.Status == ShopeeSettlementRecordStatus.PendingApproval ||
            record.Status == ShopeeSettlementRecordStatus.Approved);
        var eligible = await AsyncExecuter.ToListAsync(eligibleQuery);
        if (eligible.Count == 0) return new SettlementFinancialSummary(0, 0m, 0m, 0m, 0m);

        var billIds = eligible.Select(record => record.BillId).Distinct().ToList();
        var bills = (await _bills.GetListAsync(bill => billIds.Contains(bill.Id)))
            .ToDictionary(bill => bill.Id);
        var orderIds = eligible.Where(record => record.AffiliateOrderId.HasValue)
            .Select(record => record.AffiliateOrderId!.Value).Distinct().ToList();
        var attributionsByOrder = await LoadAttributionsByOrderAsync(orderIds);
        decimal actualCommission = 0m;
        decimal userCommission = 0m;
        decimal pendingCommission = 0m;
        foreach (var record in eligible)
        {
            var paidCommission = EffectivePaidCommission(record, bills[record.BillId]);
            var recordUserCommission = record.Status == ShopeeSettlementRecordStatus.Approved
                ? record.ApprovedUserCommission
                : record.AffiliateOrderId.HasValue
                    ? BuildAllocationPlan(paidCommission, attributionsByOrder.GetValueOrDefault(
                        record.AffiliateOrderId.Value, new List<AffiliateOrderItemAttribution>()))
                        ?.Recipients.Sum(recipient => recipient.UserCommission) ?? 0m
                    : 0m;
            actualCommission += paidCommission;
            if (record.Status == ShopeeSettlementRecordStatus.PendingApproval) pendingCommission += paidCommission;
            userCommission += Math.Min(paidCommission, Math.Max(0m, recordUserCommission));
        }

        return new SettlementFinancialSummary(eligible.Count, actualCommission, userCommission,
            Math.Max(0m, actualCommission - userCommission), pendingCommission);
    }

    private SettlementAllocationPlan? BuildAllocationPlan(decimal actualPaidCommission,
        IReadOnlyCollection<AffiliateOrderItemAttribution> attributions)
    {
        var rows = attributions.OrderBy(x => x.OrderItemId).ThenBy(x => x.AttributionValue,
            StringComparer.Ordinal).ThenBy(x => x.Id).ToList();
        var matched = rows.Where(x => x.Status == AffiliateAttributionStatus.Matched && x.UserId.HasValue)
            .ToList();
        if (matched.Count == 0) return null;

        var keyById = rows.ToDictionary(x => x.Id,
            x => $"{x.OrderItemId:N}:{x.AttributionValue}:{x.Id:N}");
        var settledNet = _calculator.AllocateAmount(actualPaidCommission,
                rows.Select(x => new AmountAllocationInput(keyById[x.Id], x.AllocatedNetCommission)), 4)
            .ToDictionary(x => x.Key, x => x.Amount, StringComparer.Ordinal);
        var settledUser = new Dictionary<Guid, decimal>();
        foreach (var userRows in matched.GroupBy(x => x.UserId!.Value))
        {
            var ordered = userRows.OrderBy(x => keyById[x.Id], StringComparer.Ordinal).ToList();
            var userNet = ordered.Sum(x => settledNet[keyById[x.Id]]);
            var rate = ordered.Select(x => x.UserShareRate).FirstOrDefault();
            // Wallet credits use whole VND. Rounding a 100% share upward must
            // not exceed this recipient's actual proceeds (e.g. 74260.7358).
            var target = Math.Min(_calculator.CalculateUserCommission(userNet, rate),
                decimal.Floor(userNet));
            foreach (var allocation in _calculator.AllocateAmount(target,
                         ordered.Select(x => new AmountAllocationInput(keyById[x.Id],
                             settledNet[keyById[x.Id]])), 0))
            {
                var attributionId = ordered.First(x => keyById[x.Id] == allocation.Key).Id;
                settledUser[attributionId] = allocation.Amount;
            }
        }

        var allocations = rows.Select(x => new SettlementAllocation(x,
            settledNet[keyById[x.Id]], settledUser.GetValueOrDefault(x.Id))).ToList();
        var recipients = matched.GroupBy(x => x.UserId!.Value)
            .Select(group => new SettlementRecipient(group.Key,
                group.Sum(x => settledUser.GetValueOrDefault(x.Id))))
            .OrderBy(x => x.UserId).ToList();
        return new SettlementAllocationPlan(allocations, recipients,
            rows.Count(x => x.Status != AffiliateAttributionStatus.Matched || !x.UserId.HasValue));
    }

    private DateTime SettlementTime(ShopeeSettlementBill bill) => bill.IsShopeePaid ? bill.PaidAt!.Value : Clock.Now;

    private static string SettlementReference(ShopeeSettlementBill bill) =>
        string.IsNullOrWhiteSpace(bill.PayoutId) ? $"validation:{bill.ValidationId}" : bill.PayoutId;

    private static decimal EffectivePaidCommission(ShopeeSettlementRecord record, ShopeeSettlementBill bill) =>
        ShopeeSettlementAmounts.For(record, bill).Net;
}
