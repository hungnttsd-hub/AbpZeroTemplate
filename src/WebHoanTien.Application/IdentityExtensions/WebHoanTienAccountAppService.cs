using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Emailing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace WebHoanTien.IdentityExtensions;

[RemoteService(IsEnabled = false)]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IAccountAppService))]
public class WebHoanTienAccountAppService : AccountAppService
{
    private readonly AdminNewUserRegistrationNotifier _adminRegistrationNotifier;

    public WebHoanTienAccountAppService(
        IdentityUserManager userManager,
        IIdentityRoleRepository roleRepository,
        IAccountEmailer accountEmailer,
        IdentitySecurityLogManager identitySecurityLogManager,
        IOptions<IdentityOptions> identityOptions,
        AdminNewUserRegistrationNotifier adminRegistrationNotifier)
        : base(userManager, roleRepository, accountEmailer, identitySecurityLogManager, identityOptions)
    {
        _adminRegistrationNotifier = adminRegistrationNotifier;
    }

    public override async Task<IdentityUserDto> RegisterAsync(RegisterDto input)
    {
        var registeredUser = await base.RegisterAsync(input);
        var user = await UserManager.GetByIdAsync(registeredUser.Id);
        await _adminRegistrationNotifier.NotifyAdminsAsync(user, UserSelfRegistrationMethod.Email);
        return registeredUser;
    }
}
