using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpIoTemplateProject.Education;

public class CourseModule : FullAuditedAggregateRoot<Guid>
{
    public CourseModule(Guid id) : base(id) { }
    public Guid CourseId { get; set; }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [StringLength(EducationConsts.TextMaxLength)] public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class CourseLesson : FullAuditedAggregateRoot<Guid>
{
    public CourseLesson(Guid id) : base(id) { }
    public Guid CourseModuleId { get; set; }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [StringLength(EducationConsts.TextMaxLength)] public string? Content { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? ResourceUrl { get; set; }
    public LessonType LessonType { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPreview { get; set; }
    public int DisplayOrder { get; set; }
}
