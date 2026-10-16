using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Integrations;

public class TikTokCashbackCreatorRegistry : ITikTokCashbackCreatorRegistry, ITransientDependency
{
    private readonly TikTokCreatorTokenService _tokens;
    public TikTokCashbackCreatorRegistry(TikTokCreatorTokenService tokens) => _tokens = tokens;
    public async Task<string?> GetOpenIdAsync(CancellationToken ct = default)
    {
        var primary = await _tokens.ReadPrimaryAsync(ct);
        return primary is { IsPrimary: true, UserType: 1 } && primary.AuthorizedByAdminId != System.Guid.Empty ? primary.OpenId : null;
    }
}
