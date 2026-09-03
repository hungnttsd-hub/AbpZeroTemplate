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
    private readonly ShopeeLinkTargetClassifier _targetClassifier;
    private readonly ShopeeShopMetadataProvider _shopMetadataProvider;
    private readonly IAffiliateProviderRegistry _providers;
    private readonly ITrackingTokenGenerator _tokenGenerator;
    private readonly IAffiliateIdResolver _affiliateIdResolver;
    private readonly ShopeeAffiliateLinkBuilder _linkBuilder;
    private readonly AffiliateUserShareRateResolver _shareRateResolver;
    private readonly AffiliateCommissionCalculator _commissionCalculator;
    private readonly IClock _clock;
    private readonly ILogger<AffiliateLinkAppService> _logger;

    public AffiliateLinkAppService(IRepository<AffiliateTracking, Guid> repository, ISafeAffiliateUrlResolver resolver,
        IAffiliateUrlNormalizer normalizer, ShopeeLinkTargetClassifier targetClassifier,
        ShopeeShopMetadataProvider shopMetadataProvider,
        IAffiliateProviderRegistry providers, ITrackingTokenGenerator tokenGenerator,
        IAffiliateIdResolver affiliateIdResolver, ShopeeAffiliateLinkBuilder linkBuilder,
        AffiliateUserShareRateResolver shareRateResolver,
        AffiliateCommissionCalculator commissionCalculator, IClock clock, ILogger<AffiliateLinkAppService> logger)
    {
        _repository = repository;
        _resolver = resolver;
        _normalizer = normalizer;
        _targetClassifier = targetClassifier;
        _shopMetadataProvider = shopMetadataProvider;
        _providers = providers;
        _tokenGenerator = tokenGenerator;
        _affiliateIdResolver = affiliateIdResolver;
        _linkBuilder = linkBuilder;
        _shareRateResolver = shareRateResolver;
        _commissionCalculator = commissionCalculator;
        _clock = clock;
        _logger = logger;
    }

    [AllowAnonymous]
    public Task<AffiliateUrlValidationDto> ValidateAsync(ValidateAffiliateUrlInput input)
    {
        if (!IsSelectableTargetType(input.TargetType))
        {
            return Task.FromResult(InvalidValidation(WebHoanTienDomainErrorCodes.AffiliateTargetTypeInvalid,
                "Vui lòng chọn loại link Shopee hợp lệ."));
        }

        var valid = _normalizer.TryNormalize(input.Url, out var normalized, out var itemId);
        if (!valid)
        {
            return Task.FromResult(InvalidValidation(WebHoanTienDomainErrorCodes.InvalidAffiliateUrl,
                "Chỉ chấp nhận link HTTPS thuộc tên miền Shopee hợp lệ."));
        }

        if (ShopeeUrlNormalizer.IsShortHost(new Uri(normalized).IdnHost))
        {
            return Task.FromResult(new AffiliateUrlValidationDto
            {
                IsValid = true,
                Platform = AffiliatePlatform.Shopee,
                NormalizedUrl = normalized,
                ItemId = itemId,
                RequiresRedirectResolution = true
            });
        }

        var detectedTargetType = _targetClassifier.Classify(normalized);
        if (detectedTargetType == AffiliateLinkTargetType.Unknown)
        {
            return Task.FromResult(InvalidValidation(WebHoanTienDomainErrorCodes.AffiliateTargetUnsupported,
                "Link này không phải trang sản phẩm hoặc cửa hàng Shopee được hỗ trợ."));
        }

        if (detectedTargetType != input.TargetType)
        {
            var result = InvalidValidation(WebHoanTienDomainErrorCodes.AffiliateTargetMismatch,
                TargetMismatchMessage(input.TargetType, detectedTargetType));
            result.DetectedTargetType = detectedTargetType;
            return Task.FromResult(result);
        }

        return Task.FromResult(new AffiliateUrlValidationDto
        {
            IsValid = true,
            Platform = AffiliatePlatform.Shopee,
            NormalizedUrl = normalized,
            ItemId = itemId,
            RequiresRedirectResolution = false,
            DetectedTargetType = detectedTargetType
        });
    }

    public async Task<AffiliateTrackingDto> CreateAsync(CreateAffiliateLinkInput input)
    {
        EnsureSelectableTargetType(input.TargetType);
        var userId = CurrentUser.GetId();
        var originalUrl = input.Url.Trim();
        var (normalized, itemId) = await _resolver.ResolveAsync(input.Url);
        var detectedTargetType = _targetClassifier.Classify(normalized);
        if (detectedTargetType == AffiliateLinkTargetType.Unknown)
        {
            throw new UserFriendlyException(
                "Link này không phải trang sản phẩm hoặc cửa hàng Shopee được hỗ trợ.",
                code: WebHoanTienDomainErrorCodes.AffiliateTargetUnsupported);
        }

        if (detectedTargetType != input.TargetType)
        {
            throw new UserFriendlyException(TargetMismatchMessage(input.TargetType, detectedTargetType),
                code: WebHoanTienDomainErrorCodes.AffiliateTargetMismatch);
        }

        var resolvedAffiliateId = await _affiliateIdResolver.ResolveAsync(userId, AffiliatePlatform.Shopee);
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
            var affiliateUrl = _linkBuilder.Build(normalized, tracking.TrackingToken,
                resolvedAffiliateId.AffiliateId);
            if (!string.Equals(tracking.NormalizedUrl, normalized, StringComparison.Ordinal))
            {
                tracking.SetResolvedUrl(normalized, affiliateUrl);
                trackingChanged = true;
            }
            else if (!string.Equals(tracking.AffiliateUrl, affiliateUrl, StringComparison.Ordinal))
            {
                tracking.SetAffiliateLink(affiliateUrl);
                trackingChanged = true;
            }
        }
        else
        {
            var token = _tokenGenerator.Create();
            tracking = new AffiliateTracking(GuidGenerator.Create(), userId, AffiliatePlatform.Shopee, token, originalUrl, normalized);
            tracking.SetAffiliateLink(_linkBuilder.Build(normalized, token, resolvedAffiliateId.AffiliateId));
        }

        var metadataChanged = false;

        if (detectedTargetType == AffiliateLinkTargetType.Product && !string.IsNullOrWhiteSpace(itemId))
        {
            var provider = _providers.Get(AffiliatePlatform.Shopee);
            try
            {
                var product = await provider.GetProductOfferAsync(itemId);
                if (product is not null)
                {
                    var estimatedUserCommission = await CalculateEstimatedUserCommissionAsync(product.EstimatedCommission);
                    tracking.SetProduct(itemId, product.ShopId, product.Name, product.ImageUrl, estimatedUserCommission);
                    metadataChanged = true;
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không làm mới được estimate cho item {ItemId}.", itemId);
            }
        }
        else if (detectedTargetType == AffiliateLinkTargetType.Shop)
        {
            var shop = await _shopMetadataProvider.GetAsync(normalized);
            var displayName = IsShopFallbackName(shop.DisplayName) && !string.IsNullOrWhiteSpace(tracking.ProductName)
                ? tracking.ProductName
                : shop.DisplayName;
            tracking.SetShop(shop.ShopId ?? tracking.ShopId, displayName, shop.ImageUrl ?? tracking.ImageUrl);
            metadataChanged = true;
        }

        if (isExisting)
        {
            if (metadataChanged || trackingChanged) await _repository.UpdateAsync(tracking, autoSave: true);
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

    private AffiliateTrackingDto Map(AffiliateTracking x, bool isExisting = false, bool wasRestored = false) => new()
    {
        Id = x.Id, IsExisting = isExisting, WasRestored = wasRestored, CreationTime = x.CreationTime, CreatorId = x.CreatorId, LastModificationTime = x.LastModificationTime,
        LastModifierId = x.LastModifierId, IsDeleted = x.IsDeleted, DeleterId = x.DeleterId, DeletionTime = x.DeletionTime,
        Platform = x.Platform, TargetType = _targetClassifier.Classify(x.NormalizedUrl),
        TrackingToken = x.TrackingToken, OriginalUrl = x.OriginalUrl, NormalizedUrl = x.NormalizedUrl,
        ProductId = x.ProductId, ShopId = x.ShopId, ProductName = x.ProductName,
        ImageUrl = x.ImageUrl, EstimatedCommission = x.EstimatedCommission, ClickCount = x.ClickCount,
        LastClickedAt = x.LastClickedAt, IsHidden = x.IsHidden, HiddenAt = x.HiddenAt,
        Status = x.Status, RedirectUrl = "/go/" + x.TrackingToken
    };

    private static bool IsSelectableTargetType(AffiliateLinkTargetType targetType) =>
        targetType is AffiliateLinkTargetType.Product or AffiliateLinkTargetType.Shop;

    private static bool IsShopFallbackName(string displayName) =>
        displayName == "Cửa hàng Shopee" || displayName.StartsWith("Shop #", StringComparison.Ordinal);

    private static void EnsureSelectableTargetType(AffiliateLinkTargetType targetType)
    {
        if (!IsSelectableTargetType(targetType))
        {
            throw new UserFriendlyException("Vui lòng chọn loại link Shopee hợp lệ.",
                code: WebHoanTienDomainErrorCodes.AffiliateTargetTypeInvalid);
        }
    }

    private static AffiliateUrlValidationDto InvalidValidation(string errorCode, string error) => new()
    {
        IsValid = false,
        ErrorCode = errorCode,
        Error = error
    };

    private static string TargetMismatchMessage(AffiliateLinkTargetType requested, AffiliateLinkTargetType detected) =>
        requested == AffiliateLinkTargetType.Product && detected == AffiliateLinkTargetType.Shop
            ? "Đây là link cửa hàng Shopee. Hãy chọn \"Link shop\" để tạo link."
            : requested == AffiliateLinkTargetType.Shop && detected == AffiliateLinkTargetType.Product
                ? "Đây là link sản phẩm Shopee. Hãy chọn \"Link sản phẩm\" để tạo link."
                : "Loại link đã chọn không khớp với link Shopee.";
}
