using System;
using System.Threading.Tasks;

namespace WebHoanTien.Affiliates;

public sealed record ResolvedAffiliateId(string AffiliateId, Guid? OverrideId)
{
    public bool IsOverride => OverrideId.HasValue;
}

public interface IAffiliateIdResolver
{
    Task<ResolvedAffiliateId> ResolveAsync(Guid userId, AffiliatePlatform platform);
}
