using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace WebHoanTien.Affiliates;

public sealed class ValidateAffiliateUrlInput
{
    [Required, StringLength(WebHoanTienConsts.UrlMaxLength)]
    public string Url { get; set; } = string.Empty;
}

public sealed class AffiliateUrlValidationDto
{
    public bool IsValid { get; set; }
    public AffiliatePlatform? Platform { get; set; }
    public string? NormalizedUrl { get; set; }
    public string? ItemId { get; set; }
    public bool RequiresRedirectResolution { get; set; }
    public string? Error { get; set; }
}

public sealed class CreateAffiliateLinkInput
{
    [Required, StringLength(WebHoanTienConsts.UrlMaxLength)]
    public string Url { get; set; } = string.Empty;
}

public sealed class AffiliateTrackingDto : FullAuditedEntityDto<Guid>
{
    public AffiliatePlatform Platform { get; set; }
    public string TrackingToken { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string NormalizedUrl { get; set; } = string.Empty;
    public string? AffiliateUrl { get; set; }
    public string? ProductId { get; set; }
    public string? ShopId { get; set; }
    public string? ProductName { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? EstimatedCommission { get; set; }
    public int ClickCount { get; set; }
    public DateTime? LastClickedAt { get; set; }
    public AffiliateTrackingStatus Status { get; set; }
    public string RedirectUrl { get; set; } = string.Empty;
}

public interface IAffiliateLinkAppService : IApplicationService
{
    Task<AffiliateUrlValidationDto> ValidateAsync(ValidateAffiliateUrlInput input);
    Task<AffiliateTrackingDto> CreateAsync(CreateAffiliateLinkInput input);
    Task<PagedResultDto<AffiliateTrackingDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<AffiliateTrackingDto> GetAsync(Guid id);
}

public sealed class AffiliateOrderListInput : PagedAndSortedResultRequestDto
{
    public AffiliateOrderStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public sealed class AffiliateOrderDto : FullAuditedEntityDto<Guid>
{
    public Guid ConversionId { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public AffiliateOrderStatus Status { get; set; }
    public DateTime PurchaseTime { get; set; }
    public string? ShopType { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal NetCommission { get; set; }
    public decimal UserCommission { get; set; }
    public decimal PayableUserCommission { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public List<AffiliateOrderItemDto> Items { get; set; } = new();
}

public sealed class AffiliateOrderItemDto : EntityDto<Guid>
{
    public string ExternalItemId { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public decimal PurchaseAmount { get; set; }
    public int Quantity { get; set; }
    public decimal UserCommission { get; set; }
    public decimal RefundAmount { get; set; }
    public bool IsFraud { get; set; }
    public string? ProviderStatus { get; set; }
}

public interface IAffiliateOrderAppService : IApplicationService
{
    Task<PagedResultDto<AffiliateOrderDto>> GetListAsync(AffiliateOrderListInput input);
    Task<AffiliateOrderDto> GetAsync(Guid id);
    Task RequestSyncAsync();
}

public sealed class CustomerProfileDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool HasGoogleLogin { get; set; }
    public bool HasCurrentLegalConsent { get; set; }
}

public sealed class CreateLegalConsentInput
{
    public bool Accepted { get; set; }
    public LegalConsentMethod Method { get; set; }
}

public interface ICustomerProfileAppService : IApplicationService
{
    Task<CustomerProfileDto> GetAsync();
    Task AcceptLegalAsync(CreateLegalConsentInput input);
}
