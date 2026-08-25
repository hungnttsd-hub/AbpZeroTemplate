using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace WebHoanTien.Affiliates;

public class AffiliateCommissionRule : FullAuditedAggregateRoot<Guid>
{
    public AffiliatePlatform Platform { get; private set; }
    public decimal UserShareRate { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }

    protected AffiliateCommissionRule() { }

    public AffiliateCommissionRule(Guid id, AffiliatePlatform platform, decimal userShareRate, DateTime effectiveFrom, DateTime? effectiveTo)
        : base(id)
    {
        if (userShareRate is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(userShareRate));
        if (effectiveTo.HasValue && effectiveTo <= effectiveFrom) throw new ArgumentOutOfRangeException(nameof(effectiveTo));
        Platform = platform;
        UserShareRate = userShareRate;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = true;
    }

    public bool AppliesAt(DateTime time) => IsActive && EffectiveFrom <= time && (!EffectiveTo.HasValue || time < EffectiveTo.Value);
    public bool Overlaps(DateTime from, DateTime? to) => EffectiveFrom < (to ?? DateTime.MaxValue) && from < (EffectiveTo ?? DateTime.MaxValue);
    public void ChangeUserShareRate(decimal userShareRate)
    {
        if (userShareRate is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(userShareRate));
        UserShareRate = userShareRate;
    }

    public void CloseAt(DateTime effectiveTo)
    {
        if (effectiveTo <= EffectiveFrom) throw new ArgumentOutOfRangeException(nameof(effectiveTo));
        EffectiveTo = effectiveTo;
    }

    public void Deactivate() => IsActive = false;
}
