using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Controllers;

[Authorize, Route("api/app/affiliate-orders")]
public class AffiliateOrdersController : WebHoanTienController
{
    private readonly IAffiliateOrderAppService _service;
    public AffiliateOrdersController(IAffiliateOrderAppService service) => _service = service;
    [HttpGet] public Task<PagedResultDto<AffiliateOrderDto>> GetListAsync([FromQuery] AffiliateOrderListInput input) => _service.GetListAsync(input);
    [HttpGet("{id:guid}")] public Task<AffiliateOrderDto> GetAsync(Guid id) => _service.GetAsync(id);
}
