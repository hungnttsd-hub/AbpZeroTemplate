using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace WebHoanTien.EntityFrameworkCore;

public class WebHoanTienEntityFrameworkCoreFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("webhoantien_tests")
        .WithUsername("webhoantien")
        .WithPassword("test-only-password")
        .Build();

    public static string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", ConnectionString);

        var options = new DbContextOptionsBuilder<WebHoanTienDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var db = new WebHoanTienDbContext(options);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", null);
        await _container.DisposeAsync();
    }
}
