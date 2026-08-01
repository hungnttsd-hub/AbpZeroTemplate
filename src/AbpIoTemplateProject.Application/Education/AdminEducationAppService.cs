using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbpIoTemplateProject.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AbpIoTemplateProject.Education;

[Authorize]
public class AdminEducationAppService : ApplicationService, IAdminEducationAppService
{
    private readonly IRepository<Course, Guid> _courseRepository;
    private readonly IRepository<Teacher, Guid> _teacherRepository;
    private readonly IRepository<CourseClass, Guid> _classRepository;
    private readonly IRepository<Campus, Guid> _campusRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<PlacementAttempt, Guid> _placementAttemptRepository;
    private readonly IRepository<Student, Guid> _studentRepository;
    private readonly IRepository<Enrollment, Guid> _enrollmentRepository;
    private readonly IRepository<Article, Guid> _articleRepository;
    private readonly IRepository<ArticleCategory, Guid> _articleCategoryRepository;
    private readonly IRepository<LearningDocument, Guid> _documentRepository;
    private readonly IRepository<PlacementTest, Guid> _placementTestRepository;
    private readonly IRepository<PlacementQuestion, Guid> _placementQuestionRepository;

    public AdminEducationAppService(
        IRepository<Course, Guid> courseRepository, IRepository<Teacher, Guid> teacherRepository,
        IRepository<CourseClass, Guid> classRepository, IRepository<Campus, Guid> campusRepository,
        IRepository<Lead, Guid> leadRepository, IRepository<PlacementAttempt, Guid> placementAttemptRepository,
        IRepository<Student, Guid> studentRepository, IRepository<Enrollment, Guid> enrollmentRepository,
        IRepository<Article, Guid> articleRepository, IRepository<ArticleCategory, Guid> articleCategoryRepository,
        IRepository<LearningDocument, Guid> documentRepository, IRepository<PlacementTest, Guid> placementTestRepository,
        IRepository<PlacementQuestion, Guid> placementQuestionRepository)
    {
        _courseRepository = courseRepository;
        _teacherRepository = teacherRepository;
        _classRepository = classRepository;
        _campusRepository = campusRepository;
        _leadRepository = leadRepository;
        _placementAttemptRepository = placementAttemptRepository;
        _studentRepository = studentRepository;
        _enrollmentRepository = enrollmentRepository;
        _articleRepository = articleRepository;
        _articleCategoryRepository = articleCategoryRepository;
        _documentRepository = documentRepository;
        _placementTestRepository = placementTestRepository;
        _placementQuestionRepository = placementQuestionRepository;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Courses.Default)]
    public async Task<EducationDashboardDto> GetDashboardAsync()
    {
        return new EducationDashboardDto
        {
            CourseCount = await _courseRepository.GetCountAsync(),
            TeacherCount = await _teacherRepository.GetCountAsync(),
            OpenClassCount = await _classRepository.CountAsync(x => x.Status == CourseClassStatus.OpenForEnrollment),
            NewLeadCount = await _leadRepository.CountAsync(x => x.Status == LeadStatus.New),
            PlacementAttemptCount = await _placementAttemptRepository.GetCountAsync()
        };
    }

    [Authorize(AbpIoTemplateProjectPermissions.Courses.Default)]
    public async Task<List<AdminCourseDto>> GetCoursesAsync()
    {
        var query = await _courseRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.OrderBy(x => x.Name).Select(x => new AdminCourseDto
        {
            Id = x.Id, Code = x.Code, Name = x.Name, Slug = x.Slug, EntryLevel = x.EntryLevel, TargetLevel = x.TargetLevel,
            SessionCount = x.SessionCount, DurationHours = x.DurationHours, TuitionFee = x.PromotionalFee ?? x.TuitionFee, IsFeatured = x.IsFeatured, Status = x.Status
        }));
    }

    [Authorize(AbpIoTemplateProjectPermissions.Courses.Update)]
    public async Task<UpsertCourseDto> GetCourseForEditAsync(Guid id)
    {
        return ToEditDto(await _courseRepository.GetAsync(id));
    }

    [Authorize(AbpIoTemplateProjectPermissions.Courses.Create)]
    public async Task<Guid> CreateCourseAsync(UpsertCourseDto input)
    {
        var course = new Course(GuidGenerator.Create());
        Apply(course, input);
        await _courseRepository.InsertAsync(course, autoSave: true);
        return course.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Courses.Update)]
    public async Task UpdateCourseAsync(Guid id, UpsertCourseDto input)
    {
        var course = await _courseRepository.GetAsync(id);
        Apply(course, input);
        await _courseRepository.UpdateAsync(course, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Courses.Delete)]
    public Task DeleteCourseAsync(Guid id) => _courseRepository.DeleteAsync(id, autoSave: true);

    [Authorize(AbpIoTemplateProjectPermissions.Teachers.Default)]
    public async Task<List<AdminTeacherDto>> GetTeachersAsync()
    {
        var query = await _teacherRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.OrderBy(x => x.FullName).Select(x => new AdminTeacherDto
        {
            Id = x.Id, FullName = x.FullName, Slug = x.Slug, Title = x.Title, Credentials = x.Credentials, IsFeatured = x.IsFeatured, Status = x.Status
        }));
    }

    [Authorize(AbpIoTemplateProjectPermissions.Teachers.Update)]
    public async Task<UpsertTeacherDto> GetTeacherForEditAsync(Guid id) => ToEditDto(await _teacherRepository.GetAsync(id));

    [Authorize(AbpIoTemplateProjectPermissions.Teachers.Create)]
    public async Task<Guid> CreateTeacherAsync(UpsertTeacherDto input)
    {
        var teacher = new Teacher(GuidGenerator.Create());
        Apply(teacher, input);
        await _teacherRepository.InsertAsync(teacher, autoSave: true);
        return teacher.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Teachers.Update)]
    public async Task UpdateTeacherAsync(Guid id, UpsertTeacherDto input)
    {
        var teacher = await _teacherRepository.GetAsync(id);
        Apply(teacher, input);
        await _teacherRepository.UpdateAsync(teacher, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Teachers.Delete)]
    public Task DeleteTeacherAsync(Guid id) => _teacherRepository.DeleteAsync(id, autoSave: true);

    [Authorize(AbpIoTemplateProjectPermissions.Classes.Default)]
    public async Task<List<AdminCourseClassDto>> GetClassesAsync()
    {
        var classes = await _classRepository.GetQueryableAsync();
        var courses = await _courseRepository.GetQueryableAsync();
        var teachers = await _teacherRepository.GetQueryableAsync();
        var campuses = await _campusRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(
            from item in classes
            join course in courses on item.CourseId equals course.Id
            join teacher in teachers on item.TeacherId equals teacher.Id into teacherJoin
            from teacher in teacherJoin.DefaultIfEmpty()
            join campus in campuses on item.CampusId equals campus.Id into campusJoin
            from campus in campusJoin.DefaultIfEmpty()
            orderby item.StartDate descending
            select new AdminCourseClassDto
            {
                Id = item.Id, Code = item.Code, CourseName = course.Name, CampusName = campus == null ? null : campus.Name,
                TeacherName = teacher == null ? null : teacher.FullName, StartDate = item.StartDate, Capacity = item.Capacity,
                EnrolledCount = item.EnrolledCount, Status = item.Status
            });
    }

    [Authorize(AbpIoTemplateProjectPermissions.Classes.Manage)]
    public async Task<UpsertCourseClassDto> GetClassForEditAsync(Guid id)
    {
        var item = await _classRepository.GetAsync(id);
        return new UpsertCourseClassDto
        {
            Code = item.Code, CourseId = item.CourseId, CampusId = item.CampusId, TeacherId = item.TeacherId,
            ScheduleText = item.ScheduleText, StartDate = item.StartDate, EndDate = item.EndDate,
            Capacity = item.Capacity, Status = item.Status
        };
    }

    [Authorize(AbpIoTemplateProjectPermissions.Classes.Manage)]
    public async Task<Guid> CreateClassAsync(UpsertCourseClassDto input)
    {
        var item = new CourseClass(GuidGenerator.Create());
        Apply(item, input);
        await _classRepository.InsertAsync(item, autoSave: true);
        return item.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Classes.Manage)]
    public async Task UpdateClassAsync(Guid id, UpsertCourseClassDto input)
    {
        var item = await _classRepository.GetAsync(id);
        Apply(item, input);
        await _classRepository.UpdateAsync(item, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Classes.Manage)]
    public Task DeleteClassAsync(Guid id) => _classRepository.DeleteAsync(id, autoSave: true);

    [Authorize(AbpIoTemplateProjectPermissions.Classes.Default)]
    public async Task<List<SelectOptionDto>> GetCourseOptionsAsync()
    {
        var query = await _courseRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.OrderBy(x => x.Name).Select(x => new SelectOptionDto { Id = x.Id, Name = x.Code + " — " + x.Name }));
    }

    [Authorize(AbpIoTemplateProjectPermissions.Classes.Default)]
    public async Task<List<SelectOptionDto>> GetTeacherOptionsAsync()
    {
        var query = await _teacherRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.OrderBy(x => x.FullName).Select(x => new SelectOptionDto { Id = x.Id, Name = x.FullName }));
    }

    [Authorize(AbpIoTemplateProjectPermissions.Classes.Default)]
    public async Task<List<SelectOptionDto>> GetCampusOptionsAsync()
    {
        var query = await _campusRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.OrderBy(x => x.Name).Select(x => new SelectOptionDto { Id = x.Id, Name = x.Name }));
    }

    [Authorize(AbpIoTemplateProjectPermissions.Students.Default)]
    public async Task<List<AdminStudentDto>> GetStudentsAsync()
    {
        var students = await _studentRepository.GetQueryableAsync();
        var enrollments = await _enrollmentRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(
            from student in students
            join enrollment in enrollments on student.Id equals enrollment.StudentId into enrollmentJoin
            orderby student.FullName
            select new AdminStudentDto
            {
                Id = student.Id, FullName = student.FullName, PhoneNumber = student.PhoneNumber, Email = student.Email,
                CurrentLevel = student.CurrentLevel, Target = student.Target, EnrollmentCount = enrollmentJoin.Count()
            });
    }

    [Authorize(AbpIoTemplateProjectPermissions.Students.Manage)]
    public async Task<UpsertStudentDto> GetStudentForEditAsync(Guid id)
    {
        var item = await _studentRepository.GetAsync(id);
        return new UpsertStudentDto { FullName = item.FullName, PhoneNumber = item.PhoneNumber, Email = item.Email, CurrentLevel = item.CurrentLevel, Target = item.Target };
    }

    [Authorize(AbpIoTemplateProjectPermissions.Students.Manage)]
    public async Task<Guid> CreateStudentAsync(UpsertStudentDto input)
    {
        var item = new Student(GuidGenerator.Create());
        Apply(item, input);
        await _studentRepository.InsertAsync(item, autoSave: true);
        return item.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Students.Manage)]
    public async Task UpdateStudentAsync(Guid id, UpsertStudentDto input)
    {
        var item = await _studentRepository.GetAsync(id);
        Apply(item, input);
        await _studentRepository.UpdateAsync(item, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Students.Manage)]
    public Task DeleteStudentAsync(Guid id) => _studentRepository.DeleteAsync(id, autoSave: true);

    [Authorize(AbpIoTemplateProjectPermissions.Enrollments.Default)]
    public async Task<List<AdminEnrollmentDto>> GetEnrollmentsAsync()
    {
        var enrollments = await _enrollmentRepository.GetQueryableAsync();
        var students = await _studentRepository.GetQueryableAsync();
        var classes = await _classRepository.GetQueryableAsync();
        var courses = await _courseRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(
            from enrollment in enrollments
            join student in students on enrollment.StudentId equals student.Id
            join courseClass in classes on enrollment.CourseClassId equals courseClass.Id
            join course in courses on courseClass.CourseId equals course.Id
            orderby enrollment.EnrolledAt descending
            select new AdminEnrollmentDto
            {
                Id = enrollment.Id, StudentName = student.FullName, ClassCode = courseClass.Code, CourseName = course.Name,
                EnrolledAt = enrollment.EnrolledAt, AgreedTuitionFee = enrollment.AgreedTuitionFee,
                PaidAmount = enrollment.PaidAmount, Status = enrollment.Status
            });
    }

    [Authorize(AbpIoTemplateProjectPermissions.Enrollments.Manage)]
    public async Task<Guid> CreateEnrollmentAsync(UpsertEnrollmentDto input)
    {
        var courseClass = await _classRepository.GetAsync(input.CourseClassId);
        if (courseClass.EnrolledCount >= courseClass.Capacity)
        {
            throw new Volo.Abp.UserFriendlyException("Lớp đã đủ sĩ số.");
        }
        var item = new Enrollment(GuidGenerator.Create())
        {
            StudentId = input.StudentId, CourseClassId = input.CourseClassId, EnrolledAt = Clock.Now,
            AgreedTuitionFee = input.AgreedTuitionFee, PaidAmount = input.PaidAmount, Status = input.Status?.Trim()
        };
        courseClass.EnrolledCount++;
        await _enrollmentRepository.InsertAsync(item, autoSave: true);
        await _classRepository.UpdateAsync(courseClass, autoSave: true);
        return item.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Enrollments.Manage)]
    public async Task DeleteEnrollmentAsync(Guid id)
    {
        var enrollment = await _enrollmentRepository.GetAsync(id);
        var courseClass = await _classRepository.FindAsync(enrollment.CourseClassId);
        if (courseClass != null && courseClass.EnrolledCount > 0)
        {
            courseClass.EnrolledCount--;
            await _classRepository.UpdateAsync(courseClass, autoSave: true);
        }
        await _enrollmentRepository.DeleteAsync(enrollment, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Default)]
    public async Task<List<AdminArticleDto>> GetArticlesAsync()
    {
        var articles = await _articleRepository.GetQueryableAsync();
        var categories = await _articleCategoryRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(
            from article in articles
            join category in categories on article.CategoryId equals category.Id into categoryJoin
            from category in categoryJoin.DefaultIfEmpty()
            orderby article.PublishedAt descending, article.CreationTime descending
            select new AdminArticleDto
            {
                Id = article.Id, Title = article.Title, Slug = article.Slug, CategoryName = category == null ? null : category.Name,
                PublishedAt = article.PublishedAt, IsFeatured = article.IsFeatured, Status = article.Status
            });
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Manage)]
    public async Task<UpsertArticleDto> GetArticleForEditAsync(Guid id)
    {
        var item = await _articleRepository.GetAsync(id);
        return new UpsertArticleDto
        {
            CategoryId = item.CategoryId, Title = item.Title, Slug = item.Slug, Excerpt = item.Excerpt, Content = item.Content,
            CoverImageUrl = item.CoverImageUrl, AuthorName = item.AuthorName, MetaTitle = item.MetaTitle,
            MetaDescription = item.MetaDescription, PublishedAt = item.PublishedAt, IsFeatured = item.IsFeatured, Status = item.Status
        };
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Manage)]
    public async Task<Guid> CreateArticleAsync(UpsertArticleDto input)
    {
        var item = new Article(GuidGenerator.Create());
        Apply(item, input);
        await _articleRepository.InsertAsync(item, autoSave: true);
        return item.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Manage)]
    public async Task UpdateArticleAsync(Guid id, UpsertArticleDto input)
    {
        var item = await _articleRepository.GetAsync(id);
        Apply(item, input);
        await _articleRepository.UpdateAsync(item, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Manage)]
    public Task DeleteArticleAsync(Guid id) => _articleRepository.DeleteAsync(id, autoSave: true);

    [Authorize(AbpIoTemplateProjectPermissions.Content.Default)]
    public async Task<List<SelectOptionDto>> GetArticleCategoryOptionsAsync()
    {
        var query = await _articleCategoryRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).Select(x => new SelectOptionDto { Id = x.Id, Name = x.Name }));
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Default)]
    public async Task<List<AdminDocumentDto>> GetDocumentsAsync()
    {
        var query = await _documentRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.OrderBy(x => x.Name).Select(x => new AdminDocumentDto
        {
            Id = x.Id, Name = x.Name, Slug = x.Slug, Skill = x.Skill, Level = x.Level,
            DownloadCount = x.DownloadCount, Status = x.Status
        }));
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Manage)]
    public async Task<UpsertDocumentDto> GetDocumentForEditAsync(Guid id)
    {
        var item = await _documentRepository.GetAsync(id);
        return new UpsertDocumentDto
        {
            Name = item.Name, Slug = item.Slug, Description = item.Description, CoverImageUrl = item.CoverImageUrl,
            FileUrl = item.FileUrl, Skill = item.Skill, Level = item.Level, AccessLevel = item.AccessLevel, Status = item.Status
        };
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Manage)]
    public async Task<Guid> CreateDocumentAsync(UpsertDocumentDto input)
    {
        var item = new LearningDocument(GuidGenerator.Create());
        Apply(item, input);
        await _documentRepository.InsertAsync(item, autoSave: true);
        return item.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Manage)]
    public async Task UpdateDocumentAsync(Guid id, UpsertDocumentDto input)
    {
        var item = await _documentRepository.GetAsync(id);
        Apply(item, input);
        await _documentRepository.UpdateAsync(item, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.Content.Manage)]
    public Task DeleteDocumentAsync(Guid id) => _documentRepository.DeleteAsync(id, autoSave: true);

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Default)]
    public async Task<List<AdminPlacementTestDto>> GetPlacementTestsAsync()
    {
        var tests = await _placementTestRepository.GetQueryableAsync();
        var questions = await _placementQuestionRepository.GetQueryableAsync();
        var attempts = await _placementAttemptRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(
            from test in tests
            join question in questions on test.Id equals question.PlacementTestId into questionJoin
            join attempt in attempts on test.Id equals attempt.PlacementTestId into attemptJoin
            orderby test.Name
            select new AdminPlacementTestDto
            {
                Id = test.Id, Name = test.Name, Slug = test.Slug, DurationMinutes = test.DurationMinutes, Status = test.Status,
                QuestionCount = questionJoin.Count(), AttemptCount = attemptJoin.Count()
            });
    }

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Manage)]
    public async Task<UpsertPlacementTestDto> GetPlacementTestForEditAsync(Guid id)
    {
        var item = await _placementTestRepository.GetAsync(id);
        return new UpsertPlacementTestDto { Name = item.Name, Slug = item.Slug, Description = item.Description, DurationMinutes = item.DurationMinutes, Status = item.Status };
    }

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Manage)]
    public async Task<Guid> CreatePlacementTestAsync(UpsertPlacementTestDto input)
    {
        var item = new PlacementTest(GuidGenerator.Create());
        Apply(item, input);
        await _placementTestRepository.InsertAsync(item, autoSave: true);
        return item.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Manage)]
    public async Task UpdatePlacementTestAsync(Guid id, UpsertPlacementTestDto input)
    {
        var item = await _placementTestRepository.GetAsync(id);
        Apply(item, input);
        await _placementTestRepository.UpdateAsync(item, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Manage)]
    public Task DeletePlacementTestAsync(Guid id) => _placementTestRepository.DeleteAsync(id, autoSave: true);

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Default)]
    public async Task<List<AdminPlacementQuestionDto>> GetPlacementQuestionsAsync(Guid testId)
    {
        var query = await _placementQuestionRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.Where(x => x.PlacementTestId == testId).OrderBy(x => x.DisplayOrder).Select(x => new AdminPlacementQuestionDto
        {
            Id = x.Id, QuestionText = x.QuestionText, OptionsJson = x.OptionsJson, CorrectAnswer = x.CorrectAnswer,
            Score = x.Score, DisplayOrder = x.DisplayOrder
        }));
    }

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Manage)]
    public async Task<UpsertPlacementQuestionDto> GetPlacementQuestionForEditAsync(Guid id)
    {
        var item = await _placementQuestionRepository.GetAsync(id);
        return new UpsertPlacementQuestionDto { QuestionText = item.QuestionText, OptionsJson = item.OptionsJson, CorrectAnswer = item.CorrectAnswer, Score = item.Score, DisplayOrder = item.DisplayOrder };
    }

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Manage)]
    public async Task<Guid> CreatePlacementQuestionAsync(Guid testId, UpsertPlacementQuestionDto input)
    {
        var item = new PlacementQuestion(GuidGenerator.Create()) { PlacementTestId = testId };
        Apply(item, input);
        await _placementQuestionRepository.InsertAsync(item, autoSave: true);
        return item.Id;
    }

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Manage)]
    public async Task UpdatePlacementQuestionAsync(Guid id, UpsertPlacementQuestionDto input)
    {
        var item = await _placementQuestionRepository.GetAsync(id);
        Apply(item, input);
        await _placementQuestionRepository.UpdateAsync(item, autoSave: true);
    }

    [Authorize(AbpIoTemplateProjectPermissions.PlacementTests.Manage)]
    public Task DeletePlacementQuestionAsync(Guid id) => _placementQuestionRepository.DeleteAsync(id, autoSave: true);

    [Authorize(AbpIoTemplateProjectPermissions.Leads.Default)]
    public async Task<List<AdminLeadDto>> GetLeadsAsync()
    {
        var leads = await _leadRepository.GetQueryableAsync();
        var courses = await _courseRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(
            from lead in leads
            join course in courses on lead.InterestedCourseId equals course.Id into courseJoin
            from course in courseJoin.DefaultIfEmpty()
            orderby lead.CreationTime descending
            select new AdminLeadDto
            {
                Id = lead.Id, FullName = lead.FullName, PhoneNumber = lead.PhoneNumber, Email = lead.Email,
                InterestedCourseName = course == null ? null : course.Name, Target = lead.Target, Note = lead.Note,
                Status = lead.Status, CreationTime = lead.CreationTime
            });
    }

    [Authorize(AbpIoTemplateProjectPermissions.Leads.Manage)]
    public async Task UpdateLeadStatusAsync(Guid id, UpdateLeadStatusDto input)
    {
        var lead = await _leadRepository.GetAsync(id);
        lead.Status = input.Status;
        if (!string.IsNullOrWhiteSpace(input.Note))
        {
            lead.Note = input.Note.Trim();
        }
        await _leadRepository.UpdateAsync(lead, autoSave: true);
    }

    private static UpsertCourseDto ToEditDto(Course course) => new()
    {
        Code = course.Code, Name = course.Name, Slug = course.Slug, EntryLevel = course.EntryLevel, TargetLevel = course.TargetLevel,
        DeliveryMode = course.DeliveryMode, SessionCount = course.SessionCount, DurationHours = course.DurationHours,
        TuitionFee = course.TuitionFee, ShortDescription = course.ShortDescription, Description = course.Description,
        CoverImageUrl = course.CoverImageUrl, IsFeatured = course.IsFeatured, Status = course.Status
    };

    private static void Apply(Course course, UpsertCourseDto input)
    {
        course.Code = input.Code.Trim(); course.Name = input.Name.Trim(); course.Slug = input.Slug.Trim().ToLowerInvariant();
        course.EntryLevel = input.EntryLevel?.Trim(); course.TargetLevel = input.TargetLevel?.Trim(); course.DeliveryMode = input.DeliveryMode;
        course.SessionCount = input.SessionCount; course.DurationHours = input.DurationHours; course.TuitionFee = input.TuitionFee;
        course.ShortDescription = input.ShortDescription?.Trim(); course.Description = input.Description?.Trim(); course.CoverImageUrl = input.CoverImageUrl?.Trim();
        course.IsFeatured = input.IsFeatured; course.Status = input.Status;
    }

    private static UpsertTeacherDto ToEditDto(Teacher teacher) => new()
    {
        FullName = teacher.FullName, Slug = teacher.Slug, Title = teacher.Title, Biography = teacher.Biography,
        Credentials = teacher.Credentials, AvatarUrl = teacher.AvatarUrl, IsFeatured = teacher.IsFeatured, Status = teacher.Status
    };

    private static void Apply(Teacher teacher, UpsertTeacherDto input)
    {
        teacher.FullName = input.FullName.Trim(); teacher.Slug = input.Slug.Trim().ToLowerInvariant(); teacher.Title = input.Title?.Trim();
        teacher.Biography = input.Biography?.Trim(); teacher.Credentials = input.Credentials?.Trim(); teacher.AvatarUrl = input.AvatarUrl?.Trim();
        teacher.IsFeatured = input.IsFeatured; teacher.Status = input.Status;
    }

    private static void Apply(CourseClass item, UpsertCourseClassDto input)
    {
        item.Code = input.Code.Trim(); item.CourseId = input.CourseId; item.CampusId = input.CampusId; item.TeacherId = input.TeacherId;
        item.ScheduleText = input.ScheduleText?.Trim(); item.StartDate = input.StartDate.Date; item.EndDate = input.EndDate?.Date;
        item.Capacity = input.Capacity; item.Status = input.Status;
    }

    private static void Apply(Student item, UpsertStudentDto input)
    {
        item.FullName = input.FullName.Trim(); item.PhoneNumber = input.PhoneNumber?.Trim(); item.Email = input.Email?.Trim();
        item.CurrentLevel = input.CurrentLevel?.Trim(); item.Target = input.Target?.Trim();
    }

    private static void Apply(Article item, UpsertArticleDto input)
    {
        item.CategoryId = input.CategoryId; item.Title = input.Title.Trim(); item.Slug = input.Slug.Trim().ToLowerInvariant();
        item.Excerpt = input.Excerpt?.Trim(); item.Content = input.Content.Trim(); item.CoverImageUrl = input.CoverImageUrl?.Trim();
        item.AuthorName = input.AuthorName?.Trim(); item.MetaTitle = input.MetaTitle?.Trim(); item.MetaDescription = input.MetaDescription?.Trim();
        item.PublishedAt = input.PublishedAt; item.IsFeatured = input.IsFeatured; item.Status = input.Status;
    }

    private static void Apply(LearningDocument item, UpsertDocumentDto input)
    {
        item.Name = input.Name.Trim(); item.Slug = input.Slug.Trim().ToLowerInvariant(); item.Description = input.Description?.Trim();
        item.CoverImageUrl = input.CoverImageUrl?.Trim(); item.FileUrl = input.FileUrl.Trim(); item.Skill = input.Skill?.Trim();
        item.Level = input.Level?.Trim(); item.AccessLevel = input.AccessLevel; item.Status = input.Status;
    }

    private static void Apply(PlacementTest item, UpsertPlacementTestDto input)
    {
        item.Name = input.Name.Trim(); item.Slug = input.Slug.Trim().ToLowerInvariant(); item.Description = input.Description?.Trim();
        item.DurationMinutes = input.DurationMinutes; item.Status = input.Status;
    }

    private static void Apply(PlacementQuestion item, UpsertPlacementQuestionDto input)
    {
        item.QuestionText = input.QuestionText.Trim(); item.OptionsJson = input.OptionsJson.Trim(); item.CorrectAnswer = input.CorrectAnswer?.Trim();
        item.Score = input.Score; item.DisplayOrder = input.DisplayOrder;
    }
}
