using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpIoTemplateProject.Education;

public class ArticleCategory : FullAuditedAggregateRoot<Guid>
{
    public ArticleCategory(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Active;
}

public class Article : FullAuditedAggregateRoot<Guid>
{
    public Article(Guid id) : base(id) { }
    public Guid? CategoryId { get; set; }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Title { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Excerpt { get; set; }
    [Required, StringLength(EducationConsts.TextMaxLength)] public string Content { get; set; } = null!;
    [StringLength(EducationConsts.UrlMaxLength)] public string? CoverImageUrl { get; set; }
    [StringLength(EducationConsts.NameMaxLength)] public string? AuthorName { get; set; }
    [StringLength(EducationConsts.NameMaxLength)] public string? MetaTitle { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? MetaDescription { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class LearningDocument : FullAuditedAggregateRoot<Guid>
{
    public LearningDocument(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Description { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? CoverImageUrl { get; set; }
    [Required, StringLength(EducationConsts.UrlMaxLength)] public string FileUrl { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Skill { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Level { get; set; }
    public DocumentAccessLevel AccessLevel { get; set; } = DocumentAccessLevel.Public;
    public int DownloadCount { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}

public class StudentAchievement : FullAuditedAggregateRoot<Guid>
{
    public StudentAchievement(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string StudentName { get; set; } = null!;
    public Guid? CourseId { get; set; }
    public Guid? TeacherId { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? BeforeResult { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? AfterResult { get; set; }
    [StringLength(EducationConsts.TextMaxLength)] public string? Story { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? PhotoUrl { get; set; }
    [StringLength(EducationConsts.UrlMaxLength)] public string? VideoUrl { get; set; }
    public bool IsFeatured { get; set; }
    public PublicationStatus Status { get; set; } = PublicationStatus.Draft;
}
