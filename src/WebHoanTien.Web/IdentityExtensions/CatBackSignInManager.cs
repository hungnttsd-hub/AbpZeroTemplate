using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Identity;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Settings;
using WebHoanTien.IdentityExtensions;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.Web.IdentityExtensions;

public class CatBackSignInManager : AbpSignInManager
{
    private readonly IAccountIdentityStore _accounts;
    public CatBackSignInManager(IdentityUserManager users, IHttpContextAccessor context,
        IUserClaimsPrincipalFactory<IdentityUser> factory, IOptions<IdentityOptions> identityOptions,
        ILogger<SignInManager<IdentityUser>> logger, IAuthenticationSchemeProvider schemes,
        IUserConfirmation<IdentityUser> confirmation, IOptions<AbpIdentityOptions> abpOptions,
        ISettingProvider settings, IAccountIdentityStore accounts)
        : base(users, context, factory, identityOptions, logger, schemes, confirmation, abpOptions, settings)
    { _accounts = accounts; }

    public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        if (userName.Contains('@'))
        {
            var user = await _accounts.FindByLoginEmailAsync(userName) ?? await UserManager.FindByNameAsync(userName);
            if (user == null || user.IsAnonymous()) return SignInResult.Failed;
            userName = user.UserName;
        }
        return await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);
    }
    protected override Task<SignInResult> PreSignInCheck(IdentityUser user) =>
        user.IsAnonymous() ? Task.FromResult(SignInResult.NotAllowed) : base.PreSignInCheck(user);
}
