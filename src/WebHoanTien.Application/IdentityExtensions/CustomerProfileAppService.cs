using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using WebHoanTien.Affiliates;

namespace WebHoanTien.IdentityExtensions;

[Authorize]
public class CustomerProfileAppService : WebHoanTienAppService, ICustomerProfileAppService
{
    private readonly IIdentityUserRepository _users;
    private readonly IRepository<UserLegalConsent, Guid> _consents;

    public CustomerProfileAppService(IIdentityUserRepository users, IRepository<UserLegalConsent, Guid> consents)
    {
        _users = users;
        _consents = consents;
    }

    public async Task<CustomerProfileDto> GetAsync()
    {
        var user = await _users.GetAsync(CurrentUser.GetId(), includeDetails: true);
        var hasConsent = await _consents.AnyAsync(x => x.UserId == user.Id &&
            x.TermsVersion == WebHoanTienConsts.TermsVersion && x.PrivacyVersion == WebHoanTienConsts.PrivacyVersion);
        var displayName = string.Join(' ', new[] { user.Name, user.Surname }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        if (string.IsNullOrWhiteSpace(displayName)) displayName = user.Email ?? user.UserName;
        return new CustomerProfileDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = displayName,
            Initials = GetInitials(displayName),
            AvatarUrl = user.GetProperty<string?>("GoogleAvatarUrl") ?? CurrentUser.FindClaim("google_avatar")?.Value,
            HasGoogleLogin = user.Logins.Any(x => x.LoginProvider.Equals("Google", StringComparison.OrdinalIgnoreCase)),
            HasCurrentLegalConsent = hasConsent
        };
    }

    public async Task AcceptLegalAsync(CreateLegalConsentInput input)
    {
        if (!input.Accepted) throw new UserFriendlyException("Bạn phải chấp thuận Điều khoản và Chính sách riêng tư.");
        var userId = CurrentUser.GetId();
        if (await _consents.AnyAsync(x => x.UserId == userId && x.TermsVersion == WebHoanTienConsts.TermsVersion &&
            x.PrivacyVersion == WebHoanTienConsts.PrivacyVersion)) return;
        await _consents.InsertAsync(new UserLegalConsent(GuidGenerator.Create(), userId, WebHoanTienConsts.TermsVersion,
            WebHoanTienConsts.PrivacyVersion, input.Method, Clock.Now), autoSave: true);
    }

    private static string GetInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "U";
        return string.Concat(parts.TakeLast(2).Select(x => char.ToUpperInvariant(x[0])));
    }
}
