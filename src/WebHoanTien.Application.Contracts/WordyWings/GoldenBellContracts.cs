using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace WebHoanTien.WordyWings;

public class GoldenBellStartInput
{
    public Guid SessionId { get; set; }
    public Guid ChildId { get; set; }
    [Range(0, uint.MaxValue)] public long Seed { get; set; }
    [StringLength(80)] public string? BankVersion { get; set; }
    [MinLength(12), MaxLength(12)] public List<string>? QuestionCodes { get; set; }
    public DateTime? StartedAt { get; set; }
}
public class GoldenBellAnswerInput
{
    public Guid Id { get; set; }
    [Required, RegularExpression("^GB-[0-9]{4}$")] public string QuestionId { get; set; } = "";
    [Range(0, 11)] public int QuestionIndex { get; set; }
    public JsonElement Input { get; set; }
    public bool HintUsed { get; set; }
    [Range(0, 86400000)] public long DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class GoldenBellCompleteInput
{
    public bool BellRung { get; set; }
    public DateTime? CompletedAt { get; set; }
}
public record GoldenBellSessionDto(Guid Id, Guid ChildId, long Seed, string BankVersion, List<JsonElement> Questions, int CurrentQuestionIndex,
    int WrongAttempts, int HintCount, long DurationMs, bool BellRung, DateTime StartedAt, DateTime? CompletedAt);
public record GoldenBellAnswerDto(bool Correct, int CurrentQuestionIndex, int WrongAttempts, bool HintUsed);
public record GoldenBellHistoryDto(List<string> Completed, int Tokens, int Sessions, double Minutes);
public interface IGoldenBellAppService : IApplicationService
{
    Task<GoldenBellSessionDto> StartAsync(GoldenBellStartInput input);
    Task<GoldenBellSessionDto> GetAsync(Guid id);
    Task<GoldenBellAnswerDto> AnswerAsync(Guid id, GoldenBellAnswerInput input);
    Task<GoldenBellSessionDto> CompleteAsync(Guid id, GoldenBellCompleteInput input);
    Task<GoldenBellHistoryDto> GetHistoryAsync(Guid childId);
    Task<JsonElement> GetQuestionAsync(string code);
    Task<int> ImportAsync(JsonElement payload);
}
