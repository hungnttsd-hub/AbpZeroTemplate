using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AbpIoTemplateProject.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class AbpIoTemplateProjectDbContextFactory : IDesignTimeDbContextFactory<AbpIoTemplateProjectDbContext>
{
    public AbpIoTemplateProjectDbContext CreateDbContext(string[] args)
    {
        AbpIoTemplateProjectEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<AbpIoTemplateProjectDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Default"));

        return new AbpIoTemplateProjectDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AbpIoTemplateProject.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
