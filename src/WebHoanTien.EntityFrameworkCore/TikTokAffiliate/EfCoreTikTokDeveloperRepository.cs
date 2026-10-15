using System;
using System.Collections.Generic;
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
public class EfCoreTikTokDeveloperRepository : ITikTokDeveloperRepository, ITransientDependency
{
    private readonly IDbContextProvider<WebHoanTienDbContext> _contexts;
    private readonly IUnitOfWorkManager _uow;
    public EfCoreTikTokDeveloperRepository(IDbContextProvider<WebHoanTienDbContext> contexts, IUnitOfWorkManager uow)
    { _contexts = contexts; _uow = uow; }
    private async Task<T> Run<T>(Func<WebHoanTienDbContext, Task<T>> action, CancellationToken ct, bool transaction = false)
    {
        using var work = _uow.Begin(requiresNew: true, isTransactional: transaction);
        var result = await action(await _contexts.GetDbContextAsync());
        await work.CompleteAsync(ct);
        return result;
    }
    private static Task Lock(WebHoanTienDbContext db, Guid owner, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"developer:" + owner.ToString("N")}, 0))", ct);

    private static async Task<TikTokDeveloperAccount> Ensure(WebHoanTienDbContext db, Guid owner, string appKey, CancellationToken ct)
    {
        var account = await db.TikTokDeveloperAccounts.SingleOrDefaultAsync(x => x.Id == owner, ct);
        if (account is null)
        {
            account = new(owner) { AppKey = appKey, UpdatedAtUtc = DateTimeOffset.UtcNow };
            db.TikTokDeveloperAccounts.Add(account);
        }
        else if (account.AppKey != appKey)
            throw new UserFriendlyException("App key đã thay đổi. Liên hệ quản trị để chuyển kết nối Developer API.");
        return account;
    }

    public Task<TikTokDeveloperAccount?> FindAccountAsync(Guid owner, string appKey, CancellationToken ct) => Run(db =>
        db.TikTokDeveloperAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == owner && x.AppKey == appKey, ct), ct);

    public Task BeginAuthorizationAsync(TikTokDeveloperAuthState state, CancellationToken ct) => Run(async db =>
    {
        await Lock(db, state.OwnerUserId, ct);
        var account = await Ensure(db, state.OwnerUserId, state.AppKey, ct);
        account.AuthorizationEpoch++;
        state.AuthorizationEpoch = account.AuthorizationEpoch;
        await db.TikTokDeveloperAuthStates.Where(x => x.OwnerUserId == state.OwnerUserId && x.AppKey == state.AppKey &&
            (x.Status == "Pending" || x.Status == "Processing")).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Failed"), ct);
        db.TikTokDeveloperAuthStates.Add(state);
        await db.SaveChangesAsync(ct);
        return true;
    }, ct, true);

    public Task<TikTokDeveloperAuthState?> ConsumeStateAsync(Guid owner, string appKey, string stateHash, CancellationToken ct) => Run(async db =>
    {
        var count = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokDeveloperAuthStates" SET "Status" = 'Processing'
            WHERE "OwnerUserId" = {owner} AND "AppKey" = {appKey} AND "StateHash" = {stateHash}
              AND "Status" = 'Pending' AND "ExpiresAtUtc" > CURRENT_TIMESTAMP
            """, ct);
        return count == 1 ? await db.TikTokDeveloperAuthStates.AsNoTracking().SingleAsync(x => x.StateHash == stateHash, ct) : null;
    }, ct, true);

    public Task SaveAuthorizationAsync(TikTokDeveloperAuthState state, TikTokDeveloperAccount incoming, CancellationToken ct) => Run(async db =>
    {
        await Lock(db, state.OwnerUserId, ct);
        var current = await Ensure(db, state.OwnerUserId, state.AppKey, ct);
        var session = await db.TikTokDeveloperAuthStates.SingleAsync(x => x.Id == state.Id && x.OwnerUserId == incoming.Id, ct);
        if (session.Status != "Processing" || current.AuthorizationEpoch != state.AuthorizationEpoch)
            throw new UserFriendlyException("Phiên cấp quyền đã bị hủy. Hãy bắt đầu lại từ Developer API.");
        Apply(current, incoming);
        current.TokenVersion++;
        current.RefreshLeaseId = null; current.RefreshLeaseUntilUtc = null;
        session.Status = "Completed";
        await db.SaveChangesAsync(ct);
        return true;
    }, ct, true);

    private static void Apply(TikTokDeveloperAccount current, TikTokDeveloperAccount incoming)
    {
        current.OpenId = incoming.OpenId; current.Username = incoming.Username;
        current.AccessTokenEncrypted = incoming.AccessTokenEncrypted; current.RefreshTokenEncrypted = incoming.RefreshTokenEncrypted;
        current.AccessExpiresAtUtc = incoming.AccessExpiresAtUtc; current.RefreshExpiresAtUtc = incoming.RefreshExpiresAtUtc;
        current.GrantedScopesJson = incoming.GrantedScopesJson; current.Status = incoming.Status;
        current.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Task<bool> AcquireRefreshAsync(Guid owner, string appKey, long version, Guid lease, CancellationToken ct) => Run(async db =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokDeveloperAccounts"
            SET "RefreshLeaseId" = {lease}, "RefreshLeaseUntilUtc" = CURRENT_TIMESTAMP + INTERVAL '60 seconds'
            WHERE "Id" = {owner} AND "AppKey" = {appKey} AND "TokenVersion" = {version} AND "Status" = 'Active'
              AND ("RefreshLeaseUntilUtc" IS NULL OR "RefreshLeaseUntilUtc" <= CURRENT_TIMESTAMP)
            """, ct) == 1, ct);

    public Task<bool> SaveRefreshAsync(TikTokDeveloperAccount incoming, long version, Guid lease, CancellationToken ct) => Run(async db =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokDeveloperAccounts"
            SET "AccessTokenEncrypted" = {incoming.AccessTokenEncrypted}, "RefreshTokenEncrypted" = {incoming.RefreshTokenEncrypted},
                "AccessExpiresAtUtc" = {incoming.AccessExpiresAtUtc}, "RefreshExpiresAtUtc" = {incoming.RefreshExpiresAtUtc},
                "GrantedScopesJson" = {incoming.GrantedScopesJson}, "Status" = 'Active', "TokenVersion" = "TokenVersion" + 1,
                "RefreshLeaseId" = NULL, "RefreshLeaseUntilUtc" = NULL, "UpdatedAtUtc" = CURRENT_TIMESTAMP
            WHERE "Id" = {incoming.Id} AND "AppKey" = {incoming.AppKey} AND "TokenVersion" = {version}
              AND "Status" = 'Active' AND "RefreshLeaseId" = {lease} AND "RefreshLeaseUntilUtc" > CURRENT_TIMESTAMP
            """, ct) == 1, ct);

    public Task ReleaseRefreshAsync(Guid owner, string appKey, long version, Guid lease, int cooldownSeconds, CancellationToken ct) => Run(db =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokDeveloperAccounts"
            SET "RefreshLeaseId" = NULL, "RefreshLeaseUntilUtc" = CURRENT_TIMESTAMP + {cooldownSeconds} * INTERVAL '1 second'
            WHERE "Id" = {owner} AND "AppKey" = {appKey} AND "TokenVersion" = {version} AND "RefreshLeaseId" = {lease}
            """, ct), ct);

    public Task MarkReauthAsync(Guid owner, string appKey, long version, CancellationToken ct) => Run(db =>
        db.TikTokDeveloperAccounts.Where(x => x.Id == owner && x.AppKey == appKey && x.TokenVersion == version && x.Status == "Active")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "ReauthRequired").SetProperty(x => x.TokenVersion, x => x.TokenVersion + 1), ct), ct);

    public Task DisconnectAsync(Guid owner, string appKey, CancellationToken ct) => Run(async db =>
    {
        await Lock(db, owner, ct);
        await db.TikTokDeveloperAccounts.Where(x => x.Id == owner && x.AppKey == appKey).ExecuteUpdateAsync(s => s
            .SetProperty(x => x.Status, "Disabled").SetProperty(x => x.AccessTokenEncrypted, "").SetProperty(x => x.RefreshTokenEncrypted, "")
            .SetProperty(x => x.OpenId, (string?)null).SetProperty(x => x.Username, (string?)null).SetProperty(x => x.GrantedScopesJson, "[]")
            .SetProperty(x => x.TokenVersion, x => x.TokenVersion + 1).SetProperty(x => x.AuthorizationEpoch, x => x.AuthorizationEpoch + 1)
            .SetProperty(x => x.RefreshLeaseId, (Guid?)null).SetProperty(x => x.RefreshLeaseUntilUtc, (DateTimeOffset?)null), ct);
        await db.TikTokDeveloperAuthStates.Where(x => x.OwnerUserId == owner && x.AppKey == appKey &&
            (x.Status == "Pending" || x.Status == "Processing")).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Failed"), ct);
        return true;
    }, ct, true);

    public async Task<IReadOnlyList<TikTokDeveloperApiKey>> GetKeysAsync(Guid owner, string appKey, CancellationToken ct) => await Run(db =>
        db.TikTokDeveloperApiKeys.AsNoTracking().Where(x => x.OwnerUserId == owner && x.AppKey == appKey)
            .OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct), ct);

    public Task CreateKeyAsync(TikTokDeveloperApiKey key, CancellationToken ct) => Run(async db =>
    {
        await Lock(db, key.OwnerUserId, ct);
        await Ensure(db, key.OwnerUserId, key.AppKey, ct);
        if (await db.TikTokDeveloperApiKeys.CountAsync(x => x.OwnerUserId == key.OwnerUserId && x.AppKey == key.AppKey &&
            x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTimeOffset.UtcNow, ct) >= 10)
            throw new UserFriendlyException("Mỗi tài khoản có tối đa 10 API key đang hoạt động. Thu hồi key cũ trước khi tạo thêm.");
        db.TikTokDeveloperApiKeys.Add(key);
        await db.SaveChangesAsync(ct);
        return true;
    }, ct, true);

    public Task<TikTokDeveloperApiKey?> FindKeyAsync(string hash, string appKey, CancellationToken ct) => Run(db =>
        db.TikTokDeveloperApiKeys.AsNoTracking().SingleOrDefaultAsync(x => x.KeyHash == hash && x.AppKey == appKey &&
            x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTimeOffset.UtcNow, ct), ct);

    public Task RevokeKeyAsync(Guid owner, Guid keyId, string appKey, CancellationToken ct) => Run(db =>
        db.TikTokDeveloperApiKeys.Where(x => x.Id == keyId && x.OwnerUserId == owner && x.AppKey == appKey && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, DateTimeOffset.UtcNow), ct), ct);

    public Task<bool> ConsumeQuotaAsync(Guid owner, string appKey, int perMinute, CancellationToken ct) => Run(async db =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE affiliate."TikTokDeveloperAccounts"
            SET "RateCount" = CASE WHEN "RateWindowUtc" IS NULL OR "RateWindowUtc" <= CURRENT_TIMESTAMP - INTERVAL '1 minute'
                                  THEN 1 ELSE "RateCount" + 1 END,
                "RateWindowUtc" = CASE WHEN "RateWindowUtc" IS NULL OR "RateWindowUtc" <= CURRENT_TIMESTAMP - INTERVAL '1 minute'
                                      THEN CURRENT_TIMESTAMP ELSE "RateWindowUtc" END
            WHERE "Id" = {owner} AND "AppKey" = {appKey} AND
              ("RateWindowUtc" IS NULL OR "RateWindowUtc" <= CURRENT_TIMESTAMP - INTERVAL '1 minute' OR "RateCount" < {perMinute})
            """, ct) == 1, ct);

    public Task<TikTokDeveloperAdminPage> GetAdminPageAsync(string appKey, string? search, string? status, int page, CancellationToken ct) => Run(async db =>
    {
        var now = DateTimeOffset.UtcNow;
        var accounts = db.TikTokDeveloperAccounts.AsNoTracking().Where(x => x.AppKey == appKey);
        var connected = accounts.Where(x => x.Status == "Active" && x.OpenId != null && x.RefreshExpiresAtUtc > now);
        var total = await accounts.LongCountAsync(ct);
        var connectedUsers = await connected.LongCountAsync(ct);
        var connectedCreators = await connected.Select(x => x.OpenId).Distinct().LongCountAsync(ct);
        var needsReauthorization = await accounts.LongCountAsync(x => x.Status == "ReauthRequired" ||
            x.Status == "Active" && (x.RefreshExpiresAtUtc == null || x.RefreshExpiresAtUtc <= now), ct);
        var query = from account in accounts
                    join user in db.Users.AsNoTracking() on account.Id equals user.Id into users
                    from user in users.DefaultIfEmpty()
                    select new { Account = account, UserName = user == null ? null : user.UserName,
                        UserActive = user != null && user.IsActive };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.UserName != null && x.UserName.ToLower().Contains(term) ||
                x.Account.Username != null && x.Account.Username.ToLower().Contains(term) ||
                x.Account.OpenId != null && x.Account.OpenId.ToLower().Contains(term));
        }
        if (status == "Active") query = query.Where(x => x.Account.Status == "Active" && x.Account.OpenId != null && x.Account.RefreshExpiresAtUtc > now);
        else if (status == "ReauthRequired") query = query.Where(x => x.Account.Status == "ReauthRequired" ||
            x.Account.Status == "Active" && (x.Account.RefreshExpiresAtUtc == null || x.Account.RefreshExpiresAtUtc <= now));
        else if (status is "Disabled" or "NotConnected") query = query.Where(x => x.Account.Status == status);
        var count = await query.LongCountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Account.UpdatedAtUtc).ThenBy(x => x.Account.Id)
            .Skip((Math.Clamp(page, 1, 100_000) - 1) * 50).Take(50)
            .Select(x => new TikTokDeveloperAdminRow(x.Account.Id, x.UserName, x.UserActive, x.Account.Username,
                x.Account.OpenId, x.Account.Status, x.Account.AccessExpiresAtUtc, x.Account.RefreshExpiresAtUtc,
                x.Account.UpdatedAtUtc, x.Account.GrantedScopesJson,
                db.TikTokDeveloperApiKeys.Count(k => k.OwnerUserId == x.Account.Id && k.AppKey == appKey &&
                    k.RevokedAtUtc == null && k.ExpiresAtUtc > now))).ToListAsync(ct);
        var cashbackCreator = await (from credential in db.TikTokCreatorCredentials.AsNoTracking()
            where credential.AppKey == appKey && credential.IsPrimary
            join admin in db.Users.AsNoTracking() on credential.AuthorizedByAdminId equals admin.Id into admins
            from admin in admins.DefaultIfEmpty()
            select new TikTokCashbackCreatorOverview(credential.OpenId, credential.Status, credential.AuthorizedByAdminId,
                admin == null ? null : admin.UserName, credential.AccessTokenExpiresAtUtc, credential.RefreshTokenExpiresAtUtc))
            .SingleOrDefaultAsync(ct);
        return new TikTokDeveloperAdminPage(total, connectedUsers, connectedCreators, needsReauthorization, count, rows, cashbackCreator);
    }, ct);
}
