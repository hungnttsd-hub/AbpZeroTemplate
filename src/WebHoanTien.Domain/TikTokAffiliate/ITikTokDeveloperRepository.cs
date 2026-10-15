using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebHoanTien.TikTokAffiliate;

public interface ITikTokDeveloperRepository
{
    Task<TikTokDeveloperAccount?> FindAccountAsync(Guid owner, string appKey, CancellationToken ct);
    Task BeginAuthorizationAsync(TikTokDeveloperAuthState state, CancellationToken ct);
    Task<TikTokDeveloperAuthState?> ConsumeStateAsync(Guid owner, string appKey, string stateHash, CancellationToken ct);
    Task SaveAuthorizationAsync(TikTokDeveloperAuthState state, TikTokDeveloperAccount incoming, CancellationToken ct);
    Task<bool> AcquireRefreshAsync(Guid owner, string appKey, long version, Guid lease, CancellationToken ct);
    Task<bool> SaveRefreshAsync(TikTokDeveloperAccount incoming, long version, Guid lease, CancellationToken ct);
    Task ReleaseRefreshAsync(Guid owner, string appKey, long version, Guid lease, int cooldownSeconds, CancellationToken ct);
    Task MarkReauthAsync(Guid owner, string appKey, long version, CancellationToken ct);
    Task DisconnectAsync(Guid owner, string appKey, CancellationToken ct);
    Task<IReadOnlyList<TikTokDeveloperApiKey>> GetKeysAsync(Guid owner, string appKey, CancellationToken ct);
    Task CreateKeyAsync(TikTokDeveloperApiKey key, CancellationToken ct);
    Task<TikTokDeveloperApiKey?> FindKeyAsync(string hash, string appKey, CancellationToken ct);
    Task RevokeKeyAsync(Guid owner, Guid keyId, string appKey, CancellationToken ct);
    Task<bool> ConsumeQuotaAsync(Guid owner, string appKey, int perMinute, CancellationToken ct);
    // Metadata only: never load token ciphertext or API-key hashes for administration.
    Task<TikTokDeveloperAdminPage> GetAdminPageAsync(string appKey, string? search, string? status, int page, CancellationToken ct);
}

public sealed record TikTokDeveloperAdminRow(Guid UserId, string? UserName, bool UserActive, string? CreatorUsername,
    string? OpenId, string Status, DateTimeOffset? AccessExpiresAtUtc, DateTimeOffset? RefreshExpiresAtUtc,
    DateTimeOffset UpdatedAtUtc, string GrantedScopesJson, int ActiveKeyCount);
public sealed record TikTokDeveloperAdminPage(long TotalAccounts, long ConnectedUsers, long ConnectedCreators,
    long NeedsReauthorization, long FilteredCount, IReadOnlyList<TikTokDeveloperAdminRow> Items,
    TikTokCashbackCreatorOverview? CashbackCreator);
public sealed record TikTokCashbackCreatorOverview(string OpenId, string Status, Guid AuthorizedByAdminId,
    string? AdminUserName, DateTimeOffset AccessExpiresAtUtc, DateTimeOffset RefreshExpiresAtUtc);
