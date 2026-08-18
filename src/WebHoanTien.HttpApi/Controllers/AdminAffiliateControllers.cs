using System;
using System.Threading;
using System.Threading.Tasks;
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
    [HttpPut] public Task<AffiliateConnectionStatusDto> UpdateAsync([FromBody] UpdateAffiliateSettingsInput input) => _service.UpdateAsync(input);
    [HttpPost("check-permission")]
    public Task<ShopeeAmsPermissionCheckDto> CheckPermissionAsync(CancellationToken cancellationToken) =>
        _service.CheckPermissionAsync(cancellationToken);
}

[Route("api/app/admin/commission-rules")]
public class AdminCommissionRulesController : WebHoanTienController
{
    private readonly IAdminCommissionRuleAppService _service;
    public AdminCommissionRulesController(IAdminCommissionRuleAppService service) => _service = service;
    [HttpGet] public Task<ListResultDto<AffiliateCommissionRuleDto>> GetListAsync() => _service.GetListAsync();
    [HttpPost] public Task<AffiliateCommissionRuleDto> CreateAsync([FromBody] CreateCommissionRuleInput input) => _service.CreateAsync(input);
    [HttpPost("{id:guid}/deactivate")] public Task DeactivateAsync(Guid id) => _service.DeactivateAsync(id);
}

[Route("api/app/admin/affiliate-sync")]
public class AdminAffiliateSyncController : WebHoanTienController
{
    private readonly IAdminAffiliateSyncAppService _service;
    public AdminAffiliateSyncController(IAdminAffiliateSyncAppService service) => _service = service;
    [HttpGet("states")] public Task<ListResultDto<AffiliateSyncStateDto>> GetStatesAsync() => _service.GetStatesAsync();
    [HttpGet("runs")] public Task<PagedResultDto<AffiliateSyncRunDto>> GetRunsAsync([FromQuery] PagedAndSortedResultRequestDto input) => _service.GetRunsAsync(input);
    [HttpPost("initial-date")] public Task SetInitialDateAsync([FromBody] SetInitialSyncDateInput input) => _service.SetInitialDateAsync(input);
    [HttpPost("sync-now")] public Task SyncNowAsync() => _service.SyncNowAsync();
    [HttpPost("reconcile")] public Task ReconcileAsync([FromBody] ReconcileInput input) => _service.ReconcileAsync(input);
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
