using System;
using Volo.Abp.Domain.Entities;

namespace WebHoanTien.WordyWings;

public class GoldenBellQuestion : Entity<string>
{
    public int Difficulty { get; set; }
    public string QuestionType { get; set; } = "";
    public string ContentVersion { get; set; } = "";
    public string DefinitionJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    protected GoldenBellQuestion() { }
    public GoldenBellQuestion(string code) : base(code) { }
}

public class GoldenBellSession : AggregateRoot<Guid>
{
    public Guid ChildProfileId { get; set; }
    public long Seed { get; set; }
    public string BankVersion { get; set; } = "";
    // Snapshot the actual ordered questions: future imports cannot alter an existing session.
    public string QuestionsJson { get; set; } = "[]";
    public int CurrentQuestionIndex { get; set; }
    public int WrongAttempts { get; set; }
    public int HintCount { get; set; }
    public long DurationMs { get; set; }
    public bool BellRung { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    protected GoldenBellSession() { }
    public GoldenBellSession(Guid id) : base(id) { }
}

public class GoldenBellAttempt : Entity<Guid>
{
    public Guid SessionId { get; set; }
    public string QuestionCode { get; set; } = "";
    public int QuestionIndex { get; set; }
    public int AttemptNumber { get; set; }
    public bool IsCorrect { get; set; }
    public bool HintUsed { get; set; }
    public long DurationMs { get; set; }
    public string InputJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    protected GoldenBellAttempt() { }
    public GoldenBellAttempt(Guid id) : base(id) { }
}
