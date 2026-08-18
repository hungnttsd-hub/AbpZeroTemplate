using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WebHoanTien.Integrations.Shopee;

public static class ShopeeRequestSigner
{
    /// <summary>
    /// Generates the HMAC-SHA256 signature used by Shopee Open Platform
    /// shop-level APIs, including /api/v2/ams/get_conversion_report.
    /// The same timestamp passed here must also be sent in the request query.
    /// </summary>
    public static string GetSignShopee(
        string partnerId,
        string apiPath,
        long timestamp,
        string accessToken,
        string shopId,
        string partnerKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerKey);

        if (!apiPath.StartsWith("/", StringComparison.Ordinal))
        {
            throw new ArgumentException("Shopee API path must start with '/'.", nameof(apiPath));
        }

        if (timestamp <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timestamp), "Timestamp must be a positive Unix timestamp.");
        }

        var baseString = string.Concat(
            partnerId,
            apiPath,
            timestamp.ToString(CultureInfo.InvariantCulture),
            accessToken,
            shopId);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(partnerKey));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        return Convert.ToHexString(signature).ToLowerInvariant();
    }

    public static string Sign(string appId, long timestamp, string exactPayload, string secret)
    {
        var value = appId + timestamp + exactPayload + secret;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    public static string CreateAuthorization(string appId, long timestamp, string signature) =>
        $"SHA256 Credential={appId}, Signature={signature}, Timestamp={timestamp}";
}
