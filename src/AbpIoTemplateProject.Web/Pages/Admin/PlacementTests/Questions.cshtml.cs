using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Admin.PlacementTests;

[Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Default)]
public class QuestionsModel : AbpIoTemplateProjectPageModel
{
    private readonly IAdminEducationAppService _adminEducationAppService;
    [BindProperty(SupportsGet = true)] public Guid TestId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty] public UpsertPlacementQuestionDto Input { get; set; } = new();
    public List<AdminPlacementQuestionDto> Questions { get; private set; } = new();
    public QuestionsModel(IAdminEducationAppService adminEducationAppService) => _adminEducationAppService = adminEducationAppService;
    public async Task OnGetAsync() { if (Id.HasValue) Input = await _adminEducationAppService.GetPlacementQuestionForEditAsync(Id.Value); Questions = await _adminEducationAppService.GetPlacementQuestionsAsync(TestId); }
    public async Task<IActionResult> OnPostSaveAsync() { if (!ModelState.IsValid) { Questions = await _adminEducationAppService.GetPlacementQuestionsAsync(TestId); return Page(); } if (Id.HasValue) await _adminEducationAppService.UpdatePlacementQuestionAsync(Id.Value, Input); else await _adminEducationAppService.CreatePlacementQuestionAsync(TestId, Input); return RedirectToPage(new { TestId }); }
    public async Task<IActionResult> OnPostDeleteAsync(Guid id) { await _adminEducationAppService.DeletePlacementQuestionAsync(id); return RedirectToPage(new { TestId }); }
}
