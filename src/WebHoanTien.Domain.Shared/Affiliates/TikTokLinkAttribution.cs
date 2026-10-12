using System;

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
}
