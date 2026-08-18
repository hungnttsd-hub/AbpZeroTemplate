namespace WebHoanTien.Affiliates;

public enum AffiliatePlatform
{
    Shopee = 1,
    TikTok = 2,
    Lazada = 3
}

public enum AffiliateTrackingStatus
{
    Active = 1,
    Disabled = 2,
    Failed = 3
}

public enum AffiliateConversionStatus
{
    Estimated = 1,
    Pending = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5,
    Refunded = 6
}

public enum AffiliateOrderStatus
{
    Unpaid = 1,
    Pending = 2,
    Completed = 3,
    Cancelled = 4,
    Refunded = 5,
    Rejected = 6
}

public enum AffiliateSyncKind
{
    Conversion = 1,
    Validation = 2,
    Reconciliation = 3,
    Retention = 4
}

public enum AffiliateSyncRunStatus
{
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

public enum CommissionSource
{
    NetCommission = 1,
    TotalCommissionFallback = 2
}

public enum LegalConsentMethod
{
    EmailRegistration = 1,
    GoogleRegistration = 2,
    AccountPrompt = 3
}
