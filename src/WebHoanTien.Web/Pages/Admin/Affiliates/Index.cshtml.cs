using System.ComponentModel.DataAnnotations;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebHoanTien.Admin;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace WebHoanTien.Web.Pages.Admin.Affiliates;

[Authorize(WebHoanTienPermissions.Admin.Default)]
public class IndexModel : PageModel
{
    private readonly IAdminAffiliateSettingsAppService _settings;
    private readonly IAdminShopeeReportImportAppService _imports;
    private readonly IAdminShopeeSettlementImportAppService _settlementImports;
    private readonly IAdminCommissionRuleAppService _commissionRules;
    private readonly IAdminUserAffiliateIdAppService _userAffiliateIds;
    public AffiliateConnectionStatusDto Connection { get; private set; } = new();
    public AffiliateCommissionRuleDto CurrentCommissionRule { get; private set; } = new();
    public PagedResultDto<AdminUserAffiliateIdDto> AffiliateIdOverrides { get; private set; } = new();
    public ListResultDto<AdminAffiliateUserOptionDto> AffiliateUsers { get; private set; } = new();
    [BindProperty] public IFormFile? Report { get; set; }
    [BindProperty] public IFormFile? SettlementReport { get; set; }
    [BindProperty, Range(0, 100)] public decimal UserShareRate { get; set; }
    [BindProperty(SupportsGet = true)] public string? AffiliateOverrideFilter { get; set; }

    public IndexModel(IAdminAffiliateSettingsAppService settings, IAdminShopeeReportImportAppService imports,
        IAdminShopeeSettlementImportAppService settlementImports, IAdminCommissionRuleAppService commissionRules,
        IAdminUserAffiliateIdAppService userAffiliateIds)
    {
        _settings = settings;
        _imports = imports;
        _settlementImports = settlementImports;
        _commissionRules = commissionRules;
        _userAffiliateIds = userAffiliateIds;
    }

    public Task OnGetAsync() => LoadAsync();

    public async Task<IActionResult> OnPostSetCommissionRateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(setRateFromCurrentRule: false);
            return Page();
        }

        var rule = await _commissionRules.SetCurrentRateAsync(new SetCurrentCommissionRateInput
        {
            UserShareRate = UserShareRate
        });
        TempData["CommissionMessage"] = $"Đã áp dụng tỷ lệ khách nhận {rule.UserShareRate:0.##}% từ bây giờ.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (Report is null || Report.Length == 0)
        {
            TempData["ImportMessage"] = "Chọn file báo cáo Shopee trước khi import.";
            return RedirectToPage();
        }

        await using var stream = Report.OpenReadStream();
        var result = await _imports.ImportAsync(stream, Report.FileName);
        TempData["ImportMessage"] = $"Đã xử lý {result.ImportedRowCount} dòng: thêm {result.InsertedCount}, cập nhật {result.UpdatedCount}, chưa ghép {result.UnmatchedCount}, lỗi {result.ErrorCount}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportSettlementAsync()
    {
        if (SettlementReport is null || SettlementReport.Length == 0)
        {
            TempData["SettlementMessage"] = "Chọn bảng kê Shopee đã thanh toán trước khi import.";
            return RedirectToPage();
        }

        await using var stream = SettlementReport.OpenReadStream();
        var result = await _settlementImports.ImportAsync(stream, SettlementReport.FileName);
        TempData["SettlementMessage"] = result.IsDuplicate
            ? "File hoặc bảng kê này đã được import trước đó."
            : $"Đã tạo batch chờ duyệt: {result.PendingApprovalCount} dòng có thể duyệt, {result.UnmatchedCount} không khớp, {result.AlreadySettledCount} đã ghi nhận, {result.ErrorCount} cần kiểm tra.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetAffiliateOverrideAsync(string userEmail, string affiliateId,
        string? adminNote)
    {
        try
        {
            var result = await _userAffiliateIds.SetAsync(new SetUserAffiliateIdInput
            {
                UserEmail = userEmail,
                Platform = AffiliatePlatform.Shopee,
                AffiliateId = affiliateId,
                AdminNote = adminNote
            });
            return new JsonResult(new { success = true, item = result });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
        catch (BusinessException exception) when (exception.Code == WebHoanTienDomainErrorCodes.AffiliateUserNotFound)
        {
            return BadRequest(new { success = false, error = "Không tìm thấy tài khoản đang hoạt động với email này." });
        }
    }

    public async Task<IActionResult> OnPostRemoveAffiliateOverrideAsync(Guid userId)
    {
        try
        {
            await _userAffiliateIds.RemoveAsync(userId, AffiliatePlatform.Shopee);
            return new JsonResult(new { success = true, userId });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
    }

    private async Task LoadAsync(bool setRateFromCurrentRule = true)
    {
        Connection = await _settings.GetAsync();
        CurrentCommissionRule = await _commissionRules.GetCurrentAsync();
        AffiliateIdOverrides = await _userAffiliateIds.GetListAsync(new AdminUserAffiliateIdListInput
        {
            Filter = AffiliateOverrideFilter,
            MaxResultCount = 100
        });
        AffiliateUsers = await _userAffiliateIds.GetUserOptionsAsync();
        if (setRateFromCurrentRule) UserShareRate = CurrentCommissionRule.UserShareRate;
    }
}
