using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Store;

[Authorize(AbpIoTemplateProjectPermissions.Categories.Default)]
public class CatalogModel : AbpIoTemplateProjectPageModel
{
    private readonly IStoreAdminAppService _admin;
    public List<CategoryDto> Categories { get; private set; } = new();
    public List<BrandDto> Brands { get; private set; } = new();
    public List<SupplierDto> Suppliers { get; private set; } = new();
    public CatalogModel(IStoreAdminAppService admin) { _admin = admin; }
    public async Task OnGetAsync() { await LoadAsync(); }
    public async Task<IActionResult> OnPostCategoryAsync(SaveCategoryInput input) { await _admin.SaveCategoryAsync(input); return RedirectToPage(); }
    public async Task<IActionResult> OnPostBrandAsync(SaveBrandInput input) { await _admin.SaveBrandAsync(input); return RedirectToPage(); }
    public async Task<IActionResult> OnPostSupplierAsync(SaveSupplierInput input) { await _admin.SaveSupplierAsync(input); return RedirectToPage(); }
    private async Task LoadAsync() { Categories = await _admin.GetCategoriesAsync(); Brands = await _admin.GetBrandsAsync(); Suppliers = await _admin.GetSuppliersAsync(); }
}
