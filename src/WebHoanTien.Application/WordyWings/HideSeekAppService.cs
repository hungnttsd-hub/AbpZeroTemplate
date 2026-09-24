using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace WebHoanTien.WordyWings;
[Authorize, RemoteService(false)]
public class HideSeekAppService : WebHoanTienAppService, IHideSeekAppService
{
    private readonly IRepository<ChildProfile, Guid> children;
    private readonly IRepository<GameWorld, string> worlds;
    private readonly IRepository<GameLevel, string> levels;
    private readonly IRepository<LevelAttempt, Guid> attempts;
    private readonly IRepository<PlayerProgress, Guid> progress;
    private readonly IRepository<WordMastery, Guid> mastery;
    public HideSeekAppService(IRepository<ChildProfile, Guid> children, IRepository<GameWorld, string> worlds,
        IRepository<GameLevel, string> levels, IRepository<LevelAttempt, Guid> attempts,
        IRepository<PlayerProgress, Guid> progress, IRepository<WordMastery, Guid> mastery)
    { this.children = children; this.worlds = worlds; this.levels = levels; this.attempts = attempts; this.progress = progress; this.mastery = mastery; }
    private async Task<ChildProfile> OwnedChild(Guid id)
    {
        var child = await children.FindAsync(id);
        if (child == null || child.ParentUserId != CurrentUser.GetId()) throw new AbpAuthorizationException("Child profile is not accessible.");
        return child;
    }
    public async Task<HideSeekResultDto> CompleteAsync(HideSeekCompleteInput input)
    {
        var child = await OwnedChild(input.ChildId);
        var level = HideSeekContent.Get(input.LevelId, input.ContentVersion);
        if (level.ValueKind == JsonValueKind.Undefined || input.SchemaVersion != "1.0.0" || input.SessionId == Guid.Empty ||
            input.StartedAt.Kind != DateTimeKind.Utc || input.CompletedAt.Kind != DateTimeKind.Utc || input.CompletedAt < input.StartedAt ||
            input.CompletedAt > Clock.Now.AddMinutes(5) || input.CompletedAt - input.StartedAt > TimeSpan.FromDays(30) ||
            input.ActiveDurationMs > (input.CompletedAt - input.StartedAt).TotalMilliseconds + 5000)
            throw new UserFriendlyException("Phiên Trốn tìm không hợp lệ hoặc dùng phiên bản nội dung khác.");
        HideSeekGrade grade;
        try { grade = HideSeekScoring.Grade(level, input.Seed, input.Attempts.Select(a => new HideSeekDecision(a.AnswerEventId, a.RevealId, a.SpotId, a.Answer)).ToArray()); }
        catch (ArgumentException) { throw new UserFriendlyException("Cần tìm và xác nhận đúng mục tiêu trước khi hoàn thành."); }
        var existing = await attempts.FindAsync(input.SessionId);
        if (existing != null) {
            if (existing.ChildProfileId != child.Id || existing.LevelId != input.LevelId) throw new AbpAuthorizationException();
            var saved = await progress.GetAsync(p => p.ChildProfileId == child.Id && p.LevelId == input.LevelId);
            return new HideSeekResultDto(saved.LevelId, saved.BestStars, saved.CompletedCount, existing.Stars, existing.HintCount > 0);
        }
        // The existing child aggregate concurrency stamp serializes simultaneous completion writes.
        child.LastPlayedAt = Clock.Now; await children.UpdateAsync(child, autoSave: true);
        await HideSeekContent.EnsureAsync(worlds, levels);
        await attempts.InsertAsync(new LevelAttempt(input.SessionId) { ChildProfileId = child.Id, LevelId = input.LevelId,
            StartedAt = input.StartedAt, CompletedAt = input.CompletedAt, WrongAttempts = grade.WrongReveals,
            HintCount = grade.Assisted ? 1 : 0, Stars = grade.Stars, DurationMs = input.ActiveDurationMs,
            PayloadJson = JsonSerializer.Serialize(new { mechanic = "hide_seek", schemaVersion = "1.0.0", starsRemaining = grade.Stars, grade, input }) });
        var record = await progress.FindAsync(p => p.ChildProfileId == child.Id && p.LevelId == input.LevelId);
        if (record == null) {
            record = new PlayerProgress(GuidGenerator.Create()) { ChildProfileId = child.Id, LevelId = input.LevelId,
                FirstCompletedAt = input.CompletedAt, BestDurationMs = input.ActiveDurationMs };
            await progress.InsertAsync(record);
        }
        record.BestStars = Math.Max(record.BestStars, grade.Stars); record.CompletedCount++;
        record.LastCompletedAt = record.LastCompletedAt > input.CompletedAt ? record.LastCompletedAt : input.CompletedAt;
        record.BestDurationMs = Math.Min(record.BestDurationMs, input.ActiveDurationMs); await progress.UpdateAsync(record);
        // NO is evidence about the requested word; it does not prove the child knows the distractor's name.
        var target = level.GetProperty("targetEntityId").GetString()!;
        var word = await mastery.FindAsync(w => w.ChildProfileId == child.Id && w.Term == target);
        if (word == null) { word = new WordMastery(GuidGenerator.Create()) { ChildProfileId = child.Id, Term = target }; await mastery.InsertAsync(word); }
        word.Record(grade.IndependentCorrect, grade.IndependentWrong, Clock.Now); await mastery.UpdateAsync(word);
        return new HideSeekResultDto(record.LevelId, record.BestStars, record.CompletedCount, grade.Stars, grade.Assisted);
    }
    public async Task<List<ProgressDto>> GetHistoryAsync(Guid childId)
    {
        await OwnedChild(childId);
        return (await progress.GetListAsync(p => p.ChildProfileId == childId && p.LevelId.StartsWith("HS-")))
            .Select(p => new ProgressDto(p.LevelId, p.BestStars, p.CompletedCount)).ToList();
    }
}
