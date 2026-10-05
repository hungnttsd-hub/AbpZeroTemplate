using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace WebHoanTien.TikTokAffiliate;

public sealed class TikTokAffiliateAccessRequirement : IAuthorizationRequirement { }

public sealed class TikTokAffiliateAuthorizationHandler : AuthorizationHandler<TikTokAffiliateAccessRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUserRepository _users;

    public TikTokAffiliateAuthorizationHandler(ICurrentUser currentUser, IIdentityUserRepository users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        TikTokAffiliateAccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true || !_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
            return;
        // Read persisted access rather than trusting a potentially stale cookie claim.
        // No implicit administrator bypass: every account needs IsTiktokDemo == true.
        var user = await _users.FindAsync(_currentUser.Id.Value);
        if (user is { IsActive: true } && user.GetProperty<bool>(TikTokAffiliateAccess.UserProperty))
            context.Succeed(requirement);
    }
}
