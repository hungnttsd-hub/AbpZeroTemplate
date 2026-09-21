using System;
using Volo.Abp.Domain.Entities;

namespace WebHoanTien.WordyWings;

public class ChildProfile : AggregateRoot<Guid>
{
    public Guid ParentUserId { get; set; }
    public string Nickname { get; set; } = "";
    public string AvatarKey { get; set; } = "pip";
    public string AgeBand { get; set; } = "5_6";
    public DateTime CreationTime { get; set; }
    public DateTime LastPlayedAt { get; set; }
    protected ChildProfile() { }
    public ChildProfile(Guid id) : base(id) { }
}
public class GameWorld : Entity<string>
{
    public string Name { get; set; } = "";
    public string VietnameseName { get; set; } = "";
    public string Hero { get; set; } = "";
    public string Theme { get; set; } = "";
    public string Goal { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    protected GameWorld() { }
    public GameWorld(string id) : base(id) { }
}
public class GameLevel : Entity<string>
{
    public string WorldId { get; set; } = "";
    public int DisplayOrder { get; set; }
    public string Mechanic { get; set; } = "";
    public int Difficulty { get; set; }
    public string Instruction { get; set; } = "";
    public string DefinitionJson { get; set; } = "{}";
    public bool IsBoss { get; set; }
    public bool IsPublished { get; set; } = true;
    public int ContentVersion { get; set; } = 1;
    protected GameLevel() { }
    public GameLevel(string id) : base(id) { }
}
public class VocabularyTerm : Entity<Guid>
{
    public string Term { get; set; } = "";
    public string Category { get; set; } = "";
    public string AudioKey { get; set; } = "";
    public string ImageKey { get; set; } = "";
    protected VocabularyTerm() { }
    public VocabularyTerm(Guid id) : base(id) { }
}
public class LevelAttempt : Entity<Guid>
{
    public Guid ChildProfileId { get; set; }
    public string LevelId { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int WrongAttempts { get; set; }
    public int HintCount { get; set; }
    public int Stars { get; set; }
    public long DurationMs { get; set; }
    public string PayloadJson { get; set; } = "{}";
    protected LevelAttempt() { }
    public LevelAttempt(Guid id) : base(id) { }
}
public class PlayerProgress : Entity<Guid>
{
    public Guid ChildProfileId { get; set; }
    public string LevelId { get; set; } = "";
    public int BestStars { get; set; }
    public int CompletedCount { get; set; }
    public DateTime FirstCompletedAt { get; set; }
    public DateTime LastCompletedAt { get; set; }
    public long BestDurationMs { get; set; }
    protected PlayerProgress() { }
    public PlayerProgress(Guid id) : base(id) { }
}
public class WordMastery : Entity<Guid>
{
    public Guid ChildProfileId { get; set; }
    public string Term { get; set; } = "";
    public int ExposureCount { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public decimal MasteryScore { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? NextReviewAt { get; set; }
    protected WordMastery() { }
    public WordMastery(Guid id) : base(id) { }
    public void Record(int correct, int incorrect, DateTime now)
    {
        ExposureCount++;
        CorrectCount += correct;
        IncorrectCount += incorrect;
        MasteryScore = Math.Round(100m * (0.4m * Math.Min(ExposureCount / 4m, 1m) +
            0.6m * CorrectCount / Math.Max(CorrectCount + IncorrectCount, 1)));
        LastSeenAt = now;
        NextReviewAt = ExposureCount >= 2 && MasteryScore < 70 ? now.AddDays(1) : null;
    }
}
