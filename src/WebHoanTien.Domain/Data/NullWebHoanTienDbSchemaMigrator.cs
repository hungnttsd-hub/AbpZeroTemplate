using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Data;

/* This is used if database provider does't define
 * IWebHoanTienDbSchemaMigrator implementation.
 */
public class NullWebHoanTienDbSchemaMigrator : IWebHoanTienDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
