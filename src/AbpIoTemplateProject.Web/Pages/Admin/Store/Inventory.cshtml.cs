using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Store;

[Authorize(AbpIoTemplateProjectPermissions.Inventory.View)]
public class InventoryModel : AbpIoTemplateProjectPageModel
{
    private readonly IStoreAdminAppService _admin;
    public List<InventoryItemDto> Items { get; private set; } = new();
    public InventoryModel(IStoreAdminAppService admin) { _admin = admin; }
    public async Task OnGetAsync() { Items = await _admin.GetInventoryAsync(); }
    public async Task<IActionResult> OnPostAdjustAsync(AdjustInventoryInput input) { await _admin.AdjustInventoryAsync(input); return RedirectToPage(); }
}
