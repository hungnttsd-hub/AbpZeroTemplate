using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Users;
using WebHoanTien.Integrations.RioHub;

namespace WebHoanTien.Affiliates;

public class TikTokAffiliateLinkCreator : ITransientDependency
{
    private readonly IRioHubAffiliateClient _client;
    private readonly RioHubOptions _options;
    private readonly IRepository<AffiliateTracking, Guid> _repository;
    private readonly IGuidGenerator _guids;
    private readonly ICurrentUser _currentUser;

    public TikTokAffiliateLinkCreator(IRioHubAffiliateClient client, IOptions<RioHubOptions> options,
        IRepository<AffiliateTracking, Guid> repository, IGuidGenerator guids, ICurrentUser currentUser)
    {
        _client = client; _options = options.Value; _repository = repository; _guids = guids;
        _currentUser = currentUser;
    }

    public async Task<(AffiliateTracking Tracking, bool IsExisting, bool WasRestored, JsonElement? Product)> CreateAsync(
        string url, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Id != userId)
            throw new UserFriendlyException("Vui lòng đăng nhập CatBack trước khi tạo link affiliate.");
        if (!TikTokAffiliateUrl.TryNormalize(url, out var normalized, out var inputProductId))
            throw new UserFriendlyException("Vui lòng dán link sản phẩm TikTok Shop hợp lệ.");
        var inputOverride = TikTokProductOverrides.Find(inputProductId);
        if (inputOverride is null && (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.CreatorUsername)))
            throw new UserFriendlyException("Tạo link TikTok chưa sẵn sàng. Vui lòng thử lại sau.");

        var existing = (await _repository.GetListAsync(x => x.UserId == userId &&
            x.Platform == AffiliatePlatform.TikTok && x.NormalizedUrl == normalized &&
            x.Status == AffiliateTrackingStatus.Active, cancellationToken: cancellationToken)).FirstOrDefault();
        var restored = existing?.IsHidden == true;
        if (existing is not null && !string.IsNullOrWhiteSpace(existing.AffiliateUrl) &&
            (inputOverride is null || existing.ProductId == inputOverride.ProductId) &&
            string.Equals(existing.GetProperty<string>("RioHubCreatorUsername"), _options.CreatorUsername, StringComparison.OrdinalIgnoreCase))
        {
            var configured = TikTokProductOverrides.Find(existing.ProductId);
            if (configured is not null)
                existing.SetProduct(configured.ProductId, existing.ShopId, configured.Title, configured.ImageUrl, null);
            if (restored || configured is not null)
            {
                existing.Show();
                await _repository.UpdateAsync(existing, autoSave: true, cancellationToken: cancellationToken);
            }
            return (existing, true, restored, null);
        }

        var id = _guids.Create();
        var tracking = existing ?? new AffiliateTracking(id, userId, AffiliatePlatform.TikTok,
            "cb_" + id.ToString("N"), url.Trim(), normalized);
        // This exact product is usable without RioHub. The destination is a product URL,
        // not an affiliate link, and must stay distinguishable in history and order attribution.
        if (inputOverride is not null)
        {
            tracking.SetAffiliateLink(normalized);
            tracking.SetProduct(inputOverride.ProductId, null, inputOverride.Title, inputOverride.ImageUrl, null);
            tracking.SetProperty(TikTokProductOverrides.ProductUrlFallbackProperty, true);
            tracking.SetProperty("RioHubCreatorUsername", _options.CreatorUsername);
            tracking.Show();
            if (existing is null) await _repository.InsertAsync(tracking, autoSave: true, cancellationToken: cancellationToken);
            else await _repository.UpdateAsync(tracking, autoSave: true, cancellationToken: cancellationToken);
            return (tracking, existing is not null, restored, null);
        }
        JsonElement response;
        try
        {
            response = await _client.CreateLinkAsync(new RioHubCreateLinkInput
            {
                CreatorUsername = _options.CreatorUsername,
                ProductUrl = normalized,
                SubId = tracking.TrackingToken
            }, includeProduct: true, cancellationToken: cancellationToken);
        }
        catch (RioHubApiException exception)
        {
            throw new UserFriendlyException(exception.Message);
        }
        catch (System.ComponentModel.DataAnnotations.ValidationException)
        {
            throw new UserFriendlyException("Cấu hình tài khoản tạo link TikTok chưa hợp lệ. Vui lòng liên hệ hỗ trợ.");
        }

        var affiliateUrl = ReadString(response, "affiliate_link");
        var productId = ReadString(response, "product_id");
        if (!TikTokAffiliateUrl.TryNormalize(affiliateUrl, out _, out _) ||
            string.IsNullOrWhiteSpace(productId) || productId.Length > 128 ||
            ReadString(response, "sub_id") != tracking.TrackingToken ||
            !string.Equals(ReadString(response, "creator_username"), _options.CreatorUsername, StringComparison.OrdinalIgnoreCase))
            throw new UserFriendlyException("TikTok chưa trả về link có mã theo dõi hợp lệ. Vui lòng thử lại sau.");

        string? title = null;
        string? image = null;
        JsonElement? metadata = null;
        if (response.TryGetProperty("product", out var product) && product.ValueKind == JsonValueKind.Object)
        {
            metadata = product.Clone();
            title = ReadString(product, "title");
            image = ReadString(product, "main_image_url");
        }
        if (title?.Length > 500) title = title[..500];
        if (!Uri.TryCreate(image, UriKind.Absolute, out var imageUri) || imageUri.Scheme != "https" ||
            image!.Length > WebHoanTienConsts.UrlMaxLength) image = null;
        var productOverride = TikTokProductOverrides.Find(productId);
        if (productOverride is not null)
        {
            title = productOverride.Title;
            image = productOverride.ImageUrl;
        }
        tracking.SetAffiliateLink(affiliateUrl!);
        // No cashback amount is inferred before TikTok order reconciliation is available.
        tracking.SetProduct(productId, null, title ?? "Sản phẩm TikTok Shop", image, null);
        tracking.SetProperty("RioHubCreatorUsername", _options.CreatorUsername);
        tracking.SetProperty(TikTokProductOverrides.ProductUrlFallbackProperty, false);
        tracking.Show();
        if (existing is null) await _repository.InsertAsync(tracking, autoSave: true, cancellationToken: cancellationToken);
        else await _repository.UpdateAsync(tracking, autoSave: true, cancellationToken: cancellationToken);
        return (tracking, existing is not null, restored, metadata);
    }

    private static string? ReadString(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
}
