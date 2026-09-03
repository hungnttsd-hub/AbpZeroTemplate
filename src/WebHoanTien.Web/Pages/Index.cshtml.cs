using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Users;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages;

[AllowAnonymous]
public class IndexModel : PageModel
{
    private readonly IAffiliateLinkAppService _links;
    private readonly ICustomerWalletAppService _wallet;
    private readonly ICurrentUser _currentUser;
    private readonly ITimeLimitedDataProtector _protector;
    private readonly IDistributedCache _cache;

    [BindProperty] public string LinkUrl { get; set; } = string.Empty;
    [BindProperty] public AffiliateLinkTargetType LinkTargetType { get; set; } = AffiliateLinkTargetType.Product;
    [BindProperty(SupportsGet = true)] public bool ShowHidden { get; set; }
    public string? Error { get; set; }
    public string? SuccessMessage { get; set; }
    public Guid? CreatedLinkId { get; set; }
    public AffiliateTrackingDto? CreatedLink { get; private set; }
    public PagedResultDto<AffiliateTrackingDto> RecentLinks { get; private set; } = new();
    public CustomerWalletOverviewDto Wallet { get; private set; } = new();

    public IndexModel(IAffiliateLinkAppService links, ICustomerWalletAppService wallet, ICurrentUser currentUser,
        IDataProtectionProvider dataProtection, IDistributedCache cache)
    {
        _links = links; _wallet = wallet; _currentUser = currentUser; _cache = cache;
        _protector = dataProtection.CreateProtector("WebHoanTien.PendingAffiliate.v1").ToTimeLimitedDataProtector();
    }

    public async Task OnGetAsync()
    {
        Error = TempData["AffiliateLinkError"] as string;
        SuccessMessage = TempData["AffiliateLinkSuccess"] as string;
        LinkUrl = TempData["AffiliateLinkUrl"] as string ?? string.Empty;
        if (Enum.TryParse<AffiliateLinkTargetType>(TempData["AffiliateLinkTargetType"] as string,
                ignoreCase: true, out var targetType) &&
            targetType is AffiliateLinkTargetType.Product or AffiliateLinkTargetType.Shop)
        {
            LinkTargetType = targetType;
        }
        if (Guid.TryParse(TempData["AffiliateCreatedLinkId"] as string, out var createdLinkId))
        {
            CreatedLinkId = createdLinkId;
        }
        await LoadDashboardAsync();
    }

    public async Task<IActionResult> OnPostPrepareAsync()
    {
        var validation = await _links.ValidateAsync(new ValidateAffiliateUrlInput
        {
            Url = LinkUrl,
            TargetType = LinkTargetType
        });
        if (!validation.IsValid)
        {
            Error = validation.Error;
            if (IsAjaxRequest()) return BadRequest(new { success = false, error = Error });
            await LoadDashboardAsync();
            return Page();
        }
        if (_currentUser.IsAuthenticated)
        {
            try
            {
                var result = await _links.CreateAsync(new CreateAffiliateLinkInput
                {
                    Url = LinkUrl,
                    TargetType = LinkTargetType
                });
                CreatedLinkId = result.Id;
                CreatedLink = result;
                ModelState.Clear();
                SuccessMessage = SuccessMessageFor(result);
                if (IsAjaxRequest())
                {
                    return new JsonResult(new
                    {
                        success = true,
                        message = SuccessMessage,
                        link = new
                        {
                            id = result.Id,
                            targetType = result.TargetType.ToString(),
                            productName = result.ProductName,
                            shopId = result.ShopId,
                            imageUrl = result.ImageUrl,
                            estimatedCommissionLabel = result.EstimatedCommission.HasValue
                                ? result.EstimatedCommission.Value.ToString("N0") + "₫"
                                : null,
                            redirectUrl = result.RedirectUrl,
                            clickCount = result.ClickCount,
                            isExisting = result.IsExisting,
                            wasRestored = result.WasRestored
                        }
                    });
                }
                await LoadDashboardAsync();
                return Page();
            }
            catch (UserFriendlyException exception)
            {
                Error = exception.Message;
                if (IsAjaxRequest()) return BadRequest(new { success = false, error = Error });
                await LoadDashboardAsync();
                return Page();
            }
            catch (BusinessException)
            {
                Error = "Không thể tạo link mua hàng lúc này. Vui lòng thử lại sau.";
                if (IsAjaxRequest()) return BadRequest(new { success = false, error = Error });
                await LoadDashboardAsync();
                return Page();
            }
        }

        var nonce = Guid.NewGuid().ToString("N");
        var payload = JsonSerializer.Serialize(new PendingAffiliateAction
        {
            Url = LinkUrl,
            TargetType = LinkTargetType,
            Nonce = nonce
        });
        Response.Cookies.Append("wht.pending", _protector.Protect(payload, TimeSpan.FromMinutes(20)), new CookieOptions
        {
            HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromMinutes(20), IsEssential = true
        });
        await _cache.SetStringAsync("affiliate:pending:" + nonce, "1", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20) });
        const string loginUrl = "/Account/Login?returnUrl=%2FPendingAffiliate";
        return IsAjaxRequest()
            ? new JsonResult(new { success = true, requiresLogin = true, redirectUrl = loginUrl })
            : Redirect(loginUrl);
    }

    public async Task<IActionResult> OnPostSetHiddenAsync(Guid linkId, string visibilityAction, bool showHidden)
    {
        if (!_currentUser.IsAuthenticated) return Challenge();

        var hidden = string.Equals(visibilityAction, "hide", StringComparison.OrdinalIgnoreCase);
        if (!hidden && !string.Equals(visibilityAction, "show", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        await _links.SetHiddenAsync(new SetAffiliateTrackingHiddenInput { Id = linkId, IsHidden = hidden });
        var message = hidden
            ? "Đã ẩn link khỏi danh sách. Dữ liệu tracking vẫn được giữ nguyên."
            : "Đã đưa link trở lại danh sách.";

        if (IsAjaxRequest())
        {
            return new JsonResult(new { success = true, isHidden = hidden, message });
        }

        TempData["AffiliateLinkSuccess"] = message;
        return LocalRedirect(showHidden ? "/?showHidden=true#your-links-heading" : "/#your-links-heading");
    }

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    private async Task LoadDashboardAsync()
    {
        if (!_currentUser.IsAuthenticated) return;

        RecentLinks = await _links.GetListAsync(new AffiliateTrackingListInput
        {
            MaxResultCount = 5,
            IncludeHidden = ShowHidden
        });
        if (CreatedLinkId.HasValue)
        {
            CreatedLink = RecentLinks.Items.FirstOrDefault(x => x.Id == CreatedLinkId.Value) ??
                          await _links.GetAsync(CreatedLinkId.Value);
        }
        Wallet = await _wallet.GetOverviewAsync();
    }

    internal static string SuccessMessageFor(AffiliateTrackingDto result)
    {
        var targetLabel = result.TargetType == AffiliateLinkTargetType.Shop ? "Link cửa hàng" : "Link";
        return result.WasRestored
            ? $"{targetLabel} đã được đưa trở lại danh sách của bạn."
            : result.IsExisting
                ? $"{targetLabel} này đã có trong danh sách của bạn."
                : result.TargetType == AffiliateLinkTargetType.Shop
                    ? "Link cửa hàng đã sẵn sàng."
                    : "Link mua hàng đã được thêm vào danh sách của bạn.";
    }

    public sealed class PendingAffiliateAction
    {
        public string Url { get; set; } = string.Empty;
        public AffiliateLinkTargetType TargetType { get; set; } = AffiliateLinkTargetType.Product;
        public string Nonce { get; set; } = string.Empty;
    }
}
