using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebHoanTien.Data;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.EntityFrameworkCore;

public class EntityFrameworkCoreWebHoanTienDbSchemaMigrator
    : IWebHoanTienDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreWebHoanTienDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the WebHoanTienDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<WebHoanTienDbContext>()
            .Database
            .MigrateAsync();
    }
}
