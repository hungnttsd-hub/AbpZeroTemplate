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

    public void SetProduct(string? productId, string? shopId, string? name, string? imageUrl, decimal? estimate)
    {
        ProductId = productId;
        ShopId = shopId;
        ProductName = name;
        ImageUrl = imageUrl;
        EstimatedCommission = estimate;
    }

    public void RegisterClick(DateTime at)
    {
        ClickCount++;
        LastClickedAt = at;
    }

    public void MarkFailed() => Status = AffiliateTrackingStatus.Failed;
    public void Disable() => Status = AffiliateTrackingStatus.Disabled;
}

public class AffiliateClick : CreationAuditedEntity<Guid>
{
    public Guid TrackingId { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime ClickedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Referer { get; private set; }

    protected AffiliateClick() { }

    public AffiliateClick(Guid id, Guid trackingId, Guid? userId, DateTime clickedAt, string? ipAddress, string? userAgent, string? referer)
        : base(id)
    {
        TrackingId = trackingId;
        UserId = userId;
        ClickedAt = clickedAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Referer = referer;
    }

    public void PurgePersonalData()
    {
        IpAddress = null;
        UserAgent = null;
        Referer = null;
    }
}

public class AffiliateConversion : FullAuditedAggregateRoot<Guid>
{
    public AffiliatePlatform Platform { get; private set; }
    public string ExternalConversionId { get; private set; } = null!;
    public Guid? TrackingId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? AttributionValue { get; private set; }
    public DateTime PurchaseTime { get; private set; }
    public DateTime? ClickTime { get; private set; }
    public AffiliateConversionStatus Status { get; private set; }
    public decimal GrossCommission { get; private set; }
    public decimal NetCommission { get; private set; }
    public CommissionSource CommissionSource { get; private set; }
    public decimal UserShareRate { get; private set; }
    public decimal UserCommissionSnapshot { get; private set; }
    public decimal PlatformRevenueSnapshot { get; private set; }
    public decimal PayableUserCommission { get; private set; }
    public DateTime LastProviderUpdateAt { get; private set; }

    protected AffiliateConversion() { }

    public AffiliateConversion(Guid id, AffiliatePlatform platform, string externalId, DateTime purchaseTime)
        : base(id)
    {
        Platform = platform;
        ExternalConversionId = externalId;
        PurchaseTime = purchaseTime;
        Status = AffiliateConversionStatus.Estimated;
        LastProviderUpdateAt = purchaseTime;
    }

    public void MapTo(Guid trackingId, Guid userId, string? attributionValue)
    {
        TrackingId = trackingId;
        UserId = userId;
        AttributionValue = attributionValue;
    }

    public void SetClickTime(DateTime? value) => ClickTime = value;

    public void ApplyCommission(decimal gross, decimal net, CommissionSource source, decimal userShareRate)
    {
        GrossCommission = gross;
        NetCommission = net;
        CommissionSource = source;
        UserShareRate = userShareRate;
        UserCommissionSnapshot = decimal.Round(net * userShareRate / 100m, 0, MidpointRounding.AwayFromZero);
        PlatformRevenueSnapshot = net - UserCommissionSnapshot;
        RefreshPayable();
    }

    public void ChangeStatus(AffiliateConversionStatus status, DateTime providerUpdateAt)
    {
        Status = status;
        LastProviderUpdateAt = providerUpdateAt;
        RefreshPayable();
    }

    private void RefreshPayable()
    {
        PayableUserCommission = Status is AffiliateConversionStatus.Cancelled
            or AffiliateConversionStatus.Refunded
            or AffiliateConversionStatus.Rejected
            ? 0m
            : UserCommissionSnapshot;
    }
}

public class AffiliateOrder : FullAuditedAggregateRoot<Guid>
{
    public Guid ConversionId { get; private set; }
    public string ExternalOrderId { get; private set; } = null!;
    public AffiliateOrderStatus Status { get; private set; }
    public string? ShopType { get; private set; }
    public decimal PurchaseAmount { get; private set; }
    public decimal NetCommission { get; private set; }
    public decimal UserCommissionSnapshot { get; private set; }
    public decimal PayableUserCommission { get; private set; }

    protected AffiliateOrder() { }

    public AffiliateOrder(Guid id, Guid conversionId, string externalOrderId) : base(id)
    {
        ConversionId = conversionId;
        ExternalOrderId = externalOrderId;
        Status = AffiliateOrderStatus.Unpaid;
    }

    public void Update(AffiliateOrderStatus status, string? shopType, decimal purchaseAmount, decimal netCommission, decimal userCommission)
    {
        Status = status;
        ShopType = shopType;
        PurchaseAmount = purchaseAmount;
        NetCommission = netCommission;
        UserCommissionSnapshot = userCommission;
        PayableUserCommission = status is AffiliateOrderStatus.Cancelled or AffiliateOrderStatus.Refunded or AffiliateOrderStatus.Rejected
            ? 0m : userCommission;
    }
}

public class AffiliateOrderItem : FullAuditedEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public string ExternalItemId { get; private set; } = null!;
    public string ModelId { get; private set; } = string.Empty;
    public string? ProductName { get; private set; }
    public decimal PurchaseAmount { get; private set; }
    public int Quantity { get; private set; }
    public decimal ItemTotalCommission { get; private set; }
    public decimal AllocatedNetCommission { get; private set; }
    public decimal UserCommissionSnapshot { get; private set; }
    public decimal RefundAmount { get; private set; }
    public bool IsFraud { get; private set; }
    public string? ProviderStatus { get; private set; }

    protected AffiliateOrderItem() { }

    public AffiliateOrderItem(Guid id, Guid orderId, string externalItemId, string? modelId) : base(id)
    {
        OrderId = orderId;
        ExternalItemId = externalItemId;
        ModelId = modelId?.Trim() ?? string.Empty;
    }

    public void Update(string? name, decimal purchaseAmount, int quantity, decimal itemCommission,
        decimal allocatedNet, decimal userCommission, decimal refundAmount, bool isFraud, string? providerStatus)
    {
        ProductName = name;
        PurchaseAmount = purchaseAmount;
        Quantity = quantity;
        ItemTotalCommission = itemCommission;
        AllocatedNetCommission = allocatedNet;
        UserCommissionSnapshot = userCommission;
        RefundAmount = refundAmount;
        IsFraud = isFraud;
        ProviderStatus = providerStatus;
    }
}

public class AffiliateCommissionRule : FullAuditedAggregateRoot<Guid>
{
    public AffiliatePlatform Platform { get; private set; }
    public decimal UserShareRate { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }

    protected AffiliateCommissionRule() { }

    public AffiliateCommissionRule(Guid id, AffiliatePlatform platform, decimal userShareRate, DateTime effectiveFrom, DateTime? effectiveTo)
        : base(id)
    {
        if (userShareRate is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(userShareRate));
        if (effectiveTo.HasValue && effectiveTo <= effectiveFrom) throw new ArgumentOutOfRangeException(nameof(effectiveTo));
        Platform = platform;
        UserShareRate = userShareRate;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = true;
    }

    public bool AppliesAt(DateTime time) => IsActive && EffectiveFrom <= time && (!EffectiveTo.HasValue || time < EffectiveTo.Value);
    public bool Overlaps(DateTime from, DateTime? to) => EffectiveFrom < (to ?? DateTime.MaxValue) && from < (EffectiveTo ?? DateTime.MaxValue);
    public void Deactivate() => IsActive = false;
}

public class AffiliateSyncState : FullAuditedAggregateRoot<Guid>
{
    public AffiliatePlatform Platform { get; private set; }
    public AffiliateSyncKind SyncKind { get; private set; }
    public DateTime? Watermark { get; private set; }
    public DateTime? InitialStartDate { get; private set; }
    public DateTime? LastSucceededAt { get; private set; }
    public string? LastError { get; private set; }

    protected AffiliateSyncState() { }
    public AffiliateSyncState(Guid id, AffiliatePlatform platform, AffiliateSyncKind kind) : base(id) { Platform = platform; SyncKind = kind; }
    public void SetInitialStartDate(DateTime value) => InitialStartDate = value;
    public void Succeeded(DateTime watermark, DateTime at) { Watermark = watermark; LastSucceededAt = at; LastError = null; }
    public void Failed(string error) => LastError = error.Length > 2000 ? error[..2000] : error;
}

public class AffiliateSyncRun : CreationAuditedAggregateRoot<Guid>
{
    public AffiliatePlatform Platform { get; private set; }
    public AffiliateSyncKind SyncKind { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public DateTime RangeFrom { get; private set; }
    public DateTime RangeTo { get; private set; }
    public AffiliateSyncRunStatus Status { get; private set; }
    public int FetchedCount { get; private set; }
    public int InsertedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int UnmatchedCount { get; private set; }
    public int ErrorCount { get; private set; }
    public string? ErrorSummary { get; private set; }

    protected AffiliateSyncRun() { }
    public AffiliateSyncRun(Guid id, AffiliatePlatform platform, AffiliateSyncKind kind, DateTime from, DateTime to, DateTime startedAt) : base(id)
    { Platform = platform; SyncKind = kind; RangeFrom = from; RangeTo = to; StartedAt = startedAt; Status = AffiliateSyncRunStatus.Running; }
    public void Complete(DateTime at, int fetched, int inserted, int updated, int unmatched, int errors, string? summary)
    { FinishedAt = at; FetchedCount = fetched; InsertedCount = inserted; UpdatedCount = updated; UnmatchedCount = unmatched; ErrorCount = errors; ErrorSummary = summary; Status = errors == 0 ? AffiliateSyncRunStatus.Succeeded : AffiliateSyncRunStatus.Failed; }
}

public class AffiliateRawPayload : CreationAuditedEntity<Guid>
{
    public Guid SyncRunId { get; private set; }
    public Guid? ConversionId { get; private set; }
    public string PayloadType { get; private set; } = null!;
    public string SanitizedJson { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }

    protected AffiliateRawPayload() { }
    public AffiliateRawPayload(Guid id, Guid syncRunId, Guid? conversionId, string type, string sanitizedJson, DateTime expiresAt) : base(id)
    { SyncRunId = syncRunId; ConversionId = conversionId; PayloadType = type; SanitizedJson = sanitizedJson; ExpiresAt = expiresAt; }
}

public class UserLegalConsent : CreationAuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string TermsVersion { get; private set; } = null!;
    public string PrivacyVersion { get; private set; } = null!;
    public LegalConsentMethod Method { get; private set; }
    public DateTime ConsentedAt { get; private set; }

    protected UserLegalConsent() { }
    public UserLegalConsent(Guid id, Guid userId, string termsVersion, string privacyVersion, LegalConsentMethod method, DateTime consentedAt) : base(id)
    { UserId = userId; TermsVersion = termsVersion; PrivacyVersion = privacyVersion; Method = method; ConsentedAt = consentedAt; }
}
