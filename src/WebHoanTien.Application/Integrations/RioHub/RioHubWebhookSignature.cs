using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WebHoanTien.Integrations.RioHub;

/// <summary>
/// Signature verification only. Call before parsing JSON, then durably deduplicate by event_id.
/// This helper does not acknowledge webhooks or update CatBack balances.
/// </summary>
public static class RioHubWebhookSignature
{
    public static bool Verify(ReadOnlySpan<byte> rawBody, string? signatureHeader, string signingSecret,
        DateTimeOffset now, TimeSpan? timestampTolerance = null)
    {
        if (string.IsNullOrWhiteSpace(signingSecret) || string.IsNullOrWhiteSpace(signatureHeader) ||
            signatureHeader.Length > 512) return false;
        string? timestamp = null;
        string? signature = null;
        foreach (var component in signatureHeader.Split(','))
        {
            var pair = component.Trim().Split('=', 2);
            if (pair.Length != 2) return false;
            switch (pair[0])
            {
                case "t" when timestamp is null: timestamp = pair[1]; break;
                case "v1" when signature is null: signature = pair[1]; break;
                default: return false;
            }
        }
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var unixTime) ||
            signature?.Length != 64) return false;
        // Optional local replay policy; durable event_id deduplication is still required.
        if (timestampTolerance is { } tolerance && (tolerance < TimeSpan.Zero ||
            Math.Abs((decimal)now.ToUnixTimeSeconds() - unixTime) > (decimal)tolerance.TotalSeconds)) return false;
        byte[] suppliedHash;
        try { suppliedHash = Convert.FromHexString(signature); }
        catch (FormatException) { return false; }
        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(signingSecret));
        hmac.AppendData(Encoding.UTF8.GetBytes(timestamp + "."));
        hmac.AppendData(rawBody);
        return CryptographicOperations.FixedTimeEquals(hmac.GetHashAndReset(), suppliedHash);
    }
}
