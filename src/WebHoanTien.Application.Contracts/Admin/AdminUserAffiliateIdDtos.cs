using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Admin;

public sealed class AdminUserAffiliateIdListInput : PagedAndSortedResultRequestDto
{
    [StringLength(256)]
    public string? Filter { get; set; }
}

public sealed class AdminUserAffiliateIdDto : FullAuditedEntityDto<Guid>
{
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public AffiliatePlatform Platform { get; set; }
    public string AffiliateId { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
}

public sealed class AdminAffiliateUserOptionDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public sealed class SetUserAffiliateIdInput
{
    [Required, EmailAddress, StringLength(256)]
    public string UserEmail { get; set; } = string.Empty;

    public AffiliatePlatform Platform { get; set; } = AffiliatePlatform.Shopee;

    [Required, StringLength(WebHoanTienConsts.AffiliateIdMaxLength),
     RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Affiliate ID chỉ được gồm chữ, số, dấu gạch dưới và gạch ngang.")]
    public string AffiliateId { get; set; } = string.Empty;

    [StringLength(WebHoanTienConsts.AffiliateOverrideNoteMaxLength)]
    public string? AdminNote { get; set; }
}

public interface IAdminUserAffiliateIdAppService : IApplicationService
{
    Task<PagedResultDto<AdminUserAffiliateIdDto>> GetListAsync(AdminUserAffiliateIdListInput input);
    Task<ListResultDto<AdminAffiliateUserOptionDto>> GetUserOptionsAsync();
    Task<AdminUserAffiliateIdDto> SetAsync(SetUserAffiliateIdInput input);
    Task RemoveAsync(Guid userId, AffiliatePlatform platform = AffiliatePlatform.Shopee);
}
