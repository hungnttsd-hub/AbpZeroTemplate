using System;

namespace WebHoanTien.Affiliates;

public static class AffiliateIdRules
{
    public static string Normalize(string affiliateId)
    {
        var normalized = affiliateId?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > WebHoanTienConsts.AffiliateIdMaxLength)
            throw new ArgumentException($"Affiliate ID phải có từ 1 đến {WebHoanTienConsts.AffiliateIdMaxLength} ký tự.", nameof(affiliateId));

        foreach (var character in normalized)
            if (!IsAsciiLetterOrDigit(character) && character is not '_' and not '-')
                throw new ArgumentException("Affiliate ID chỉ được gồm chữ, số, dấu gạch dưới và gạch ngang.", nameof(affiliateId));

        return normalized;
    }

    private static bool IsAsciiLetterOrDigit(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9';
}
