using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Volo.Abp;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.IdentityExtensions;

// Anonymous accounts have an empty email, never a fabricated contact address.
public class CatBackUserValidator : UserValidator<IdentityUser>
{
    private readonly IAccountIdentityStore _accounts;
    public CatBackUserValidator(IAccountIdentityStore accounts) => _accounts = accounts;

    public override async Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user)
    {
        if (user.IsAnonymous())
        {
            var allowed = manager.Options.User.AllowedUserNameCharacters;
            if (string.IsNullOrWhiteSpace(user.UserName) || user.UserName.Length > 256 ||
                (!string.IsNullOrEmpty(allowed) && System.Linq.Enumerable.Any(user.UserName, c => !allowed.Contains(c))))
                return IdentityResult.Failed(manager.ErrorDescriber.InvalidUserName(user.UserName));
            var owner = await manager.FindByNameAsync(user.UserName);
            return owner != null && owner.Id != user.Id
                ? IdentityResult.Failed(manager.ErrorDescriber.DuplicateUserName(user.UserName)) : IdentityResult.Success;
        }
        var result = await base.ValidateAsync(manager, user);
        if (!result.Succeeded || user.IsAnonymous()) return result;
        if (string.IsNullOrWhiteSpace(user.GetLoginEmail())) user.SetLoginEmail(user.Email);
        var loginOwner = await _accounts.FindByLoginEmailAsync(user.GetLoginEmail()!);
        var contactOwner = await manager.FindByEmailAsync(user.GetLoginEmail()!);
        var contactLoginOwner = string.IsNullOrWhiteSpace(user.Email) ? null : await _accounts.FindByLoginEmailAsync(user.Email);
        var loginNameOwner = await manager.FindByNameAsync(user.GetLoginEmail()!);
        var nameEmailOwner = await _accounts.FindByLoginEmailAsync(user.UserName);
        if ((loginOwner != null && loginOwner.Id != user.Id) || (contactOwner != null && contactOwner.Id != user.Id) ||
            (contactLoginOwner != null && contactLoginOwner.Id != user.Id) ||
            (loginNameOwner != null && loginNameOwner.Id != user.Id) || (nameEmailOwner != null && nameEmailOwner.Id != user.Id))
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Email này đang được sử dụng bởi tài khoản khác." });
        return result;
    }
}
