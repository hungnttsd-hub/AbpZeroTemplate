using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using WebHoanTien.Integrations;

namespace WebHoanTien.Affiliates;

[Authorize]
public class AffiliateLinkAppService : WebHoanTienAppService, IAffiliateLinkAppService
{
    private readonly IRepository<AffiliateTracking, Guid> _repository;
    private readonly ISafeAffiliateUrlResolver _resolver;
    private readonly IAffiliateUrlNormalizer _normalizer;
    private readonly IAffiliateProviderRegistry _providers;
    private readonly ITrackingTokenGenerator _tokenGenerator;
    private readonly ILogger<AffiliateLinkAppService> _logger;

    public AffiliateLinkAppService(IRepository<AffiliateTracking, Guid> repository, ISafeAffiliateUrlResolver resolver,
        IAffiliateUrlNormalizer normalizer, IAffiliateProviderRegistry providers, ITrackingTokenGenerator tokenGenerator,
        ILogger<AffiliateLinkAppService> logger)
    {
        _repository = repository;
        _resolver = resolver;
        _normalizer = normalizer;
        _providers = providers;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
    }

    [AllowAnonymous]
    public Task<AffiliateUrlValidationDto> ValidateAsync(ValidateAffiliateUrlInput input)
    {
        var valid = _normalizer.TryNormalize(input.Url, out var normalized, out var itemId);
        return Task.FromResult(new AffiliateUrlValidationDto
        {
            IsValid = valid,
            Platform = valid ? AffiliatePlatform.Shopee : null,
            NormalizedUrl = valid ? normalized : null,
            ItemId = itemId,
            RequiresRedirectResolution = valid && ShopeeUrlNormalizer.IsShortHost(new Uri(normalized).IdnHost),
            Error = valid ? null : "Chỉ chấp nhận link HTTPS thuộc tên miền Shopee hợp lệ."
        });
    }

    public async Task<AffiliateTrackingDto> CreateAsync(CreateAffiliateLinkInput input)
    {
        var userId = CurrentUser.GetId();
        var (normalized, itemId) = await _resolver.ResolveAsync(input.Url);
        var existing = (await _repository.GetListAsync(x => x.UserId == userId && x.Platform == AffiliatePlatform.Shopee &&
            x.NormalizedUrl == normalized && x.Status == AffiliateTrackingStatus.Active)).FirstOrDefault();
        if (existing is not null) return Map(existing);

        var token = _tokenGenerator.Create();
        var tracking = new AffiliateTracking(GuidGenerator.Create(), userId, AffiliatePlatform.Shopee, token, input.Url.Trim(), normalized);
        var provider = _providers.Get(AffiliatePlatform.Shopee);
        var shortLink = await provider.GenerateShortLinkAsync(normalized, token);
        tracking.SetAffiliateLink(shortLink.Url);

        if (!string.IsNullOrWhiteSpace(itemId))
        {
            try
            {
                var product = await provider.GetProductOfferAsync(itemId);
                tracking.SetProduct(itemId, product?.ShopId, product?.Name, product?.ImageUrl, product?.EstimatedCommission);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không lấy được estimate cho item {ItemId}; short link vẫn được tạo.", itemId);
                tracking.SetProduct(itemId, null, null, null, null);
            }
        }

        await _repository.InsertAsync(tracking, autoSave: true);
        return Map(tracking);
    }

    public async Task<PagedResultDto<AffiliateTrackingDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var userId = CurrentUser.GetId();
        var query = (await _repository.GetQueryableAsync()).Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreationTime);
        var total = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));
        return new PagedResultDto<AffiliateTrackingDto>(total, rows.Select(Map).ToList());
    }

    public async Task<AffiliateTrackingDto> GetAsync(Guid id)
    {
        var tracking = await _repository.GetAsync(id);
        if (tracking.UserId != CurrentUser.GetId()) throw new BusinessException(WebHoanTienDomainErrorCodes.TrackingNotOwned);
        return Map(tracking);
    }

    private static AffiliateTrackingDto Map(AffiliateTracking x) => new()
    {
        Id = x.Id, CreationTime = x.CreationTime, CreatorId = x.CreatorId, LastModificationTime = x.LastModificationTime,
        LastModifierId = x.LastModifierId, IsDeleted = x.IsDeleted, DeleterId = x.DeleterId, DeletionTime = x.DeletionTime,
        Platform = x.Platform, TrackingToken = x.TrackingToken, OriginalUrl = x.OriginalUrl, NormalizedUrl = x.NormalizedUrl,
        AffiliateUrl = x.AffiliateUrl, ProductId = x.ProductId, ShopId = x.ShopId, ProductName = x.ProductName,
        ImageUrl = x.ImageUrl, EstimatedCommission = x.EstimatedCommission, ClickCount = x.ClickCount,
        LastClickedAt = x.LastClickedAt, Status = x.Status, RedirectUrl = "/go/" + x.TrackingToken
    };
}
