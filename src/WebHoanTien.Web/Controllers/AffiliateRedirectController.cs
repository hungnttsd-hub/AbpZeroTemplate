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

namespace WebHoanTien.Web.Controllers;

[AllowAnonymous]
public class AffiliateRedirectController : Controller
{
    private readonly IRepository<AffiliateTracking, Guid> _trackings;
    private readonly IRepository<AffiliateClick, Guid> _clicks;
    private readonly IAffiliateUrlNormalizer _normalizer;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public AffiliateRedirectController(IRepository<AffiliateTracking, Guid> trackings, IRepository<AffiliateClick, Guid> clicks,
        IAffiliateUrlNormalizer normalizer, IGuidGenerator guidGenerator, IClock clock, ICurrentUser currentUser)
    { _trackings = trackings; _clicks = clicks; _normalizer = normalizer; _guidGenerator = guidGenerator; _clock = clock; _currentUser = currentUser; }

    [HttpGet("/go/{trackingToken}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GoAsync(string trackingToken)
    {
        var tracking = (await _trackings.GetListAsync(x => x.TrackingToken == trackingToken && x.Status == AffiliateTrackingStatus.Active)).FirstOrDefault();
        if (tracking?.AffiliateUrl is null || !_normalizer.TryNormalize(tracking.AffiliateUrl, out var safeUrl, out _)) return NotFound();
        var click = new AffiliateClick(_guidGenerator.Create(), tracking.Id, _currentUser.Id, _clock.Now,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), Request.Headers.Referer.ToString());
        tracking.RegisterClick(_clock.Now);
        await _clicks.InsertAsync(click);
        await _trackings.UpdateAsync(tracking, autoSave: true);
        return Redirect(safeUrl);
    }
}
