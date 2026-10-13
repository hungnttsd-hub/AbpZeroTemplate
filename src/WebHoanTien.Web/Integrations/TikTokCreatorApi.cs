using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Web.Integrations;

public class TikTokCreatorApi : ITikTokCreatorApi, ITransientDependency
{
    private readonly TikTokCreatorTokenService _tokens;
    private readonly TikTokCreatorOAuthClient _client;
    public TikTokCreatorApi(TikTokCreatorTokenService tokens, TikTokCreatorOAuthClient client)
    { _tokens = tokens; _client = client; }

    public async Task<string> GetCreatorOpenIdAsync(CancellationToken ct = default) =>
        (await _tokens.GetTokensAsync(ct: ct)).OpenId;

    public async Task<IReadOnlyList<TikTokCreatorProduct>> GetProductsAsync(IReadOnlyCollection<string> productIds, CancellationToken ct = default)
    {
        var tokens = await _tokens.GetTokensAsync(ct: ct);
        // Product information is optional; sharing links still work when this read scope is absent.
        if (!tokens.GrantedScopes.Contains("creator.affiliate_collaboration.read", StringComparer.Ordinal))
            return Array.Empty<TikTokCreatorProduct>();
        try { return await ReadProducts(tokens, productIds, ct); }
        catch (TikTokCreatorApiException ex) when (ex.TikTokCode == 105002)
        { return await ReadProducts(await _tokens.GetTokensAsync(true, tokens.TokenVersion, ct), productIds, ct); }
    }

    private async Task<IReadOnlyList<TikTokCreatorProduct>> ReadProducts(TikTokCreatorTokens tokens,
        IReadOnlyCollection<string> productIds, CancellationToken ct)
    {
        try { return await _client.GetProducts(tokens, productIds, ct); }
        catch (TikTokCreatorApiException ex) when (ex.TikTokCode == 105001)
        { await _tokens.ReportInvalidTokenAsync(tokens.TokenVersion, ct); throw; }
    }

    public async Task<TikTokSharingLink> GenerateLinkAsync(string productId, CancellationToken ct = default)
    {
        var tokens = await _tokens.GetTokensAsync(ct: ct);
        try { return await _client.GenerateSharingLink(tokens, productId, ct: ct); }
        catch (TikTokCreatorApiException ex) when (ex.TikTokCode == 105001)
        { await _tokens.ReportInvalidTokenAsync(tokens.TokenVersion, ct); throw; }
    }

    public async Task<TikTokCreatorOrderPage> SearchOrdersAsync(long fromUnix, long toUnix,
        string? pageToken, CancellationToken ct = default)
    {
        var tokens = await _tokens.GetTokensAsync(ct: ct);
        try { return await ReadOrders(tokens, fromUnix, toUnix, pageToken, ct); }
        catch (TikTokCreatorApiException ex) when (ex.TikTokCode == 105002)
        {
            return await ReadOrders(await _tokens.GetTokensAsync(true, tokens.TokenVersion, ct),
                fromUnix, toUnix, pageToken, ct);
        }
    }

    private async Task<TikTokCreatorOrderPage> ReadOrders(TikTokCreatorTokens tokens,
        long fromUnix, long toUnix, string? pageToken, CancellationToken ct)
    {
        try { return await _client.SearchOrders(tokens, fromUnix, toUnix, pageToken, ct); }
        catch (TikTokCreatorApiException ex) when (ex.TikTokCode == 105001)
        { await _tokens.ReportInvalidTokenAsync(tokens.TokenVersion, ct); throw; }
    }
}
