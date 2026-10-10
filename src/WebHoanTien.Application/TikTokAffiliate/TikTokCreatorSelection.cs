using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace WebHoanTien.TikTokAffiliate;

// Server-side selection belongs to the signed-in CatBack account; it is not proof of TikTok identity.
public class TikTokCreatorSelection : ITransientDependency
{
    private readonly IIdentityUserRepository _users;
    private readonly ICurrentUser _currentUser;
    public TikTokCreatorSelection(IIdentityUserRepository users, ICurrentUser currentUser)
    { _users = users; _currentUser = currentUser; }

    public static string Normalize(string? username) => (username ?? string.Empty).Trim().TrimStart('@').ToLowerInvariant();
    public static bool IsLocal(string? username) => Normalize(username) == TikTokAffiliateAccess.LocalCreatorUsername;

    public static string ValidateUsername(string? username)
    {
        var value = Normalize(username);
        if (!Regex.IsMatch(value, @"\A[a-z0-9._]{1,100}\z"))
            throw new UserFriendlyException("Vui lòng nhập Username TikTok hợp lệ, không phải tên hiển thị hoặc URL.");
        return value;
    }

    public async Task SelectAsync(string? username)
    {
        var value = ValidateUsername(username);
        var user = await UserAsync();
        user.SetProperty(TikTokAffiliateAccess.PendingCreatorProperty, value);
        user.SetProperty(TikTokAffiliateAccess.WorkspaceConnectionProperty, false);
        user.SetProperty(TikTokAffiliateAccess.WorkspaceCreatorProperty, string.Empty);
        await _users.UpdateAsync(user, autoSave: true);
    }

    public async Task<string?> GetPendingAsync() =>
        (await UserAsync()).GetProperty<string>(TikTokAffiliateAccess.PendingCreatorProperty);

    public async Task<bool> IsLocalConnectedAsync()
    {
        var user = await UserAsync();
        return user.GetProperty<bool>(TikTokAffiliateAccess.WorkspaceConnectionProperty) &&
            IsLocal(user.GetProperty<string>(TikTokAffiliateAccess.WorkspaceCreatorProperty));
    }

    private async Task<IdentityUser> UserAsync()
    {
        if (!_currentUser.IsAuthenticated) throw new UserFriendlyException("Vui lòng đăng nhập CatBack.");
        var user = await _users.FindAsync(_currentUser.GetId());
        if (user is not { IsActive: true } || !user.GetProperty<bool>(TikTokAffiliateAccess.UserProperty))
            throw new UserFriendlyException("Tài khoản chưa được cấp quyền dùng TikTok Affiliate.");
        return user;
    }
}
