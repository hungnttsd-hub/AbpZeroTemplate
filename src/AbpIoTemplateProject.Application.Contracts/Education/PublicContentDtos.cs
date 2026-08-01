using System;
using System.Collections.Generic;

namespace AbpIoTemplateProject.Education;

public class LearningPathDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? EntryLevel { get; set; }
    public string? TargetLevel { get; set; }
    public string? IntendedAudience { get; set; }
    public string? Description { get; set; }
    public int DurationMonths { get; set; }
    public List<LearningPathStepDto> Steps { get; set; } = new();
}

public class LearningPathStepDto
{
    public string Name { get; set; } = null!;
    public string? EntryLevel { get; set; }
    public string? TargetLevel { get; set; }
    public string? Description { get; set; }
    public int DurationWeeks { get; set; }
    public int DisplayOrder { get; set; }
}

public class ArticleCardDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Excerpt { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CategoryName { get; set; }
    public string? AuthorName { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class ArticleDetailDto : ArticleCardDto
{
    public string Content { get; set; } = null!;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class LearningDocumentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string FileUrl { get; set; } = null!;
    public string? Skill { get; set; }
    public string? Level { get; set; }
}

public class StudentAchievementDto
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = null!;
    public string? BeforeResult { get; set; }
    public string? AfterResult { get; set; }
    public string? Story { get; set; }
    public string? PhotoUrl { get; set; }
}

public class CampusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Hotline { get; set; }
    public string? MapUrl { get; set; }
}
