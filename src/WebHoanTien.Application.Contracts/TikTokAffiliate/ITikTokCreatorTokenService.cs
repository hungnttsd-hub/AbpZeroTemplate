using System.Threading;
using System.Threading.Tasks;

namespace WebHoanTien.TikTokAffiliate;

// Backend-only dependency. Never expose this interface as an application API.
public interface ITikTokCreatorTokenService
{
    Task<string> GetValidAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<string> RefreshAccessTokenAsync(bool force = false, CancellationToken cancellationToken = default);
}
