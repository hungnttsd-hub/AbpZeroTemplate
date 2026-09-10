using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.Identity;

namespace WebHoanTien.IdentityExtensions;

public static class AccountIdentity
{
    public static AccountType GetAccountType(this IdentityUser user) =>
        user.ExtraProperties.TryGetValue(CatBackAccountProperties.Type, out var value)
            ? (AccountType)Convert.ToInt32(value) : AccountType.Registered;
    public static bool IsAnonymous(this IdentityUser user) => user.GetAccountType() == AccountType.Anonymous;
    public static string? GetLoginEmail(this IdentityUser user) => user.GetProperty<string?>(CatBackAccountProperties.LoginEmail);
    public static void SetLoginEmail(this IdentityUser user, string email)
    {
        user.SetProperty(CatBackAccountProperties.LoginEmail, email.Trim());
        user.SetProperty(CatBackAccountProperties.NormalizedLoginEmail, email.Trim().ToUpperInvariant());
    }
}

public interface IAccountIdentityStore
{
    Task<IdentityUser?> FindByLoginEmailAsync(string email);
    Task LockAsync(string key);
    Task<bool> ConsumeLimitAsync(string key, int limit, TimeSpan window);
}

public interface IRecoveryCodeProtector
{
    string Protect(string code);
    string Unprotect(string ciphertext);
}
