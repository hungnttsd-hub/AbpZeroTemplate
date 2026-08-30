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
        await _adminRegistrationNotifier.EnqueueAsync(
            registeredUser.Id,
            UserSelfRegistrationMethod.Email);
        return registeredUser;
    }
}
