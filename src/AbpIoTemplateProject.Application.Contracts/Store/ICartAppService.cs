using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AbpIoTemplateProject.Store;

public interface ICartAppService : IApplicationService
{
    Task<CartDto> GetAsync(string cartKey);
    Task<CartDto> AddAsync(AddCartItemInput input);
    Task<CartDto> UpdateAsync(UpdateCartItemInput input);
    Task<CartDto> RemoveAsync(string cartKey, Guid itemId);
    Task<CartDto> ClearAsync(string cartKey);
    Task<CartDto> ApplyPromotionAsync(ApplyPromotionInput input);
}
