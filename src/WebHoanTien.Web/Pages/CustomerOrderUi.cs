using System;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages;

public static class CustomerOrderUi
{
    public static bool IsPending(AffiliateOrderStatus status) =>
        status is AffiliateOrderStatus.Unpaid or AffiliateOrderStatus.Pending or AffiliateOrderStatus.Completed;

    public static bool IsConfirmed(AffiliateOrderStatus status) => status == AffiliateOrderStatus.Settled;

    public static bool IsCancelled(AffiliateOrderStatus status) =>
        status is AffiliateOrderStatus.Cancelled or AffiliateOrderStatus.Refunded or AffiliateOrderStatus.Rejected;

    public static string GetStatusClass(AffiliateOrderStatus status) =>
        IsPending(status) ? "pending" : IsConfirmed(status) ? "confirmed" : "cancelled";

    public static string PlatformName(AffiliatePlatform platform) => platform == AffiliatePlatform.TikTok ? "TikTok Shop" : "Shopee";

    public static string GetStatusLabel(AffiliateOrderStatus status, AffiliatePlatform platform = AffiliatePlatform.Shopee) => status switch
    {
        AffiliateOrderStatus.Unpaid => "Chờ thanh toán",
        AffiliateOrderStatus.Pending => "Chờ xử lý",
        AffiliateOrderStatus.Completed => platform == AffiliatePlatform.TikTok ? "Chờ CatBack đối soát" : "Chờ Shopee thanh toán",
        AffiliateOrderStatus.Settled => "Đã đối soát",
        AffiliateOrderStatus.Cancelled => "Đã hủy",
        AffiliateOrderStatus.Refunded => "Đã hoàn tiền",
        AffiliateOrderStatus.Rejected => "Không được ghi nhận",
        _ => status.ToString()
    };

    public static string GetStatusDescription(AffiliateOrderStatus status, AffiliatePlatform platform = AffiliatePlatform.Shopee) => status switch
    {
        AffiliateOrderStatus.Completed => platform == AffiliatePlatform.TikTok
            ? "TikTok Shop đã xác nhận hoa hồng; đang chờ CatBack duyệt đối soát."
            : $"Đơn đã hoàn thành và đang chờ {PlatformName(platform)} đối soát hoa hồng.",
        AffiliateOrderStatus.Settled => platform == AffiliatePlatform.TikTok
            ? "CatBack đã duyệt đối soát và ghi nhận hoàn tiền cho đơn TikTok Shop."
            : $"{PlatformName(platform)} đã ghi nhận hoa hồng được đối soát.",
        AffiliateOrderStatus.Cancelled => "Đơn đã hủy nên không phát sinh hoàn tiền.",
        AffiliateOrderStatus.Refunded => "Đơn đã hoàn nên không phát sinh hoàn tiền.",
        AffiliateOrderStatus.Rejected => $"{PlatformName(platform)} không ghi nhận hoa hồng cho đơn này.",
        _ => $"{PlatformName(platform)} đang kiểm tra và đối soát đơn hàng."
    };

    public static string FormatMoney(decimal value) => $"{value:N0}đ";

    public static decimal DisplayCommission(AffiliateOrderDto order) =>
        IsCancelled(order.Status) ? 0m :
        order.Status == AffiliateOrderStatus.Settled ? order.PayableUserCommission : order.ExpectedUserCommission;

    public static string FormatDate(DateTime value) => value.ToLocalTime().ToString("HH:mm · dd/MM/yyyy");
}
