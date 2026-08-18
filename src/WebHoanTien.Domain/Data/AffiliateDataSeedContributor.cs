using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Data;

public class AffiliateDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<AffiliateCommissionRule, Guid> _rules;
    private readonly IdentityRoleManager _roleManager;
    private readonly IGuidGenerator _guidGenerator;

    public AffiliateDataSeedContributor(IRepository<AffiliateCommissionRule, Guid> rules, IdentityRoleManager roleManager, IGuidGenerator guidGenerator)
    { _rules = rules; _roleManager = roleManager; _guidGenerator = guidGenerator; }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (!await _rules.AnyAsync(x => x.Platform == AffiliatePlatform.Shopee))
            await _rules.InsertAsync(new AffiliateCommissionRule(_guidGenerator.Create(), AffiliatePlatform.Shopee,
                WebHoanTienConsts.DefaultUserShareRate, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null), autoSave: true);

        if (!await _roleManager.RoleExistsAsync("User"))
            await _roleManager.CreateAsync(new IdentityRole(_guidGenerator.Create(), "User"));
    }
}
