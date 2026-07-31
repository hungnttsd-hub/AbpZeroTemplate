using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using AbpIoTemplateProject.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace AbpIoTemplateProject.Web.Pages.Admin.Store;

[Authorize(AbpIoTemplateProjectPermissions.Products.View)]
public class ProductsModel : AbpIoTemplateProjectPageModel
{
    private readonly IStoreAdminAppService _adminAppService;
    public PagedResultDto<ProductListItemDto> Products { get; private set; } = new();
    public List<CategoryDto> Categories { get; private set; } = new();
    public List<BrandDto> Brands { get; private set; } = new();
    public List<SupplierDto> Suppliers { get; private set; } = new();

    [BindProperty]
    public SaveProductInput Input { get; set; } = new() { Unit = "Sản phẩm", TaxRate = 0, MinimumOrderQuantity = 1, MaximumOrderQuantity = 99 };

    public ProductsModel(IStoreAdminAppService adminAppService) { _adminAppService = adminAppService; }
    public async Task OnGetAsync() { await LoadAsync(); }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }
        await _adminAppService.SaveProductAsync(Input);
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostVariantAsync(SaveProductVariantInput input) { await _adminAppService.AddProductVariantAsync(input); return RedirectToPage(); }
    public async Task<IActionResult> OnPostImageAsync(SaveProductImageInput input) { await _adminAppService.AddProductImageAsync(input); return RedirectToPage(); }
    private async Task LoadAsync()
    {
        Products = await _adminAppService.GetProductsAsync(new ProductListInput { MaxResultCount = 60 });
        Categories = await _adminAppService.GetCategoriesAsync();
        Brands = await _adminAppService.GetBrandsAsync();
        Suppliers = await _adminAppService.GetSuppliersAsync();
    }
}
