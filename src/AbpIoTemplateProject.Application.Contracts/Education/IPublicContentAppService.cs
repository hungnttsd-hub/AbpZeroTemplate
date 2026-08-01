using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AbpIoTemplateProject.Education;

public interface IPublicContentAppService : IApplicationService
{
    Task<List<LearningPathDto>> GetLearningPathsAsync();
    Task<List<ArticleCardDto>> GetArticlesAsync();
    Task<ArticleDetailDto?> GetArticleBySlugAsync(string slug);
    Task<List<LearningDocumentDto>> GetDocumentsAsync();
    Task<List<StudentAchievementDto>> GetFeaturedAchievementsAsync();
    Task<List<CampusDto>> GetCampusesAsync();
}
