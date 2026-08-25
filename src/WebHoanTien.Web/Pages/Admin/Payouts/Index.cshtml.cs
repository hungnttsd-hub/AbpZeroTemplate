using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp;
using WebHoanTien.Admin;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;

namespace WebHoanTien.Web.Pages.Admin.Payouts;

[Authorize(WebHoanTienPermissions.Admin.Payouts)]
[RequestSizeLimit(WebHoanTienConsts.MaximumWithdrawalProofSize + 1024 * 1024)]
public class IndexModel : PageModel
{
    private const int PageSize = 20;
    private readonly IAdminPayoutAppService _payouts;

    [BindProperty(SupportsGet = true)] public string? Filter { get; set; }
    [BindProperty(SupportsGet = true)] public WithdrawalRequestStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public AdminPayoutPageDto Data { get; private set; } = new();
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Data.Requests.TotalCount / (double)PageSize));

    public IndexModel(IAdminPayoutAppService payouts) => _payouts = payouts;

    public async Task OnGetAsync()
    {
        PageNumber = Math.Max(1, PageNumber);
        Data = await _payouts.GetListAsync(new AdminPayoutListInput
        {
            Filter = Filter,
            Status = Status,
            From = From.HasValue ? ToUtcBoundary(From.Value.Date) : null,
            To = To.HasValue ? ToUtcBoundary(To.Value.Date.AddDays(1)) : null,
            SkipCount = (PageNumber - 1) * PageSize,
            MaxResultCount = PageSize
        });
    }

    public async Task<IActionResult> OnPostPayAsync(Guid requestId, string paymentReference, DateTime paidAt,
        string? adminNote, IFormFile? proof)
    {
        if (string.IsNullOrWhiteSpace(paymentReference))
            return BadRequest(new { success = false, error = "Vui lòng nhập mã giao dịch." });
        if (paidAt == default)
            return BadRequest(new { success = false, error = "Vui lòng nhập thời gian chuyển khoản." });
        if (proof is null || proof.Length == 0)
            return BadRequest(new { success = false, error = "Vui lòng chọn ảnh chứng từ thanh toán." });

        try
        {
            await using var stream = proof.OpenReadStream();
            var request = await _payouts.MarkPaidAsync(requestId, new MarkWithdrawalPaidInput
            {
                PaymentReference = paymentReference,
                PaidAt = paidAt,
                AdminNote = adminNote
            }, stream, proof.FileName, proof.ContentType, proof.Length, HttpContext.RequestAborted);
            return new JsonResult(new { success = true, message = "Đã ghi nhận thanh toán thành công.", request });
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

    public async Task<IActionResult> OnPostRejectAsync(Guid requestId, string reason, string? adminNote)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return BadRequest(new { success = false, error = "Vui lòng nhập lý do từ chối." });
        try
        {
            var request = await _payouts.RejectAsync(requestId, new RejectWithdrawalInput
            {
                Reason = reason,
                AdminNote = adminNote
            });
            return new JsonResult(new { success = true, message = "Đã từ chối yêu cầu rút tiền.", request });
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
        WebHoanTienDomainErrorCodes.WithdrawalNotBacked => "Hoa hồng đã xác nhận không còn đủ bảo chứng yêu cầu này.",
        WebHoanTienDomainErrorCodes.WithdrawalInvalidState => "Yêu cầu đã được xử lý hoặc không còn hợp lệ.",
        WebHoanTienDomainErrorCodes.WithdrawalProofInvalid => "Ảnh chứng từ không hợp lệ. Chỉ nhận JPEG, PNG hoặc WebP tối đa 5 MB.",
        _ => exception.Message
    };

    private static DateTime ToUtcBoundary(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
}
