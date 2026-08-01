using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Content.Documents;

[Authorize(AbpIoTemplateProjectPermissions.Content.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public List<AdminDocumentDto> Documents { get; private set; } = new();
    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => Documents = await _adminEducationAppService.GetDocumentsAsync();
    public async Task<IActionResult> OnPostDeleteAsync(Guid id) { await _adminEducationAppService.DeleteDocumentAsync(id); return RedirectToPage(); }
}
