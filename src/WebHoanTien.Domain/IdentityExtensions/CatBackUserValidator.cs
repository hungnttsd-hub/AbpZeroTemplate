using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.IdentityExtensions;

// Accounts created without email keep it empty, never a fabricated contact address.
public class CatBackUserValidator : UserValidator<IdentityUser>
{
    private readonly IAccountIdentityStore _accounts;
    public CatBackUserValidator(IAccountIdentityStore accounts) => _accounts = accounts;

    public override async Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user)
    {
        if (user.IsAnonymous()) return await ValidateUserNameAsync(manager, user);
        var result = user.IsUserNameRegistration() && string.IsNullOrWhiteSpace(user.Email)
            ? await ValidateUserNameAsync(manager, user)
            : await base.ValidateAsync(manager, user);
        if (!result.Succeeded) return result;
        if (!user.IsUserNameRegistration() && string.IsNullOrWhiteSpace(user.GetLoginEmail())) user.SetLoginEmail(user.Email);
        var loginEmail = user.GetLoginEmail();
        var loginOwner = string.IsNullOrWhiteSpace(loginEmail) ? null : await _accounts.FindByLoginEmailAsync(loginEmail);
        var contactOwner = string.IsNullOrWhiteSpace(loginEmail) ? null : await manager.FindByEmailAsync(loginEmail);
        var contactLoginOwner = string.IsNullOrWhiteSpace(user.Email) ? null : await _accounts.FindByLoginEmailAsync(user.Email);
        var contactNameOwner = string.IsNullOrWhiteSpace(user.Email) ? null : await manager.FindByNameAsync(user.Email);
        var loginNameOwner = string.IsNullOrWhiteSpace(loginEmail) ? null : await manager.FindByNameAsync(loginEmail);
        var nameEmailOwner = await _accounts.FindByLoginEmailAsync(user.UserName);
        var nameContactOwner = await manager.FindByEmailAsync(user.UserName);
        if ((nameEmailOwner != null && nameEmailOwner.Id != user.Id) || (nameContactOwner != null && nameContactOwner.Id != user.Id))
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName", Description = "Username này đã được sử dụng." });
        if ((loginOwner != null && loginOwner.Id != user.Id) || (contactOwner != null && contactOwner.Id != user.Id) ||
            (contactLoginOwner != null && contactLoginOwner.Id != user.Id) ||
            (contactNameOwner != null && contactNameOwner.Id != user.Id) ||
            (loginNameOwner != null && loginNameOwner.Id != user.Id))
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Email này đang được sử dụng bởi tài khoản khác." });
        return result;
    }

    private static async Task<IdentityResult> ValidateUserNameAsync(UserManager<IdentityUser> manager, IdentityUser user)
    {
        var allowed = manager.Options.User.AllowedUserNameCharacters;
        if (string.IsNullOrWhiteSpace(user.UserName) || user.UserName.Length > IdentityUserConsts.MaxUserNameLength ||
            (!string.IsNullOrEmpty(allowed) && System.Linq.Enumerable.Any(user.UserName, c => !allowed.Contains(c))))
            return IdentityResult.Failed(manager.ErrorDescriber.InvalidUserName(user.UserName));
        var owner = await manager.FindByNameAsync(user.UserName);
        return owner != null && owner.Id != user.Id
            ? IdentityResult.Failed(manager.ErrorDescriber.DuplicateUserName(user.UserName)) : IdentityResult.Success;
    }
}
