using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AbpIoTemplateProject.Data;

/* This is used if database provider does't define
 * IAbpIoTemplateProjectDbSchemaMigrator implementation.
 */
public class NullAbpIoTemplateProjectDbSchemaMigrator : IAbpIoTemplateProjectDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
