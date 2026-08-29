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
    public bool IsExisting { get; set; }
    public bool WasRestored { get; set; }
    public AffiliatePlatform Platform { get; set; }
    public string TrackingToken { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string NormalizedUrl { get; set; } = string.Empty;
    public string? ProductId { get; set; }
    public string? ShopId { get; set; }
    public string? ProductName { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? EstimatedCommission { get; set; }
    public int ClickCount { get; set; }
    public DateTime? LastClickedAt { get; set; }
    public bool IsHidden { get; set; }
    public DateTime? HiddenAt { get; set; }
    public AffiliateTrackingStatus Status { get; set; }
    public string RedirectUrl { get; set; } = string.Empty;
}

public sealed class AffiliateTrackingListInput : PagedAndSortedResultRequestDto
{
    public bool IncludeHidden { get; set; }
}

public sealed class SetAffiliateTrackingHiddenInput
{
    public Guid Id { get; set; }
    public bool IsHidden { get; set; }
}

public interface IAffiliateLinkAppService : IApplicationService
{
    Task<AffiliateUrlValidationDto> ValidateAsync(ValidateAffiliateUrlInput input);
    Task<AffiliateTrackingDto> CreateAsync(CreateAffiliateLinkInput input);
    Task<PagedResultDto<AffiliateTrackingDto>> GetListAsync(AffiliateTrackingListInput input);
    Task<AffiliateTrackingDto> GetAsync(Guid id);
    Task SetHiddenAsync(SetAffiliateTrackingHiddenInput input);
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
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public AffiliateOrderStatus Status { get; set; }
    public DateTime PurchaseTime { get; set; }
    public DateTime? ClickTime { get; set; }
    public string? ShopType { get; set; }
    public string? ProductImageUrl { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal ExpectedUserCommission { get; set; }
    public decimal PayableUserCommission { get; set; }
    public decimal? SettledNetCommission { get; set; }
    public string? SettlementReference { get; set; }
    public DateTime? SettledAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public List<AffiliateOrderItemDto> Items { get; set; } = new();
}

public sealed class AffiliateOrderItemDto : EntityDto<Guid>
{
    public string? ProductName { get; set; }
    public decimal PurchaseAmount { get; set; }
    public int Quantity { get; set; }
}

public interface IAffiliateOrderAppService : IApplicationService
{
    Task<PagedResultDto<AffiliateOrderDto>> GetListAsync(AffiliateOrderListInput input);
    Task<AffiliateOrderDto> GetAsync(Guid id);
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
    public PayoutAccountDto? PayoutAccount { get; set; }
}

public sealed class PayoutAccountDto
{
    public string BankCode { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
}

public sealed class UpdatePayoutAccountInput
{
    [Required(ErrorMessage = "Vui lòng chọn ngân hàng."), StringLength(32)]
    public string BankCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số tài khoản."), RegularExpression(@"^\d{6,30}$", ErrorMessage = "Số tài khoản phải gồm từ 6 đến 30 chữ số.")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản."), StringLength(150, MinimumLength = 2, ErrorMessage = "Tên chủ tài khoản phải có từ 2 đến 150 ký tự.")]
    public string AccountHolderName { get; set; } = string.Empty;
}

public sealed class CreateLegalConsentInput
{
    public bool Accepted { get; set; }
    public LegalConsentMethod Method { get; set; }
}

public interface ICustomerProfileAppService : IApplicationService
{
    Task<CustomerProfileDto> GetAsync();
    Task<PayoutAccountDto> UpdatePayoutAccountAsync(UpdatePayoutAccountInput input);
    Task AcceptLegalAsync(CreateLegalConsentInput input);
}
