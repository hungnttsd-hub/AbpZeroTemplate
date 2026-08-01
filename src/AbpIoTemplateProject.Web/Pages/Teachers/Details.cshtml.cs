using System.Threading.Tasks;
using AbpIoTemplateProject.Education;
using Microsoft.AspNetCore.Mvc;

namespace AbpIoTemplateProject.Web.Pages.Teachers;

public class DetailsModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicEducationAppService _publicEducationAppService;
    public TeacherDetailDto? Teacher { get; private set; }
    public DetailsModel(IPublicEducationAppService publicEducationAppService) => _publicEducationAppService = publicEducationAppService;
    public async Task<IActionResult> OnGetAsync(string slug) { Teacher = await _publicEducationAppService.GetTeacherBySlugAsync(slug); return Teacher == null ? NotFound() : Page(); }
}
