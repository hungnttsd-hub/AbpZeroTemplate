using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Threading;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using WebHoanTien.Affiliates;
using WebHoanTien.Notifications;
using WebHoanTien.Permissions;

namespace WebHoanTien.TikTokAffiliate;

[Authorize(Roles = "admin")]
[Authorize(WebHoanTienPermissions.Admin.Orders)]
[RemoteService(IsEnabled = false)]
[UnitOfWork(IsDisabled = true)]
public class AdminTikTokSettlementAppService : WebHoanTienAppService, IAdminTikTokSettlementAppService
{
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;
    private readonly IRepository<AffiliateOrderItemAttribution, Guid> _attributions;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly ITikTokCashbackCreatorRegistry _creator;
    private readonly ITikTokManualSyncLock _syncLock;
    private readonly ITikTokSettlementLedgerLock _ledgerLock;
    private readonly IUnitOfWorkManager _uow;
    private readonly ICurrentTenant _tenant;
    private readonly ICancellationTokenProvider _cancellation;
    private readonly AffiliateCommissionCalculator _calculator;
    private readonly CustomerNotificationManager _notifications;

    public AdminTikTokSettlementAppService(IRepository<AffiliateOrder, Guid> orders,
        IRepository<AffiliateOrderItem, Guid> items, IRepository<AffiliateOrderItemAttribution, Guid> attributions,
        IRepository<AffiliateConversion, Guid> conversions, IRepository<AffiliateTracking, Guid> trackings,
        IRepository<IdentityUser, Guid> users, ITikTokCashbackCreatorRegistry creator,
        ITikTokManualSyncLock syncLock, ITikTokSettlementLedgerLock ledgerLock, IUnitOfWorkManager uow,
        ICurrentTenant tenant, ICancellationTokenProvider cancellation, AffiliateCommissionCalculator calculator,
        CustomerNotificationManager notifications)
    {
        _orders = orders; _items = items; _attributions = attributions; _conversions = conversions;
        _trackings = trackings; _users = users; _creator = creator; _syncLock = syncLock; _ledgerLock = ledgerLock;
        _uow = uow; _tenant = tenant; _cancellation = cancellation; _calculator = calculator; _notifications = notifications;
    }

    private void RequireHostAdmin()
    {
        if (_tenant.Id.HasValue || !CurrentUser.IsAuthenticated || !CurrentUser.IsInRole("admin"))
            throw new AbpAuthorizationException("Chỉ admin hệ thống được đối soát TikTok.");
    }

    public async Task<TikTokSettlementPreview> PreviewAsync(Guid orderId, TikTokSettlementInput? input = null)
    {
        RequireHostAdmin();
        using var work = _uow.Begin(requiresNew: true, isTransactional: false);
        var plan = await BuildAsync(orderId, input);
        await work.CompleteAsync(_cancellation.Token);
        return plan.Preview;
    }

    public async Task<TikTokSettlementApprovalResult> ApproveAsync(Guid orderId, TikTokSettlementInput input, string expectedPlanHash)
        => await ApproveCoreAsync(orderId, input, expectedPlanHash, requirePaid: false);

    public async Task<List<TikTokSettlementPreview>> PreviewPaidOrdersAsync(string reason)
    {
        RequireHostAdmin();
        ValidateReason(reason);
        List<(Guid Id, string ExternalOrderId)> candidates;
        using (var work = _uow.Begin(requiresNew: true, isTransactional: false))
        {
            var orders = await _orders.GetListAsync(x => x.Platform == AffiliatePlatform.TikTok &&
                !x.SettledAt.HasValue && x.Status != AffiliateOrderStatus.Settled, cancellationToken: _cancellation.Token);
            candidates = orders.Where(x => x.GetProperty<string>("TikTokProviderStatus") == "SETTLED")
                .OrderBy(x => x.ExternalOrderId, StringComparer.Ordinal).Select(x => (x.Id, x.ExternalOrderId)).ToList();
            await work.CompleteAsync(_cancellation.Token);
        }
        var previews = new List<TikTokSettlementPreview>();
        foreach (var candidate in candidates)
        {
            _cancellation.Token.ThrowIfCancellationRequested();
            try
            {
                using var work = _uow.Begin(requiresNew: true, isTransactional: false);
                var plan = await BuildAsync(candidate.Id, PaidInput(reason));
                if (!plan.Preview.AlreadySettled && plan.Preview.ProviderStatus != "SETTLED")
                    plan.Preview.Issues.Add("TikTok đã thay đổi trạng thái; đơn không còn đủ điều kiện duyệt hàng loạt.");
                await work.CompleteAsync(_cancellation.Token);
                previews.Add(plan.Preview);
            }
            catch (AbpAuthorizationException) { throw; }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogWarning("TikTok bulk preview failed for order {OrderId}: {ErrorType}", candidate.Id, ex.GetType().Name);
                previews.Add(new TikTokSettlementPreview
                {
                    OrderId = candidate.Id, ExternalOrderId = candidate.ExternalOrderId,
                    Issues = new() { ex is UserFriendlyException friendly ? friendly.Message :
                        "Không đọc được dữ liệu đối soát của đơn. Mở đơn để kiểm tra và xem trước lại." }
                });
            }
        }
        return previews;
    }

    public async Task<TikTokBulkSettlementResult> ApprovePaidOrdersAsync(List<TikTokBulkSettlementPlan> plans, string reason)
    {
        RequireHostAdmin();
        ValidateReason(reason);
        if (plans.Count == 0 || plans.Select(x => x.OrderId).Distinct().Count() != plans.Count)
            throw new UserFriendlyException("Danh sách duyệt trống hoặc trùng đơn. Hãy xem trước lại.");
        var result = new TikTokBulkSettlementResult();
        foreach (var plan in plans)
        {
            _cancellation.Token.ThrowIfCancellationRequested();
            try
            {
                // One transaction and a fresh eligibility/hash check per order. A blocked order must not stop other orders.
                var approved = await ApproveCoreAsync(plan.OrderId, PaidInput(reason), plan.PlanHash, requirePaid: true);
                result.Orders.Add(new(plan.OrderId, plan.ExternalOrderId, !approved.AlreadySettled,
                    approved.AlreadySettled, approved.CreditedAmount, null));
            }
            catch (AbpAuthorizationException) { throw; }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogWarning("TikTok bulk settlement failed for order {OrderId}: {ErrorType}", plan.OrderId, ex.GetType().Name);
                var message = ex is UserFriendlyException friendly ? friendly.Message :
                    "Chưa xác nhận duyệt được đơn này. Mở lại đối soát để kiểm tra trạng thái và xem trước lại.";
                result.Orders.Add(new(plan.OrderId, plan.ExternalOrderId, false, false, 0, message));
            }
        }
        return result;
    }

    private static TikTokSettlementInput PaidInput(string reason) => new()
    { Source = TikTokSettlementAmountSource.ActualCommission, Reason = reason.Trim() };

    private static void ValidateReason(string? reason)
    {
        reason = reason?.Trim();
        if (reason is not { Length: >= 10 and <= 1000 } || reason.Any(char.IsControl))
            throw new UserFriendlyException("Nhập lý do duyệt từ 10–1.000 ký tự, trên một dòng.");
    }

    private async Task<TikTokSettlementApprovalResult> ApproveCoreAsync(Guid orderId, TikTokSettlementInput input,
        string expectedPlanHash, bool requirePaid)
    {
        RequireHostAdmin();
        input.Reason = input.Reason?.Trim();
        ValidateReason(input.Reason);
        if (expectedPlanHash is not { Length: 64 } || !expectedPlanHash.All(char.IsAsciiHexDigit))
            throw new UserFriendlyException("Hãy xem trước đối soát trước khi duyệt.");
        var ct = _cancellation.Token;
        // Share the import lock across instances; synchronize the ledger only inside this short transaction.
        await using var sync = await _syncLock.AcquireAsync(ct);
        using var work = _uow.Begin(requiresNew: true, isTransactional: true);
        await _ledgerLock.LockAsync(orderId, ct);
        var plan = await BuildAsync(orderId, input);
        var preview = plan.Preview;
        if (preview.AlreadySettled)
        {
            await work.CompleteAsync(ct);
            return new(true, 0);
        }
        if (!preview.CanApprove) throw new UserFriendlyException(string.Join(" ", preview.Issues));
        if (requirePaid && (preview.ProviderStatus != "SETTLED" || input.Source != TikTokSettlementAmountSource.ActualCommission))
            throw new UserFriendlyException("Chỉ duyệt hàng loạt đơn TikTok đã SETTLED bằng hoa hồng thực nhận.");
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expectedPlanHash), Encoding.ASCII.GetBytes(preview.PlanHash)))
            throw new UserFriendlyException("Dữ liệu đơn, người nhận hoặc số tiền đã đổi. Hãy xem trước lại trước khi duyệt.");
        foreach (var row in plan.Rows) row.Attribution.Settle(row.Net, row.UserAmount);
        var reference = "tiktok:" + plan.Order.ExternalOrderId;
        if (reference.Length > 128) reference = "tiktok:" + plan.Order.Id.ToString("N");
        plan.Order.SetProperty("TikTokSettlementApprovedBy", CurrentUser.GetId());
        plan.Order.SetProperty("TikTokSettlementReason", input.Reason);
        plan.Order.SetProperty("TikTokSettlementAmountSource", (int)input.Source);
        plan.Order.SetProperty("TikTokSettlementGross", preview.GrossCommission);
        plan.Order.SetProperty("TikTokSettlementTaxPercent", preview.TaxPercent);
        plan.Order.SetProperty("TikTokSettlementTaxAmount", preview.TaxAmount);
        plan.Order.SetProperty("TikTokSettlementProviderTaxAmount", preview.ProviderTaxAmount);
        plan.Order.SetProperty("TikTokSettlementAdditionalTaxBase", preview.AdditionalTaxBase);
        plan.Order.SetProperty("TikTokSettlementTaxPolicy", "ZeroWithholdingTenPercentV1");
        plan.Order.SetProperty("TikTokSettlementProviderStatus", preview.ProviderStatus);
        plan.Order.SetProperty("TikTokSettlementOrderStatus", (int)plan.Order.Status);
        plan.Order.SetProperty("TikTokSettlementCreatorOpenId", preview.CreatorOpenId);
        plan.Order.SetProperty("TikTokSettlementPlanHash", preview.PlanHash);
        plan.Order.SetProperty("TikTokSettlementAllocations", JsonSerializer.Serialize(plan.Rows.Select(x => new
        {
            attributionId = x.Attribution.Id, x.Attribution.UserId, x.Attribution.TrackingId,
            x.Attribution.UserShareRate, net = x.Net, userAmount = x.UserAmount
        })));
        plan.Order.Settle(preview.NetCommission, preview.UserCommission, reference, Clock.Now);
        await _attributions.UpdateManyAsync(plan.Rows.Select(x => x.Attribution), autoSave: false, cancellationToken: ct);
        await _orders.UpdateAsync(plan.Order, autoSave: false, cancellationToken: ct);
        foreach (var recipient in preview.Recipients.Where(x => x.UserCommission > 0))
            await _notifications.NotifyOrderStatusAsync(recipient.UserId, plan.Order, settledUserCommission: recipient.UserCommission);
        // WalletBalanceCalculator derives the credit from these settled ledger rows. No second wallet increment.
        await work.CompleteAsync(ct);
        return new(false, preview.UserCommission);
    }

    private async Task<Plan> BuildAsync(Guid orderId, TikTokSettlementInput? input)
    {
        var order = await _orders.GetAsync(orderId);
        if (order.Platform != AffiliatePlatform.TikTok) throw new UserFriendlyException("Đây không phải đơn TikTok.");
        var conversion = await _conversions.GetAsync(order.ConversionId);
        if (conversion.Platform != AffiliatePlatform.TikTok) throw new UserFriendlyException("Conversion của đơn không khớp TikTok.");
        var actual = order.GetProperty<decimal?>("TikTokActualCommission");
        var useDefaults = input is null;
        input ??= new TikTokSettlementInput
        {
            Source = actual.HasValue ? TikTokSettlementAmountSource.ActualCommission : TikTokSettlementAmountSource.ManualGross,
            // Neither estimated_commission nor actual_commission proves the before-tax gross.
            // Manual gross must be entered explicitly; never prefill it with an API net amount.
            GrossCommission = null,
            TaxPercent = 10m
        };
        if (!useDefaults) ValidateAmounts(input);
        if (input.TaxPercent == 0) input.TaxPercent = 10m;
        var preview = new TikTokSettlementPreview
        {
            OrderId = order.Id, ExternalOrderId = order.ExternalOrderId, Input = input,
            CreatorOpenId = order.GetProperty<string>(TikTokAffiliateAccess.OrderCreatorProperty),
            ProviderStatus = order.GetProperty<string>("TikTokProviderStatus"), OrderStatus = order.Status.ToString(),
            LastCheckedAt = order.GetProperty<DateTime?>("TikTokLastCheckedAt"),
            AlreadySettled = order.SettledAt.HasValue || order.Status == AffiliateOrderStatus.Settled,
            SettledAt = order.SettledAt, SettlementReference = order.SettlementReference,
            ApprovedBy = order.GetProperty<Guid?>("TikTokSettlementApprovedBy"),
            ApprovalReason = order.GetProperty<string>("TikTokSettlementReason"),
            ActualCommission = actual, EstimatedCommission = order.GetProperty<decimal?>("TikTokEstimatedCommission")
        };
        var items = await _items.GetListAsync(x => x.OrderId == order.Id);
        var itemIds = items.Select(x => x.Id).ToList();
        var attrs = itemIds.Count == 0 ? new List<AffiliateOrderItemAttribution>() :
            await _attributions.GetListAsync(x => itemIds.Contains(x.OrderItemId));
        var rows = attrs.OrderBy(x => x.OrderItemId).ThenBy(x => x.Id).Select(x => new Allocation(x)).ToList();
        if (preview.AlreadySettled)
        {
            preview.GrossCommission = order.GetProperty<decimal?>("TikTokSettlementGross") ?? order.SettledNetCommission ?? 0;
            preview.TaxPercent = order.GetProperty<decimal?>("TikTokSettlementTaxPercent") ?? 0;
            preview.TaxAmount = order.GetProperty<decimal?>("TikTokSettlementTaxAmount") ?? 0;
            preview.ProviderTaxAmount = order.GetProperty<decimal?>("TikTokSettlementProviderTaxAmount");
            preview.AdditionalTaxBase = order.GetProperty<decimal?>("TikTokSettlementAdditionalTaxBase") ??
                (preview.TaxPercent > 0 ? preview.GrossCommission : 0);
            preview.NetCommission = order.SettledNetCommission ?? 0; preview.UserCommission = order.SettledUserCommission ?? 0;
            foreach (var row in rows) { row.Net = row.Attribution.SettledNetCommission ?? 0; row.UserAmount = row.Attribution.SettledUserCommission ?? 0; }
            preview.UnmatchedNetCommission = rows.Where(x => x.Attribution.Status != AffiliateAttributionStatus.Matched || !x.Attribution.UserId.HasValue).Sum(x => x.Net);
            await AddRecipientsAsync(preview, rows);
            return new(order, preview, rows);
        }
        var registeredCreator = await _creator.GetOpenIdAsync(_cancellation.Token);
        if (string.IsNullOrWhiteSpace(registeredCreator) || preview.CreatorOpenId != registeredCreator)
            preview.Issues.Add("Đơn không thuộc Creator hoàn tiền chính do admin đăng ký.");
        if (items.Count == 0 || attrs.Count == 0 || items.Any(x => attrs.All(a => a.OrderItemId != x.Id)))
            preview.Issues.Add("Đơn thiếu dòng SKU hoặc thông tin phân bổ hoa hồng.");
        if (attrs.Any(x => x.Status == AffiliateAttributionStatus.Conflict || x.SettledNetCommission.HasValue || x.SettledUserCommission.HasValue))
            preview.Issues.Add("Phân bổ có xung đột hoặc đã được chốt một phần; cần kiểm tra sổ đơn.");
        if (attrs.Any(x => x.Status == AffiliateAttributionStatus.Matched && (!x.UserId.HasValue || !x.TrackingId.HasValue)))
            preview.Issues.Add("Dòng đã ghép thiếu user hoặc tracking; cần kiểm tra người nhận.");
        var matched = rows.Where(x => x.Attribution.Status == AffiliateAttributionStatus.Matched && x.Attribution.UserId.HasValue && x.Attribution.TrackingId.HasValue).ToList();
        if (matched.Count == 0) preview.Issues.Add("Chưa xác định được người nhận. Ghép affiliate link hợp lệ trước khi duyệt.");
        if (matched.Any(x => x.Attribution.UserShareRate is < 0 or > 100)) preview.Issues.Add("Tỷ lệ chia hoàn tiền không hợp lệ.");
        var trackingIds = matched.Select(x => x.Attribution.TrackingId!.Value).Distinct().ToList();
        var trackings = trackingIds.Count == 0 ? new List<AffiliateTracking>() : await _trackings.GetListAsync(x => trackingIds.Contains(x.Id));
        foreach (var row in matched)
        {
            var tracking = trackings.SingleOrDefault(x => x.Id == row.Attribution.TrackingId);
            if (tracking is null || tracking.Platform != AffiliatePlatform.TikTok || tracking.UserId != row.Attribution.UserId ||
                tracking.GetProperty<string>(TikTokLinkAttribution.CreatorProperty) != preview.CreatorOpenId ||
                tracking.GetProperty<string>(TikTokLinkAttribution.SourceProperty) != TikTokLinkAttribution.Source)
                preview.Issues.Add("Người nhận/affiliate link không khớp Creator hoàn tiền của đơn.");
        }
        var weights = rows.ToDictionary(x => x.Attribution.Id, x => Math.Max(0, x.Attribution.AllocatedNetCommission));
        if (input.Source == TikTokSettlementAmountSource.ActualCommission)
        {
            var snapshot = ReadActualSnapshot(order, conversion.PurchaseTime, preview);
            if (snapshot is not null)
            {
                if (snapshot.Skus.Count != items.Count || items.Any(x => !snapshot.Skus.ContainsKey(x.ExternalItemId)))
                    preview.Issues.Add("Tập SKU khác dữ liệu TikTok đã lưu; đồng bộ lại đơn trước khi duyệt.");
                decimal total = 0, providerTax = 0, additionalTax = 0, taxBase = 0;
                var hasZeroTax = false;
                foreach (var item in items)
                {
                    if (!snapshot.Skus.TryGetValue(item.ExternalItemId, out var sku) || !TikTokCreatorOrderSnapshot.TryMoney(sku, "actual_commission", out var amount))
                    { preview.Issues.Add("TikTok thiếu hoa hồng thực nhận VND của SKU."); continue; }
                    if (Round(amount) != amount) preview.Issues.Add("Hoa hồng thực nhận vượt độ chính xác sổ đơn; cần kiểm tra số tiền.");
                    total += amount;
                    if (!TikTokCreatorOrderSnapshot.TryMoney(sku, "pit", out var withheld))
                    { preview.Issues.Add("TikTok thiếu số thuế khấu trừ pit hợp lệ của SKU; đồng bộ lại để xác định thuế trước khi duyệt."); continue; }
                    providerTax += withheld;
                    var tax = withheld == 0 ? Round(amount * 10m / 100m) : 0;
                    if (withheld == 0) { hasZeroTax = true; taxBase += amount; }
                    additionalTax += tax;
                    var itemRows = rows.Where(x => x.Attribution.OrderItemId == item.Id).ToList();
                    if (itemRows.Count != 1) { preview.Issues.Add("SKU TikTok có nhiều phân bổ không rõ nguồn; cần kiểm tra trước khi dùng actual_commission."); continue; }
                    weights[itemRows[0].Attribution.Id] = amount - tax;
                }
                preview.GrossCommission = total;
                preview.ProviderTaxAmount = providerTax;
                preview.AdditionalTaxBase = taxBase;
                preview.TaxPercent = hasZeroTax ? 10m : 0m;
                preview.TaxAmount = additionalTax;
                preview.NetCommission = total - additionalTax;
                if (hasZeroTax) preview.Warnings.Add("Theo cấu hình đối soát CatBack: SKU có thuế TikTok pit = 0đ được trừ bổ sung 10% trên actual_commission. SKU đã có thuế khấu trừ không bị trừ thêm.");
                if (total > 1_000_000_000_000m) preview.Issues.Add("Tổng hoa hồng vượt hạn mức đối soát; cần kiểm tra dữ liệu.");
                if (actual != total) preview.Issues.Add("Tổng actual_commission khác số tiền đã lưu; cần đồng bộ lại.");
            }
        }
        else
        {
            if (!input.GrossCommission.HasValue) preview.Issues.Add("Nhập hoa hồng trước thuế để lập phương án đối soát thủ công.");
            preview.GrossCommission = input.GrossCommission ?? 0;
            preview.TaxPercent = input.TaxPercent;
            preview.AdditionalTaxBase = preview.GrossCommission;
            preview.TaxAmount = Round(preview.GrossCommission * input.TaxPercent / 100m);
            preview.NetCommission = preview.GrossCommission - preview.TaxAmount;
            preview.Warnings.Add("Duyệt theo hoa hồng admin nhập, không phải xác nhận TikTok đã thanh toán.");
        }
        if (preview.ProviderStatus != "SETTLED") preview.Warnings.Add("TikTok chưa trả SETTLED. Admin vẫn được duyệt theo số tiền đã xem trước và lý do đã nhập.");
        if (items.Any(x => x.IsFraud) || attrs.Any(x => x.IsFraud)) preview.Warnings.Add("Đơn có dấu hiệu gian lận/tạm giữ từ sàn; cân nhắc trước khi duyệt thủ công.");
        var review = order.GetProperty<string>("TikTokOrderReview");
        if (!string.IsNullOrWhiteSpace(review)) preview.Warnings.Add("Ghi chú dữ liệu: " + review);
        if (rows.Count > matched.Count) preview.Warnings.Add("Có dòng chưa ghép người nhận. Phần hoa hồng đó được giữ lại, không cộng cho user và không tự ghép sau khi chốt.");
        if (preview.NetCommission > 0 && rows.Count > 1 && weights.Values.Sum() == 0)
            preview.Issues.Add("Nhiều dòng đều có trọng số hoa hồng bằng 0; chưa thể chia khoản nhập tay cho từng dòng.");
        if (preview.Issues.Count == 0)
        {
            var net = _calculator.AllocateAmount(preview.NetCommission, rows.Select(x => new AmountAllocationInput(x.Attribution.Id.ToString("N"), weights[x.Attribution.Id])), 4)
                .ToDictionary(x => x.Key, x => x.Amount);
            foreach (var row in rows) row.Net = net[row.Attribution.Id.ToString("N")];
            foreach (var group in matched.GroupBy(x => x.Attribution.UserId!.Value))
            {
                var userNet = group.Sum(x => x.Net);
                var target = Math.Min(decimal.Round(group.Sum(x => x.Net * x.Attribution.UserShareRate / 100m), 0, MidpointRounding.AwayFromZero), decimal.Floor(userNet));
                var allocations = _calculator.AllocateAmount(target, group.Select(x => new AmountAllocationInput(x.Attribution.Id.ToString("N"), x.Net * x.Attribution.UserShareRate / 100m)), 0)
                    .ToDictionary(x => x.Key, x => x.Amount);
                foreach (var row in group) row.UserAmount = allocations[row.Attribution.Id.ToString("N")];
            }
            preview.UserCommission = rows.Sum(x => x.UserAmount);
            preview.UnmatchedNetCommission = rows.Where(x => !matched.Contains(x)).Sum(x => x.Net);
        }
        await AddRecipientsAsync(preview, rows);
        preview.Issues = preview.Issues.Distinct().ToList();
        var planData = JsonSerializer.Serialize(new
        {
            order.Id, order.ConcurrencyStamp, status = order.Status, creator = preview.CreatorOpenId, registeredCreator,
            snapshot = order.GetProperty<string>("TikTokOrderSnapshot"), fingerprint = order.GetProperty<string>("TikTokOrderFingerprint"),
            providerStatus = preview.ProviderStatus, review = order.GetProperty<string>("TikTokOrderReview"),
            taxPolicy = "ZeroWithholdingTenPercentV1", preview.LastCheckedAt, input.Source, preview.GrossCommission,
            preview.ProviderTaxAmount, preview.AdditionalTaxBase, preview.TaxPercent, preview.TaxAmount, reason = input.Reason?.Trim(),
            rows = rows.Select(x => new { x.Attribution.Id, x.Attribution.AttributionValue, x.Attribution.UserId,
                x.Attribution.TrackingId, x.Attribution.Status, x.Attribution.UserShareRate, x.Attribution.AllocatedNetCommission,
                x.Net, x.UserAmount }),
            trackings = trackings.OrderBy(x => x.Id).Select(x => new { x.Id, x.UserId, x.ConcurrencyStamp }),
            preview.Recipients
        });
        preview.PlanHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(planData)));
        return new(order, preview, rows);
    }

    private async Task AddRecipientsAsync(TikTokSettlementPreview preview, List<Allocation> rows)
    {
        foreach (var group in rows.Where(x => x.Attribution.Status == AffiliateAttributionStatus.Matched && x.Attribution.UserId.HasValue).GroupBy(x => x.Attribution.UserId!.Value).OrderBy(x => x.Key))
        {
            var user = await _users.FindAsync(group.Key);
            if (user is null && !preview.AlreadySettled) preview.Issues.Add("Người nhận đã bị xóa; cần kiểm tra trước khi cộng ví.");
            preview.Recipients.Add(new(group.Key, user?.Email ?? user?.UserName, group.Sum(x => x.Net), group.Sum(x => x.UserAmount)));
        }
    }

    private static TikTokCreatorOrderSnapshot? ReadActualSnapshot(AffiliateOrder order, DateTime purchaseTime, TikTokSettlementPreview preview)
    {
        try
        {
            var raw = order.GetProperty<string>("TikTokOrderSnapshot");
            if (string.IsNullOrWhiteSpace(raw)) throw new JsonException();
            using var json = JsonDocument.Parse(raw);
            var from = new DateTimeOffset(DateTime.SpecifyKind(purchaseTime, DateTimeKind.Utc)).ToUnixTimeSeconds();
            var snapshot = TikTokCreatorOrderSnapshot.Merge(new[] { json.RootElement }, from, from + 1).Single();
            if (snapshot.Id != order.ExternalOrderId || snapshot.Status != "SETTLED" || preview.ProviderStatus != "SETTLED")
                throw new JsonException();
            return snapshot;
        }
        catch (Exception ex) when (ex is JsonException or UserFriendlyException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            preview.Issues.Add("Chưa có snapshot SETTLED hợp lệ để dùng actual_commission. Hãy đồng bộ lại hoặc chọn nhập hoa hồng trước thuế.");
            return null;
        }
    }

    private static void ValidateAmounts(TikTokSettlementInput input)
    {
        if (input.Source is not (TikTokSettlementAmountSource.ActualCommission or TikTokSettlementAmountSource.ManualGross))
            throw new UserFriendlyException("Chọn nguồn số tiền đối soát hợp lệ.");
        if (input.Source == TikTokSettlementAmountSource.ManualGross &&
            (input.GrossCommission is null or < 0 or > 1_000_000_000_000m || input.TaxPercent is < 0 or > 100 ||
             Round(input.GrossCommission.Value) != input.GrossCommission || Round(input.TaxPercent) != input.TaxPercent))
            throw new UserFriendlyException("Nhập hoa hồng không âm, tối đa 1.000 tỷ; thuế từ 0–100%, tối đa 4 chữ số thập phân.");
    }
    private static decimal Round(decimal amount) => decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
    private sealed class Allocation
    {
        public AffiliateOrderItemAttribution Attribution { get; }
        public decimal Net { get; set; }
        public decimal UserAmount { get; set; }
        public Allocation(AffiliateOrderItemAttribution attribution) => Attribution = attribution;
    }
    private sealed record Plan(AffiliateOrder Order, TikTokSettlementPreview Preview, List<Allocation> Rows);
}
