using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateTracking : FullAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public AffiliatePlatform Platform { get; private set; }
    public string TrackingToken { get; private set; } = null!;
    public string OriginalUrl { get; private set; } = null!;
    public string NormalizedUrl { get; private set; } = null!;
    public string? AffiliateUrl { get; private set; }
    public string? ProductId { get; private set; }
    public string? ShopId { get; private set; }
    public string? ProductName { get; private set; }
    public string? ImageUrl { get; private set; }
    public decimal? EstimatedCommission { get; private set; }
    public int ClickCount { get; private set; }
    public DateTime? LastClickedAt { get; private set; }
    public bool IsHidden { get; private set; }
    public DateTime? HiddenAt { get; private set; }
    public AffiliateTrackingStatus Status { get; private set; }

    protected AffiliateTracking() { }

    public AffiliateTracking(Guid id, Guid userId, AffiliatePlatform platform, string token, string originalUrl, string normalizedUrl)
        : base(id)
    {
        UserId = userId;
        Platform = platform;
        TrackingToken = token;
        OriginalUrl = originalUrl;
        NormalizedUrl = normalizedUrl;
        Status = AffiliateTrackingStatus.Active;
    }

    public void SetAffiliateLink(string affiliateUrl) => AffiliateUrl = affiliateUrl;

    public void SetResolvedUrl(string normalizedUrl, string affiliateUrl)
    {
        NormalizedUrl = normalizedUrl;
        AffiliateUrl = affiliateUrl;
    }

    public void SetProduct(string? productId, string? shopId, string? name, string? imageUrl, decimal? estimate)
    {
        ProductId = productId;
        ShopId = shopId;
        ProductName = name;
        ImageUrl = imageUrl;
        EstimatedCommission = estimate;
    }

    public void SetShop(string? shopId, string displayName, string? imageUrl)
    {
        ProductId = null;
        ShopId = shopId;
        ProductName = displayName;
        ImageUrl = imageUrl;
        EstimatedCommission = null;
    }

    public void RegisterClick(DateTime at)
    {
        ClickCount++;
        LastClickedAt = at;
    }

    public void Hide(DateTime at)
    {
        IsHidden = true;
        HiddenAt = at;
    }

    public void Show()
    {
        IsHidden = false;
        HiddenAt = null;
    }

    public void MarkFailed() => Status = AffiliateTrackingStatus.Failed;
    public void Disable() => Status = AffiliateTrackingStatus.Disabled;
}
