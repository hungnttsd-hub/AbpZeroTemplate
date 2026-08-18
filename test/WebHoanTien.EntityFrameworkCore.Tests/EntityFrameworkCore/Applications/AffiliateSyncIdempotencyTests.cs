using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using WebHoanTien.Affiliates;
using WebHoanTien.Operations;
using Xunit;

namespace WebHoanTien.EntityFrameworkCore.Applications;

[Collection(WebHoanTienTestConsts.CollectionDefinitionName)]
public class AffiliateSyncIdempotencyTests : WebHoanTienEntityFrameworkCoreTestBase
{
    private readonly AffiliateSyncJob _job;
    private readonly IRepository<AffiliateConversion, Guid> _conversions;
    private readonly IRepository<AffiliateOrder, Guid> _orders;
    private readonly IRepository<AffiliateOrderItem, Guid> _items;

    public AffiliateSyncIdempotencyTests()
    {
        _job = GetRequiredService<AffiliateSyncJob>();
        _conversions = GetRequiredService<IRepository<AffiliateConversion, Guid>>();
        _orders = GetRequiredService<IRepository<AffiliateOrder, Guid>>();
        _items = GetRequiredService<IRepository<AffiliateOrderItem, Guid>>();
    }

    [Fact]
    public async Task Repeated_Sync_Should_Upsert_Without_Duplicates()
    {
        var args = new AffiliateSyncJobArgs
        {
            Platform = AffiliatePlatform.Shopee,
            Kind = AffiliateSyncKind.Conversion,
            From = DateTime.UtcNow.AddHours(-1),
            To = DateTime.UtcNow
        };

        await WithUnitOfWorkAsync(() => _job.ExecuteAsync(args));
        await WithUnitOfWorkAsync(() => _job.ExecuteAsync(args));

        await WithUnitOfWorkAsync(async () =>
        {
            var conversions = await _conversions.GetListAsync(x => x.ExternalConversionId == "TEST-CONVERSION");
            conversions.Count.ShouldBe(1);
            conversions[0].UserCommissionSnapshot.ShouldBe(70_000m);
            (await _orders.GetListAsync(x => x.ConversionId == conversions[0].Id)).Count.ShouldBe(1);
            (await _items.GetListAsync()).Count(x => x.ExternalItemId == "TEST-ITEM" && x.ModelId == "MODEL").ShouldBe(1);
        });
    }
}
