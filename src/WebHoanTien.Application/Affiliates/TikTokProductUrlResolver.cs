using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace WebHoanTien.Affiliates;

public class TikTokProductUrlResolver : ITransientDependency
{
    private readonly IHttpClientFactory _clients;
    public TikTokProductUrlResolver(IHttpClientFactory clients) => _clients = clients;

    public async Task<(string Url, string ProductId)> ResolveAsync(string input, CancellationToken ct)
    {
        if (!TikTokAffiliateUrl.TryNormalize(input, out var url, out var productId))
            throw new UserFriendlyException("Vui lòng dán link sản phẩm TikTok Shop hợp lệ.");
        if (productId is not null) return (Canonical(productId), productId);
        var current = new Uri(url);
        // Only known short-link hosts can cause an outbound request, using the pinned public-IP resolver.
        if (!IsShortHost(current.Host)) throw new UserFriendlyException("Link TikTok chưa có ID sản phẩm. Hãy sao chép link sản phẩm TikTok Shop.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        var client = _clients.CreateClient("AffiliateRedirectResolver");
        for (var hop = 0; hop < 5; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if ((int)response.StatusCode is not (>= 300 and < 400) || response.Headers.Location is null)
                throw new UserFriendlyException("Không mở được link TikTok rút gọn. Hãy dùng link sản phẩm có ID.");
            var target = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location : new Uri(current, response.Headers.Location);
            if (!TikTokAffiliateUrl.TryNormalize(target.AbsoluteUri, out url, out productId))
                throw new UserFriendlyException("TikTok chuyển hướng tới địa chỉ không được hỗ trợ.");
            if (productId is not null) return (Canonical(productId), productId);
            if (!IsShortHost(target.Host)) throw new UserFriendlyException("Link TikTok không dẫn đến trang sản phẩm được hỗ trợ.");
            current = target;
        }
        throw new UserFriendlyException("Link TikTok chuyển hướng quá nhiều lần.");
    }

    public static bool IsShortHost(string host) => host.Equals("vt.tiktok.com", StringComparison.OrdinalIgnoreCase)
        || host.Equals("vm.tiktok.com", StringComparison.OrdinalIgnoreCase);
    public static string Canonical(string id) => "https://shop.tiktok.com/vn/pdp/" + id;
}
