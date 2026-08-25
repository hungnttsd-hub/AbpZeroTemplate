using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WebHoanTien.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class WebHoanTienDbContextFactory : IDesignTimeDbContextFactory<WebHoanTienDbContext>
{
    public WebHoanTienDbContext CreateDbContext(string[] args)
    {
        WebHoanTienEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<WebHoanTienDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Default"));

        return new WebHoanTienDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../WebHoanTien.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.secrets.json", optional: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
