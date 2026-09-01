using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Volo.Abp.Auditing;
using WebHoanTien.Admin;
using WebHoanTien.Web.Operations;

namespace WebHoanTien.Web.Controllers;

[ApiController]
[Route("api/public/shopee-automation")]
public class ShopeeAutomationController : ControllerBase
{
    private const long MaximumAcceptedBodySize = 20L * 1024 * 1024;
    private readonly ShopeeAutomationTokenService _tokenService;
    private readonly IShopeeAutomationImportAppService _importService;
    private readonly IShopeeAutomationSettlementImportAppService _settlementImportService;
    private readonly IOptionsMonitor<ShopeeAutomationOptions> _options;

    public ShopeeAutomationController(ShopeeAutomationTokenService tokenService,
        IShopeeAutomationImportAppService importService,
        IShopeeAutomationSettlementImportAppService settlementImportService,
        IOptionsMonitor<ShopeeAutomationOptions> options)
    {
        _tokenService = tokenService;
        _importService = importService;
        _settlementImportService = settlementImportService;
        _options = options;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [DisableAuditing]
    [EnableRateLimiting(ShopeeAutomationAuthenticationDefaults.TokenRateLimitPolicy)]
    public IActionResult GenerateToken([FromBody] ShopeeAutomationTokenRequest request)
    {
        var result = _tokenService.Issue(request.ClientId, request.ClientSecret);
        if (result.Status == ShopeeAutomationTokenIssueStatus.ConfigurationUnavailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "shopee_automation_unavailable",
                error_description = "API tự động import Shopee chưa được cấu hình."
            });
        }

        if (result.Status == ShopeeAutomationTokenIssueStatus.InvalidCredentials)
        {
            return Unauthorized(new
            {
                error = "invalid_client",
                error_description = "Client ID hoặc Client Secret không hợp lệ."
            });
        }

        return Ok(new ShopeeAutomationTokenResponse
        {
            AccessToken = result.AccessToken!,
            ExpiresIn = result.ExpiresInSeconds,
            ExpiresAtUtc = result.ExpiresAt!.Value
        });
    }

    [HttpPost("reports/import")]
    [Authorize(AuthenticationSchemes = ShopeeAutomationAuthenticationDefaults.AuthenticationScheme)]
    [DisableAuditing]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumAcceptedBodySize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumAcceptedBodySize)]
    public async Task<ActionResult<ShopeeReportImportResultDto>> ImportReportAsync(
        [FromForm] IFormFile? report, CancellationToken cancellationToken)
    {
        if (report is null || report.Length == 0)
        {
            return BadRequest(new { error = "report_required", error_description = "Cần gửi file báo cáo Shopee." });
        }

        var maxSizeMb = Math.Clamp(_options.CurrentValue.MaxReportSizeMb, 1, 20);
        if (report.Length > maxSizeMb * 1024L * 1024L)
        {
            return BadRequest(new
            {
                error = "report_too_large",
                error_description = $"File báo cáo không được vượt quá {maxSizeMb} MB."
            });
        }

        await using var stream = report.OpenReadStream();
        return Ok(await _importService.ImportAsync(stream, report.FileName, cancellationToken));
    }

    [HttpPost("settlements/import")]
    [Authorize(AuthenticationSchemes = ShopeeAutomationAuthenticationDefaults.AuthenticationScheme)]
    [DisableAuditing]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumAcceptedBodySize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumAcceptedBodySize)]
    public async Task<ActionResult<ShopeeSettlementImportResultDto>> ImportSettlementAsync(
        [FromForm] IFormFile? report, CancellationToken cancellationToken)
    {
        if (report is null || report.Length == 0)
            return BadRequest(new { error = "report_required", error_description = "Cần gửi file đối soát Shopee." });
        var maxSizeMb = Math.Clamp(_options.CurrentValue.MaxReportSizeMb, 1, 20);
        if (report.Length > maxSizeMb * 1024L * 1024L)
            return BadRequest(new { error = "report_too_large", error_description = $"File đối soát không được vượt quá {maxSizeMb} MB." });
        await using var stream = report.OpenReadStream();
        return Ok(await _settlementImportService.ImportAsync(stream, report.FileName, cancellationToken));
    }
}

public sealed class ShopeeAutomationTokenRequest
{
    [Required, StringLength(128)]
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [Required, StringLength(512)]
    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;
}

public sealed class ShopeeAutomationTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("expires_at_utc")]
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
