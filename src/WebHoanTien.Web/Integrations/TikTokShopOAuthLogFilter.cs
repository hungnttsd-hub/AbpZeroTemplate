using Serilog.Events;

namespace WebHoanTien.Web.Integrations;

public static class TikTokShopOAuthLogFilter
{
    // Hosting request-start logs include the full query string before MVC can disable auditing.
    // Do not persist callback request logs containing authorization codes or state values.
    public static bool ContainsCallbackRequest(LogEvent logEvent)
    {
        foreach (var key in new[] { "Path", "RequestPath" })
        {
            if (logEvent.Properties.TryGetValue(key, out var property)
                && property is ScalarValue { Value: string path }
                && TikTokShopOAuthRoutes.IsPublicEndpoint(path.Split('?')[0]))
                return true;
        }

        return false;
    }
}
