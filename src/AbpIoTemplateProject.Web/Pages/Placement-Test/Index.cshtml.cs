using System;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.PlacementTest;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicEducationAppService _educationAppService;
    public PlacementTestDto? Test { get; private set; }

    [BindProperty]
    public StartPlacementAttemptDto Input { get; set; } = new();

    public IndexModel(IPublicEducationAppService educationAppService) => _educationAppService = educationAppService;

    public async Task OnGetAsync() => Test = await _educationAppService.GetPublishedPlacementTestAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        Test = await _educationAppService.GetPublishedPlacementTestAsync();
        if (Test is null)
        {
            return NotFound();
        }

        Input.PlacementTestId = Test.Id;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var attemptId = await _educationAppService.StartPlacementAttemptAsync(Input);
        return RedirectToPage("/Placement-Test/Attempt", new { attemptId });
    }
}
