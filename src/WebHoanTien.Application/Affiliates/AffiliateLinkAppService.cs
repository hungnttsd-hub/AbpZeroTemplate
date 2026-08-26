using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using WebHoanTien.Integrations.Shopee;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;
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
    private readonly ShopeeAffiliateLinkBuilder _linkBuilder;
    private readonly AffiliateUserShareRateResolver _shareRateResolver;
    private readonly AffiliateCommissionCalculator _commissionCalculator;
    private readonly IClock _clock;
    private readonly ILogger<AffiliateLinkAppService> _logger;

    public AffiliateLinkAppService(IRepository<AffiliateTracking, Guid> repository, ISafeAffiliateUrlResolver resolver,
        IAffiliateUrlNormalizer normalizer, IAffiliateProviderRegistry providers, ITrackingTokenGenerator tokenGenerator,
        ShopeeAffiliateLinkBuilder linkBuilder, AffiliateUserShareRateResolver shareRateResolver,
        AffiliateCommissionCalculator commissionCalculator, IClock clock, ILogger<AffiliateLinkAppService> logger)
    {
        _repository = repository;
        _resolver = resolver;
        _normalizer = normalizer;
        _providers = providers;
        _tokenGenerator = tokenGenerator;
        _linkBuilder = linkBuilder;
        _shareRateResolver = shareRateResolver;
        _commissionCalculator = commissionCalculator;
        _clock = clock;
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
        var originalUrl = input.Url.Trim();
        var (normalized, itemId) = await _resolver.ResolveAsync(input.Url);
        var candidates = await _repository.GetListAsync(x => x.UserId == userId && x.Platform == AffiliatePlatform.Shopee &&
            (x.NormalizedUrl == normalized || x.OriginalUrl == originalUrl));
        var existing = candidates.FirstOrDefault(x => x.NormalizedUrl == normalized) ?? candidates.FirstOrDefault();
        var isExisting = existing is not null;
        var wasRestored = existing?.IsHidden == true;
        var trackingChanged = false;
        AffiliateTracking tracking;
        if (isExisting)
        {
            tracking = existing!;
            if (tracking.IsHidden)
            {
                tracking.Show();
                trackingChanged = true;
            }
            if (!string.Equals(tracking.NormalizedUrl, normalized, StringComparison.Ordinal))
            {
                tracking.SetResolvedUrl(normalized, _linkBuilder.Build(normalized, tracking.TrackingToken));
                trackingChanged = true;
            }
        }
        else
        {
            var token = _tokenGenerator.Create();
            tracking = new AffiliateTracking(GuidGenerator.Create(), userId, AffiliatePlatform.Shopee, token, originalUrl, normalized);
            tracking.SetAffiliateLink(_linkBuilder.Build(normalized, token));
        }

        var provider = _providers.Get(AffiliatePlatform.Shopee);
        var refreshedProduct = false;

        if (!string.IsNullOrWhiteSpace(itemId))
        {
            try
            {
                var product = await provider.GetProductOfferAsync(itemId);
                if (product is not null)
                {
                    var estimatedUserCommission = await CalculateEstimatedUserCommissionAsync(product.EstimatedCommission);
                    tracking.SetProduct(itemId, product.ShopId, product.Name, product.ImageUrl, estimatedUserCommission);
                    refreshedProduct = true;
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không làm mới được estimate cho item {ItemId}.", itemId);
            }
        }

        if (isExisting)
        {
            if (refreshedProduct || trackingChanged) await _repository.UpdateAsync(tracking, autoSave: true);
            return Map(tracking, isExisting: true, wasRestored: wasRestored);
        }

        await _repository.InsertAsync(tracking, autoSave: true);
        return Map(tracking);
    }

    public async Task<PagedResultDto<AffiliateTrackingDto>> GetListAsync(AffiliateTrackingListInput input)
    {
        var userId = CurrentUser.GetId();
        var query = (await _repository.GetQueryableAsync()).Where(x => x.UserId == userId);
        if (!input.IncludeHidden) query = query.Where(x => !x.IsHidden);
        query = query.OrderByDescending(x => x.CreationTime);
        var total = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));
        return new PagedResultDto<AffiliateTrackingDto>(total, rows.Select(x => Map(x)).ToList());
    }

    public async Task<AffiliateTrackingDto> GetAsync(Guid id)
    {
        var tracking = await _repository.GetAsync(id);
        if (tracking.UserId != CurrentUser.GetId()) throw new BusinessException(WebHoanTienDomainErrorCodes.TrackingNotOwned);
        return Map(tracking);
    }

    public async Task SetHiddenAsync(SetAffiliateTrackingHiddenInput input)
    {
        var tracking = await _repository.GetAsync(input.Id);
        if (tracking.UserId != CurrentUser.GetId()) throw new BusinessException(WebHoanTienDomainErrorCodes.TrackingNotOwned);

        if (input.IsHidden) tracking.Hide(_clock.Now);
        else tracking.Show();
        await _repository.UpdateAsync(tracking, autoSave: true);
    }

    private async Task<decimal?> CalculateEstimatedUserCommissionAsync(decimal? providerCommission)
    {
        if (!providerCommission.HasValue) return null;

        try
        {
            var userShareRate = await _shareRateResolver.GetForNextOrderAsync(CurrentUser.GetId(),
                AffiliatePlatform.Shopee, _clock.Now);
            return _commissionCalculator.CalculateUserCommission(providerCommission.Value, userShareRate);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Không tính được hoa hồng dự kiến của người dùng.");
            return null;
        }
    }

    private static AffiliateTrackingDto Map(AffiliateTracking x, bool isExisting = false, bool wasRestored = false) => new()
    {
        Id = x.Id, IsExisting = isExisting, WasRestored = wasRestored, CreationTime = x.CreationTime, CreatorId = x.CreatorId, LastModificationTime = x.LastModificationTime,
        LastModifierId = x.LastModifierId, IsDeleted = x.IsDeleted, DeleterId = x.DeleterId, DeletionTime = x.DeletionTime,
        Platform = x.Platform, TrackingToken = x.TrackingToken, OriginalUrl = x.OriginalUrl, NormalizedUrl = x.NormalizedUrl,
        AffiliateUrl = x.AffiliateUrl, ProductId = x.ProductId, ShopId = x.ShopId, ProductName = x.ProductName,
        ImageUrl = x.ImageUrl, EstimatedCommission = x.EstimatedCommission, ClickCount = x.ClickCount,
        LastClickedAt = x.LastClickedAt, IsHidden = x.IsHidden, HiddenAt = x.HiddenAt,
        Status = x.Status, RedirectUrl = "/go/" + x.TrackingToken
    };
}
