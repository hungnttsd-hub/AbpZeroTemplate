using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Notifications;

public class CustomerNotificationManager : DomainService
{
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IRepository<CustomerNotification, Guid> _notifications;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;

    public CustomerNotificationManager(IRepository<CustomerNotification, Guid> notifications,
        IGuidGenerator guidGenerator, IClock clock)
    {
        _notifications = notifications;
        _guidGenerator = guidGenerator;
        _clock = clock;
    }

    public async Task<bool> CreateOnceAsync(Guid userId, CustomerNotificationCategory category,
        CustomerNotificationKind kind, string title, string message, string? actionUrl, string eventKey)
    {
        var normalizedActionUrl = NormalizeActionUrl(actionUrl);
        var normalizedEventKey = eventKey.Trim();
        if (await _notifications.AnyAsync(x => x.UserId == userId && x.EventKey == normalizedEventKey))
            return false;

        await _notifications.InsertAsync(new CustomerNotification(_guidGenerator.Create(), userId, category, kind,
            title, message, normalizedActionUrl, normalizedEventKey));
        return true;
    }

    public Task<bool> NotifyOrderStatusAsync(Guid userId, AffiliateOrder order)
    {
        var orderLabel = Shorten(order.ExternalOrderId, 40);
        var actionUrl = $"/Orders/{order.Id}";
        return order.Status switch
        {
            AffiliateOrderStatus.Unpaid or AffiliateOrderStatus.Pending => CreateOnceAsync(userId,
                CustomerNotificationCategory.Cashback, CustomerNotificationKind.CashbackPending,
                "Hoa hồng sắp ghi nhận",
                $"{FormatMoney(order.UserCommissionSnapshot)} từ đơn Shopee {orderLabel} đang chờ đối soát.",
                actionUrl, $"order:{order.Id:N}:pending"),
            AffiliateOrderStatus.Completed => CreateOnceAsync(userId, CustomerNotificationCategory.Order,
                CustomerNotificationKind.OrderReconciled, "Đơn hàng đã đối soát",
                $"Đơn Shopee {orderLabel} đã hoàn tất đối soát và đang chờ Shopee thanh toán hoa hồng.",
                actionUrl, $"order:{order.Id:N}:completed"),
            AffiliateOrderStatus.Settled => CreateOnceAsync(userId, CustomerNotificationCategory.Cashback,
                CustomerNotificationKind.CashbackRecorded, "Hoàn tiền đã ghi nhận",
                $"{FormatMoney(order.PayableUserCommission)} từ đơn Shopee {orderLabel} đã được cộng vào ví.",
                "/Wallet", $"order:{order.Id:N}:settled"),
            AffiliateOrderStatus.Cancelled => CreateOnceAsync(userId, CustomerNotificationCategory.Order,
                CustomerNotificationKind.OrderCancelled, "Đơn hàng đã hủy",
                $"Đơn Shopee {orderLabel} đã bị hủy và không phát sinh hoàn tiền.", actionUrl,
                $"order:{order.Id:N}:cancelled"),
            AffiliateOrderStatus.Refunded => CreateOnceAsync(userId, CustomerNotificationCategory.Order,
                CustomerNotificationKind.OrderRefunded, "Đơn hàng đã hoàn trả",
                $"Đơn Shopee {orderLabel} đã hoàn trả và không còn đủ điều kiện hoàn tiền.", actionUrl,
                $"order:{order.Id:N}:refunded"),
            AffiliateOrderStatus.Rejected => CreateOnceAsync(userId, CustomerNotificationCategory.Order,
                CustomerNotificationKind.OrderRejected, "Đơn hàng không được ghi nhận",
                $"Đơn Shopee {orderLabel} đã bị từ chối ghi nhận hoa hồng.", actionUrl,
                $"order:{order.Id:N}:rejected"),
            _ => Task.FromResult(false)
        };
    }

    public async Task<int> NotifySettledOrdersAsync(
        IEnumerable<(Guid UserId, AffiliateOrder Order)> settledOrders)
    {
        var rows = settledOrders.GroupBy(value => value.Order.Id)
            .Select(group => group.First()).ToList();
        if (rows.Count == 0) return 0;

        var existingKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var chunk in rows.Select(value => $"order:{value.Order.Id:N}:settled").Chunk(500))
        {
            var chunkIds = chunk.ToList();
            var notifications = await _notifications.GetListAsync(notification =>
                chunkIds.Contains(notification.EventKey));
            foreach (var notification in notifications) existingKeys.Add(notification.EventKey);
        }

        var additions = rows.Select(value => new
            {
                value.UserId,
                value.Order,
                EventKey = $"order:{value.Order.Id:N}:settled"
            })
            .Where(value => !existingKeys.Contains(value.EventKey))
            .Select(value => new CustomerNotification(_guidGenerator.Create(), value.UserId,
                CustomerNotificationCategory.Cashback, CustomerNotificationKind.CashbackRecorded,
                "Hoàn tiền đã ghi nhận",
                $"{FormatMoney(value.Order.PayableUserCommission)} từ đơn Shopee {Shorten(value.Order.ExternalOrderId, 40)} đã được cộng vào ví.",
                "/Wallet", value.EventKey))
            .ToList();
        if (additions.Count > 0) await _notifications.InsertManyAsync(additions);
        return additions.Count;
    }

    public Task<bool> NotifyWithdrawalStatusAsync(WithdrawalRequest request)
    {
        var actionUrl = "/Wallet/History";
        return request.Status switch
        {
            WithdrawalRequestStatus.Pending => CreateOnceAsync(request.UserId,
                CustomerNotificationCategory.Wallet, CustomerNotificationKind.WithdrawalPending,
                "Tiền rút đang xử lý",
                $"Yêu cầu {request.RequestCode} trị giá {FormatMoney(request.NetAmount)} đang được xử lý.",
                actionUrl, $"withdrawal:{request.Id:N}:pending"),
            WithdrawalRequestStatus.Paid => CreateOnceAsync(request.UserId,
                CustomerNotificationCategory.Wallet, CustomerNotificationKind.WithdrawalPaid,
                "Tiền rút đã thanh toán",
                $"Yêu cầu {request.RequestCode} trị giá {FormatMoney(request.NetAmount)} đã được chuyển khoản.",
                actionUrl, $"withdrawal:{request.Id:N}:paid"),
            WithdrawalRequestStatus.Rejected => CreateOnceAsync(request.UserId,
                CustomerNotificationCategory.Wallet, CustomerNotificationKind.WithdrawalRejected,
                "Yêu cầu rút tiền bị từ chối",
                $"Yêu cầu {request.RequestCode} bị từ chối: {Shorten(request.RejectionReason, 260)}",
                actionUrl, $"withdrawal:{request.Id:N}:rejected"),
            WithdrawalRequestStatus.Cancelled => CreateOnceAsync(request.UserId,
                CustomerNotificationCategory.Wallet, CustomerNotificationKind.WithdrawalCancelled,
                "Yêu cầu rút tiền đã hủy",
                $"Yêu cầu {request.RequestCode} trị giá {FormatMoney(request.NetAmount)} đã được hủy.",
                actionUrl, $"withdrawal:{request.Id:N}:cancelled"),
            _ => Task.FromResult(false)
        };
    }

    public Task<bool> NotifyPayoutAccountUpdatedAsync(Guid userId, Guid payoutAccountId, string eventVersion)
        => CreateOnceAsync(userId, CustomerNotificationCategory.Wallet,
            CustomerNotificationKind.PayoutAccountUpdated, "Tài khoản ngân hàng đã cập nhật",
            "Thông tin tài khoản nhận tiền của bạn đã được cập nhật thành công.", "/Account/Profile",
            $"payout-account:{payoutAccountId:N}:{eventVersion}");

    public async Task CreatePromotionForUsersAsync(IEnumerable<Guid> userIds, Guid campaignId, string title,
        string message, string? actionUrl)
    {
        var normalizedActionUrl = NormalizeActionUrl(actionUrl);
        var recipients = userIds.Distinct().ToList();
        const int batchSize = 500;
        for (var offset = 0; offset < recipients.Count; offset += batchSize)
        {
            var batch = recipients.Skip(offset).Take(batchSize)
                .Select(userId => new CustomerNotification(_guidGenerator.Create(), userId,
                    CustomerNotificationCategory.Promotion, CustomerNotificationKind.Promotion,
                    title, message, normalizedActionUrl, $"promotion:{campaignId:N}"))
                .ToList();
            await _notifications.InsertManyAsync(batch);
        }
    }

    public static string? NormalizeActionUrl(string? actionUrl)
    {
        if (string.IsNullOrWhiteSpace(actionUrl)) return null;
        var normalized = actionUrl.Trim();
        if (normalized.Length > WebHoanTienConsts.NotificationActionUrlMaxLength ||
            !normalized.StartsWith("/", StringComparison.Ordinal) ||
            normalized.StartsWith("//", StringComparison.Ordinal) || normalized.Contains('\\') ||
            !Uri.TryCreate(normalized, UriKind.Relative, out _))
            throw new BusinessException(WebHoanTienDomainErrorCodes.NotificationInvalidActionUrl);
        return normalized;
    }

    private static string FormatMoney(decimal value) => $"{Math.Max(0m, value).ToString("N0", VietnameseCulture)}đ";

    private static string Shorten(string? value, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "không xác định" : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "…";
    }
}
