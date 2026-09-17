using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace WebHoanTien.Admin;

public sealed class AdminWalletListInput : PagedResultRequestDto
{
    [StringLength(256)] public string? Filter { get; set; }
}

public sealed class AdminUserWalletDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal ConfirmedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal ReservedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RawBalance { get; set; }
}

public interface IAdminWalletAppService : IApplicationService
{
    Task<PagedResultDto<AdminUserWalletDto>> GetListAsync(AdminWalletListInput input);
}
