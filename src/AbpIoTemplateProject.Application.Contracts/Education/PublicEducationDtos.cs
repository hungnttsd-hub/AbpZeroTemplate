using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AbpIoTemplateProject.Education;

public class CourseCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? EntryLevel { get; set; }
    public string? TargetLevel { get; set; }
    public int SessionCount { get; set; }
    public int DurationHours { get; set; }
    public decimal? TuitionFee { get; set; }
    public string? ShortDescription { get; set; }
    public string? CoverImageUrl { get; set; }
}

public class TeacherCardDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Title { get; set; }
    public string? Credentials { get; set; }
    public string? AvatarUrl { get; set; }
}

public class CourseDetailDto : CourseCardDto
{
    public string? Description { get; set; }
    public string? IntroVideoUrl { get; set; }
    public List<TeacherCardDto> Teachers { get; set; } = new();
    public List<CourseModuleDto> Modules { get; set; } = new();
}

public class CourseModuleDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class TeacherDetailDto : TeacherCardDto
{
    public string? Biography { get; set; }
}

public class CourseClassDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public string? CampusName { get; set; }
    public string? TeacherName { get; set; }
    public string? ScheduleText { get; set; }
    public DateTime StartDate { get; set; }
    public int RemainingSeats { get; set; }
}

public class SubmitLeadDto
{
    [Required, StringLength(EducationConsts.NameMaxLength)] public string FullName { get; set; } = null!;
    [Required, StringLength(EducationConsts.PhoneMaxLength)] public string PhoneNumber { get; set; } = null!;
    [EmailAddress, StringLength(EducationConsts.EmailMaxLength)] public string? Email { get; set; }
    public Guid? InterestedCourseId { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? CurrentLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Target { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Note { get; set; }
}

public class PlacementTestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
}

public class StartPlacementAttemptDto
{
    [Required] public Guid PlacementTestId { get; set; }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string FullName { get; set; } = null!;
    [Required, StringLength(EducationConsts.PhoneMaxLength)] public string PhoneNumber { get; set; } = null!;
    [EmailAddress, StringLength(EducationConsts.EmailMaxLength)] public string? Email { get; set; }
}

public class PlacementQuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = null!;
    public List<string> Options { get; set; } = new();
    public int DisplayOrder { get; set; }
}

public class PlacementAnswerInputDto
{
    [Required] public Guid PlacementQuestionId { get; set; }
    [Required, StringLength(EducationConsts.ShortTextMaxLength)] public string Answer { get; set; } = null!;
}

public class SubmitPlacementAttemptDto
{
    [Required] public Guid PlacementAttemptId { get; set; }
    [Required, MinLength(1)] public List<PlacementAnswerInputDto> Answers { get; set; } = new();
}

public class PlacementResultDto
{
    public decimal Score { get; set; }
    public string RecommendedLevel { get; set; } = null!;
}
