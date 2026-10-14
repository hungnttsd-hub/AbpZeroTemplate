using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;
using WebHoanTien.EntityFrameworkCore;

namespace WebHoanTien.TikTokAffiliate;

[DisableAuditing]
public class EfCoreTikTokCreatorCredentialRepository : ITikTokCreatorCredentialRepository, ITransientDependency
{
    private readonly IDbContextProvider<WebHoanTienDbContext> _contexts;
    private readonly IUnitOfWorkManager _uow;
    public EfCoreTikTokCreatorCredentialRepository(IDbContextProvider<WebHoanTienDbContext> contexts, IUnitOfWorkManager uow)
    { _contexts = contexts; _uow = uow; }

    private async Task<T> Run<T>(Func<WebHoanTienDbContext, Task<T>> action, CancellationToken ct, bool transaction = false)
    {
        using var work = _uow.Begin(requiresNew: true, isTransactional: transaction);
        var result = await action(await _contexts.GetDbContextAsync());
        await work.CompleteAsync(ct);
        return result;
    }

    public Task CreateStateAsync(TikTokCreatorAuthState state, CancellationToken ct) => Run(async db =>
    { db.TikTokCreatorAuthStates.Add(state); await db.SaveChangesAsync(ct); return true; }, ct);

    public Task<TikTokCreatorAuthState?> ConsumeStateAsync(string hash, string appKey, CancellationToken ct) => Run(async db =>
    {
        // Database time and a conditional UPDATE make state single-use across instances.
        var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokCreatorAuthStates"
            SET "Status" = 'Processing', "UsedAtUtc" = CURRENT_TIMESTAMP
            WHERE "StateHash" = {hash} AND "AppKey" = {appKey} AND "Status" = 'Pending'
              AND "UsedAtUtc" IS NULL AND "ExpiresAtUtc" > CURRENT_TIMESTAMP
            """, ct);
        return changed == 1 ? await db.TikTokCreatorAuthStates.AsNoTracking().SingleAsync(x => x.StateHash == hash, ct) : null;
    }, ct, transaction: true);

    public Task FailStateAsync(Guid id, CancellationToken ct) => Run(db => db.TikTokCreatorAuthStates
        .Where(x => x.Id == id && x.Status == "Processing")
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Failed"), ct), ct);

    public Task<TikTokCreatorCredentials?> GetPrimaryAsync(string appKey, CancellationToken ct) => Run(db =>
        db.TikTokCreatorCredentials.AsNoTracking().SingleOrDefaultAsync(x => x.AppKey == appKey && x.IsPrimary, ct), ct);

    public Task DisablePrimaryAsync(string appKey, CancellationToken ct) => Run(async db =>
    {
        // Serialize against reauthorization and invalidate in-flight callbacks/refresh writes.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({appKey}, 0))", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokCreatorCredentials"
            SET "Status" = 'Disabled', "AccessTokenEncrypted" = '', "RefreshTokenEncrypted" = '',
                "TokenVersion" = "TokenVersion" + 1, "RefreshLockId" = NULL, "RefreshLockUntilUtc" = NULL,
                "UpdatedAtUtc" = CURRENT_TIMESTAMP
            WHERE "AppKey" = {appKey} AND "IsPrimary" = TRUE
            """, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokCreatorAuthStates"
            SET "Status" = 'Failed', "UsedAtUtc" = COALESCE("UsedAtUtc", CURRENT_TIMESTAMP)
            WHERE "AppKey" = {appKey} AND "Status" IN ('Pending', 'Processing')
            """, ct);
        return true;
    }, ct, transaction: true);

    public Task SaveAuthorizationAsync(TikTokCreatorAuthState state, TikTokCreatorCredentials incoming, CancellationToken ct) => Run(async db =>
    {
        // Serialize the first connection and subsequent reauthorizations per app, including two admins.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({incoming.AppKey}, 0))", ct);
        var session = await db.TikTokCreatorAuthStates.SingleAsync(x => x.Id == state.Id, ct);
        if (session.Status != "Processing") throw new UserFriendlyException("Phiên cấp quyền không còn hợp lệ.");
        var candidates = await db.TikTokCreatorCredentials.FromSqlInterpolated($"""
            SELECT * FROM affiliate."TikTokCreatorCredentials" WHERE "AppKey" = {incoming.AppKey} AND "IsPrimary" = TRUE FOR UPDATE
            """).ToListAsync(ct);
        var current = candidates.SingleOrDefault();
        if (current is not null && current.OpenId != incoming.OpenId)
            throw new UserFriendlyException("Tài khoản vừa cấp quyền khác Creator chính. CatBack giữ nguyên tài khoản hiện tại; hãy authorize lại đúng Creator.");
        if (current is null)
        {
            incoming.IsPrimary = true;
            incoming.TokenVersion = 1;
            db.TikTokCreatorCredentials.Add(incoming);
        }
        else
        {
            current.AccessTokenEncrypted = incoming.AccessTokenEncrypted;
            current.RefreshTokenEncrypted = incoming.RefreshTokenEncrypted;
            current.AccessTokenExpiresAtUtc = incoming.AccessTokenExpiresAtUtc;
            current.RefreshTokenExpiresAtUtc = incoming.RefreshTokenExpiresAtUtc;
            current.GrantedScopesJson = incoming.GrantedScopesJson;
            current.UserType = incoming.UserType;
            current.Status = incoming.Status;
            current.AuthorizedByAdminId = state.InitiatedByAdminId;
            current.UpdatedAtUtc = incoming.UpdatedAtUtc;
            current.TokenVersion++;
            current.RefreshLockId = null;
            current.RefreshLockUntilUtc = null;
        }
        session.Status = "Completed";
        await db.SaveChangesAsync(ct);
        return true;
    }, ct, transaction: true);

    public Task<bool> AcquireRefreshAsync(Guid id, long version, Guid leaseId, CancellationToken ct) => Run(async db =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokCreatorCredentials"
            SET "RefreshLockId" = {leaseId}, "RefreshLockUntilUtc" = CURRENT_TIMESTAMP + INTERVAL '60 seconds'
            WHERE "Id" = {id} AND "TokenVersion" = {version} AND "Status" IN ('Active', 'MissingScopes')
              AND ("RefreshLockUntilUtc" IS NULL OR "RefreshLockUntilUtc" <= CURRENT_TIMESTAMP)
            """, ct) == 1, ct);

    public Task<bool> SaveRefreshAsync(TikTokCreatorCredentials value, long version, Guid leaseId, CancellationToken ct) => Run(async db =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokCreatorCredentials"
            SET "AccessTokenEncrypted" = {value.AccessTokenEncrypted}, "RefreshTokenEncrypted" = {value.RefreshTokenEncrypted},
                "AccessTokenExpiresAtUtc" = {value.AccessTokenExpiresAtUtc}, "RefreshTokenExpiresAtUtc" = {value.RefreshTokenExpiresAtUtc},
                "GrantedScopesJson" = {value.GrantedScopesJson}, "Status" = {value.Status}, "UserType" = {value.UserType},
                "TokenVersion" = "TokenVersion" + 1, "LastRefreshedAtUtc" = CURRENT_TIMESTAMP,
                "UpdatedAtUtc" = CURRENT_TIMESTAMP, "RefreshLockId" = NULL, "RefreshLockUntilUtc" = NULL
            WHERE "Id" = {value.Id} AND "TokenVersion" = {version} AND "RefreshLockId" = {leaseId}
              AND "RefreshLockUntilUtc" > CURRENT_TIMESTAMP
            """, ct) == 1, ct);

    public Task ReleaseRefreshAsync(Guid id, long version, Guid leaseId, CancellationToken ct) => Run(db =>
        db.TikTokCreatorCredentials.Where(x => x.Id == id && x.TokenVersion == version && x.RefreshLockId == leaseId)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.RefreshLockId, (Guid?)null)
            .SetProperty(x => x.RefreshLockUntilUtc, (DateTimeOffset?)null), ct), ct);

    public Task DeferRefreshAsync(Guid id, long version, Guid leaseId, int seconds, CancellationToken ct) => Run(db =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokCreatorCredentials"
            SET "RefreshLockId" = NULL, "RefreshLockUntilUtc" = CURRENT_TIMESTAMP + {seconds} * INTERVAL '1 second'
            WHERE "Id" = {id} AND "TokenVersion" = {version} AND "RefreshLockId" = {leaseId}
              AND "RefreshLockUntilUtc" > CURRENT_TIMESTAMP
            """, ct), ct);

    public Task MarkReauthAsync(Guid id, long version, Guid? leaseId, CancellationToken ct) => Run(db =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokCreatorCredentials"
            SET "Status" = 'ReauthRequired', "TokenVersion" = "TokenVersion" + 1, "UpdatedAtUtc" = CURRENT_TIMESTAMP,
                "RefreshLockId" = NULL, "RefreshLockUntilUtc" = NULL
            WHERE "Id" = {id} AND "TokenVersion" = {version}
              AND (({leaseId}::uuid IS NULL AND "RefreshLockId" IS NULL)
                   OR ("RefreshLockId" = {leaseId}::uuid AND "RefreshLockUntilUtc" > CURRENT_TIMESTAMP))
            """, ct), ct);
}
