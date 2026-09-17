using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using WebHoanTien.Affiliates;
using WebHoanTien.Permissions;

namespace WebHoanTien.Admin;

[Authorize(WebHoanTienPermissions.Admin.Payouts)]
[RemoteService(IsEnabled = false)]
public class AdminWalletAppService : WebHoanTienAppService, IAdminWalletAppService
{
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly WalletBalanceCalculator _balances;

    public AdminWalletAppService(IRepository<IdentityUser, Guid> users, WalletBalanceCalculator balances)
    {
        _users = users;
        _balances = balances;
    }

    public async Task<PagedResultDto<AdminUserWalletDto>> GetListAsync(AdminWalletListInput input)
    {
        var query = await _users.GetQueryableAsync();
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var term = input.Filter.Trim().ToUpperInvariant();
            var hasUserId = Guid.TryParse(input.Filter.Trim(), out var userId);
            query = query.Where(user => user.NormalizedUserName.Contains(term) ||
                (user.NormalizedEmail != null && user.NormalizedEmail.Contains(term)) ||
                (user.Name != null && user.Name.ToUpper().Contains(term)) ||
                (user.Surname != null && user.Surname.ToUpper().Contains(term)) ||
                (hasUserId && user.Id == userId));
        }

        var count = await AsyncExecuter.CountAsync(query);
        var users = await AsyncExecuter.ToListAsync(query.OrderBy(user => user.UserName).ThenBy(user => user.Id)
            .Skip(Math.Max(0, input.SkipCount)).Take(Math.Clamp(input.MaxResultCount, 1, 100)));
        var balances = await _balances.GetManyAsync(users.Select(user => user.Id).ToList());
        return new PagedResultDto<AdminUserWalletDto>(count, users.Select(user =>
        {
            var balance = balances[user.Id];
            return new AdminUserWalletDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                DisplayName = string.Join(" ", new[] { user.Name, user.Surname }
                    .Where(part => !string.IsNullOrWhiteSpace(part))),
                IsActive = user.IsActive,
                AvailableBalance = balance.AvailableBalance,
                ConfirmedAmount = balance.ConfirmedAmount,
                PendingAmount = balance.PendingAmount,
                ReservedAmount = balance.ReservedAmount,
                PaidAmount = balance.PaidAmount,
                RawBalance = balance.RawBalance
            };
        }).ToList());
    }
}
