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

    public static string GetStatusLabel(AffiliateOrderStatus status) => status switch
    {
        AffiliateOrderStatus.Unpaid => "Chờ thanh toán",
        AffiliateOrderStatus.Pending => "Chờ xử lý",
        AffiliateOrderStatus.Completed => "Chờ Shopee thanh toán",
        AffiliateOrderStatus.Settled => "Đã đối soát",
        AffiliateOrderStatus.Cancelled => "Đã hủy",
        AffiliateOrderStatus.Refunded => "Đã hoàn tiền",
        AffiliateOrderStatus.Rejected => "Không được ghi nhận",
        _ => status.ToString()
    };

    public static string GetStatusDescription(AffiliateOrderStatus status) => status switch
    {
        AffiliateOrderStatus.Completed => "Đơn đã hoàn thành và đang chờ Shopee đối soát thanh toán.",
        AffiliateOrderStatus.Settled => "Shopee đã đối soát và ghi nhận thanh toán hoa hồng.",
        AffiliateOrderStatus.Cancelled => "Đơn đã hủy nên không phát sinh hoàn tiền.",
        AffiliateOrderStatus.Refunded => "Đơn đã hoàn nên không phát sinh hoàn tiền.",
        AffiliateOrderStatus.Rejected => "Shopee không ghi nhận hoa hồng cho đơn này.",
        _ => "Shopee đang kiểm tra và đối soát đơn hàng."
    };

    public static string FormatMoney(decimal value) => $"{value:N0}đ";

    public static decimal DisplayCommission(AffiliateOrderDto order) =>
        order.Status == AffiliateOrderStatus.Settled ? order.PayableUserCommission : order.ExpectedUserCommission;

    public static string FormatDate(DateTime value) => value.ToLocalTime().ToString("HH:mm · dd/MM/yyyy");
}
