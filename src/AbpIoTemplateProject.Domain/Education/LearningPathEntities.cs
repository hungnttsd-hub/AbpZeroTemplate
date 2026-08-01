using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpIoTemplateProject.Education;

public class LearningPath : FullAuditedAggregateRoot<Guid>
{
    public LearningPath(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.CodeMaxLength)] public string Code { get; set; } = null!;
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? EntryLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? TargetLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? IntendedAudience { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Description { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? CoverImageUrl { get; set; }
    public int DurationMonths { get; set; }
    public int DisplayOrder { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class LearningPathStep : FullAuditedAggregateRoot<Guid>
{
    public LearningPathStep(Guid id) : base(id) { }
    public Guid LearningPathId { get; set; }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? EntryLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? TargetLevel { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Description { get; set; }
    public int DurationWeeks { get; set; }
    public int DisplayOrder { get; set; }
}

public class LearningPathCourse : FullAuditedAggregateRoot<Guid>
{
    public LearningPathCourse(Guid id) : base(id) { }
    public Guid LearningPathId { get; set; }
    public Guid CourseId { get; set; }
    public int DisplayOrder { get; set; }
}
