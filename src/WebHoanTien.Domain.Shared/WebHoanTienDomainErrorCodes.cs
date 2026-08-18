namespace WebHoanTien;

public static class WebHoanTienDomainErrorCodes
{
    public const string InvalidAffiliateUrl = "WebHoanTien:Affiliate:InvalidUrl";
    public const string UnsafeRedirect = "WebHoanTien:Affiliate:UnsafeRedirect";
    public const string CommissionRuleOverlap = "WebHoanTien:Commission:RuleOverlap";
    public const string CommissionRuleNotFound = "WebHoanTien:Commission:RuleNotFound";
    public const string ProviderNotConfigured = "WebHoanTien:Provider:NotConfigured";
    public const string ProviderRequestFailed = "WebHoanTien:Provider:RequestFailed";
    public const string SyncStartDateRequired = "WebHoanTien:Sync:StartDateRequired";
    public const string SyncRequestCooldown = "WebHoanTien:Sync:Cooldown";
    public const string TrackingNotOwned = "WebHoanTien:Tracking:NotOwned";
}
