using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp.Authorization.Permissions;
using WebHoanTien.Permissions;

namespace WebHoanTien.TikTokAffiliate;

public sealed class TikTokAffiliateAccessRequirement : IAuthorizationRequirement { }

public sealed class TikTokCreatorConnectionRequirement : IAuthorizationRequirement { }

public sealed class TikTokCreatorOrdersRequirement : IAuthorizationRequirement
{
    public bool RequireSync { get; }
    public TikTokCreatorOrdersRequirement(bool requireSync) => RequireSync = requireSync;
}

public sealed class TikTokCreatorOrdersAuthorizationHandler : AuthorizationHandler<TikTokCreatorOrdersRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUserRepository _users;
    private readonly IPermissionChecker _permissions;
    private readonly Volo.Abp.MultiTenancy.ICurrentTenant _tenant;

    public TikTokCreatorOrdersAuthorizationHandler(ICurrentUser currentUser, IIdentityUserRepository users,
        IPermissionChecker permissions, Volo.Abp.MultiTenancy.ICurrentTenant tenant)
    { _currentUser = currentUser; _users = users; _permissions = permissions; _tenant = tenant; }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TikTokCreatorOrdersRequirement requirement)
    {
        if (_tenant.Id.HasValue || context.User.Identity?.IsAuthenticated != true ||
            !_currentUser.IsAuthenticated || !_currentUser.Id.HasValue) return;
        var user = await _users.FindAsync(_currentUser.Id.Value);
        if (user is not { IsActive: true }) return;
        if (user.GetProperty<bool>(TikTokAffiliateAccess.UserProperty) ||
            await _permissions.IsGrantedAsync(WebHoanTienPermissions.Admin.Orders) &&
            (!requirement.RequireSync || context.User.IsInRole("admin") &&
                await _permissions.IsGrantedAsync(WebHoanTienPermissions.Admin.Sync)))
            context.Succeed(requirement);
    }
}

public sealed class TikTokCreatorConnectionAuthorizationHandler : AuthorizationHandler<TikTokCreatorConnectionRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUserRepository _users;
    private readonly IPermissionChecker _permissions;

    public TikTokCreatorConnectionAuthorizationHandler(ICurrentUser currentUser, IIdentityUserRepository users, IPermissionChecker permissions)
    { _currentUser = currentUser; _users = users; _permissions = permissions; }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TikTokCreatorConnectionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true || !_currentUser.IsAuthenticated || !_currentUser.Id.HasValue) return;
        var user = await _users.FindAsync(_currentUser.Id.Value);
        if (user is not { IsActive: true }) return;
        // This policy changes CatBack's cashback Creator, never a Developer's personal connection.
        if (context.User.IsInRole("admin") &&
            await _permissions.IsGrantedAsync(WebHoanTienPermissions.Admin.Settings))
            context.Succeed(requirement);
    }
}

public sealed class TikTokAffiliateAuthorizationHandler : AuthorizationHandler<TikTokAffiliateAccessRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUserRepository _users;
    private readonly IPermissionChecker _permissions;

    public TikTokAffiliateAuthorizationHandler(ICurrentUser currentUser, IIdentityUserRepository users, IPermissionChecker permissions)
    {
        _currentUser = currentUser;
        _users = users;
        _permissions = permissions;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        TikTokAffiliateAccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true || !_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
            return;
        // Read persisted access rather than trusting a potentially stale cookie claim.
        var user = await _users.FindAsync(_currentUser.Id.Value);
        if (user is not { IsActive: true }) return;
        // Keep existing portal access for provisioned accounts. Administrators with TikTok
        // connection/order permissions can manage the real integration without a demo flag.
        if (user.GetProperty<bool>(TikTokAffiliateAccess.UserProperty) || context.User.IsInRole("admin") &&
            (await _permissions.IsGrantedAsync(WebHoanTienPermissions.Admin.Settings)
             || await _permissions.IsGrantedAsync(WebHoanTienPermissions.Admin.Orders)))
            context.Succeed(requirement);
    }
}
