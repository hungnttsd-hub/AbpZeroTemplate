using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using WebHoanTien.Admin;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;

namespace WebHoanTien.Controllers;

[Route("api/app/wallet")]
[Authorize]
public class WalletController : WebHoanTienController
{
    private readonly ICustomerWalletAppService _service;

    public WalletController(ICustomerWalletAppService service) => _service = service;

    [HttpGet("overview")]
    public Task<CustomerWalletOverviewDto> GetOverviewAsync() => _service.GetOverviewAsync();

    [HttpGet("history")]
    public Task<PagedResultDto<WalletMovementDto>> GetHistoryAsync([FromQuery] WalletHistoryInput input) =>
        _service.GetHistoryAsync(input);

    [HttpPost("withdrawal-requests")]
    public Task<WithdrawalRequestDto> CreateWithdrawalRequestAsync([FromBody] CreateWithdrawalRequestInput input) =>
        _service.CreateWithdrawalRequestAsync(input);

    [HttpPost("withdrawal-requests/{id:guid}/cancel")]
    public Task<WithdrawalRequestDto> CancelWithdrawalRequestAsync(Guid id) =>
        _service.CancelWithdrawalRequestAsync(id);

    [HttpGet("withdrawal-requests/{id:guid}/proof")]
    [DisableAuditing]
    public async Task<IActionResult> GetProofAsync(Guid id)
    {
        var proof = await _service.GetProofAsync(id);
        Response.Headers.CacheControl = "no-store, private";
        return File(proof.Content, proof.ContentType, proof.FileName);
    }
}

[Route("api/app/admin/payouts")]
[Authorize(WebHoanTienPermissions.Admin.Payouts)]
public class AdminPayoutController : WebHoanTienController
{
    private readonly IAdminPayoutAppService _service;

    public AdminPayoutController(IAdminPayoutAppService service) => _service = service;

    [HttpGet]
    public Task<AdminPayoutPageDto> GetListAsync([FromQuery] AdminPayoutListInput input) => _service.GetListAsync(input);

    [HttpGet("{id:guid}")]
    public Task<AdminPayoutRequestDto> GetAsync(Guid id) => _service.GetAsync(id);

    [HttpPost("{id:guid}/paid")]
    [DisableAuditing]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(WebHoanTienConsts.MaximumWithdrawalProofSize + 1024 * 1024)]
    public async Task<AdminPayoutRequestDto> MarkPaidAsync(Guid id, [FromForm] MarkWithdrawalPaidInput input,
        [FromForm] IFormFile proof, CancellationToken cancellationToken)
    {
        if (proof is null || proof.Length == 0)
            throw new UserFriendlyException("Vui lòng chọn ảnh chứng từ thanh toán.");
        await using var stream = proof.OpenReadStream();
        return await _service.MarkPaidAsync(id, input, stream, proof.FileName,
            proof.ContentType, proof.Length, cancellationToken);
    }

    [HttpPost("{id:guid}/reject")]
    public Task<AdminPayoutRequestDto> RejectAsync(Guid id, [FromBody] RejectWithdrawalInput input) =>
        _service.RejectAsync(id, input);

    [HttpGet("{id:guid}/proof")]
    [DisableAuditing]
    public async Task<IActionResult> GetProofAsync(Guid id)
    {
        var proof = await _service.GetProofAsync(id);
        Response.Headers.CacheControl = "no-store, private";
        return File(proof.Content, proof.ContentType, proof.FileName);
    }
}
