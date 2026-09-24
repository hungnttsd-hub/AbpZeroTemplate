using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace WebHoanTien.WordyWings;
public class HideSeekAnswerInput
{
    public Guid AnswerEventId { get; set; }
    public Guid RevealId { get; set; }
    [Required, StringLength(40)] public string SpotId { get; set; } = "";
    public bool Answer { get; set; }
}
public class HideSeekCompleteInput
{
    [Required, RegularExpression("^1\\.0\\.0$")] public string SchemaVersion { get; set; } = "";
    public Guid SessionId { get; set; }
    public Guid ChildId { get; set; }
    [Required, RegularExpression("^HS-[0-9]{2}$")] public string LevelId { get; set; } = "";
    [Required, StringLength(30)] public string ContentVersion { get; set; } = "";
    [Range(1, int.MaxValue)] public int Seed { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    [Range(0, 86400000)] public long ActiveDurationMs { get; set; }
    [Required, MinLength(1), MaxLength(10000)] public List<HideSeekAnswerInput> Attempts { get; set; } = new();
}
public record HideSeekResultDto(string LevelId, int BestStars, int CompletedCount, int StarsRemaining, bool Assisted);
public interface IHideSeekAppService : IApplicationService
{
    Task<HideSeekResultDto> CompleteAsync(HideSeekCompleteInput input);
    Task<List<ProgressDto>> GetHistoryAsync(Guid childId);
}
