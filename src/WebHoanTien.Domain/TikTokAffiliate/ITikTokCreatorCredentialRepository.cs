using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebHoanTien.TikTokAffiliate;

// Each operation owns a short unit of work. No transaction spans a TikTok HTTP call.
public interface ITikTokCreatorCredentialRepository
{
    Task CreateStateAsync(TikTokCreatorAuthState state, CancellationToken ct);
    Task<TikTokCreatorAuthState?> ConsumeStateAsync(string hash, string appKey, CancellationToken ct);
    Task FailStateAsync(Guid id, CancellationToken ct);
    Task<TikTokCreatorCredentials?> GetPrimaryAsync(string appKey, CancellationToken ct);
    Task SaveAuthorizationAsync(TikTokCreatorAuthState state, TikTokCreatorCredentials credentials, CancellationToken ct);
    Task<bool> AcquireRefreshAsync(Guid id, long version, Guid leaseId, CancellationToken ct);
    Task<bool> SaveRefreshAsync(TikTokCreatorCredentials credentials, long version, Guid leaseId, CancellationToken ct);
    Task ReleaseRefreshAsync(Guid id, long version, Guid leaseId, CancellationToken ct);
    Task DeferRefreshAsync(Guid id, long version, Guid leaseId, int seconds, CancellationToken ct);
    Task MarkReauthAsync(Guid id, long version, Guid? leaseId, CancellationToken ct);
}
