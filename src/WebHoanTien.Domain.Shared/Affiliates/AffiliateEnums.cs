namespace WebHoanTien.Affiliates;

public enum AffiliatePlatform
{
    Shopee = 1,
    TikTok = 2,
    Lazada = 3
}

public enum AffiliateLinkTargetType
{
    Unknown = 0,
    Product = 1,
    Shop = 2
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
    Rejected = 6,
    Settled = 7
}

public enum AffiliateAttributionStatus
{
    Unmatched = 0,
    Matched = 1,
    Conflict = 2
}

public enum AffiliateSyncKind
{
    Conversion = 1,
    Validation = 2,
    Reconciliation = 3,
    Retention = 4,
    Import = 5
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

public enum WithdrawalRequestStatus
{
    Pending = 1,
    Paid = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum WalletMovementKind
{
    Commission = 1,
    Withdrawal = 2
}

public enum ShopeeSettlementImportSource
{
    Automation = 1,
    Manual = 2
}

public enum ShopeeSettlementBatchStatus
{
    PendingApproval = 1,
    PartiallyApproved = 2,
    Approved = 3,
    CompletedWithIssues = 4,
    WaitingForShopee = 5
}

public enum ShopeeSettlementRecordStatus
{
    PendingApproval = 1,
    Approved = 2,
    Unmatched = 3,
    AlreadySettled = 4,
    Invalid = 5,
    AwaitingShopeePayment = 6
}
