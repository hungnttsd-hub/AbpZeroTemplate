using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpIoTemplateProject.Education;

public class Teacher : FullAuditedAggregateRoot<Guid>
{
    public Teacher(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string FullName { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.NameMaxLength)] public string? Title { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Biography { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Credentials { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? AvatarUrl { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class Student : FullAuditedAggregateRoot<Guid>
{
    public Student(Guid id) : base(id) { }
    public Guid? IdentityUserId { get; set; }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string FullName { get; set; } = null!;
    [StringLength(EducationConsts.PhoneMaxLength)] public string? PhoneNumber { get; set; }
    [StringLength(EducationConsts.EmailMaxLength)] public string? Email { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? CurrentLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Target { get; set; }
}

public class Lead : FullAuditedAggregateRoot<Guid>
{
    public Lead(Guid id) : base(id)
    {
    }

    [Required, StringLength(EducationConsts.NameMaxLength)] public string FullName { get; set; } = null!;
    [Required, StringLength(EducationConsts.PhoneMaxLength)] public string PhoneNumber { get; set; } = null!;
    [StringLength(EducationConsts.EmailMaxLength)] public string? Email { get; set; }
    public Guid? InterestedCourseId { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? CurrentLevel { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Target { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Source { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Note { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
}
