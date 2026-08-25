using System;
using Volo.Abp;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages.Wallet;

public static class WalletPageUi
{
    public static string FormatMoney(decimal value) => $"{value:N0}đ";

    public static string FormatDate(DateTime value) => value.ToLocalTime().ToString("HH:mm · dd/MM/yyyy");

    public static string MaskAccount(string value) => value.Length <= 4
        ? value
        : new string('*', Math.Min(4, value.Length - 4)) + " " + value[^4..];

    public static string WithdrawalStatusLabel(WithdrawalRequestStatus status) => status switch
    {
        WithdrawalRequestStatus.Pending => "Đang xử lý",
        WithdrawalRequestStatus.Paid => "Đã thanh toán",
        WithdrawalRequestStatus.Rejected => "Từ chối",
        _ => "Đã hủy"
    };

    public static string WithdrawalStatusClass(WithdrawalRequestStatus status) => status switch
    {
        WithdrawalRequestStatus.Pending => "pending",
        WithdrawalRequestStatus.Paid => "confirmed",
        _ => "cancelled"
    };

    public static string ErrorMessage(BusinessException exception) => exception.Code switch
    {
        WebHoanTienDomainErrorCodes.PayoutAccountRequired => "Vui lòng lưu tài khoản nhận tiền trước khi yêu cầu rút.",
        WebHoanTienDomainErrorCodes.WithdrawalBelowMinimum => "Số tiền rút tối thiểu là 10.000đ.",
        WebHoanTienDomainErrorCodes.WithdrawalInsufficientBalance => "Số dư khả dụng không đủ cho yêu cầu này.",
        WebHoanTienDomainErrorCodes.WithdrawalPendingExists => "Bạn đang có một yêu cầu rút tiền chờ xử lý.",
        WebHoanTienDomainErrorCodes.WithdrawalInvalidState => "Yêu cầu này đã được xử lý và không thể thay đổi.",
        WebHoanTienDomainErrorCodes.WithdrawalNotOwned => "Bạn không có quyền truy cập yêu cầu này.",
        _ => "Không thể xử lý yêu cầu lúc này. Vui lòng thử lại."
    };
}
