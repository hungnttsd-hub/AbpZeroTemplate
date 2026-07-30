using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AbpIoTemplateProject.Data;
using Volo.Abp.DependencyInjection;

namespace AbpIoTemplateProject.EntityFrameworkCore;

public class EntityFrameworkCoreAbpIoTemplateProjectDbSchemaMigrator
    : IAbpIoTemplateProjectDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreAbpIoTemplateProjectDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the AbpIoTemplateProjectDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<AbpIoTemplateProjectDbContext>()
            .Database
            .MigrateAsync();
    }
}
