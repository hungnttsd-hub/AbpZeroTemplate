using System;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages.Admin.Payouts;

public static class PayoutPageUi
{
    public static string FormatMoney(decimal value) => $"{value:N0}đ";
    public static string FormatDate(DateTime value) => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public static string StatusLabel(WithdrawalRequestStatus status) => status switch
    {
        WithdrawalRequestStatus.Pending => "Chờ xử lý",
        WithdrawalRequestStatus.Paid => "Đã thanh toán",
        WithdrawalRequestStatus.Rejected => "Từ chối",
        _ => "Đã hủy"
    };

    public static string StatusClass(WithdrawalRequestStatus status) => status switch
    {
        WithdrawalRequestStatus.Pending => "pending",
        WithdrawalRequestStatus.Paid => "paid",
        WithdrawalRequestStatus.Rejected => "rejected",
        _ => "cancelled"
    };
}
