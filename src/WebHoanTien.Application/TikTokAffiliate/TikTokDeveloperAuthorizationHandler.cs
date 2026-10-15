using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace WebHoanTien.TikTokAffiliate;

public sealed class TikTokDeveloperRequirement : IAuthorizationRequirement { }

public sealed class TikTokDeveloperAuthorizationHandler : AuthorizationHandler<TikTokDeveloperRequirement>
{
    private readonly IIdentityUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _tenant;
    public TikTokDeveloperAuthorizationHandler(IIdentityUserRepository users, ICurrentUser currentUser, ICurrentTenant tenant)
    { _users = users; _currentUser = currentUser; _tenant = tenant; }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TikTokDeveloperRequirement requirement)
    {
        if (_tenant.Id.HasValue || !_currentUser.IsAuthenticated || !_currentUser.Id.HasValue || context.User.Identity?.IsAuthenticated != true) return;
        var user = await _users.FindAsync(_currentUser.Id.Value);
        if (user is { IsActive: true } && user.GetProperty<bool>(TikTokDeveloperAccess.UserProperty)) context.Succeed(requirement);
    }
}
