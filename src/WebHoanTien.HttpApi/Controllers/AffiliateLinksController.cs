using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Controllers;

[Route("api/app/affiliate-links")]
public class AffiliateLinksController : WebHoanTienController
{
    private readonly IAffiliateLinkAppService _service;
    public AffiliateLinksController(IAffiliateLinkAppService service) => _service = service;

    [HttpPost("validate"), AllowAnonymous]
    public Task<AffiliateUrlValidationDto> ValidateAsync([FromBody] ValidateAffiliateUrlInput input) => _service.ValidateAsync(input);
    [HttpPost, Authorize]
    public Task<AffiliateTrackingDto> CreateAsync([FromBody] CreateAffiliateLinkInput input) => _service.CreateAsync(input);
    [HttpGet, Authorize]
    public Task<PagedResultDto<AffiliateTrackingDto>> GetListAsync([FromQuery] AffiliateTrackingListInput input) => _service.GetListAsync(input);
    [HttpGet("{id:guid}"), Authorize]
    public Task<AffiliateTrackingDto> GetAsync(Guid id) => _service.GetAsync(id);
    [HttpPut("visibility"), Authorize]
    public Task SetHiddenAsync([FromBody] SetAffiliateTrackingHiddenInput input) => _service.SetHiddenAsync(input);
}
