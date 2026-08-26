using System;

namespace WebHoanTien.Affiliates;

public static class AffiliateUserShareRatePolicy
{
    public static decimal Resolve(int orderNumber, decimal configuredRate)
    {
        if (orderNumber < 1) throw new ArgumentOutOfRangeException(nameof(orderNumber));
        if (configuredRate is < 0m or > 100m) throw new ArgumentOutOfRangeException(nameof(configuredRate));

        if (orderNumber == 1) return WebHoanTienConsts.FirstOrderUserShareRate;
        if (orderNumber <= WebHoanTienConsts.IntroductoryOrderCount)
            return WebHoanTienConsts.IntroductoryUserShareRate;

        return configuredRate;
    }
}
