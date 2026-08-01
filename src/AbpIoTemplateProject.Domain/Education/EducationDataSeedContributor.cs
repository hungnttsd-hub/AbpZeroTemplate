using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;

namespace AbpIoTemplateProject.Education;

public class EducationDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly IRepository<CourseCategory, Guid> _categoryRepository;
    private readonly IRepository<CourseLevel, Guid> _levelRepository;
    private readonly IRepository<Course, Guid> _courseRepository;
    private readonly IRepository<Teacher, Guid> _teacherRepository;
    private readonly IRepository<Campus, Guid> _campusRepository;
    private readonly IRepository<CourseClass, Guid> _classRepository;
    private readonly IRepository<PlacementTest, Guid> _testRepository;
    private readonly IRepository<PlacementQuestion, Guid> _questionRepository;

    public EducationDataSeedContributor(
        IGuidGenerator guidGenerator, IClock clock,
        IRepository<CourseCategory, Guid> categoryRepository, IRepository<CourseLevel, Guid> levelRepository,
        IRepository<Course, Guid> courseRepository, IRepository<Teacher, Guid> teacherRepository,
        IRepository<Campus, Guid> campusRepository, IRepository<CourseClass, Guid> classRepository,
        IRepository<PlacementTest, Guid> testRepository, IRepository<PlacementQuestion, Guid> questionRepository)
    {
        _guidGenerator = guidGenerator; _clock = clock; _categoryRepository = categoryRepository; _levelRepository = levelRepository;
        _courseRepository = courseRepository; _teacherRepository = teacherRepository; _campusRepository = campusRepository;
        _classRepository = classRepository; _testRepository = testRepository; _questionRepository = questionRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _courseRepository.GetCountAsync() > 0)
        {
            return;
        }

        var category = new CourseCategory(_guidGenerator.Create()) { Name = "Khóa học IELTS", Slug = "khoa-hoc-ielts", Description = "Các khóa học IELTS theo lộ trình", DisplayOrder = 1 };
        var foundation = new CourseLevel(_guidGenerator.Create()) { Name = "Nền tảng", Slug = "nen-tang", EntryRequirement = "Mất gốc đến 3.0", TargetOutcome = "IELTS 4.0", DisplayOrder = 1 };
        var preIelts = new CourseLevel(_guidGenerator.Create()) { Name = "Pre IELTS", Slug = "pre-ielts", EntryRequirement = "IELTS 4.0", TargetOutcome = "IELTS 5.0", DisplayOrder = 2 };
        await _categoryRepository.InsertAsync(category, autoSave: true);
        await _levelRepository.InsertManyAsync(new[] { foundation, preIelts }, autoSave: true);

        var teacher = new Teacher(_guidGenerator.Create())
        {
            FullName = "Đội ngũ IZONE", Slug = "doi-ngu-izone", Title = "Giảng viên IELTS", Credentials = "Đồng hành theo lộ trình cá nhân", IsFeatured = true, Status = PublicationStatus.Active
        };
        await _teacherRepository.InsertAsync(teacher, autoSave: true);

        var course1 = CreateCourse("IZ-FOUNDATION", "IELTS Vỡ lòng", "ielts-vo-long", category.Id, foundation.Id, "0.0 – 3.0 IELTS", "4.0 IELTS", 27, 54, 3900000m, "Xây nền tảng tiếng Anh và làm quen với IELTS.", true);
        var course2 = CreateCourse("IZ-PRE", "Pre IELTS", "pre-ielts", category.Id, preIelts.Id, "4.0 IELTS", "5.0 IELTS", 26, 52, 4300000m, "Hệ thống hóa kỹ năng và kiến thức trọng tâm.", true);
        var course3 = CreateCourse("IZ-STRATEGY", "IELTS Chiến lược", "ielts-chien-luoc", category.Id, preIelts.Id, "5.0 IELTS", "6.0 IELTS", 31, 62, 4900000m, "Nâng hiệu quả xử lý các dạng bài IELTS.", true);
        var course4 = CreateCourse("IZ-ADVANCED", "IELTS Chuyên sâu", "ielts-chuyen-sau", category.Id, null, "6.0 IELTS", "7.0+ IELTS", 30, 60, 5300000m, "Chinh phục mục tiêu điểm số cao hơn.", true);
        await _courseRepository.InsertManyAsync(new[] { course1, course2, course3, course4 }, autoSave: true);

        var campus = new Campus(_guidGenerator.Create()) { Name = "Cơ sở Hoàng Cầu", Address = "Số 4, ngõ 95 Hoàng Cầu, Đống Đa, Hà Nội", Hotline = "0969 091 503" };
        await _campusRepository.InsertAsync(campus, autoSave: true);
        await _classRepository.InsertAsync(new CourseClass(_guidGenerator.Create())
        {
            Code = "IZ-FDN-01", CourseId = course1.Id, CampusId = campus.Id, TeacherId = teacher.Id,
            ScheduleText = "Thứ 2 - 4 - 6, 18:30 - 20:30", StartDate = _clock.Now.Date.AddDays(14), Capacity = 18, EnrolledCount = 6, Status = CourseClassStatus.OpenForEnrollment
        }, autoSave: true);

        var test = new PlacementTest(_guidGenerator.Create()) { Name = "Kiểm tra trình độ IELTS", Slug = "kiem-tra-trinh-do-ielts", Description = "Bài kiểm tra ngắn để gợi ý điểm bắt đầu phù hợp.", DurationMinutes = 30, Status = PlacementTestStatus.Published };
        await _testRepository.InsertAsync(test, autoSave: true);
        await _questionRepository.InsertAsync(new PlacementQuestion(_guidGenerator.Create())
        {
            PlacementTestId = test.Id, QuestionText = "Choose the correct sentence.", OptionsJson = "[\"She go to school every day.\",\"She goes to school every day.\",\"She going to school every day.\"]", CorrectAnswer = "1", Score = 1, DisplayOrder = 1
        }, autoSave: true);
    }

    private Course CreateCourse(string code, string name, string slug, Guid categoryId, Guid? levelId, string entryLevel, string targetLevel, int sessions, int hours, decimal fee, string description, bool featured)
        => new(_guidGenerator.Create())
        {
            Code = code, Name = name, Slug = slug, CategoryId = categoryId, LevelId = levelId, EntryLevel = entryLevel, TargetLevel = targetLevel,
            DeliveryMode = CourseDeliveryMode.Offline, SessionCount = sessions, DurationHours = hours, TuitionFee = fee, ShortDescription = description,
            Description = description, IsFeatured = featured, Status = PublicationStatus.Active
        };
}
