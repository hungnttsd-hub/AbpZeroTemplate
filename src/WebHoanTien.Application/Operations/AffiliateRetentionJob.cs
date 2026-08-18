using System;
using System.Threading.Tasks;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Operations;

public class AffiliateRetentionJob : IAsyncBackgroundJob<AffiliateRetentionJobArgs>, ITransientDependency
{
    private readonly IRepository<AffiliateRawPayload, Guid> _payloads;
    private readonly IRepository<AffiliateClick, Guid> _clicks;
    private readonly Volo.Abp.Timing.IClock _clock;
    public AffiliateRetentionJob(IRepository<AffiliateRawPayload, Guid> payloads, IRepository<AffiliateClick, Guid> clicks, Volo.Abp.Timing.IClock clock)
    { _payloads = payloads; _clicks = clicks; _clock = clock; }

    [UnitOfWork]
    public async Task ExecuteAsync(AffiliateRetentionJobArgs args)
    {
        var now = _clock.Now;
        await _payloads.DeleteAsync(x => x.ExpiresAt <= now, autoSave: true);
        var clicks = await _clicks.GetListAsync(x => x.ClickedAt <= now.AddDays(-WebHoanTienConsts.RetentionDays) && x.IpAddress != null);
        foreach (var click in clicks)
            click.PurgePersonalData();
        if (clicks.Count > 0) await _clicks.UpdateManyAsync(clicks, autoSave: true);
    }
}

[Serializable]
public sealed class AffiliateRetentionJobArgs { }
