using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages;

[Authorize]
public class LinksModel : PageModel
{
    public const int PageSize = 10;

    private readonly IAffiliateLinkAppService _links;

    [BindProperty]
    public string LinkUrl { get; set; } = string.Empty;

    public PagedResultDto<AffiliateTrackingDto> Links { get; private set; } = new();
    public bool HasMore => Links.Items.Count < Links.TotalCount;

    public LinksModel(IAffiliateLinkAppService links) => _links = links;

    public async Task OnGetAsync() => Links = await GetPageAsync(0);

    public async Task<IActionResult> OnGetMoreAsync(int skip = 0)
    {
        skip = Math.Max(0, skip);
        var result = await GetPageAsync(skip);
        Links = result;
        ViewData.Model = this;
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Has-More"] = (skip + result.Items.Count < result.TotalCount).ToString().ToLowerInvariant();
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();

        return new PartialViewResult
        {
            ViewName = "/Pages/Shared/_AffiliateLinkCards.cshtml",
            ViewData = ViewData
        };
    }

    public async Task<IActionResult> OnPostPrepareAsync()
    {
        var validation = await _links.ValidateAsync(new ValidateAffiliateUrlInput
        {
            Url = LinkUrl,
            TargetType = AffiliateLinkTargetType.Auto
        });
        if (!validation.IsValid)
        {
            return IsAjaxRequest()
                ? BadRequest(new { success = false, error = validation.Error })
                : RedirectToPage();
        }

        try
        {
            var result = await _links.CreateAsync(new CreateAffiliateLinkInput
            {
                Url = LinkUrl,
                TargetType = AffiliateLinkTargetType.Auto
            });
            var message = IndexModel.SuccessMessageFor(result);
            if (!IsAjaxRequest()) return RedirectToPage();

            return new JsonResult(new
            {
                success = true,
                message,
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
        catch (UserFriendlyException exception)
        {
            return IsAjaxRequest()
                ? BadRequest(new { success = false, error = exception.Message })
                : RedirectToPage();
        }
        catch (BusinessException)
        {
            const string error = "Không thể tạo link mua hàng lúc này. Vui lòng thử lại sau.";
            return IsAjaxRequest()
                ? BadRequest(new { success = false, error })
                : RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostSetHiddenAsync(Guid linkId, string visibilityAction)
    {
        if (!string.Equals(visibilityAction, "hide", StringComparison.OrdinalIgnoreCase)) return BadRequest();

        await _links.SetHiddenAsync(new SetAffiliateTrackingHiddenInput { Id = linkId, IsHidden = true });
        if (IsAjaxRequest())
        {
            return new JsonResult(new
            {
                success = true,
                isHidden = true,
                message = "Đã ẩn link khỏi danh sách. Dữ liệu tracking vẫn được giữ nguyên."
            });
        }

        return RedirectToPage();
    }

    private Task<PagedResultDto<AffiliateTrackingDto>> GetPageAsync(int skip) =>
        _links.GetListAsync(new AffiliateTrackingListInput
        {
            SkipCount = skip,
            MaxResultCount = PageSize
        });

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
