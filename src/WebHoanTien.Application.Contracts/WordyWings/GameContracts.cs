using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace WebHoanTien.WordyWings;

public record ChildDto(Guid Id, string Nickname, string AvatarKey, string AgeBand);
public class CreateChildInput
{
    [Required, StringLength(40, MinimumLength = 1)] public string Nickname { get; set; } = "";
    [Required, RegularExpression("^(pip|poki|momo|lulu)$")] public string AvatarKey { get; set; } = "pip";
    [Required, RegularExpression("^(5_6|6_7|7_8)$")] public string AgeBand { get; set; } = "5_6";
}
public class TargetResultInput
{
    [Required, StringLength(80)] public string Term { get; set; } = "";
    public bool Correct { get; set; }
    [Range(0, 86400000)] public int ResponseMs { get; set; }
}
public class AttemptInput
{
    public Guid AttemptId { get; set; }
    public Guid ChildId { get; set; }
    [Required, RegularExpression("^W[0-9]{2}-L[0-9]{2}$")] public string LevelId { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    [Range(0, 10000)] public int WrongAttempts { get; set; }
    [Range(0, 10000)] public int HintCount { get; set; }
    [Required, MinLength(1), MaxLength(10000)] public List<TargetResultInput> TargetResults { get; set; } = new();
    public BalloonDartSummaryInput? BalloonDart { get; set; }
}
public class BalloonDartSummaryInput
{
    [Required, RegularExpression("^balloon_dart$")] public string Mechanic { get; set; } = "balloon_dart";
    [Range(3, 3)] public int RoundsCompleted { get; set; }
    [Range(3, 100000)] public int Shots { get; set; }
    [Range(0, 10000)] public int WrongHits { get; set; }
    [Range(0, 100000)] public int Misses { get; set; }
    [Range(0, 10000)] public int HintCount { get; set; }
    [Range(0, 86400)] public int DurationSeconds { get; set; }
    [Range(3, 3)] public int Stars { get; set; }
}
public record ProgressDto(string LevelId, int BestStars, int CompletedCount);
public record MasteryDto(string Term, int ExposureCount, int CorrectCount, int IncorrectCount, decimal MasteryScore);
public record DashboardDto(int CompletedLevels, int Stars, double Minutes, List<MasteryDto> Words, List<MasteryDto> Review);
public record WorldDto(string Id, string Name, string Vi, string Hero, string Theme, string Goal);
public record AdminLevelDto(string Id, string WorldId, string Instruction, int Difficulty, string Mechanic, bool IsPublished, int ContentVersion);
public class EditLevelInput
{
    [Required, StringLength(300)] public string Instruction { get; set; } = "";
    [Range(1, 4)] public int Difficulty { get; set; }
    [Required, RegularExpression("^(word_shot|balloon_pop|balloon_dart|drag_sort|letter_puzzle|boss_challenge)$")] public string Mechanic { get; set; } = "word_shot";
    public bool IsPublished { get; set; }
}
public interface IWordyGameAppService : IApplicationService
{
    Task<List<WorldDto>> GetWorldsAsync();
    Task<List<JsonElement>> GetLevelsAsync(string? worldId = null);
    Task<JsonElement> GetLevelAsync(string id);
    Task<List<ChildDto>> GetChildrenAsync();
    Task<ChildDto> CreateChildAsync(CreateChildInput input);
    Task<List<ProgressDto>> GetProgressAsync(Guid childId);
    Task<ProgressDto> SaveAttemptAsync(AttemptInput input);
    Task<DashboardDto> GetDashboardAsync(Guid childId);
    Task<List<AdminLevelDto>> GetAdminLevelsAsync(string? worldId = null);
    Task UpdateLevelAsync(string id, EditLevelInput input);
}
