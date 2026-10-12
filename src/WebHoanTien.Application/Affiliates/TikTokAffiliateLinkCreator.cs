using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Users;
using WebHoanTien.TikTokAffiliate;

namespace WebHoanTien.Affiliates;

public class TikTokAffiliateLinkCreator : ITransientDependency
{
    private readonly ITikTokCreatorApi _api;
    private readonly TikTokProductUrlResolver _resolver;
    private readonly IRepository<AffiliateTracking, Guid> _repository;
    private readonly IGuidGenerator _guids;
    private readonly ICurrentUser _currentUser;

    public TikTokAffiliateLinkCreator(ITikTokCreatorApi api, TikTokProductUrlResolver resolver,
        IRepository<AffiliateTracking, Guid> repository, IGuidGenerator guids, ICurrentUser currentUser)
    { _api = api; _resolver = resolver; _repository = repository; _guids = guids; _currentUser = currentUser; }

    public async Task<(AffiliateTracking Tracking, bool IsExisting, bool WasRestored, JsonElement? Product)> CreateAsync(
        string url, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Id != userId)
            throw new UserFriendlyException("Vui lòng đăng nhập CatBack trước khi tạo link hoàn tiền.");
        var (normalized, productId) = await _resolver.ResolveAsync(url, cancellationToken);
        var creatorOpenId = await _api.GetCreatorOpenIdAsync(cancellationToken);
        var candidates = await _repository.GetListAsync(x => x.UserId == userId && x.Platform == AffiliatePlatform.TikTok
            && x.ProductId == productId && x.Status == AffiliateTrackingStatus.Active, cancellationToken: cancellationToken);
        var existing = candidates.FirstOrDefault(x => x.GetProperty<string>(TikTokLinkAttribution.SourceProperty) == TikTokLinkAttribution.Source
            && x.GetProperty<string>(TikTokLinkAttribution.CreatorProperty) == creatorOpenId);
        var restored = existing?.IsHidden == true;
        if (existing is not null && TikTokAffiliateUrl.TryNormalize(existing.AffiliateUrl, out _, out _))
        {
            if (restored) { existing.Show(); await _repository.UpdateAsync(existing, autoSave: true, cancellationToken: cancellationToken); }
            return (existing, true, restored, null);
        }

        var link = await _api.GenerateLinkAsync(productId, cancellationToken);
        if (link.CreatorOpenId != creatorOpenId)
            throw new UserFriendlyException("Kết nối Creator vừa thay đổi. Vui lòng tạo lại link.");
        if (!TikTokAffiliateUrl.TryNormalize(link.SharingLink, out _, out _))
            throw new UserFriendlyException("TikTok trả về sharing link không hợp lệ.");
        var id = _guids.Create();
        var tracking = existing ?? new AffiliateTracking(id, userId, AffiliatePlatform.TikTok,
            id.ToString("N"), url.Trim(), normalized);
        var metadata = TikTokProductOverrides.Find(productId);
        tracking.SetResolvedUrl(normalized, link.SharingLink);
        tracking.SetProduct(productId, null, metadata?.Title ?? "Sản phẩm TikTok Shop", metadata?.ImageUrl, null);
        tracking.SetProperty(TikTokLinkAttribution.SourceProperty, TikTokLinkAttribution.Source);
        tracking.SetProperty(TikTokLinkAttribution.CreatorProperty, link.CreatorOpenId);
        tracking.SetProperty(TikTokProductOverrides.ProductUrlFallbackProperty, false);
        if (!string.IsNullOrWhiteSpace(link.OneLink)) tracking.SetProperty(TikTokLinkAttribution.OneLinkProperty, link.OneLink);
        tracking.Show();
        if (existing is null) await _repository.InsertAsync(tracking, autoSave: true, cancellationToken: cancellationToken);
        else await _repository.UpdateAsync(tracking, autoSave: true, cancellationToken: cancellationToken);
        return (tracking, existing is not null, restored, null);
    }
}
