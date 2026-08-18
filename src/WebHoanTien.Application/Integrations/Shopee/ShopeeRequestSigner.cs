using System;
using System.Security.Cryptography;
using System.Text;

namespace WebHoanTien.Integrations.Shopee;

public static class ShopeeRequestSigner
{
    public static string Sign(string appId, long timestamp, string exactPayload, string secret)
    {
        var value = appId + timestamp + exactPayload + secret;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    public static string CreateAuthorization(string appId, long timestamp, string signature) =>
        $"SHA256 Credential={appId}, Signature={signature}, Timestamp={timestamp}";
}
