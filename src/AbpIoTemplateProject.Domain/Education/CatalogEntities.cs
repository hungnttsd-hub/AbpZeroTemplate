using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpIoTemplateProject.Education;

public class CourseCategory : FullAuditedAggregateRoot<Guid>
{
    public CourseCategory(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Active;
}

public class CourseLevel : FullAuditedAggregateRoot<Guid>
{
    public CourseLevel(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? EntryRequirement { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? TargetOutcome { get; set; }
    public int DisplayOrder { get; set; }
}

public class Course : FullAuditedAggregateRoot<Guid>
{
    public Course(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.CodeMaxLength)] public string Code { get; set; } = null!;
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    public Guid? CategoryId { get; set; }
    public Guid? LevelId { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? EntryLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? TargetLevel { get; set; }
    public CourseDeliveryMode DeliveryMode { get; set; }
    public int SessionCount { get; set; }
    public int DurationHours { get; set; }
    public decimal? TuitionFee { get; set; }
    public decimal? PromotionalFee { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? ShortDescription { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Description { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? CoverImageUrl { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? IntroVideoUrl { get; set; }
    [StringLength(EducationConsts.NameMaxLength)] public string? MetaTitle { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? MetaDescription { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class CourseTeacher : FullAuditedAggregateRoot<Guid>
{
    public CourseTeacher(Guid id) : base(id) { }
    public Guid CourseId { get; set; }
    public Guid TeacherId { get; set; }
    public int DisplayOrder { get; set; }
}

public class CourseBenefit : FullAuditedAggregateRoot<Guid>
{
    public CourseBenefit(Guid id) : base(id) { }
    public Guid CourseId { get; set; }
    [Required, StringLength(EducationConsts.ShortTextMaxLength)] public string Content { get; set; } = null!;
    public int DisplayOrder { get; set; }
}

public class CourseFaq : FullAuditedAggregateRoot<Guid>
{
    public CourseFaq(Guid id) : base(id) { }
    public Guid CourseId { get; set; }
    [Required, StringLength(EducationConsts.ShortTextMaxLength)] public string Question { get; set; } = null!;
    [Required, StringLength(EducationConsts.TextMaxLength)] public string Answer { get; set; } = null!;
    public int DisplayOrder { get; set; }
}
