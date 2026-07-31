using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AbpIoTemplateProject.Store;

public interface IOrderAppService : IApplicationService
{
    Task<List<ShippingMethodDto>> GetShippingMethodsAsync();
    Task<OrderDto> CheckoutAsync(CheckoutInput input);
    Task<OrderDto> TrackAsync(TrackOrderInput input);
    Task<List<OrderDto>> GetMyOrdersAsync();
}
