using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.SettingManagement;
using Volo.Abp.Users;
using WebHoanTien.Settings;
using WebHoanTien.Web.Operations;
using WebHoanTien.Admin;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages.Admin.Settlements;

[Authorize(WebHoanTienPermissions.Admin.Orders)]
public class IndexModel : PageModel
{
    private const int BatchPageSize = 20;
    private const int RecordPageSize = 50;
    private readonly IAdminShopeeSettlementApprovalAppService _settlements;
    private readonly ISettingManager _settings;
    private readonly IBackgroundJobClient _jobs;
    private readonly ICurrentUser _currentUser;

    [BindProperty(SupportsGet = true)] public string? Filter { get; set; }
    [BindProperty(SupportsGet = true)] public bool? IsApproved { get; set; }
    [BindProperty(SupportsGet = true)] public bool? IsShopeePaid { get; set; }
    [BindProperty(SupportsGet = true)] public string? AffiliateId { get; set; }
    [BindProperty(SupportsGet = true), DataType(DataType.Date)] public DateTime? FromDate { get; set; }
    [BindProperty(SupportsGet = true), DataType(DataType.Date)] public DateTime? ToDate { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public Guid? BatchId { get; set; }
    [BindProperty(SupportsGet = true)] public bool AllRecords { get; set; }
    [BindProperty(SupportsGet = true)] public int RecordPageNumber { get; set; } = 1;

    public AdminShopeeSettlementPageDto Data { get; private set; } = new();
    public AdminShopeeSettlementBatchDetailsDto? Details { get; private set; }
    public PagedResultDto<AdminShopeeSettlementRecordDto>? Records { get; private set; }
    public bool AutoApprovalEnabled { get; private set; }
    public ShopeeAutoApprovalRunDto? AutoApprovalLastRun { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Data.Batches.TotalCount / (double)BatchPageSize));
    public int TotalRecordPages => Details is null && Records is null
        ? 1
        : Math.Max(1, (int)Math.Ceiling((AllRecords
            ? Records!.TotalCount
            : Details!.Records.TotalCount) / (double)RecordPageSize));

    public IndexModel(IAdminShopeeSettlementApprovalAppService settlements, ISettingManager settings,
        IBackgroundJobClient jobs, ICurrentUser currentUser)
    { _settlements = settlements; _settings = settings; _jobs = jobs; _currentUser = currentUser; }

    private AdminShopeeSettlementBatchListInput CurrentFilters() => new()
    {
        Filter = Filter,
        IsApproved = IsApproved,
        IsShopeePaid = IsShopeePaid,
        AffiliateId = AffiliateId,
        FromDate = FromDate,
        ToDate = ToDate
    };

    private bool ValidateFilters()
    {
        var input = CurrentFilters();
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), errors, validateAllProperties: true);
        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error.ErrorMessage ?? "Bộ lọc không hợp lệ.");
        return ModelState.IsValid;
    }

    public async Task OnGetAsync()
    {
        await LoadAutoApprovalAsync();
        if (!ValidateFilters())
        {
            AllRecords = true;
            BatchId = null;
            Records = new PagedResultDto<AdminShopeeSettlementRecordDto>();
            return;
        }
        PageNumber = Math.Max(1, PageNumber);
        RecordPageNumber = Math.Max(1, RecordPageNumber);
        var input = CurrentFilters();
        input.SkipCount = (PageNumber - 1) * BatchPageSize;
        input.MaxResultCount = BatchPageSize;
        Data = await _settlements.GetListAsync(input);
        if (AllRecords || !BatchId.HasValue)
        {
            AllRecords = true;
            BatchId = null;
            Records = await _settlements.GetRecordsAsync(input,
                (RecordPageNumber - 1) * RecordPageSize, RecordPageSize);
        }
        if (!AllRecords && BatchId.HasValue)
            Details = await _settlements.GetAsync(BatchId.Value,
                (RecordPageNumber - 1) * RecordPageSize, RecordPageSize, input);
    }

    private async Task LoadAutoApprovalAsync()
    {
        AutoApprovalEnabled = string.Equals(await _settings.GetOrNullGlobalAsync(ShopeeAutoApprovalSettings.Enabled),
            "true", StringComparison.OrdinalIgnoreCase);
        var lastRun = await _settings.GetOrNullGlobalAsync(ShopeeAutoApprovalSettings.LastRun);
        if (!string.IsNullOrWhiteSpace(lastRun))
        {
            try { AutoApprovalLastRun = JsonSerializer.Deserialize<ShopeeAutoApprovalRunDto>(lastRun); }
            catch (JsonException) { AutoApprovalLastRun = null; }
        }
    }

    public async Task<IActionResult> OnPostConfigureAutoApprovalAsync(bool? enabled)
    {
        if (!enabled.HasValue || !ModelState.IsValid)
            return BadRequest(new { success = false, error = "Cấu hình không hợp lệ. Hãy tải lại trang và thử lại." });
        if (enabled.Value)
            await _settings.SetGlobalAsync(ShopeeAutoApprovalSettings.ApprovedBy, _currentUser.GetId().ToString());
        await _settings.SetGlobalAsync(ShopeeAutoApprovalSettings.Enabled, enabled.Value ? "true" : "false");
        return new JsonResult(new
        {
            success = true,
            message = enabled.Value ? "Đã bật tự động duyệt Shopee lúc 03:00 mỗi ngày (giờ Việt Nam)."
                : "Đã tắt lịch tự động duyệt Shopee. Lượt đang xử lý, nếu có, vẫn hoàn tất."
        });
    }

    public IActionResult OnPostRunAutoApproval()
    {
        var adminId = _currentUser.GetId();
        _jobs.Enqueue<ShopeeAutoApprovalJob>(job => job.ExecuteAsync(true, adminId));
        return new JsonResult(new
        {
            success = true,
            message = "Đã đưa lượt duyệt vào hàng chờ. Job chạy nền; tải lại trang để xem kết quả."
        });
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid recordId, bool useManualAmounts)
    {
        try
        {
            if (useManualAmounts && !ModelState.IsValid)
                return BadRequest(new { success = false, error = "Vui lòng nhập đủ hoa hồng, thuế và phí hợp lệ." });
            AdminShopeeSettlementManualInput? manual = null;
            if (useManualAmounts)
            {
                var form = await Request.ReadFormAsync();
                if (!TryReadManualDecimal(form, "manual.GrossCommission", out var gross) ||
                    !TryReadManualDecimal(form, "manual.TaxPercent", out var tax) ||
                    !TryReadManualDecimal(form, "manual.ServiceFeePercent", out var fee))
                    return BadRequest(new { success = false, error = "Vui lòng nhập đủ hoa hồng, thuế và phí hợp lệ." });
                manual = new AdminShopeeSettlementManualInput
                {
                    GrossCommission = gross,
                    TaxPercent = tax,
                    ServiceFeePercent = fee
                };
            }
            var result = await _settlements.ApproveAsync(recordId, manual);
            return new JsonResult(new
            {
                success = true,
                message = result.ApprovedCount > 0
                    ? "Đã duyệt đối soát và cộng tiền vào ví."
                    : "Bản ghi chưa đủ dữ liệu để cộng ví hoặc đã được xử lý trước đó.",
                result
            });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { success = false, error = ErrorMessage(exception) });
        }
    }

    public async Task<IActionResult> OnPostApproveAllAsync(Guid batchId)
    {
        if (!ValidateFilters())
            return BadRequest(new { success = false, error = "Bộ lọc không hợp lệ. Kiểm tra lại khoảng ngày và thử lại." });
        try
        {
            var result = await _settlements.ApproveAllAsync(batchId, CurrentFilters());
            return new JsonResult(new
            {
                success = true,
                message = result.ApprovedCount > 0
                    ? $"Đã duyệt {result.ApprovedCount} đơn và cộng tiền vào ví." +
                      (result.SkippedCount > 0 ? $" Có {result.SkippedCount} bản ghi được bỏ qua để kiểm tra lại." : string.Empty)
                    : "Batch không còn bản ghi đủ điều kiện để duyệt.",
                result
            });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { success = false, error = ErrorMessage(exception) });
        }
    }

    public async Task<IActionResult> OnPostApproveFilteredAsync(Guid? batchId)
    {
        if (!ValidateFilters())
            return BadRequest(new { success = false, error = "Bộ lọc không hợp lệ. Hãy tải lại trang và thử lại." });
        try
        {
            var result = await _settlements.ApproveFilteredAsync(CurrentFilters(), batchId);
            return new JsonResult(new
            {
                success = true,
                message = (result.ApprovedCount > 0
                    ? $"Đã duyệt {result.ApprovedCount} đơn theo bộ lọc và cộng tiền vào ví."
                    : "Không còn đơn phù hợp đủ điều kiện để duyệt.") +
                    (result.SkippedCount > 0 ? $" Có {result.SkippedCount} bản ghi được bỏ qua để kiểm tra lại." : string.Empty),
                result
            });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { success = false, error = ErrorMessage(exception) });
        }
    }

    public async Task<IActionResult> OnPostApproveAllPaidAsync()
    {
        if (!ValidateFilters())
            return BadRequest(new { success = false, error = "Bộ lọc không hợp lệ. Hãy tải lại trang và thử lại." });
        try
        {
            var result = await _settlements.ApproveAllPaidAsync(CurrentFilters());
            return new JsonResult(new
            {
                success = true,
                message = result.ApprovedCount > 0
                    ? $"Đã duyệt {result.ApprovedCount} đơn Shopee đã thanh toán và cộng tiền vào ví." +
                      (result.SkippedCount > 0 ? $" Có {result.SkippedCount} bản ghi được bỏ qua để kiểm tra lại." : string.Empty)
                    : "Không còn đơn Shopee đã thanh toán đủ điều kiện để duyệt." +
                      (result.SkippedCount > 0 ? $" Có {result.SkippedCount} bản ghi cần kiểm tra lại." : string.Empty),
                result
            });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { success = false, error = ErrorMessage(exception) });
        }
    }

    public async Task<IActionResult> OnPostRefreshMatchesAsync(Guid batchId)
    {
        try
        {
            var result = await _settlements.RefreshMatchesAsync(batchId);
            return new JsonResult(new
            {
                success = true,
                message = result.CheckedCount == 0
                    ? "Batch không có bản ghi cần đối chiếu lại."
                    : $"Đã kiểm tra lại {result.CheckedCount} bản ghi; {result.ReadyForApprovalCount} đơn đang chờ duyệt.",
                result
            });
        }
        catch (UserFriendlyException exception)
        {
            return BadRequest(new { success = false, error = exception.Message });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { success = false, error = ErrorMessage(exception) });
        }
    }

    private static bool TryReadManualDecimal(IFormCollection form, string field, out decimal value)
    {
        value = 0m;
        var values = form[field];
        // HTML number inputs submit a dot decimal separator regardless of UI language.
        // The Vietnamese form binder treats that dot as grouping (0.98 becomes 98).
        // Disallow grouping instead of guessing a culture for monetary values.
        return values.Count == 1 && decimal.TryParse(values[0],
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);
    }

    private static string ErrorMessage(BusinessException exception) => exception.Code switch
    {
        WebHoanTienDomainErrorCodes.AffiliateOrderSettlementInvalidState =>
            "Một hoặc nhiều đơn đã thay đổi trạng thái. Hãy tải lại trang và kiểm tra trước khi duyệt.",
        WebHoanTienDomainErrorCodes.InvalidShopeeSettlementReport =>
            "Dữ liệu đối soát không còn hợp lệ để duyệt.",
        _ => exception.Message
    };
}

public static class SettlementPageUi
{
    public static string Money(decimal value) => $"{value:N0}đ";
    public static string Deduction(decimal value) => value == 0m ? Money(0m) : $"-{Money(value)}";
    public static string Date(DateTime value) => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public static string VietnamTime(DateTime value) => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc))
        .ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    public static string PaymentTime(DateTime? value) => value.HasValue
        ? $"Shopee trả {Date(value.Value)}"
        : "Shopee chưa ghi nhận thời gian thanh toán";
    public static string Code(int? value) => value?.ToString() ?? "-";
    public static string ShopeePaymentLabel(bool isPaid) => isPaid ? "Đã thanh toán" : "Chờ xử lý";
    public static string OrderLabel(AffiliateOrderStatus status) => status switch
    {
        AffiliateOrderStatus.Unpaid => "Chưa thanh toán",
        AffiliateOrderStatus.Pending => "Đang xử lý",
        AffiliateOrderStatus.Completed => "Hoàn thành",
        AffiliateOrderStatus.Cancelled => "Đã hủy",
        AffiliateOrderStatus.Refunded => "Đã hoàn trả",
        AffiliateOrderStatus.Rejected => "Bị từ chối",
        AffiliateOrderStatus.Settled => "Đã duyệt hoàn tiền",
        _ => status.ToString()
    };

    public static string BatchLabel(ShopeeSettlementBatchStatus status) => status switch
    {
        ShopeeSettlementBatchStatus.PendingApproval => "Chờ duyệt",
        ShopeeSettlementBatchStatus.PartiallyApproved => "Đã duyệt một phần",
        ShopeeSettlementBatchStatus.Approved => "Đã duyệt",
        ShopeeSettlementBatchStatus.CompletedWithIssues => "Hoàn tất có cảnh báo",
        ShopeeSettlementBatchStatus.WaitingForShopee => "Chờ Shopee thanh toán",
        _ => status.ToString()
    };

    public static string RecordLabel(ShopeeSettlementRecordStatus status) => status switch
    {
        ShopeeSettlementRecordStatus.PendingApproval => "Chờ duyệt",
        ShopeeSettlementRecordStatus.Approved => "Đã duyệt",
        ShopeeSettlementRecordStatus.Unmatched => "Không khớp",
        ShopeeSettlementRecordStatus.AlreadySettled => "Đã ghi nhận trước",
        ShopeeSettlementRecordStatus.Invalid => "Chưa hợp lệ",
        ShopeeSettlementRecordStatus.AwaitingShopeePayment => "Chờ Shopee thanh toán",
        _ => status.ToString()
    };

    public static string StatusClass(Enum status) => status.ToString().ToLowerInvariant();
}
