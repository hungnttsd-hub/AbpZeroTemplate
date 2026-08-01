using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AbpIoTemplateProject.Education;

public class PublicContentAppService : ApplicationService, IPublicContentAppService
{
    private readonly IRepository<LearningPath, Guid> _pathRepository;
    private readonly IRepository<LearningPathStep, Guid> _stepRepository;
    private readonly IRepository<Article, Guid> _articleRepository;
    private readonly IRepository<ArticleCategory, Guid> _articleCategoryRepository;
    private readonly IRepository<LearningDocument, Guid> _documentRepository;
    private readonly IRepository<StudentAchievement, Guid> _achievementRepository;
    private readonly IRepository<Campus, Guid> _campusRepository;

    public PublicContentAppService(
        IRepository<LearningPath, Guid> pathRepository, IRepository<LearningPathStep, Guid> stepRepository,
        IRepository<Article, Guid> articleRepository, IRepository<ArticleCategory, Guid> articleCategoryRepository,
        IRepository<LearningDocument, Guid> documentRepository, IRepository<StudentAchievement, Guid> achievementRepository,
        IRepository<Campus, Guid> campusRepository)
    {
        _pathRepository = pathRepository; _stepRepository = stepRepository; _articleRepository = articleRepository;
        _articleCategoryRepository = articleCategoryRepository; _documentRepository = documentRepository;
        _achievementRepository = achievementRepository; _campusRepository = campusRepository;
    }

    public async Task<List<LearningPathDto>> GetLearningPathsAsync()
    {
        var pathQuery = await _pathRepository.GetQueryableAsync();
        var stepQuery = await _stepRepository.GetQueryableAsync();
        var paths = await AsyncExecuter.ToListAsync(pathQuery.Where(x => x.Status == PublicationStatus.Active).OrderBy(x => x.DisplayOrder));
        var steps = await AsyncExecuter.ToListAsync(stepQuery.OrderBy(x => x.DisplayOrder));
        return paths.Select(path => new LearningPathDto
        {
            Id = path.Id, Name = path.Name, Slug = path.Slug, EntryLevel = path.EntryLevel, TargetLevel = path.TargetLevel,
            IntendedAudience = path.IntendedAudience, Description = path.Description, DurationMonths = path.DurationMonths,
            Steps = steps.Where(step => step.LearningPathId == path.Id).Select(step => new LearningPathStepDto { Name = step.Name, EntryLevel = step.EntryLevel, TargetLevel = step.TargetLevel, Description = step.Description, DurationWeeks = step.DurationWeeks, DisplayOrder = step.DisplayOrder }).ToList()
        }).ToList();
    }

    public async Task<List<ArticleCardDto>> GetArticlesAsync()
    {
        var articles = await _articleRepository.GetQueryableAsync();
        var categories = await _articleCategoryRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(
            from article in articles
            join category in categories on article.CategoryId equals category.Id into categoryJoin
            from category in categoryJoin.DefaultIfEmpty()
            where article.Status == PublicationStatus.Active && article.PublishedAt != null && article.PublishedAt <= Clock.Now
            orderby article.IsFeatured descending, article.PublishedAt descending
            select new ArticleCardDto { Id = article.Id, Title = article.Title, Slug = article.Slug, Excerpt = article.Excerpt, CoverImageUrl = article.CoverImageUrl, CategoryName = category == null ? null : category.Name, AuthorName = article.AuthorName, PublishedAt = article.PublishedAt });
    }

    public async Task<ArticleDetailDto?> GetArticleBySlugAsync(string slug)
    {
        var articles = await _articleRepository.GetQueryableAsync();
        var categories = await _articleCategoryRepository.GetQueryableAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(
            from article in articles
            join category in categories on article.CategoryId equals category.Id into categoryJoin
            from category in categoryJoin.DefaultIfEmpty()
            where article.Slug == slug && article.Status == PublicationStatus.Active && article.PublishedAt != null && article.PublishedAt <= Clock.Now
            select new ArticleDetailDto { Id = article.Id, Title = article.Title, Slug = article.Slug, Excerpt = article.Excerpt, Content = article.Content, CoverImageUrl = article.CoverImageUrl, CategoryName = category == null ? null : category.Name, AuthorName = article.AuthorName, PublishedAt = article.PublishedAt, MetaTitle = article.MetaTitle, MetaDescription = article.MetaDescription });
    }

    public async Task<List<LearningDocumentDto>> GetDocumentsAsync()
    {
        var query = await _documentRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.Where(x => x.Status == PublicationStatus.Active && x.AccessLevel == DocumentAccessLevel.Public).OrderBy(x => x.Name).Select(x => new LearningDocumentDto { Id = x.Id, Name = x.Name, Slug = x.Slug, Description = x.Description, CoverImageUrl = x.CoverImageUrl, FileUrl = x.FileUrl, Skill = x.Skill, Level = x.Level }));
    }

    public async Task<List<StudentAchievementDto>> GetFeaturedAchievementsAsync()
    {
        var query = await _achievementRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.Where(x => x.Status == PublicationStatus.Active && x.IsFeatured).Select(x => new StudentAchievementDto { Id = x.Id, StudentName = x.StudentName, BeforeResult = x.BeforeResult, AfterResult = x.AfterResult, Story = x.Story, PhotoUrl = x.PhotoUrl }));
    }

    public async Task<List<CampusDto>> GetCampusesAsync()
    {
        var query = await _campusRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.Where(x => x.Status == PublicationStatus.Active).OrderBy(x => x.Name).Select(x => new CampusDto { Id = x.Id, Name = x.Name, Address = x.Address, Hotline = x.Hotline, MapUrl = x.MapUrl }));
    }
}
