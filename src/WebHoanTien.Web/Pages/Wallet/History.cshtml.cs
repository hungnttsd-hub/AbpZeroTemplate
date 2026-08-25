using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Application.Dtos;
using WebHoanTien.Affiliates;

namespace WebHoanTien.Web.Pages.Wallet;

[Authorize]
public class HistoryModel : PageModel
{
    private readonly ICustomerWalletAppService _wallet;
    private const int PageSize = 20;

    public PagedResultDto<WalletMovementDto> Movements { get; private set; } = new();
    public WalletMovementKind? Kind { get; private set; }
    public int CurrentPage { get; private set; } = 1;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Movements.TotalCount / (double)PageSize));

    public HistoryModel(ICustomerWalletAppService wallet) => _wallet = wallet;

    public async Task OnGetAsync(WalletMovementKind? kind, int page = 1)
    {
        Kind = kind;
        CurrentPage = Math.Max(1, page);
        Movements = await _wallet.GetHistoryAsync(new WalletHistoryInput
        {
            Kind = kind,
            SkipCount = (CurrentPage - 1) * PageSize,
            MaxResultCount = PageSize
        });
    }
}
