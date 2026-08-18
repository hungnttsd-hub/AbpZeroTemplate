using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace WebHoanTien.Data;

public class WebHoanTienDbMigrationService : ITransientDependency
{
    private readonly IDataSeeder _dataSeeder;
    private readonly IEnumerable<IWebHoanTienDbSchemaMigrator> _dbSchemaMigrators;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebHoanTienDbMigrationService> _logger;
    private readonly IdentityUserManager _userManager;

    public WebHoanTienDbMigrationService(
        IDataSeeder dataSeeder,
        IEnumerable<IWebHoanTienDbSchemaMigrator> dbSchemaMigrators,
        IConfiguration configuration,
        IdentityUserManager userManager,
        ILogger<WebHoanTienDbMigrationService> logger)
    {
        _dataSeeder = dataSeeder;
        _dbSchemaMigrators = dbSchemaMigrators;
        _configuration = configuration;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task MigrateAsync()
    {
        _logger.LogInformation("Bắt đầu migrate database webhoantien...");
        foreach (var migrator in _dbSchemaMigrators)
        {
            await migrator.MigrateAsync();
        }

        var context = new DataSeedContext();
        var email = _configuration["InitialAdmin:Email"];
        var password = _configuration["InitialAdmin:Password"];
        var administratorExisted = !string.IsNullOrWhiteSpace(email) && await _userManager.FindByEmailAsync(email) is not null;
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            context.WithProperty(IdentityDataSeedContributor.AdminEmailPropertyName, email)
                .WithProperty(IdentityDataSeedContributor.AdminPasswordPropertyName, password);
        }
        else
        {
            _logger.LogWarning("InitialAdmin chưa được cấu hình; không seed credential quản trị mặc định.");
        }

        await _dataSeeder.SeedAsync(context);
        if (!string.IsNullOrWhiteSpace(email) && !administratorExisted)
        {
            var administrator = await _userManager.FindByEmailAsync(email);
            if (administrator is not null)
            {
                administrator.SetProperty("MustChangePassword", true);
                await _userManager.UpdateAsync(administrator);
            }
        }
        _logger.LogInformation("Hoàn tất migrate database webhoantien.");
    }
}
