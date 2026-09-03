using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Affiliates;

public class SafeAffiliateUrlResolver : ISafeAffiliateUrlResolver, ITransientDependency
{
    private const int MaxShortLinkPageBytes = 512 * 1024;
    private static readonly Regex HttpUrlPattern = new(
        "httpUrl\\s*:\\s*\"(?<url>(?:\\\\.|[^\"])*)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
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
                if (!ShopeeUrlNormalizer.IsShortHost(current.IdnHost))
                    return (normalized, itemId);
                continue;
            }

            response.EnsureSuccessStatusCode();
            var resolvedUrl = await TryReadResolvedUrlAsync(response, cancellationToken);
            if (!string.IsNullOrWhiteSpace(resolvedUrl))
            {
                if (!_normalizer.TryNormalize(resolvedUrl, out normalized, out itemId) ||
                    ShopeeUrlNormalizer.IsShortHost(new Uri(normalized).IdnHost))
                {
                    throw new BusinessException(WebHoanTienDomainErrorCodes.UnsafeRedirect);
                }

                return (normalized, itemId);
            }

            if (ShopeeUrlNormalizer.IsShortHost(current.IdnHost))
                throw new BusinessException(WebHoanTienDomainErrorCodes.InvalidAffiliateUrl)
                    .WithData("Reason", "ShortLinkTargetNotFound");

            if (!_normalizer.TryNormalize(current.AbsoluteUri, out normalized, out itemId))
                throw new BusinessException(WebHoanTienDomainErrorCodes.UnsafeRedirect);
            return (normalized, itemId);
        }

        throw new BusinessException(WebHoanTienDomainErrorCodes.UnsafeRedirect).WithData("Reason", "TooManyRedirects");
    }

    private static async Task<string?> TryReadResolvedUrlAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is null || !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)) return null;
        if (response.Content.Headers.ContentLength > MaxShortLinkPageBytes) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length), cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > MaxShortLinkPageBytes) return null;
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        var html = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, checked((int)buffer.Length));
        var match = HttpUrlPattern.Match(html);
        if (!match.Success) return null;

        try
        {
            return JsonSerializer.Deserialize<string>($"\"{match.Groups["url"].Value}\"");
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
