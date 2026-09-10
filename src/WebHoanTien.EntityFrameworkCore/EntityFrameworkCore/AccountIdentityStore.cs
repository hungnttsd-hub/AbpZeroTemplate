using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using WebHoanTien.IdentityExtensions;

namespace WebHoanTien.EntityFrameworkCore;

public class AccountIdentityStore : IAccountIdentityStore, ITransientDependency
{
    private readonly IDbContextProvider<WebHoanTienDbContext> _contexts;
    private readonly IUnitOfWorkManager _uow;
    public AccountIdentityStore(IDbContextProvider<WebHoanTienDbContext> contexts, IUnitOfWorkManager uow)
    { _contexts = contexts; _uow = uow; }
    public async Task<IdentityUser?> FindByLoginEmailAsync(string email)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await (await _contexts.GetDbContextAsync()).Users.FirstOrDefaultAsync(x =>
            EF.Property<string>(x, CatBackAccountProperties.NormalizedLoginEmail) == normalized);
    }
    public async Task LockAsync(string key)
    {
        var db = await _contexts.GetDbContextAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))");
    }
    public async Task<bool> ConsumeLimitAsync(string key, int limit, TimeSpan window)
    {
        // Independent commit: a failed authentication attempt must still consume its quota.
        using var scope = _uow.Begin(requiresNew: true, isTransactional: true);
        var db = await _contexts.GetDbContextAsync();
        var now = DateTime.UtcNow;
        var bucket = now.Ticks / window.Ticks;
        var partition = key + ":" + bucket;
        await db.Database.OpenConnectionAsync();
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "INSERT INTO \"CatBackAccountRateLimit\" (\"Key\", \"Count\", \"ExpiresAt\") VALUES (@key, 1, @expires) ON CONFLICT (\"Key\") DO UPDATE SET \"Count\" = \"CatBackAccountRateLimit\".\"Count\" + 1 RETURNING \"Count\"";
        var p = command.CreateParameter(); p.ParameterName = "key"; p.Value = partition; command.Parameters.Add(p);
        var e = command.CreateParameter(); e.ParameterName = "expires"; e.Value = now.Add(window + window); command.Parameters.Add(e);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"CatBackAccountRateLimit\" WHERE \"ExpiresAt\" < {now}");
        await scope.CompleteAsync();
        return count <= limit;
    }
}
