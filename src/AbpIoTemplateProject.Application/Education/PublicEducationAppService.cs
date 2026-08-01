using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AbpIoTemplateProject.Education;

public class PublicEducationAppService : ApplicationService, IPublicEducationAppService
{
    private readonly IRepository<Course, Guid> _courseRepository;
    private readonly IRepository<Teacher, Guid> _teacherRepository;
    private readonly IRepository<CourseClass, Guid> _courseClassRepository;
    private readonly IRepository<Campus, Guid> _campusRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<PlacementTest, Guid> _placementTestRepository;
    private readonly IRepository<PlacementAttempt, Guid> _placementAttemptRepository;
    private readonly IRepository<PlacementQuestion, Guid> _placementQuestionRepository;
    private readonly IRepository<PlacementAnswer, Guid> _placementAnswerRepository;
    private readonly IRepository<CourseTeacher, Guid> _courseTeacherRepository;
    private readonly IRepository<CourseModule, Guid> _courseModuleRepository;

    public PublicEducationAppService(
        IRepository<Course, Guid> courseRepository,
        IRepository<Teacher, Guid> teacherRepository,
        IRepository<CourseClass, Guid> courseClassRepository,
        IRepository<Campus, Guid> campusRepository,
        IRepository<Lead, Guid> leadRepository,
        IRepository<PlacementTest, Guid> placementTestRepository,
        IRepository<PlacementAttempt, Guid> placementAttemptRepository,
        IRepository<PlacementQuestion, Guid> placementQuestionRepository,
        IRepository<PlacementAnswer, Guid> placementAnswerRepository,
        IRepository<CourseTeacher, Guid> courseTeacherRepository,
        IRepository<CourseModule, Guid> courseModuleRepository)
    {
        _courseRepository = courseRepository;
        _teacherRepository = teacherRepository;
        _courseClassRepository = courseClassRepository;
        _campusRepository = campusRepository;
        _leadRepository = leadRepository;
        _placementTestRepository = placementTestRepository;
        _placementAttemptRepository = placementAttemptRepository;
        _placementQuestionRepository = placementQuestionRepository;
        _placementAnswerRepository = placementAnswerRepository;
        _courseTeacherRepository = courseTeacherRepository;
        _courseModuleRepository = courseModuleRepository;
    }

    public async Task<List<CourseCardDto>> GetCoursesAsync()
    {
        var query = await _courseRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query
            .Where(x => x.Status == PublicationStatus.Active)
            .OrderByDescending(x => x.IsFeatured).ThenBy(x => x.Name)
            .Select(x => ToCourseCard(x)));
    }

    public async Task<List<CourseCardDto>> GetFeaturedCoursesAsync()
    {
        var query = await _courseRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query
            .Where(x => x.Status == PublicationStatus.Active && x.IsFeatured)
            .OrderBy(x => x.Name).Take(6)
            .Select(x => ToCourseCard(x)));
    }

    public async Task<CourseDetailDto?> GetCourseBySlugAsync(string slug)
    {
        var courseQuery = await _courseRepository.GetQueryableAsync();
        var course = await AsyncExecuter.FirstOrDefaultAsync(courseQuery.Where(x => x.Status == PublicationStatus.Active && x.Slug == slug.ToLower()).Select(x => new
        {
            x.Id, x.Name, x.Slug, x.EntryLevel, x.TargetLevel, x.SessionCount, x.DurationHours,
            TuitionFee = x.PromotionalFee ?? x.TuitionFee, x.ShortDescription, x.CoverImageUrl, x.Description, x.IntroVideoUrl
        }));
        if (course == null) return null;

        var courseTeachers = await _courseTeacherRepository.GetQueryableAsync();
        var teachers = await _teacherRepository.GetQueryableAsync();
        var modules = await _courseModuleRepository.GetQueryableAsync();
        var result = new CourseDetailDto
        {
            Id = course.Id, Name = course.Name, Slug = course.Slug, EntryLevel = course.EntryLevel, TargetLevel = course.TargetLevel,
            SessionCount = course.SessionCount, DurationHours = course.DurationHours, TuitionFee = course.TuitionFee,
            ShortDescription = course.ShortDescription, CoverImageUrl = course.CoverImageUrl, Description = course.Description, IntroVideoUrl = course.IntroVideoUrl
        };
        result.Teachers = await AsyncExecuter.ToListAsync(
            from link in courseTeachers
            join teacher in teachers on link.TeacherId equals teacher.Id
            where link.CourseId == course.Id && teacher.Status == PublicationStatus.Active
            orderby link.DisplayOrder
            select new TeacherCardDto { Id = teacher.Id, FullName = teacher.FullName, Slug = teacher.Slug, Title = teacher.Title, Credentials = teacher.Credentials, AvatarUrl = teacher.AvatarUrl });
        result.Modules = await AsyncExecuter.ToListAsync(modules.Where(x => x.CourseId == course.Id).OrderBy(x => x.DisplayOrder).Select(x => new CourseModuleDto { Name = x.Name, Description = x.Description, DisplayOrder = x.DisplayOrder }));
        return result;
    }

    public async Task<List<TeacherCardDto>> GetTeachersAsync()
    {
        var query = await _teacherRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query.Where(x => x.Status == PublicationStatus.Active).OrderByDescending(x => x.IsFeatured).ThenBy(x => x.FullName).Select(x => new TeacherCardDto { Id = x.Id, FullName = x.FullName, Slug = x.Slug, Title = x.Title, Credentials = x.Credentials, AvatarUrl = x.AvatarUrl }));
    }

    public async Task<List<TeacherCardDto>> GetFeaturedTeachersAsync()
    {
        var query = await _teacherRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(query
            .Where(x => x.Status == PublicationStatus.Active && x.IsFeatured)
            .OrderBy(x => x.FullName).Take(6)
            .Select(x => new TeacherCardDto { Id = x.Id, FullName = x.FullName, Slug = x.Slug, Title = x.Title, Credentials = x.Credentials, AvatarUrl = x.AvatarUrl }));
    }

    public async Task<TeacherDetailDto?> GetTeacherBySlugAsync(string slug)
    {
        var query = await _teacherRepository.GetQueryableAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Status == PublicationStatus.Active && x.Slug == slug.ToLower()).Select(x => new TeacherDetailDto
        {
            Id = x.Id, FullName = x.FullName, Slug = x.Slug, Title = x.Title, Credentials = x.Credentials, AvatarUrl = x.AvatarUrl, Biography = x.Biography
        }));
    }

    public async Task<List<CourseClassDto>> GetUpcomingClassesAsync()
    {
        var classes = await _courseClassRepository.GetQueryableAsync();
        var courses = await _courseRepository.GetQueryableAsync();
        var teachers = await _teacherRepository.GetQueryableAsync();
        var campuses = await _campusRepository.GetQueryableAsync();

        return await AsyncExecuter.ToListAsync(
            from courseClass in classes
            join course in courses on courseClass.CourseId equals course.Id
            join teacher in teachers on courseClass.TeacherId equals teacher.Id into teacherJoin
            from teacher in teacherJoin.DefaultIfEmpty()
            join campus in campuses on courseClass.CampusId equals campus.Id into campusJoin
            from campus in campusJoin.DefaultIfEmpty()
            where courseClass.Status == CourseClassStatus.OpenForEnrollment && courseClass.StartDate >= Clock.Now.Date
            orderby courseClass.StartDate
            select new CourseClassDto
            {
                Id = courseClass.Id, Code = courseClass.Code, CourseName = course.Name,
                CampusName = campus == null ? null : campus.Name,
                TeacherName = teacher == null ? null : teacher.FullName,
                ScheduleText = courseClass.ScheduleText, StartDate = courseClass.StartDate,
                RemainingSeats = courseClass.Capacity - courseClass.EnrolledCount
            });
    }

    public async Task<Guid> SubmitLeadAsync(SubmitLeadDto input)
    {
        var lead = new Lead(GuidGenerator.Create())
        {
            FullName = input.FullName.Trim(), PhoneNumber = input.PhoneNumber.Trim(),
            Email = input.Email?.Trim(), InterestedCourseId = input.InterestedCourseId,
            CurrentLevel = input.CurrentLevel?.Trim(), Target = input.Target?.Trim(), Note = input.Note?.Trim(), Source = "Website"
        };
        await _leadRepository.InsertAsync(lead, autoSave: true);
        return lead.Id;
    }

    public async Task<PlacementTestDto?> GetPublishedPlacementTestAsync()
    {
        var query = await _placementTestRepository.GetQueryableAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Status == PlacementTestStatus.Published)
            .OrderBy(x => x.Name)
            .Select(x => new PlacementTestDto { Id = x.Id, Name = x.Name, Slug = x.Slug, Description = x.Description, DurationMinutes = x.DurationMinutes }));
    }

    public async Task<Guid> StartPlacementAttemptAsync(StartPlacementAttemptDto input)
    {
        var test = await _placementTestRepository.GetAsync(input.PlacementTestId);
        if (test.Status != PlacementTestStatus.Published)
        {
            throw new BusinessException("Education:PlacementTestNotAvailable");
        }

        var attempt = new PlacementAttempt(GuidGenerator.Create())
        {
            PlacementTestId = test.Id, FullName = input.FullName.Trim(),
            PhoneNumber = input.PhoneNumber.Trim(), Email = input.Email?.Trim(), StartedAt = Clock.Now
        };
        await _placementAttemptRepository.InsertAsync(attempt, autoSave: true);
        return attempt.Id;
    }

    public async Task<List<PlacementQuestionDto>> GetPlacementQuestionsAsync(Guid placementAttemptId)
    {
        var attempt = await _placementAttemptRepository.GetAsync(placementAttemptId);
        if (attempt.Status != PlacementAttemptStatus.Started)
        {
            throw new BusinessException("Education:PlacementAttemptUnavailable");
        }

        var query = await _placementQuestionRepository.GetQueryableAsync();
        var questions = await AsyncExecuter.ToListAsync(query.Where(x => x.PlacementTestId == attempt.PlacementTestId).OrderBy(x => x.DisplayOrder));
        return questions.Select(x => new PlacementQuestionDto
        {
            Id = x.Id, QuestionText = x.QuestionText, Options = JsonSerializer.Deserialize<List<string>>(x.OptionsJson) ?? new List<string>(), DisplayOrder = x.DisplayOrder
        }).ToList();
    }

    public async Task<PlacementResultDto> SubmitPlacementAttemptAsync(SubmitPlacementAttemptDto input)
    {
        var attempt = await _placementAttemptRepository.GetAsync(input.PlacementAttemptId);
        if (attempt.Status != PlacementAttemptStatus.Started)
        {
            throw new BusinessException("Education:PlacementAttemptUnavailable");
        }

        var questionQuery = await _placementQuestionRepository.GetQueryableAsync();
        var questions = await AsyncExecuter.ToListAsync(questionQuery.Where(x => x.PlacementTestId == attempt.PlacementTestId));
        var answers = new List<PlacementAnswer>();
        decimal score = 0;
        foreach (var question in questions)
        {
            var submitted = input.Answers.FirstOrDefault(x => x.PlacementQuestionId == question.Id);
            var isCorrect = submitted is not null && string.Equals(submitted.Answer, question.CorrectAnswer, StringComparison.Ordinal);
            var awardedScore = isCorrect ? question.Score : 0;
            score += awardedScore;
            answers.Add(new PlacementAnswer(GuidGenerator.Create()) { PlacementAttemptId = attempt.Id, PlacementQuestionId = question.Id, Answer = submitted?.Answer ?? string.Empty, IsCorrect = isCorrect, AwardedScore = awardedScore });
        }

        var maximumScore = questions.Sum(x => x.Score);
        var recommendedLevel = maximumScore == 0 || score / maximumScore < .4m ? "IELTS Vỡ lòng" : score / maximumScore < .7m ? "Pre IELTS" : "IELTS Chiến lược";
        attempt.Score = score;
        attempt.RecommendedLevel = recommendedLevel;
        attempt.SubmittedAt = Clock.Now;
        attempt.Status = PlacementAttemptStatus.Submitted;
        await _placementAnswerRepository.InsertManyAsync(answers, autoSave: true);
        await _placementAttemptRepository.UpdateAsync(attempt, autoSave: true);
        return new PlacementResultDto { Score = score, RecommendedLevel = recommendedLevel };
    }

    private static CourseCardDto ToCourseCard(Course course) => new()
    {
        Id = course.Id, Name = course.Name, Slug = course.Slug, EntryLevel = course.EntryLevel, TargetLevel = course.TargetLevel,
        SessionCount = course.SessionCount, DurationHours = course.DurationHours, TuitionFee = course.PromotionalFee ?? course.TuitionFee,
        ShortDescription = course.ShortDescription, CoverImageUrl = course.CoverImageUrl
    };
}
