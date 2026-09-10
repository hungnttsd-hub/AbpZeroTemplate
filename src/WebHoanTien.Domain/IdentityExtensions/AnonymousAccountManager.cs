using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Identity;
using WebHoanTien.Affiliates;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace WebHoanTien.IdentityExtensions;

// All mutations run inside the caller's transaction; the Web boundary commits before signing in.
[DisableAuditing]
public class AnonymousAccountManager : DomainService
{
    private readonly IdentityUserManager _users;
    private readonly IUserStore<IdentityUser> _userStore;
    private readonly IAccountIdentityStore _accounts;
    private readonly IRepository<AnonymousRecovery, Guid> _recoveries;
    private readonly IRepository<AnonymousDevice, Guid> _devices;
    private readonly IRepository<PendingAccountUpgrade, Guid> _pending;
    private readonly IRepository<UserLegalConsent, Guid> _consents;
    private readonly IRecoveryCodeProtector _protector;
    private readonly AdminNewUserRegistrationNotifier _registrationNotifier;
    public const string InvalidRecovery = "Mã khôi phục không hợp lệ hoặc đã hết hiệu lực.";

    public AnonymousAccountManager(IdentityUserManager users, IUserStore<IdentityUser> userStore,
        IAccountIdentityStore accounts, IRepository<AnonymousRecovery, Guid> recoveries,
        IRepository<AnonymousDevice, Guid> devices, IRepository<PendingAccountUpgrade, Guid> pending,
        IRepository<UserLegalConsent, Guid> consents, IRecoveryCodeProtector protector,
        AdminNewUserRegistrationNotifier registrationNotifier)
    { _users = users; _userStore = userStore; _accounts = accounts; _recoveries = recoveries;
      _devices = devices; _pending = pending; _consents = consents; _protector = protector; _registrationNotifier = registrationNotifier; }

    public static string NewSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string NormalizeCode(string value) => new(value.Where(c => !char.IsWhiteSpace(c) && c != '-').Select(char.ToUpperInvariant).ToArray());
    public static void Check(IdentityResult result)
    { if (!result.Succeeded) throw new UserFriendlyException(string.Join(" ", result.Errors.Select(x => x.Description))); }

    public async Task<IdentityUser> RequireAnonymousAsync(Guid userId)
    {
        await _accounts.LockAsync("account:" + userId);
        var user = await _users.GetByIdAsync(userId);
        if (!user.IsAnonymous() || !user.IsActive || await _users.IsLockedOutAsync(user))
            throw new UserFriendlyException(InvalidRecovery);
        return user;
    }

    public async Task<AnonymousDevice?> FindDeviceAsync(string secret)
    {
        if (secret.Length != 64 || !secret.All(Uri.IsHexDigit)) return null;
        var hash = Hash(secret);
        var now = Clock.Now;
        return await _devices.FindAsync(x => x.SecretHash == hash && x.RevokedAt == null && x.ExpiresAt > now);
    }
    public Task<bool> IsKnownDeviceAsync(string secret)
    {
        var hash = Hash(secret);
        return _devices.AnyAsync(x => x.SecretHash == hash);
    }

    public async Task<IdentityUser> CreateOrResumeAsync(string secret, bool creationEnabled)
    {
        var hash = Hash(secret);
        await _accounts.LockAsync("device:" + hash);
        var device = await FindDeviceAsync(secret);
        if (device != null)
        {
            var existing = await RequireAnonymousAsync(device.UserId);
            device.LastUsedAt = Clock.Now;
            await _devices.UpdateAsync(device);
            return existing;
        }
        if (!creationEnabled) throw new UserFriendlyException("Tạo tài khoản ẩn danh đang tạm dừng. Bạn vẫn có thể khôi phục bằng mã.");
        if (await _devices.AnyAsync(x => x.SecretHash == hash))
            throw new UserFriendlyException("Thiết bị đã hết hiệu lực. Vui lòng tải lại trang và thử lại.");
        string name;
        do { name = "cat_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6)); }
        while (await _users.FindByNameAsync(name) != null);
        var user = new IdentityUser(GuidGenerator.Create(), name, "");
        user.SetProperty(CatBackAccountProperties.Type, 0);
        Check(await _users.CreateAsync(user));
        await GenerateRecoveryAsync(user.Id);
        await RememberAsync(user.Id, secret);
        await ConsentAsync(user.Id, LegalConsentMethod.AnonymousRegistration);
        Logger.LogInformation("AnonymousAccountCreated {UserId}", user.Id);
        return user;
    }

    public async Task<IdentityUser> RecoverAsync(string code, string deviceSecret)
    {
        var normalized = NormalizeCode(code);
        if (normalized.Length != 34 || !normalized.StartsWith("CB", StringComparison.Ordinal) || !normalized[2..].All(Uri.IsHexDigit))
            throw new UserFriendlyException(InvalidRecovery);
        var hash = Hash(normalized);
        var recovery = await _recoveries.FindAsync(x => x.CodeHash == hash && x.RevokedAt == null);
        if (recovery == null) throw new UserFriendlyException(InvalidRecovery);
        var user = await RequireAnonymousAsync(recovery.UserId);
        // Re-read after the account lock: regeneration and upgrade use the same lock.
        if (!await _recoveries.AnyAsync(x => x.Id == recovery.Id && x.RevokedAt == null))
            throw new UserFriendlyException(InvalidRecovery);
        recovery.LastUsedAt = Clock.Now;
        await _recoveries.UpdateAsync(recovery);
        await RememberAsync(user.Id, deviceSecret);
        Logger.LogInformation("RecoveryAttemptSucceeded {UserId}", user.Id);
        return user;
    }

    public async Task RememberAsync(Guid userId, string secret)
    {
        await _devices.InsertAsync(new AnonymousDevice(GuidGenerator.Create())
        { UserId = userId, SecretHash = Hash(secret), CreatedAt = Clock.Now, LastUsedAt = Clock.Now, ExpiresAt = Clock.Now.AddDays(90) });
    }

    public async Task<string> GetRecoveryAsync(Guid userId)
    {
        await RequireAnonymousAsync(userId);
        var item = await _recoveries.GetAsync(x => x.UserId == userId && x.RevokedAt == null);
        return _protector.Unprotect(item.ProtectedCode);
    }
    public async Task<string> RegenerateAsync(Guid userId)
    {
        await RequireAnonymousAsync(userId);
        foreach (var item in await _recoveries.GetListAsync(x => x.UserId == userId && x.RevokedAt == null))
        { item.RevokedAt = Clock.Now; item.ProtectedCode = ""; await _recoveries.UpdateAsync(item, autoSave: true); }
        return await GenerateRecoveryAsync(userId);
    }
    private async Task<string> GenerateRecoveryAsync(Guid userId)
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var code = "CB-" + string.Join("-", Enumerable.Range(0, 8).Select(i => raw.Substring(i * 4, 4)));
        await _recoveries.InsertAsync(new AnonymousRecovery(GuidGenerator.Create())
        { UserId = userId, CodeHash = Hash(NormalizeCode(code)), ProtectedCode = _protector.Protect(code), CreatedAt = Clock.Now });
        return code;
    }
    public async Task RenameAsync(Guid userId, string name)
    {
        var user = await RequireAnonymousAsync(userId);
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 256)
            throw new UserFriendlyException("Username không hợp lệ.");
        var loginOwner = await _accounts.FindByLoginEmailAsync(name);
        if (loginOwner != null && loginOwner.Id != user.Id) throw new UserFriendlyException("Username đã được sử dụng.");
        Check(await _users.SetUserNameAsync(user, name));
        Logger.LogInformation("AnonymousUsernameChanged {UserId}", userId);
    }
    public async Task ForgetAsync(Guid userId, string secret)
    {
        await _accounts.LockAsync("account:" + userId);
        var device = await FindDeviceAsync(secret);
        if (device != null && device.UserId == userId)
        { device.RevokedAt = Clock.Now; await _devices.UpdateAsync(device); Logger.LogInformation("DeviceForgotten {UserId}", userId); }
    }

    public async Task ValidateRegistrationAsync(Guid userId, string username, string email)
    {
        var allowed = _users.Options.User.AllowedUserNameCharacters;
        if (string.IsNullOrWhiteSpace(username) || username.Length > 256 ||
            (!string.IsNullOrEmpty(allowed) && username.Any(c => !allowed.Contains(c))) ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || email.Length > 256)
            throw new UserFriendlyException("Vui lòng nhập username và địa chỉ email hợp lệ.");
        var nameOwner = await _users.FindByNameAsync(username);
        var emailOwner = await _accounts.FindByLoginEmailAsync(email);
        var contactOwner = await _users.FindByEmailAsync(email);
        var loginNameOwner = await _users.FindByNameAsync(email);
        var nameEmailOwner = await _accounts.FindByLoginEmailAsync(username);
        if (nameOwner != null && nameOwner.Id != userId) throw new UserFriendlyException("Username đã được sử dụng.");
        if ((emailOwner != null && emailOwner.Id != userId) || (contactOwner != null && contactOwner.Id != userId) ||
            (loginNameOwner != null && loginNameOwner.Id != userId) || (nameEmailOwner != null && nameEmailOwner.Id != userId))
            throw new UserFriendlyException("Email đã được sử dụng. CatBack không tự gộp tài khoản.");
    }

    public async Task<PendingAccountUpgrade> BeginUpgradeAsync(Guid userId, string name, string email,
        string password, string token, string returnUrl)
    {
        var user = await RequireAnonymousAsync(userId);
        name = name.Trim(); email = email.Trim();
        await ValidateRegistrationAsync(userId, name, email);
        foreach (var validator in _users.PasswordValidators) Check(await validator.ValidateAsync(_users, user, password));
        await RevokePendingAsync(userId);
        var item = new PendingAccountUpgrade(GuidGenerator.Create())
        { UserId = userId, UserName = name, Email = email, PasswordHash = _users.PasswordHasher.HashPassword(user, password),
          TokenHash = Hash(token), ExpiresAt = Clock.Now.AddHours(24), ReturnUrl = returnUrl,
          TermsVersion = WebHoanTienConsts.TermsVersion, PrivacyVersion = WebHoanTienConsts.PrivacyVersion, AcceptedAt = Clock.Now };
        await _pending.InsertAsync(item, autoSave: true);
        return item;
    }

    public async Task<PendingAccountUpgrade?> GetPendingAsync(Guid userId) =>
        await _pending.FindAsync(x => x.UserId == userId && x.RevokedAt == null);

    public async Task<PendingAccountUpgrade> RenewUpgradeAsync(Guid userId, string token)
    {
        await RequireAnonymousAsync(userId);
        var item = await _pending.FindAsync(x => x.UserId == userId && x.RevokedAt == null)
            ?? throw new UserFriendlyException("Chưa có yêu cầu nâng cấp. Vui lòng nhập thông tin và đăng ký trước.");
        item.TokenHash = Hash(token); item.ExpiresAt = Clock.Now.AddHours(24);
        await _pending.UpdateAsync(item); return item;
    }

    public async Task<(IdentityUser User, string ReturnUrl)> ConfirmUpgradeAsync(string token, bool emailVerified = true)
    {
        if (token.Length != 64) throw new UserFriendlyException("Liên kết xác minh không hợp lệ hoặc đã hết hạn.");
        var hash = Hash(token);
        var item = await _pending.FindAsync(x => x.TokenHash == hash && x.RevokedAt == null);
        if (item == null) throw new UserFriendlyException("Liên kết xác minh không hợp lệ hoặc đã hết hạn.");
        var user = await RequireAnonymousAsync(item.UserId);
        if (item.ExpiresAt <= Clock.Now || !await _pending.AnyAsync(x => x.Id == item.Id && x.TokenHash == hash && x.RevokedAt == null))
            throw new UserFriendlyException("Liên kết xác minh không hợp lệ hoặc đã hết hạn.");
        if (item.TermsVersion != WebHoanTienConsts.TermsVersion || item.PrivacyVersion != WebHoanTienConsts.PrivacyVersion)
            throw new UserFriendlyException("Điều khoản đã thay đổi. Vui lòng gửi lại thông tin nâng cấp.");
        await ValidateRegistrationAsync(user.Id, item.UserName, item.Email);
        Check(await _users.SetUserNameAsync(user, item.UserName));
        Check(await _users.SetEmailAsync(user, item.Email));
        user.SetLoginEmail(item.Email);
        await ((IUserPasswordStore<IdentityUser>)_userStore).SetPasswordHashAsync(user, item.PasswordHash, CancellationToken.None);
        user.SetEmailConfirmed(emailVerified);
        await FinishUpgradeAsync(user, LegalConsentMethod.EmailRegistration);
        return (user, item.ReturnUrl);
    }

    public async Task<IdentityUser> UpgradeGoogleAsync(Guid userId, string email, string subject, string? avatar)
    {
        var user = await RequireAnonymousAsync(userId);
        await ValidateRegistrationAsync(userId, user.UserName, email);
        var owner = await _users.FindByLoginAsync("Google", subject);
        if (owner != null && owner.Id != userId) throw new UserFriendlyException("Google này đã thuộc tài khoản khác. Không thể gộp tài khoản.");
        Check(await _users.SetEmailAsync(user, email));
        user.SetLoginEmail(email); user.SetEmailConfirmed(true);
        if (owner == null) Check(await _users.AddLoginAsync(user, new UserLoginInfo("Google", subject, "Google")));
        if (!string.IsNullOrWhiteSpace(avatar)) user.SetProperty("GoogleAvatarUrl", avatar);
        await FinishUpgradeAsync(user, LegalConsentMethod.GoogleRegistration);
        return user;
    }
    private async Task FinishUpgradeAsync(IdentityUser user, LegalConsentMethod method)
    {
        user.SetProperty(CatBackAccountProperties.Type, 1);
        Check(await _users.UpdateAsync(user));
        Check(await _users.UpdateSecurityStampAsync(user));
        foreach (var item in await _recoveries.GetListAsync(x => x.UserId == user.Id && x.RevokedAt == null))
        { item.RevokedAt = Clock.Now; item.ProtectedCode = ""; await _recoveries.UpdateAsync(item); }
        foreach (var item in await _devices.GetListAsync(x => x.UserId == user.Id && x.RevokedAt == null))
        { item.RevokedAt = Clock.Now; await _devices.UpdateAsync(item); }
        await RevokePendingAsync(user.Id);
        await ConsentAsync(user.Id, method);
        await _registrationNotifier.EnqueueAsync(user.Id, method == LegalConsentMethod.GoogleRegistration ? UserSelfRegistrationMethod.Google : UserSelfRegistrationMethod.Email);
        Logger.LogInformation("AnonymousAccountUpgraded {UserId}", user.Id);
    }
    private async Task RevokePendingAsync(Guid userId)
    {
        foreach (var item in await _pending.GetListAsync(x => x.UserId == userId && x.RevokedAt == null))
        { item.RevokedAt = Clock.Now; item.PasswordHash = ""; await _pending.UpdateAsync(item, autoSave: true); }
    }
    private async Task ConsentAsync(Guid userId, LegalConsentMethod method)
    {
        if (!await _consents.AnyAsync(x => x.UserId == userId && x.TermsVersion == WebHoanTienConsts.TermsVersion && x.PrivacyVersion == WebHoanTienConsts.PrivacyVersion))
            await _consents.InsertAsync(new UserLegalConsent(GuidGenerator.Create(), userId, WebHoanTienConsts.TermsVersion, WebHoanTienConsts.PrivacyVersion, method, Clock.Now));
    }
}
