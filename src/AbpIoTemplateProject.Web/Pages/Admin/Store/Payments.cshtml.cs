using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Store;

[Authorize(AbpIoTemplateProjectPermissions.Payments.View)]
public class PaymentsModel : AbpIoTemplateProjectPageModel
{
    private readonly IStoreAdminAppService _admin;
    public List<AdminPaymentDto> Payments { get; private set; } = new();
    public PaymentsModel(IStoreAdminAppService admin) { _admin = admin; }
    public async Task OnGetAsync() { Payments = await _admin.GetPaymentsAsync(); }
    public async Task<IActionResult> OnPostConfirmAsync(ConfirmPaymentInput input) { await _admin.ConfirmPaymentAsync(input); return RedirectToPage(); }
}
