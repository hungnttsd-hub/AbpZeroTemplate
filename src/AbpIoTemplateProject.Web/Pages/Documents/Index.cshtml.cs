using System.Collections.Generic;
using System.Threading.Tasks;
using AbpIoTemplateProject.Education;

namespace AbpIoTemplateProject.Web.Pages.Documents;

public class IndexModel : AbpIoTemplateProjectPageModel
{
    private readonly IPublicContentAppService _contentAppService;
    public List<LearningDocumentDto> Documents { get; private set; } = new();
    public IndexModel(IPublicContentAppService contentAppService) => _contentAppService = contentAppService;
    public async Task OnGetAsync() => Documents = await _contentAppService.GetDocumentsAsync();
}
