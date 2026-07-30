using System.Threading.Tasks;

namespace AbpIoTemplateProject.Data;

public interface IAbpIoTemplateProjectDbSchemaMigrator
{
    Task MigrateAsync();
}
