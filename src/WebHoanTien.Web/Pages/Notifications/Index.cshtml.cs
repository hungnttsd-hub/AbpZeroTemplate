using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using WebHoanTien.Notifications;

namespace WebHoanTien.Web.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private const int PageSize = 20;
    private readonly ICustomerNotificationAppService _notifications;

    [BindProperty(SupportsGet = true)] public CustomerNotificationCategory? Category { get; set; }
    [BindProperty(SupportsGet = true)] public bool UnreadOnly { get; set; }
    public CustomerNotificationPageDto Data { get; private set; } = new();

    public IndexModel(ICustomerNotificationAppService notifications)
    {
        _notifications = notifications;
    }

    public async Task OnGetAsync()
    {
        Data = await _notifications.GetListAsync(new GetCustomerNotificationsInput
        {
            Category = Category,
            UnreadOnly = UnreadOnly,
            MaxResultCount = PageSize
        });
    }

    public async Task<IActionResult> OnGetListAsync(CustomerNotificationCategory? category, bool unreadOnly,
        int skipCount = 0)
    {
        var data = await _notifications.GetListAsync(new GetCustomerNotificationsInput
        {
            Category = category,
            UnreadOnly = unreadOnly,
            SkipCount = Math.Max(0, skipCount),
            MaxResultCount = PageSize
        });
        return new JsonResult(new
        {
            data.TotalCount,
            data.UnreadCount,
            items = data.Items.Select(x => new
            {
                x.Id,
                category = (int)x.Category,
                kind = (int)x.Kind,
                x.Title,
                x.Message,
                x.CreationTime,
                x.IsRead
            })
        });
    }

    public async Task<IActionResult> OnPostReadAsync(Guid notificationId)
    {
        try
        {
            var result = await _notifications.MarkAsReadAsync(notificationId);
            return new JsonResult(new { success = true, result.ActionUrl, result.UnreadCount });
        }
        catch (BusinessException)
        {
            return BadRequest(new { success = false, error = "Bạn không thể mở thông báo này." });
        }
    }

    public async Task<IActionResult> OnPostReadAllAsync()
    {
        await _notifications.MarkAllAsReadAsync();
        return new JsonResult(new { success = true, unreadCount = 0 });
    }
}
