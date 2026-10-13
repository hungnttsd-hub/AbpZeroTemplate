using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Integrations;

// Business API integration uses the single admin Creator from the database.
public class TikTokCreatorConnection : ITikTokCreatorConnection, ITransientDependency
{
    private readonly TikTokCreatorTokenService _tokens;
    private readonly TikTokCreatorOAuthClient _client;
    private readonly ICurrentUser _user;
    public bool IsConfigured => _client.IsConfigured;
    public TikTokCreatorConnection(TikTokCreatorTokenService tokens, TikTokCreatorOAuthClient client, ICurrentUser user)
    { _tokens = tokens; _client = client; _user = user; }

    public async Task<TikTokCreatorDto?> GetProfile()
    {
        if (!_user.IsAuthenticated || !IsConfigured || await _tokens.ReadPrimaryAsync() is null) return null;
        var tokens = await _tokens.GetTokensAsync();
        try { return await ReadProfile(tokens); }
        catch (TikTokCreatorApiException ex) when (ex.TikTokCode == 105002)
        {
            // Read calls may retry once; forced refresh reuses a token another instance already rotated.
            return await ReadProfile(await _tokens.GetTokensAsync(true, tokens.TokenVersion));
        }
    }

    public async Task<string> GetLink(string productId)
    {
        var tokens = await _tokens.GetTokensAsync();
        try { return await _client.GetLink(tokens, productId); }
        catch (TikTokCreatorApiException ex) when (ex.TikTokCode == 105001)
        { await _tokens.ReportInvalidTokenAsync(tokens.TokenVersion); throw; }
    }

    private async Task<TikTokCreatorDto> ReadProfile(TikTokCreatorTokens tokens)
    {
        try { return await _client.GetProfile(tokens); }
        catch (TikTokCreatorApiException ex) when (ex.TikTokCode == 105001)
        { await _tokens.ReportInvalidTokenAsync(tokens.TokenVersion); throw; }
    }
}
