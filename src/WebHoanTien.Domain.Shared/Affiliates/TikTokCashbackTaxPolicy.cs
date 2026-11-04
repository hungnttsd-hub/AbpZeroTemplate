using System;

namespace WebHoanTien.Affiliates;

public static class TikTokCashbackTaxPolicy
{
    public const decimal DefaultTaxPercent = 10m;

    public static decimal AdditionalTax(decimal commission, decimal? withheld = null) =>
        withheld is > 0 ? 0m : decimal.Round(Math.Max(0m, commission) * DefaultTaxPercent / 100m,
            4, MidpointRounding.AwayFromZero);

    public static decimal AfterTax(decimal commission, decimal? withheld = null) =>
        Math.Max(0m, commission - AdditionalTax(commission, withheld));
}
