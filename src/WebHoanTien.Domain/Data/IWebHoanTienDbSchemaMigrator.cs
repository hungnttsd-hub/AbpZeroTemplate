using System.Threading.Tasks;

namespace WebHoanTien.Data;

public interface IWebHoanTienDbSchemaMigrator
{
    Task MigrateAsync();
}
