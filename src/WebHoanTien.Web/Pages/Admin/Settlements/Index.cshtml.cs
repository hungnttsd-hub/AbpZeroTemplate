using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
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
    [BindProperty(SupportsGet = true)] public ShopeeSettlementBatchStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public Guid? BatchId { get; set; }
    [BindProperty(SupportsGet = true)] public int RecordPageNumber { get; set; } = 1;

    public AdminShopeeSettlementPageDto Data { get; private set; } = new();
    public AdminShopeeSettlementBatchDetailsDto? Details { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Data.Batches.TotalCount / (double)BatchPageSize));
    public int TotalRecordPages => Details is null
        ? 1
        : Math.Max(1, (int)Math.Ceiling(Details.Records.TotalCount / (double)RecordPageSize));

    public IndexModel(IAdminShopeeSettlementApprovalAppService settlements) => _settlements = settlements;

    public async Task OnGetAsync()
    {
        PageNumber = Math.Max(1, PageNumber);
        RecordPageNumber = Math.Max(1, RecordPageNumber);
        Data = await _settlements.GetListAsync(new AdminShopeeSettlementBatchListInput
        {
            Filter = Filter,
            Status = Status,
            SkipCount = (PageNumber - 1) * BatchPageSize,
            MaxResultCount = BatchPageSize
        });
        BatchId ??= Data.Batches.Items.Count > 0 ? Data.Batches.Items[0].Id : null;
        if (BatchId.HasValue)
            Details = await _settlements.GetAsync(BatchId.Value,
                (RecordPageNumber - 1) * RecordPageSize, RecordPageSize);
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid recordId)
    {
        try
        {
            var result = await _settlements.ApproveAsync(recordId);
            return new JsonResult(new
            {
                success = true,
                message = result.ApprovedCount > 0 ? "Đã duyệt đối soát và cộng tiền vào ví." : "Bản ghi đã được xử lý trước đó.",
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
            var result = await _settlements.ApproveAllAsync(batchId);
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
    public static string Date(DateTime value) => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public static string BatchLabel(ShopeeSettlementBatchStatus status) => status switch
    {
        ShopeeSettlementBatchStatus.PendingApproval => "Chờ duyệt",
        ShopeeSettlementBatchStatus.PartiallyApproved => "Đã duyệt một phần",
        ShopeeSettlementBatchStatus.Approved => "Đã duyệt",
        ShopeeSettlementBatchStatus.CompletedWithIssues => "Hoàn tất có cảnh báo",
        _ => status.ToString()
    };

    public static string RecordLabel(ShopeeSettlementRecordStatus status) => status switch
    {
        ShopeeSettlementRecordStatus.PendingApproval => "Chờ duyệt",
        ShopeeSettlementRecordStatus.Approved => "Đã duyệt",
        ShopeeSettlementRecordStatus.Unmatched => "Không khớp",
        ShopeeSettlementRecordStatus.AlreadySettled => "Đã ghi nhận trước",
        ShopeeSettlementRecordStatus.Invalid => "Chưa hợp lệ",
        _ => status.ToString()
    };

    public static string StatusClass(Enum status) => status.ToString().ToLowerInvariant();
}
