using System.Linq;
using Serilog.Events;

namespace WebHoanTien.Web.Integrations;

public static class TikTokShopOAuthLogFilter
{
    // Hosting request-start logs include the full query string before MVC can disable auditing.
    // Do not persist callback request logs containing authorization codes or state values.
    public static bool ContainsCallbackRequest(LogEvent logEvent)
    {
        if (IsSafeDiagnostic(logEvent))
        {
            // ABP enriches events with the callback URL. Keep only the explicitly safe
            // fields so the diagnostic survives this filter without persisting that context.
            foreach (var key in logEvent.Properties.Keys.ToArray())
                if (key is not ("SourceContext" or "EventId" or "Stage" or "Status" or "Code"
                    or "RequestId" or "TokenSummary" or "StateCount" or "StateLength"
                    or "HasStateCookie" or "CookieLength" or "StateMatches" or "HasCode"))
                    logEvent.RemovePropertyIfPresent(key);
            return false;
        }
        foreach (var key in new[] { "Path", "RequestPath" })
        {
            if (logEvent.Properties.TryGetValue(key, out var property)
                && property is ScalarValue { Value: string path }
                && (TikTokShopOAuthRoutes.IsPublicEndpoint(path.Split('?')[0]) ||
                    WebHoanTien.TikTokAffiliate.TikTokDeveloperAccess.IsApiPath(path.Split('?')[0]) ||
                    path.StartsWith(WebHoanTien.TikTokAffiliate.TikTokDeveloperAccess.Portal, System.StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        // MVC can log RedirectResult's Destination, including the bearer OAuth state.
        // The callback no longer relies on a browser state cookie, so keep that URL out of logs.
        foreach (var property in logEvent.Properties.Values)
            if (property is ScalarValue { Value: string value }
                && value.Contains("https://shop.tiktok.com/alliance/creator/auth?", System.StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private static bool IsSafeDiagnostic(LogEvent logEvent)
    {
        if (logEvent.Exception is not null
            || !logEvent.Properties.TryGetValue("SourceContext", out var source)
            || source is not ScalarValue { Value: string sourceName }
            || !logEvent.Properties.TryGetValue("EventId", out var eventId)
            || eventId is not StructureValue structure) return false;
        var id = structure.Properties.Where(p => p.Name == "Id").Select(p => p.Value).FirstOrDefault();
        var name = structure.Properties.Where(p => p.Name == "Name").Select(p => p.Value).FirstOrDefault();
        return id is ScalarValue { Value: int number } && name is ScalarValue { Value: string eventName }
            && ((number == 41001 && eventName == "TikTokTokenResponseDiagnostic"
                && sourceName == "WebHoanTien.Web.Integrations.TikTokCreatorOAuthClient")
                || (number == 41002 && eventName == "TikTokOAuthCallbackDiagnostic"
                && sourceName == "WebHoanTien.Web.Controllers.TikTokShopOAuthController"));
    }
}
