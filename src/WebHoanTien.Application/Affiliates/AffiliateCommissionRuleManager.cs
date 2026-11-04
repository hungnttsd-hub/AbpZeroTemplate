using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace WebHoanTien.Affiliates;

public class AffiliateCommissionRuleManager : ITransientDependency
{
    private readonly IRepository<AffiliateCommissionRule, Guid> _repository;

    public AffiliateCommissionRuleManager(IRepository<AffiliateCommissionRule, Guid> repository)
    {
        _repository = repository;
    }

    public async Task EnsureNoOverlapAsync(AffiliatePlatform platform, DateTime from, DateTime? to, Guid? excludingId = null)
    {
        var rules = await _repository.GetListAsync(x => x.Platform == platform && x.IsActive && (!excludingId.HasValue || x.Id != excludingId));
        if (rules.Any(x => x.Overlaps(from, to)))
        {
            throw new BusinessException(WebHoanTienDomainErrorCodes.CommissionRuleOverlap);
        }
    }

    public async Task<AffiliateCommissionRule> GetForPurchaseAsync(AffiliatePlatform platform, DateTime purchaseTime)
    {
        return ResolveForPurchase(await GetActiveRulesAsync(platform), purchaseTime);
    }

    public Task<List<AffiliateCommissionRule>> GetActiveRulesAsync(AffiliatePlatform platform) =>
        _repository.GetListAsync(x => x.Platform == platform && x.IsActive);

    public static AffiliateCommissionRule ResolveForPurchase(IEnumerable<AffiliateCommissionRule> rules,
        DateTime purchaseTime)
    {
        var rule = rules.Where(x => x.AppliesAt(purchaseTime)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefault();
        return rule ?? throw new BusinessException(WebHoanTienDomainErrorCodes.CommissionRuleNotFound);
    }
}
