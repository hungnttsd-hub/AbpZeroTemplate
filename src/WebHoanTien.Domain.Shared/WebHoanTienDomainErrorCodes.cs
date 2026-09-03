namespace WebHoanTien;

public static class WebHoanTienDomainErrorCodes
{
    public const string InvalidAffiliateUrl = "WebHoanTien:Affiliate:InvalidUrl";
    public const string UnsafeRedirect = "WebHoanTien:Affiliate:UnsafeRedirect";
    public const string AffiliateTargetTypeInvalid = "WebHoanTien:Affiliate:TargetTypeInvalid";
    public const string AffiliateTargetUnsupported = "WebHoanTien:Affiliate:TargetUnsupported";
    public const string AffiliateTargetMismatch = "WebHoanTien:Affiliate:TargetMismatch";
    public const string CommissionRuleOverlap = "WebHoanTien:Commission:RuleOverlap";
    public const string CommissionRuleNotFound = "WebHoanTien:Commission:RuleNotFound";
    public const string ProviderNotConfigured = "WebHoanTien:Provider:NotConfigured";
    public const string ProviderRequestFailed = "WebHoanTien:Provider:RequestFailed";
    public const string SyncStartDateRequired = "WebHoanTien:Sync:StartDateRequired";
    public const string SyncRequestCooldown = "WebHoanTien:Sync:Cooldown";
    public const string TrackingNotOwned = "WebHoanTien:Tracking:NotOwned";
    public const string AffiliateUserNotFound = "WebHoanTien:Affiliate:UserNotFound";
    public const string AffiliateIdOverrideConflict = "WebHoanTien:Affiliate:IdOverrideConflict";
    public const string InvalidShopeeReport = "WebHoanTien:ShopeeReport:Invalid";
    public const string InvalidShopeeSettlementReport = "WebHoanTien:ShopeeSettlementReport:Invalid";
    public const string AffiliateOrderSettlementInvalidState = "WebHoanTien:AffiliateOrder:SettlementInvalidState";
    public const string PayoutAccountRequired = "WebHoanTien:Wallet:PayoutAccountRequired";
    public const string WithdrawalBelowMinimum = "WebHoanTien:Wallet:WithdrawalBelowMinimum";
    public const string WithdrawalInsufficientBalance = "WebHoanTien:Wallet:InsufficientBalance";
    public const string WithdrawalPendingExists = "WebHoanTien:Wallet:PendingExists";
    public const string WithdrawalInvalidState = "WebHoanTien:Wallet:InvalidState";
    public const string WithdrawalNotOwned = "WebHoanTien:Wallet:NotOwned";
    public const string WithdrawalProofInvalid = "WebHoanTien:Wallet:ProofInvalid";
    public const string WithdrawalNotBacked = "WebHoanTien:Wallet:NotBacked";
    public const string NotificationNotOwned = "WebHoanTien:Notification:NotOwned";
    public const string NotificationTargetNotFound = "WebHoanTien:Notification:TargetNotFound";
    public const string NotificationInvalidActionUrl = "WebHoanTien:Notification:InvalidActionUrl";
}
