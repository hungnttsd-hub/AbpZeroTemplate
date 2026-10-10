using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebHoanTien.Integrations.RioHub;
using WebHoanTien.Permissions;

namespace WebHoanTien.Controllers;

// Creator-wide data is restricted to administrators, never exposed as a customer order feed.
[Authorize(WebHoanTienPermissions.Admin.Settings)]
[Route("api/app/admin/riohub")]
public class RioHubAffiliateController : WebHoanTienController
{
    private readonly IRioHubAffiliateClient _client;
    public RioHubAffiliateController(IRioHubAffiliateClient client) => _client = client;

    [HttpPost("links")]
    public Task<IActionResult> CreateLink([FromBody] RioHubCreateLinkInput input, CancellationToken cancellationToken) =>
        Execute(() => _client.CreateLinkAsync(input, cancellationToken: cancellationToken));

    [HttpPost("product-links")]
    public Task<IActionResult> CreateProductLink([FromBody] RioHubCreateLinkInput input, CancellationToken cancellationToken) =>
        Execute(() => _client.CreateLinkAsync(input, includeProduct: true, cancellationToken: cancellationToken));

    [HttpPost("general-links")]
    public Task<IActionResult> CreateGeneralLinks([FromBody] RioHubGeneralLinksInput input, CancellationToken cancellationToken) =>
        Execute(() => _client.CreateGeneralLinksAsync(input, cancellationToken));

    [HttpGet("links")]
    public Task<IActionResult> GetLinks([FromQuery] RioHubLinksQuery input, CancellationToken cancellationToken) =>
        Execute(() => _client.GetLinksAsync(input, cancellationToken));

    [HttpGet("orders")]
    public Task<IActionResult> GetOrders([FromQuery] RioHubOrdersQuery input, CancellationToken cancellationToken) =>
        Execute(() => _client.GetOrdersAsync(input, cancellationToken));

    [HttpGet("products")]
    public Task<IActionResult> GetProducts([FromQuery] RioHubProductsQuery input, CancellationToken cancellationToken) =>
        Execute(() => _client.GetProductsAsync(input, cancellationToken));

    private async Task<IActionResult> Execute(Func<Task<JsonElement>> action)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try { return new JsonResult(await action()); }
        catch (ValidationException exception) { return BadRequest(new { error = exception.Message }); }
        catch (RioHubApiException exception)
        {
            if (exception.RetryAfter is { } delay)
                Response.Headers["Retry-After"] = Math.Ceiling(delay.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            // An upstream 401/403 is not the CatBack user's authentication failure.
            var status = exception.UpstreamStatus == System.Net.HttpStatusCode.TooManyRequests ? 429 : 502;
            return StatusCode(status, new
            {
                error = exception.Message,
                code = exception.ErrorCode,
                upstreamStatus = exception.UpstreamStatus.HasValue ? (int?)exception.UpstreamStatus.Value : null
            });
        }
    }
}
