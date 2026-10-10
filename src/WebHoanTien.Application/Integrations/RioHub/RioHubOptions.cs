using System;

namespace WebHoanTien.Integrations.RioHub;

public sealed class RioHubOptions
{
    public const string SectionName = "RioHub";
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string CreatorUsername { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://riohub.vn/api/v1";
    public string[] FallbackBaseUrls { get; set; } =
        { "https://riohub.riokupon.com/api/v1", "https://riohub.riokupon.me/api/v1" };
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxRateLimitRetries { get; set; } = 2;
    public int MaxRetryAfterSeconds { get; set; } = 30;
    public bool SyncEnabled { get; set; }
    public string SyncCron { get; set; } = "*/5 * * * *";
    public int InitialSyncLookbackDays { get; set; } = 30;
    public long? InitialSyncFromUnix { get; set; }

    public static bool IsAllowedBaseUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
        uri.IsDefaultPort && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        uri.AbsolutePath.TrimEnd('/') == "/api/v1" &&
        (uri.IdnHost == "riohub.vn" || uri.IdnHost == "riohub.riokupon.com" || uri.IdnHost == "riohub.riokupon.me");
}
