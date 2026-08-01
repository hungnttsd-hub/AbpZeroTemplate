using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;

namespace AbpIoTemplateProject.Education;

public class EducationContentSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly IRepository<Course, Guid> _courseRepository;
    private readonly IRepository<LearningPath, Guid> _pathRepository;
    private readonly IRepository<LearningPathStep, Guid> _stepRepository;
    private readonly IRepository<LearningPathCourse, Guid> _pathCourseRepository;
    private readonly IRepository<ArticleCategory, Guid> _articleCategoryRepository;
    private readonly IRepository<Article, Guid> _articleRepository;
    private readonly IRepository<LearningDocument, Guid> _documentRepository;
    private readonly IRepository<StudentAchievement, Guid> _achievementRepository;
    private readonly IRepository<Banner, Guid> _bannerRepository;
    private readonly IRepository<SiteSetting, Guid> _siteSettingRepository;

    public EducationContentSeedContributor(
        IGuidGenerator guidGenerator, IClock clock, IRepository<Course, Guid> courseRepository,
        IRepository<LearningPath, Guid> pathRepository, IRepository<LearningPathStep, Guid> stepRepository,
        IRepository<LearningPathCourse, Guid> pathCourseRepository, IRepository<ArticleCategory, Guid> articleCategoryRepository,
        IRepository<Article, Guid> articleRepository, IRepository<LearningDocument, Guid> documentRepository,
        IRepository<StudentAchievement, Guid> achievementRepository, IRepository<Banner, Guid> bannerRepository,
        IRepository<SiteSetting, Guid> siteSettingRepository)
    {
        _guidGenerator = guidGenerator; _clock = clock; _courseRepository = courseRepository; _pathRepository = pathRepository;
        _stepRepository = stepRepository; _pathCourseRepository = pathCourseRepository; _articleCategoryRepository = articleCategoryRepository;
        _articleRepository = articleRepository; _documentRepository = documentRepository; _achievementRepository = achievementRepository;
        _bannerRepository = bannerRepository; _siteSettingRepository = siteSettingRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _pathRepository.GetCountAsync() == 0)
        {
            var path = new LearningPath(_guidGenerator.Create()) { Code = "IELTS-ROADMAP", Name = "Lộ trình IELTS từ mất gốc đến 7.0+", Slug = "lo-trinh-ielts", EntryLevel = "Mất gốc", TargetLevel = "IELTS 7.0+", IntendedAudience = "Người học cần một lộ trình rõ ràng", Description = "Đi từ nền tảng tiếng Anh đến kỹ năng làm bài IELTS theo từng giai đoạn.", DurationMonths = 12, DisplayOrder = 1, Status = PublicationStatus.Active };
            await _pathRepository.InsertAsync(path, autoSave: true);
            await _stepRepository.InsertManyAsync(new[]
            {
                new LearningPathStep(_guidGenerator.Create()) { LearningPathId = path.Id, Name = "Xây nền tảng", EntryLevel = "0.0", TargetLevel = "4.0", Description = "Củng cố phát âm, ngữ pháp và vốn từ cốt lõi.", DurationWeeks = 12, DisplayOrder = 1 },
                new LearningPathStep(_guidGenerator.Create()) { LearningPathId = path.Id, Name = "Làm chủ kỹ năng", EntryLevel = "4.0", TargetLevel = "5.5", Description = "Hệ thống hóa bốn kỹ năng và chiến thuật xử lý dạng bài.", DurationWeeks = 12, DisplayOrder = 2 },
                new LearningPathStep(_guidGenerator.Create()) { LearningPathId = path.Id, Name = "Bứt phá mục tiêu", EntryLevel = "5.5", TargetLevel = "7.0+", Description = "Luyện đề, nhận phản hồi và tối ưu điểm yếu cá nhân.", DurationWeeks = 16, DisplayOrder = 3 }
            }, autoSave: true);

            var courses = await _courseRepository.GetListAsync();
            var order = 1;
            foreach (var course in courses)
            {
                await _pathCourseRepository.InsertAsync(new LearningPathCourse(_guidGenerator.Create()) { LearningPathId = path.Id, CourseId = course.Id, DisplayOrder = order++ }, autoSave: true);
            }
        }

        if (await _articleRepository.GetCountAsync() == 0)
        {
            var category = new ArticleCategory(_guidGenerator.Create()) { Name = "Kinh nghiệm IELTS", Slug = "kinh-nghiem-ielts", Description = "Kiến thức và chiến thuật học IELTS", DisplayOrder = 1, Status = PublicationStatus.Active };
            await _articleCategoryRepository.InsertAsync(category, autoSave: true);
            await _articleRepository.InsertManyAsync(new[]
            {
                new Article(_guidGenerator.Create()) { CategoryId = category.Id, Title = "Cách xây nền tảng từ vựng cho IELTS", Slug = "xay-nen-tang-tu-vung-ielts", Excerpt = "Một cách học từ vựng có hệ thống, dễ duy trì và ứng dụng được.", Content = "Hãy bắt đầu từ cụm từ, ngữ cảnh và ôn tập lặp lại. Mỗi ngày chọn một lượng vừa phải, đặt câu và dùng lại trong bốn kỹ năng.", AuthorName = "IZONE", PublishedAt = _clock.Now, IsFeatured = true, Status = PublicationStatus.Active },
                new Article(_guidGenerator.Create()) { CategoryId = category.Id, Title = "Luyện Reading hiệu quả với Skimming và Scanning", Slug = "reading-skimming-scanning", Excerpt = "Hiểu đúng hai kỹ năng đọc nhanh thường gặp trong IELTS Reading.", Content = "Skimming để nắm ý chính, Scanning để tìm dữ kiện. Điều quan trọng là xác định từ khóa và giới hạn thời gian cho từng đoạn.", AuthorName = "IZONE", PublishedAt = _clock.Now, IsFeatured = true, Status = PublicationStatus.Active }
            }, autoSave: true);
        }

        if (await _documentRepository.GetCountAsync() == 0)
        {
            await _documentRepository.InsertAsync(new LearningDocument(_guidGenerator.Create()) { Name = "Checklist tự học IELTS", Slug = "checklist-tu-hoc-ielts", Description = "Danh sách công việc giúp bạn duy trì nhịp tự học hằng tuần.", FileUrl = "/izone-assets/documents/checklist-tu-hoc-ielts.txt", Skill = "General", Level = "Mọi trình độ", AccessLevel = DocumentAccessLevel.Public, Status = PublicationStatus.Active }, autoSave: true);
        }

        if (await _achievementRepository.GetCountAsync() == 0)
        {
            await _achievementRepository.InsertAsync(new StudentAchievement(_guidGenerator.Create()) { StudentName = "Học viên IZONE", BeforeResult = "5.5", AfterResult = "7.0", Story = "Tiến bộ nhờ lộ trình rõ ràng, phản hồi đều đặn và duy trì thực hành.", IsFeatured = true, Status = PublicationStatus.Active }, autoSave: true);
        }

        if (await _bannerRepository.GetCountAsync() == 0)
        {
            await _bannerRepository.InsertAsync(new Banner(_guidGenerator.Create()) { Name = "Trang chủ", Heading = "Học IELTS có lộ trình", Description = "Tiến bộ theo từng bước cùng IZONE.", CallToActionText = "Nhận tư vấn", CallToActionUrl = "/#dang-ky", DisplayOrder = 1, Status = PublicationStatus.Active }, autoSave: true);
        }

        if (await _siteSettingRepository.GetCountAsync() == 0)
        {
            await _siteSettingRepository.InsertManyAsync(new[]
            {
                new SiteSetting(_guidGenerator.Create()) { Key = "Contact.Hotline", Value = "0969091503", Description = "Hotline tư vấn" },
                new SiteSetting(_guidGenerator.Create()) { Key = "Contact.Email", Value = "ielts.izone@gmail.com", Description = "Email tư vấn" }
            }, autoSave: true);
        }
    }
}
