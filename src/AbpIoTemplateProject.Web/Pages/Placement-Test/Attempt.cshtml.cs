using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.PlacementTest;

public class AttemptModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicEducationAppService _educationAppService;

    [BindProperty(SupportsGet = true)]
    public Guid AttemptId { get; set; }

    public List<PlacementQuestionDto> Questions { get; private set; } = new();

    [BindProperty]
    public List<PlacementAnswerInputDto> Answers { get; set; } = new();

    public PlacementResultDto? Result { get; private set; }

    public AttemptModel(IPublicEducationAppService educationAppService) => _educationAppService = educationAppService;

    public async Task OnGetAsync()
    {
        Questions = await _educationAppService.GetPlacementQuestionsAsync(AttemptId);
        Answers = Questions.ConvertAll(question => new PlacementAnswerInputDto { PlacementQuestionId = question.Id });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Questions = await _educationAppService.GetPlacementQuestionsAsync(AttemptId);
            return Page();
        }

        Result = await _educationAppService.SubmitPlacementAttemptAsync(new SubmitPlacementAttemptDto { PlacementAttemptId = AttemptId, Answers = Answers });
        return Page();
    }
}
