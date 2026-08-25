namespace WebHoanTien;

public static class WebHoanTienConsts
{
    public const string DbTablePrefix = "";
    public const string AffiliateDbSchema = "affiliate";
    public const string HangfireDbSchema = "hangfire";
    public const int UrlMaxLength = 2048;
    public const int TrackingTokenLength = 32;
    public const decimal DefaultUserShareRate = 60m;
    public const decimal MinimumWithdrawalAmount = 10_000m;
    public const decimal WithdrawalFeeAmount = 0m;
    public const long MaximumWithdrawalProofSize = 5 * 1024 * 1024;
    public const int RetentionDays = 90;
    public const string TermsVersion = "2026-08-18";
    public const string PrivacyVersion = "2026-08-18";
}
