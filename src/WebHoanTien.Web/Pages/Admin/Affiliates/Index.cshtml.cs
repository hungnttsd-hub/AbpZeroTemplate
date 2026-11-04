using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebHoanTien.Admin;
using WebHoanTien.Affiliates;
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
    {
        _settings = settings;
        _imports = imports;
        _settlementImports = settlementImports;
        _commissionRules = commissionRules;
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
        var errorDetails = result.Errors.Count == 0
            ? string.Empty
            : $" Chi tiết: {string.Join(" | ", result.Errors.Take(3))}";
        TempData["ImportMessage"] = $"Đã xử lý {result.ImportedRowCount} dòng: thêm {result.InsertedCount}, cập nhật {result.UpdatedCount}, " +
            $"bỏ qua đơn đã duyệt {result.SkippedSettledCount}, " +
            $"item đã ghép {result.MatchedItemCount}, item chưa ghép {result.UnmatchedItemCount}, " +
            $"đơn nhiều link {result.MultiTrackingOrderCount}, lỗi {result.ErrorCount}.{errorDetails}";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportSettlementAsync()
    {
        if (SettlementReport is null || SettlementReport.Length == 0)
        {
            TempData["SettlementMessage"] = "Chọn file tổng hợp bảng kê Shopee trước khi import.";
            return RedirectToPage();
        }

        await using var stream = SettlementReport.OpenReadStream();
        var result = await _settlementImports.ImportAsync(stream, SettlementReport.FileName);
        var duplicateNote = result.IsDuplicate ? " Không có dữ liệu mới cần cập nhật." : string.Empty;
        TempData["SettlementMessage"] =
            $"Đã xử lý {result.ImportedRowCount} dòng thuộc {result.ValidationCount} bảng kê: " +
            $"{result.PendingApprovalCount} dòng có thể duyệt, {result.ApprovedCount} đã duyệt (bỏ qua), " +
            $"{result.WaitingPaymentCount} đang chờ Shopee thanh toán, {result.UpdatedValidationCount} bảng kê được cập nhật, " +
            $"{result.UnmatchedCount} không khớp, {result.AlreadySettledCount} đã ghi nhận, {result.ErrorCount} cần kiểm tra." +
            duplicateNote;
        return RedirectToPage();
    }

    private async Task LoadAsync(bool setRateFromCurrentRule = true)
    {
        Connection = await _settings.GetAsync();
        CurrentCommissionRule = await _commissionRules.GetCurrentAsync();
        if (setRateFromCurrentRule) UserShareRate = CurrentCommissionRule.UserShareRate;
    }
}
