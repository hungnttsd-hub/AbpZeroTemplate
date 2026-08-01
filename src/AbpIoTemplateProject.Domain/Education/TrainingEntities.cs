using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpIoTemplateProject.Education;

public class Campus : FullAuditedAggregateRoot<Guid>
{
    public Campus(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Address { get; set; }
    [StringLength(EducationConsts.PhoneMaxLength)] public string? Hotline { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? MapUrl { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Active;
}

public class CourseClass : FullAuditedAggregateRoot<Guid>
{
    public CourseClass(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.CodeMaxLength)] public string Code { get; set; } = null!;
    public Guid CourseId { get; set; }
    public Guid? CampusId { get; set; }
    public Guid? TeacherId { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? ScheduleText { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Capacity { get; set; }
    public int EnrolledCount { get; set; }
    public CourseClassStatus Status { get; set; } = CourseClassStatus.Planned;
}

public class Enrollment : FullAuditedAggregateRoot<Guid>
{
    public Enrollment(Guid id) : base(id) { }
    public Guid StudentId { get; set; }
    public Guid CourseClassId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public decimal AgreedTuitionFee { get; set; }
    public decimal PaidAmount { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Status { get; set; }
}
