using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Auditing;
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
    private readonly IRepository<UserPayoutAccount, Guid> _payoutAccounts;

    public CustomerProfileAppService(IIdentityUserRepository users, IRepository<UserLegalConsent, Guid> consents,
        IRepository<UserPayoutAccount, Guid> payoutAccounts)
    {
        _users = users;
        _consents = consents;
        _payoutAccounts = payoutAccounts;
    }

    public async Task<CustomerProfileDto> GetAsync()
    {
        var user = await _users.GetAsync(CurrentUser.GetId(), includeDetails: true);
        var hasConsent = await _consents.AnyAsync(x => x.UserId == user.Id &&
            x.TermsVersion == WebHoanTienConsts.TermsVersion && x.PrivacyVersion == WebHoanTienConsts.PrivacyVersion);
        var payoutAccount = await _payoutAccounts.FindAsync(x => x.UserId == user.Id);
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
            HasCurrentLegalConsent = hasConsent,
            PayoutAccount = payoutAccount is null ? null : MapPayoutAccount(payoutAccount)
        };
    }

    [DisableAuditing]
    public async Task<PayoutAccountDto> UpdatePayoutAccountAsync(UpdatePayoutAccountInput input)
    {
        var bankCode = input.BankCode.Trim().ToUpperInvariant();
        var accountNumber = input.AccountNumber.Trim();
        var accountHolderName = input.AccountHolderName.Trim();
        if (!PayoutBankCatalog.IsSupported(bankCode))
            throw new UserFriendlyException("Ngân hàng đã chọn chưa được CatBack hỗ trợ.");
        if (!Regex.IsMatch(accountNumber, @"^\d{6,30}$"))
            throw new UserFriendlyException("Số tài khoản phải gồm từ 6 đến 30 chữ số.");
        if (accountHolderName.Length is < 2 or > 150)
            throw new UserFriendlyException("Tên chủ tài khoản phải có từ 2 đến 150 ký tự.");

        var userId = CurrentUser.GetId();
        var payoutAccount = await _payoutAccounts.FindAsync(x => x.UserId == userId);
        if (payoutAccount is null)
        {
            payoutAccount = new UserPayoutAccount(GuidGenerator.Create(), userId, bankCode, accountNumber, accountHolderName);
            await _payoutAccounts.InsertAsync(payoutAccount, autoSave: true);
        }
        else
        {
            payoutAccount.Update(bankCode, accountNumber, accountHolderName);
            await _payoutAccounts.UpdateAsync(payoutAccount, autoSave: true);
        }

        return MapPayoutAccount(payoutAccount);
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

    private static PayoutAccountDto MapPayoutAccount(UserPayoutAccount account) => new()
    {
        BankCode = account.BankCode,
        AccountNumber = account.AccountNumber,
        AccountHolderName = account.AccountHolderName
    };
}
