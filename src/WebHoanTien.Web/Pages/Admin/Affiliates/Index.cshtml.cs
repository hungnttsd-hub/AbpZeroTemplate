using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebHoanTien.Admin;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages.Admin.Affiliates;

[Authorize(WebHoanTienPermissions.Admin.Default)]
public class IndexModel : PageModel
{
    private readonly IAdminAffiliateSettingsAppService _settings;
    private readonly IAdminShopeeReportImportAppService _imports;
    private readonly IAdminShopeeSettlementImportAppService _settlementImports;
    private readonly IAdminCommissionRuleAppService _commissionRules;
    public AffiliateConnectionStatusDto Connection { get; private set; } = new();
    public AffiliateCommissionRuleDto CurrentCommissionRule { get; private set; } = new();
    [BindProperty] public IFormFile? Report { get; set; }
    [BindProperty] public IFormFile? SettlementReport { get; set; }
    [BindProperty, Range(0, 100)] public decimal UserShareRate { get; set; }

    public IndexModel(IAdminAffiliateSettingsAppService settings, IAdminShopeeReportImportAppService imports,
        IAdminShopeeSettlementImportAppService settlementImports, IAdminCommissionRuleAppService commissionRules)
    { _settings = settings; _imports = imports; _settlementImports = settlementImports; _commissionRules = commissionRules; }

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
        TempData["SettlementMessage"] = $"Đã đọc {result.ImportedRowCount} dòng: ghi nhận {result.SettledCount}, đã có {result.AlreadySettledCount}, không khớp {result.UnmatchedCount}, lỗi {result.ErrorCount}.";
        return RedirectToPage();
    }

    private async Task LoadAsync(bool setRateFromCurrentRule = true)
    {
        Connection = await _settings.GetAsync();
        CurrentCommissionRule = await _commissionRules.GetCurrentAsync();
        if (setRateFromCurrentRule) UserShareRate = CurrentCommissionRule.UserShareRate;
    }
}
