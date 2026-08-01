using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpIoTemplateProject.Education;

public class PlacementTest : FullAuditedAggregateRoot<Guid>
{
    public PlacementTest(Guid id) : base(id) { }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string Name { get; set; } = null!;
    [Required, StringLength(EducationConsts.SlugMaxLength)] public string Slug { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public PlacementTestStatus Status { get; set; } = PlacementTestStatus.Draft;
}

public class PlacementQuestion : FullAuditedAggregateRoot<Guid>
{
    public PlacementQuestion(Guid id) : base(id) { }
    public Guid PlacementTestId { get; set; }
    [Required, StringLength(EducationConsts.TextMaxLength)] public string QuestionText { get; set; } = null!;
    [Required, StringLength(EducationConsts.TextMaxLength)] public string OptionsJson { get; set; } = null!;
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? CorrectAnswer { get; set; }
    public decimal Score { get; set; } = 1;
    public int DisplayOrder { get; set; }
}

public class PlacementAttempt : FullAuditedAggregateRoot<Guid>
{
    public PlacementAttempt(Guid id) : base(id)
    {
    }

    public Guid PlacementTestId { get; set; }
    public Guid? StudentId { get; set; }
    [Required, StringLength(EducationConsts.NameMaxLength)] public string FullName { get; set; } = null!;
    [Required, StringLength(EducationConsts.PhoneMaxLength)] public string PhoneNumber { get; set; } = null!;
    [StringLength(EducationConsts.EmailMaxLength)] public string? Email { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    [StringLength(EducationConsts.ShortTextMaxLength)] public string? RecommendedLevel { get; set; }
    public PlacementAttemptStatus Status { get; set; } = PlacementAttemptStatus.Started;
}

public class PlacementAnswer : FullAuditedAggregateRoot<Guid>
{
    public PlacementAnswer(Guid id) : base(id) { }
    public Guid PlacementAttemptId { get; set; }
    public Guid PlacementQuestionId { get; set; }
    [Required, StringLength(EducationConsts.TextMaxLength)] public string Answer { get; set; } = null!;
    public bool? IsCorrect { get; set; }
    public decimal? AwardedScore { get; set; }
}
