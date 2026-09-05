using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;
using WebHoanTien.Affiliates;
using WebHoanTien.Integrations.Shopee;

namespace WebHoanTien.Web.Controllers;

[AllowAnonymous]
public class AffiliateRedirectController : Controller
{
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateClick, Guid> _clicks;
    private readonly IAffiliateUrlNormalizer _normalizer;
    private readonly IAffiliateIdResolver _affiliateIdResolver;
    private readonly ShopeeAffiliateLinkBuilder _linkBuilder;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public AffiliateRedirectController(IRepository<AffiliateTracking, Guid> trackings, IRepository<AffiliateClick, Guid> clicks,
        IAffiliateUrlNormalizer normalizer, IAffiliateIdResolver affiliateIdResolver,
        ShopeeAffiliateLinkBuilder linkBuilder, IGuidGenerator guidGenerator, IClock clock, ICurrentUser currentUser)
    {
        _trackings = trackings;
        _clicks = clicks;
        _normalizer = normalizer;
        _affiliateIdResolver = affiliateIdResolver;
        _linkBuilder = linkBuilder;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _currentUser = currentUser;
    }

    [HttpGet("/go/{trackingToken}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GoAsync(string trackingToken)
    {
        var navigation = await ResolveNavigationAsync(trackingToken);
        if (navigation.ErrorResult is not null) return navigation.ErrorResult;

        await RegisterClickAsync(navigation.Tracking!, navigation.AffiliateId!);
        return Redirect(navigation.AffiliateUrl!);
    }

    [HttpPost("/go/{trackingToken}/click")]
    [IgnoreAntiforgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> TrackAsync(string trackingToken)
    {
        var navigation = await ResolveNavigationAsync(trackingToken);
        if (navigation.ErrorResult is not null) return navigation.ErrorResult;

        await RegisterClickAsync(navigation.Tracking!, navigation.AffiliateId!);
        return NoContent();
    }

    private async Task<AffiliateNavigation> ResolveNavigationAsync(string trackingToken)
    {
        var tracking = (await _trackings.GetListAsync(x => x.TrackingToken == trackingToken &&
            x.Status == AffiliateTrackingStatus.Active)).FirstOrDefault();
        if (tracking is null || tracking.Platform != AffiliatePlatform.Shopee ||
            !_normalizer.TryNormalize(tracking.NormalizedUrl, out var normalizedUrl, out _))
            return AffiliateNavigation.Failed(NotFound());

        ResolvedAffiliateId resolvedAffiliateId;
        try
        {
            resolvedAffiliateId = await _affiliateIdResolver.ResolveAsync(tracking.UserId, tracking.Platform);
        }
        catch (Volo.Abp.UserFriendlyException exception)
        {
            return AffiliateNavigation.Failed(StatusCode(503, exception.Message));
        }

        return AffiliateNavigation.Succeeded(tracking, resolvedAffiliateId,
            _linkBuilder.Build(normalizedUrl, tracking.TrackingToken, resolvedAffiliateId.AffiliateId));
    }

    private async Task RegisterClickAsync(AffiliateTracking tracking, ResolvedAffiliateId resolvedAffiliateId)
    {
        var clickedAt = _clock.Now;
        var click = new AffiliateClick(_guidGenerator.Create(), tracking.Id, _currentUser.Id, clickedAt,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(),
            Request.Headers.Referer.ToString(), resolvedAffiliateId.AffiliateId, resolvedAffiliateId.OverrideId);
        tracking.RegisterClick(clickedAt);
        await _clicks.InsertAsync(click);
        await _trackings.UpdateAsync(tracking, autoSave: true);
    }

    private sealed record AffiliateNavigation(AffiliateTracking? Tracking, ResolvedAffiliateId? AffiliateId,
        string? AffiliateUrl, IActionResult? ErrorResult)
    {
        public static AffiliateNavigation Succeeded(AffiliateTracking tracking, ResolvedAffiliateId affiliateId,
            string affiliateUrl) => new(tracking, affiliateId, affiliateUrl, null);

        public static AffiliateNavigation Failed(IActionResult errorResult) => new(null, null, null, errorResult);
    }
}
