using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AbpIoTemplateProject.Education;

public class EducationDashboardDto
{
    public long CourseCount { get; set; }
    public long TeacherCount { get; set; }
    public long OpenClassCount { get; set; }
    public long NewLeadCount { get; set; }
    public long PlacementAttemptCount { get; set; }
}

public class AdminCourseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? EntryLevel { get; set; }
    public string? TargetLevel { get; set; }
    public int SessionCount { get; set; }
    public int DurationHours { get; set; }
    public decimal? TuitionFee { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; }
}

public class UpsertCourseDto
{
    [Required, StringLength(EducationConsts.CodeMaxLength)] public string Code { get; set; } = null!;
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? EntryLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? TargetLevel { get; set; }
    public CourseDeliveryMode DeliveryMode { get; set; } = CourseDeliveryMode.Offline;
    [Range(0, 500)] public int SessionCount { get; set; }
    [Range(0, 2_000)] public int DurationHours { get; set; }
    [Range(0, 999_999_999)] public decimal? TuitionFee { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? ShortDescription { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Description { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? CoverImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class AdminTeacherDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Title { get; set; }
    public string? Credentials { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; }
}

public class UpsertTeacherDto
{
    [Required, StringLength(EducationConsts.NameMaxLength)] public string FullName { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.NameMaxLength)] public string? Title { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Biography { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Credentials { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? AvatarUrl { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class AdminLeadDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string? InterestedCourseName { get; set; }
    public string? Target { get; set; }
    public string? Note { get; set; }
    public LeadStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
}

public class UpdateLeadStatusDto
{
    public LeadStatus Status { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Note { get; set; }
}

public class AdminCourseClassDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public string? CampusName { get; set; }
    public string? TeacherName { get; set; }
    public DateTime StartDate { get; set; }
    public int Capacity { get; set; }
    public int EnrolledCount { get; set; }
    public CourseClassStatus Status { get; set; }
}

public class SelectOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class UpsertCourseClassDto
{
    [Required, StringLength(EducationConsts.CodeMaxLength)] public string Code { get; set; } = null!;
    [Required] public Guid CourseId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? TeacherId { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? ScheduleText { get; set; }
    [DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    [Range(1, 10_000)] public int Capacity { get; set; } = 20;
    public CourseClassStatus Status { get; set; } = CourseClassStatus.Planned;
}

public class AdminStudentDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? CurrentLevel { get; set; }
    public string? Target { get; set; }
    public int EnrollmentCount { get; set; }
}

public class UpsertStudentDto
{
    [Required, StringLength(EducationConsts.NameMaxLength)] public string FullName { get; set; } = null!;
    [StringLength(EducationConsts.PhoneMaxLength)] public string? PhoneNumber { get; set; }
    [EmailAddress, StringLength(EducationConsts.EmailMaxLength)] public string? Email { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? CurrentLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Target { get; set; }
}

public class AdminEnrollmentDto
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = null!;
    public string ClassCode { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public DateTime EnrolledAt { get; set; }
    public decimal AgreedTuitionFee { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Status { get; set; }
}

public class UpsertEnrollmentDto
{
    [Required] public Guid StudentId { get; set; }
    [Required] public Guid CourseClassId { get; set; }
    [Range(0, 999_999_999)] public decimal AgreedTuitionFee { get; set; }
    [Range(0, 999_999_999)] public decimal PaidAmount { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Status { get; set; } = "Đã ghi danh";
}

public class AdminArticleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? CategoryName { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; }
}

public class UpsertArticleDto
{
    public Guid? CategoryId { get; set; }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Title { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Excerpt { get; set; }
    [Required, StringLength(EducationConsts.TextMaxLength)] public string Content { get; set; } = null!;
    [StringLength(EducationConsts.UrlMaxLength)] public string? CoverImageUrl { get; set; }
    [StringLength(EducationConsts.NameMaxLength)] public string? AuthorName { get; set; }
    [StringLength(EducationConsts.NameMaxLength)] public string? MetaTitle { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? MetaDescription { get; set; }
    [DataType(DataType.DateTime)] public DateTime? PublishedAt { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class AdminDocumentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Skill { get; set; }
    public string? Level { get; set; }
    public int DownloadCount { get; set; }
    public PublicationStatus Status { get; set; }
}

public class UpsertDocumentDto
{
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Description { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? CoverImageUrl { get; set; }
    [Required, StringLength(EducationConsts.UrlMaxLength)] public string FileUrl { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Skill { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Level { get; set; }
    public DocumentAccessLevel AccessLevel { get; set; } = DocumentAccessLevel.Public;
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class AdminPlacementTestDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public PlacementTestStatus Status { get; set; }
    public int QuestionCount { get; set; }
    public int AttemptCount { get; set; }
}

public class UpsertPlacementTestDto
{
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Description { get; set; }
    [Range(1, 300)] public int DurationMinutes { get; set; } = 30;
    public PlacementTestStatus Status { get; set; } = PlacementTestStatus.Draft;
}

public class AdminPlacementQuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = null!;
    public string OptionsJson { get; set; } = null!;
    public string? CorrectAnswer { get; set; }
    public decimal Score { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpsertPlacementQuestionDto
{
    [Required, StringLength(EducationConsts.TextMaxLength)] public string QuestionText { get; set; } = null!;
    [Required, StringLength(EducationConsts.TextMaxLength)] public string OptionsJson { get; set; } = "[]";
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? CorrectAnswer { get; set; }
    [Range(0.1, 100)] public decimal Score { get; set; } = 1;
    [Range(0, 10_000)] public int DisplayOrder { get; set; }
}
