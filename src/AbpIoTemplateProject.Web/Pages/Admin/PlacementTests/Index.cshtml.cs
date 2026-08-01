using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.PlacementTests;

[Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public List<AdminPlacementTestDto> Tests { get; private set; } = new();
    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => Tests = await _adminEducationAppService.GetPlacementTestsAsync();
    public async Task<IActionResult> OnPostDeleteAsync(Guid id) { await _adminEducationAppService.DeletePlacementTestAsync(id); return RedirectToPage(); }
}
