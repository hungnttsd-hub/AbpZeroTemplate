using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Content.Articles;

[Authorize(AbpIoTemplateProjectPermissions.Content.Default)]
public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    public List<AdminArticleDto> Articles { get; private set; } = new();
    public IndexModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() => Articles = await _adminEducationAppService.GetArticlesAsync();
    public async Task<IActionResult> OnPostDeleteAsync(Guid id) { await _adminEducationAppService.DeleteArticleAsync(id); return RedirectToPage(); }
}
