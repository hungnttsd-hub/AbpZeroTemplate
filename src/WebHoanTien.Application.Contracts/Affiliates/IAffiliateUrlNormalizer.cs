namespace WebHoanTien.Affiliates;

public interface IAffiliateUrlNormalizer
{
    bool TryNormalize(string input, out string normalizedUrl, out string? itemId);
}
