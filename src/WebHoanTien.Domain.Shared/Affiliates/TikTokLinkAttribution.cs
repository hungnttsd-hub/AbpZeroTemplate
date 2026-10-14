using System;
using System.Linq;

namespace WebHoanTien.Affiliates;

public static class TikTokLinkAttribution
{
    public const string Source = "TikTokCreatorApi202505";
    public const string SourceProperty = "TikTokLinkSource";
    public const string CreatorProperty = "TikTokCreatorOpenId";
    public const string OneLinkProperty = "TikTokOneLink";
    public static string EventId(Guid clickId) => "cbclk_" + clickId.ToString("N");
    public static bool TryGetClickId(string? eventId, out Guid id)
    {
        id = default;
        return eventId is { Length: 38 } && eventId.StartsWith("cbclk_", StringComparison.Ordinal)
            && Guid.TryParseExact(eventId[6..], "N", out id);
    }
    public static string AddEventId(string sharingLink, Guid clickId)
    {
        // Keep TikTok's encoded chain_key/trackParams byte-for-byte; append only the documented parameter.
        if (!TikTokAffiliateUrl.TryNormalize(sharingLink, out _, out _)) throw new ArgumentException("Invalid TikTok sharing link.");
        var fragment = sharingLink.IndexOf('#');
        if (fragment >= 0) sharingLink = sharingLink[..fragment];
        return sharingLink + (sharingLink.Contains('?') ? "&" : "?") + "event_id=" + EventId(clickId);
    }

    public static bool TryCreateAppLink(string? oneLink, Guid clickId, out string destination)
    {
        destination = string.Empty;
        // Generate General Sharing Links 202505: replace every o_event_id placeholder
        // in the API's one_link. Preserve its nested encoded parameters byte-for-byte.
        if (string.IsNullOrWhiteSpace(oneLink) || oneLink.Length > 8192 || oneLink.Any(char.IsControl)
            || !Uri.TryCreate(oneLink, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || !uri.IsDefaultPort || uri.UserInfo.Length > 0
            || !uri.IdnHost.Equals("snssdk1180.onelink.me", StringComparison.OrdinalIgnoreCase)
            || !oneLink.Contains("o_event_id", StringComparison.Ordinal)) return false;
        var target = oneLink.Replace("o_event_id", EventId(clickId), StringComparison.Ordinal);
        if (target.Length > 8192) return false;
        destination = target;
        return true;
    }
}
