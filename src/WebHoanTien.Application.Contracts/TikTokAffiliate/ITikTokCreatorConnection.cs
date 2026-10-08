using System.Threading.Tasks;

namespace WebHoanTien.TikTokAffiliate;

// Infrastructure boundary: no tokens or app secrets are exposed to the UI.
public interface ITikTokCreatorConnection
{
    bool IsConfigured { get; }
    Task<TikTokCreatorDto?> GetProfile();
    Task Disconnect();
}
