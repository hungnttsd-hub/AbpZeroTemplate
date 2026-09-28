using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
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

    [BindProperty(SupportsGet = true)] public string? Filter { get; set; }
    [BindProperty(SupportsGet = true)] public bool? IsApproved { get; set; }
    [BindProperty(SupportsGet = true)] public bool? IsShopeePaid { get; set; }
    [BindProperty(SupportsGet = true)] public string? AffiliateId { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public Guid? BatchId { get; set; }
    [BindProperty(SupportsGet = true)] public bool AllRecords { get; set; }
    [BindProperty(SupportsGet = true)] public int RecordPageNumber { get; set; } = 1;

    public AdminShopeeSettlementPageDto Data { get; private set; } = new();
    public AdminShopeeSettlementBatchDetailsDto? Details { get; private set; }
    public PagedResultDto<AdminShopeeSettlementRecordDto>? Records { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Data.Batches.TotalCount / (double)BatchPageSize));
    public int TotalRecordPages => Details is null && Records is null
        ? 1
        : Math.Max(1, (int)Math.Ceiling((AllRecords
            ? Records!.TotalCount
            : Details!.Records.TotalCount) / (double)RecordPageSize));

    public IndexModel(IAdminShopeeSettlementApprovalAppService settlements) => _settlements = settlements;

    private AdminShopeeSettlementBatchListInput CurrentFilters() => new()
    {
        Filter = Filter,
        IsApproved = IsApproved,
        IsShopeePaid = IsShopeePaid,
        AffiliateId = AffiliateId
    };

    public async Task OnGetAsync()
    {
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
    public static string PaymentTime(DateTime? value) => value.HasValue
        ? $"Shopee trả {Date(value.Value)}"
        : "Shopee chưa ghi nhận thời gian thanh toán";
    public static string Code(int? value) => value?.ToString() ?? "-";
    public static string ShopeePaymentLabel(bool isPaid) => isPaid ? "Đã thanh toán" : "Chờ xử lý";

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
