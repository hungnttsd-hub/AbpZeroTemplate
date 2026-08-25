using System;
using System.Security.Cryptography;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Affiliates;

public class TrackingTokenGenerator : ITrackingTokenGenerator, ITransientDependency
{
    public string Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(WebHoanTienConsts.TrackingTokenLength / 2);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
