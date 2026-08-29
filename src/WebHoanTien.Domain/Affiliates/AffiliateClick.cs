using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateClick : CreationAuditedEntity<Guid>
{
    public Guid TrackingId { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime ClickedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Referer { get; private set; }
    public string? AffiliateIdSnapshot { get; private set; }
    public Guid? UserAffiliateIdOverrideId { get; private set; }

    protected AffiliateClick() { }

    public AffiliateClick(Guid id, Guid trackingId, Guid? userId, DateTime clickedAt, string? ipAddress,
        string? userAgent, string? referer, string? affiliateIdSnapshot = null,
        Guid? userAffiliateIdOverrideId = null)
        : base(id)
    {
        TrackingId = trackingId;
        UserId = userId;
        ClickedAt = clickedAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Referer = referer;
        AffiliateIdSnapshot = string.IsNullOrWhiteSpace(affiliateIdSnapshot)
            ? null
            : AffiliateIdRules.Normalize(affiliateIdSnapshot);
        UserAffiliateIdOverrideId = userAffiliateIdOverrideId;
    }

    public void PurgePersonalData()
    {
        IpAddress = null;
        UserAgent = null;
        Referer = null;
    }
}
