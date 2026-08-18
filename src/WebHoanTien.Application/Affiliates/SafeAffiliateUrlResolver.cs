using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Affiliates;

public interface ISafeAffiliateUrlResolver
{
    Task<(string NormalizedUrl, string? ItemId)> ResolveAsync(string input, CancellationToken cancellationToken = default);
}

public class SafeAffiliateUrlResolver : ISafeAffiliateUrlResolver, ITransientDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAffiliateUrlNormalizer _normalizer;

    public SafeAffiliateUrlResolver(IHttpClientFactory httpClientFactory, IAffiliateUrlNormalizer normalizer)
    {
        _httpClientFactory = httpClientFactory;
        _normalizer = normalizer;
    }

    public async Task<(string NormalizedUrl, string? ItemId)> ResolveAsync(string input, CancellationToken cancellationToken = default)
    {
        if (!_normalizer.TryNormalize(input, out var normalized, out var itemId))
            throw new BusinessException(WebHoanTienDomainErrorCodes.InvalidAffiliateUrl);

        var current = new Uri(normalized);
        if (!ShopeeUrlNormalizer.IsShortHost(current.IdnHost)) return (normalized, itemId);

        var client = _httpClientFactory.CreateClient("AffiliateRedirectResolver");
        for (var hop = 0; hop < 5; hop++)
        {
            await AffiliateNetworkSafety.ResolvePublicAddressesAsync(current.DnsSafeHost, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.UserAgent.ParseAdd("webHoanTien-link-validator/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null)
            {
                current = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location : new Uri(current, response.Headers.Location);
                if (!_normalizer.TryNormalize(current.AbsoluteUri, out normalized, out itemId))
                    throw new BusinessException(WebHoanTienDomainErrorCodes.UnsafeRedirect);
                current = new Uri(normalized);
                continue;
            }

            response.EnsureSuccessStatusCode();
            if (!_normalizer.TryNormalize(current.AbsoluteUri, out normalized, out itemId))
                throw new BusinessException(WebHoanTienDomainErrorCodes.UnsafeRedirect);
            return (normalized, itemId);
        }

        throw new BusinessException(WebHoanTienDomainErrorCodes.UnsafeRedirect).WithData("Reason", "TooManyRedirects");
    }

}
