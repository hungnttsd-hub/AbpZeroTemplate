using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using WebHoanTien.Notifications;

namespace WebHoanTien.Web.Pages.Notifications;

[Authorize]
public class DetailModel : PageModel
{
    private readonly ICustomerNotificationAppService _notifications;

    public CustomerNotificationDetailDto Notification { get; private set; } = new();
    public string ActionLabel => GetActionLabel(Notification.ActionUrl);
    public string RelatedTitle => GetRelatedTitle(Notification.Category);

    public DetailModel(ICustomerNotificationAppService notifications)
    {
        _notifications = notifications;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Notification = await _notifications.GetAsync(id);
            return Page();
        }
        catch (BusinessException exception) when (
            exception.Code == WebHoanTienDomainErrorCodes.NotificationNotOwned)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _notifications.DeleteAsync(id);
            return new JsonResult(new { success = true, redirectUrl = "/Notifications" });
        }
        catch (BusinessException exception) when (
            exception.Code == WebHoanTienDomainErrorCodes.NotificationNotOwned)
        {
            return NotFound(new { success = false, error = "Không tìm thấy thông báo." });
        }
    }

    private static string GetActionLabel(string? actionUrl)
    {
        if (actionUrl?.StartsWith("/Orders", StringComparison.OrdinalIgnoreCase) == true) return "Xem đơn hàng";
        if (actionUrl?.StartsWith("/Wallet", StringComparison.OrdinalIgnoreCase) == true) return "Xem ví tiền";
        if (actionUrl?.StartsWith("/Account", StringComparison.OrdinalIgnoreCase) == true) return "Xem tài khoản";
        return "Xem chi tiết";
    }

    private static string GetRelatedTitle(CustomerNotificationCategory category) => category switch
    {
        CustomerNotificationCategory.Order => "Đơn hàng Shopee của bạn",
        CustomerNotificationCategory.Cashback => "Hoàn tiền đơn Shopee",
        CustomerNotificationCategory.Wallet => "Ví tiền CatsBack",
        CustomerNotificationCategory.Promotion => "Ưu đãi dành cho bạn",
        _ => "Nội dung liên quan"
    };
}
