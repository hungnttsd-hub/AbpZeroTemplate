using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.Content.Articles;

[Authorize(AbpIoTemplateProjectPermissions.Content.Default)]
public class EditModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty] public UpsertArticleDto Input { get; set; } = new() { Content = string.Empty };
    public List<SelectOptionDto> Categories { get; private set; } = new();
    public EditModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() { if (Id.HasValue) Input = await _adminEducationAppService.GetArticleForEditAsync(Id.Value); Categories = await _adminEducationAppService.GetArticleCategoryOptionsAsync(); }
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) { Categories = await _adminEducationAppService.GetArticleCategoryOptionsAsync(); return Page(); } if (Id.HasValue) await _adminEducationAppService.UpdateArticleAsync(Id.Value, Input); else await _adminEducationAppService.CreateArticleAsync(Input); return RedirectToPage("/Admin/Content/Articles/Index"); }
}
