using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Admin;

namespace WebHoanTien.Controllers;

[Route("api/app/admin/affiliate-settings")]
public class AdminAffiliateSettingsController : WebHoanTienController
{
    private readonly IAdminAffiliateSettingsAppService _service;
    public AdminAffiliateSettingsController(IAdminAffiliateSettingsAppService service) => _service = service;
    [HttpGet] public Task<AffiliateConnectionStatusDto> GetAsync() => _service.GetAsync();
}

[Route("api/app/admin/shopee-reports")]
public class AdminShopeeReportsController : WebHoanTienController
{
    private readonly IAdminShopeeReportImportAppService _service;
    public AdminShopeeReportsController(IAdminShopeeReportImportAppService service) => _service = service;

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ShopeeReportImportResultDto> ImportAsync([FromForm] IFormFile report,
        CancellationToken cancellationToken)
    {
        if (report is null || report.Length == 0) throw new Volo.Abp.UserFriendlyException("Chọn file báo cáo Shopee trước khi import.");
        await using var stream = report.OpenReadStream();
        return await _service.ImportAsync(stream, report.FileName, cancellationToken);
    }
}

[Route("api/app/admin/shopee-settlements")]
public class AdminShopeeSettlementsController : WebHoanTienController
{
    private readonly IAdminShopeeSettlementImportAppService _service;
    public AdminShopeeSettlementsController(IAdminShopeeSettlementImportAppService service) => _service = service;

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ShopeeSettlementImportResultDto> ImportAsync([FromForm] IFormFile report,
        CancellationToken cancellationToken)
    {
        if (report is null || report.Length == 0)
            throw new Volo.Abp.UserFriendlyException("Chọn bảng kê Shopee đã thanh toán trước khi import.");
        await using var stream = report.OpenReadStream();
        return await _service.ImportAsync(stream, report.FileName, cancellationToken);
    }
}

[Route("api/app/admin/commission-rules")]
public class AdminCommissionRulesController : WebHoanTienController
{
    private readonly IAdminCommissionRuleAppService _service;
    public AdminCommissionRulesController(IAdminCommissionRuleAppService service) => _service = service;
    [HttpGet] public Task<ListResultDto<AffiliateCommissionRuleDto>> GetListAsync() => _service.GetListAsync();
    [HttpGet("current")] public Task<AffiliateCommissionRuleDto> GetCurrentAsync() => _service.GetCurrentAsync();
    [HttpPut("current")] public Task<AffiliateCommissionRuleDto> SetCurrentRateAsync([FromBody] SetCurrentCommissionRateInput input) => _service.SetCurrentRateAsync(input);
    [HttpPost] public Task<AffiliateCommissionRuleDto> CreateAsync([FromBody] CreateCommissionRuleInput input) => _service.CreateAsync(input);
    [HttpPost("{id:guid}/deactivate")] public Task DeactivateAsync(Guid id) => _service.DeactivateAsync(id);
}

[Route("api/app/admin/affiliate-conversions")]
public class AdminAffiliateOrdersController : WebHoanTienController
{
    private readonly IAdminAffiliateOrderAppService _service;
    public AdminAffiliateOrdersController(IAdminAffiliateOrderAppService service) => _service = service;
    [HttpGet] public Task<PagedResultDto<AdminAffiliateConversionDto>> GetListAsync([FromQuery] AdminAffiliateConversionListInput input) => _service.GetListAsync(input);
    [HttpGet("{conversionId:guid}")] public Task<AdminAffiliateConversionDetailsDto> GetAsync(Guid conversionId) => _service.GetAsync(conversionId);
    [HttpPost("{conversionId:guid}/manual-match")] public Task ManualMatchAsync(Guid conversionId, [FromBody] ManualMatchInput input) => _service.ManualMatchAsync(conversionId, input);
}
