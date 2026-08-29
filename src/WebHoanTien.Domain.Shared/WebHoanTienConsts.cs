namespace WebHoanTien;

public static class WebHoanTienConsts
{
    public const string DbTablePrefix = "";
    public const string AffiliateDbSchema = "affiliate";
    public const string NotificationDbSchema = "notification";
    public const string HangfireDbSchema = "hangfire";
    public const int UrlMaxLength = 2048;
    public const int TrackingTokenLength = 32;
    public const int AffiliateIdMaxLength = 128;
    public const int AffiliateOverrideNoteMaxLength = 500;
    public const decimal DefaultUserShareRate = 60m;
    public const decimal FirstOrderUserShareRate = 100m;
    public const decimal IntroductoryUserShareRate = 80m;
    public const int IntroductoryOrderCount = 2;
    public const decimal MinimumWithdrawalAmount = 10_000m;
    public const decimal WithdrawalFeeAmount = 0m;
    public const long MaximumWithdrawalProofSize = 5 * 1024 * 1024;
    public const int RetentionDays = 90;
    public const int NotificationTitleMaxLength = 160;
    public const int NotificationMessageMaxLength = 700;
    public const int NotificationActionUrlMaxLength = 500;
    public const int NotificationEventKeyMaxLength = 256;
    public const string TermsVersion = "2026-08-18";
    public const string PrivacyVersion = "2026-08-18";
}
