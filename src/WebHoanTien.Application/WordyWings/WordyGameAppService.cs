using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace WebHoanTien.WordyWings;

[Authorize]
[RemoteService(false)]
public class WordyGameAppService : WebHoanTienAppService, IWordyGameAppService
{
    private readonly IRepository<ChildProfile, Guid> children;
    private readonly IRepository<GameWorld, string> worlds;
    private readonly IRepository<GameLevel, string> levels;
    private readonly IRepository<LevelAttempt, Guid> attempts;
    private readonly IRepository<PlayerProgress, Guid> progress;
    private readonly IRepository<WordMastery, Guid> mastery;
    public WordyGameAppService(IRepository<ChildProfile, Guid> children, IRepository<GameWorld, string> worlds,
        IRepository<GameLevel, string> levels, IRepository<LevelAttempt, Guid> attempts,
        IRepository<PlayerProgress, Guid> progress, IRepository<WordMastery, Guid> mastery)
    { this.children = children; this.worlds = worlds; this.levels = levels; this.attempts = attempts; this.progress = progress; this.mastery = mastery; }

    private async Task<ChildProfile> OwnedChild(Guid id)
    {
        var child = await children.FindAsync(id);
        if (child == null || child.ParentUserId != CurrentUser.GetId()) throw new AbpAuthorizationException("Child profile is not accessible.");
        return child;
    }
    private static JsonElement Definition(GameLevel level)
    {
        var node = JsonNode.Parse(level.DefinitionJson)!;
        node["contentVersion"] = level.ContentVersion;
        return JsonSerializer.SerializeToElement(node);
    }
    public async Task<List<WorldDto>> GetWorldsAsync() => (await worlds.GetListAsync(x => x.IsPublished))
        .OrderBy(x => x.DisplayOrder).Select(x => new WorldDto(x.Id, x.Name, x.VietnameseName, x.Hero, x.Theme, x.Goal)).ToList();
    public async Task<List<JsonElement>> GetLevelsAsync(string? worldId = null)
    {
        var publishedWorlds = (await worlds.GetListAsync(x => x.IsPublished)).Select(x => x.Id).ToHashSet();
        return (await levels.GetListAsync(x => x.IsPublished && (worldId == null || x.WorldId == worldId)))
            .Where(x => publishedWorlds.Contains(x.WorldId)).OrderBy(x => x.WorldId).ThenBy(x => x.DisplayOrder).Select(Definition).ToList();
    }
    public async Task<JsonElement> GetLevelAsync(string id)
    {
        var level = await levels.GetAsync(id);
        if (!level.IsPublished || !(await worlds.GetAsync(level.WorldId)).IsPublished) throw new UserFriendlyException("Màn chơi đang được chuẩn bị.");
        return Definition(level);
    }
    public async Task<List<ChildDto>> GetChildrenAsync()
    {
        var owner = CurrentUser.GetId();
        return (await children.GetListAsync(x => x.ParentUserId == owner)).OrderBy(x => x.CreationTime)
            .Select(x => new ChildDto(x.Id, x.Nickname, x.AvatarKey, x.AgeBand)).ToList();
    }
    public async Task<ChildDto> CreateChildAsync(CreateChildInput input)
    {
        var name = input.Nickname.Trim();
        if (name.Length == 0) throw new UserFriendlyException("Hãy nhập biệt danh của bé.");
        var child = new ChildProfile(GuidGenerator.Create()) { ParentUserId = CurrentUser.GetId(), Nickname = name,
            AvatarKey = input.AvatarKey, AgeBand = input.AgeBand, CreationTime = Clock.Now, LastPlayedAt = Clock.Now };
        await children.InsertAsync(child);
        return new ChildDto(child.Id, child.Nickname, child.AvatarKey, child.AgeBand);
    }
    public async Task<List<ProgressDto>> GetProgressAsync(Guid childId)
    {
        await OwnedChild(childId);
        return (await progress.GetListAsync(x => x.ChildProfileId == childId)).Select(ToDto).ToList();
    }
    private static ProgressDto ToDto(PlayerProgress p) => new(p.LevelId, p.BestStars, p.CompletedCount);

    public async Task<ProgressDto> SaveAttemptAsync(AttemptInput input)
    {
        var child = await OwnedChild(input.ChildId);
        if (input.AttemptId == Guid.Empty || input.StartedAt.Kind != DateTimeKind.Utc || input.CompletedAt.Kind != DateTimeKind.Utc ||
            input.CompletedAt < input.StartedAt || input.CompletedAt > Clock.Now.AddMinutes(5) ||
            input.CompletedAt - input.StartedAt > TimeSpan.FromDays(1)) throw new UserFriendlyException("Dữ liệu lượt chơi không hợp lệ.");
        var existing = await attempts.FindAsync(input.AttemptId);
        if (existing != null)
        {
            if (existing.ChildProfileId != child.Id || existing.LevelId != input.LevelId) throw new AbpAuthorizationException();
            return ToDto(await progress.GetAsync(x => x.ChildProfileId == child.Id && x.LevelId == input.LevelId));
        }
        // Updating the aggregate's concurrency stamp serializes progress writes for this child.
        // A racing request rolls back atomically and remains in the client's retry queue.
        child.LastPlayedAt = Clock.Now;
        await children.UpdateAsync(child, autoSave: true);
        var level = await levels.GetAsync(input.LevelId);
        if (level.Mechanic == "hide_seek") throw new UserFriendlyException("Hãy gửi lượt Trốn tìm qua bộ chấm điểm Hide & Seek.");
        // Accept a queued completion even if content was unpublished after the child started.
        var ordered = (await levels.GetListAsync(x => x.Mechanic != "hide_seek")).OrderBy(x => x.WorldId).ThenBy(x => x.DisplayOrder).ToList();
        var index = ordered.FindIndex(x => x.Id == level.Id);
        if (index > 0 && !await progress.AnyAsync(x => x.ChildProfileId == child.Id && x.LevelId == ordered[index - 1].Id && x.BestStars >= 1))
            throw new UserFriendlyException("Hãy hoàn thành màn trước để mở màn này.");
        using var definition = JsonDocument.Parse(level.DefinitionJson);
        var root = definition.RootElement;
        var targets = root.GetProperty("targets").EnumerateArray().Select(x => x.GetProperty("value").GetString()!).ToHashSet();
        var vocabulary = root.GetProperty("targetVocabulary").EnumerateArray().Select(x => x.GetString()!).ToList();
        var reviews = root.GetProperty("reviewVocabulary").EnumerateArray().Select(x => x.GetString()!).ToList();
        var allowed = targets.Concat(vocabulary).Concat(reviews).ToHashSet();
        var isBalloon = level.Mechanic is "balloon_pop" or "balloon_dart";
        if (isBalloon && root.TryGetProperty("balloonDart", out var balloonDefinition))
            foreach (var round in balloonDefinition.GetProperty("rounds").EnumerateArray())
                foreach (var balloon in round.GetProperty("balloons").EnumerateArray())
                {
                    var semantic = balloon.GetProperty("semantic");
                    allowed.Add(semantic.TryGetProperty("word", out var word) ? word.GetString()! : semantic.GetProperty("id").GetString()!);
                }
        var results = input.TargetResults;
        if (results.Any(x => !allowed.Contains(x.Term)) || results.Count(x => !x.Correct) != input.WrongAttempts ||
            !vocabulary.All(t => results.Any(x => x.Term == t && x.Correct)) ||
            (level.IsBoss && results.Count(x => x.Correct) < 4)) throw new UserFriendlyException("Lượt chơi chưa hoàn thành.");
        if (input.BalloonDart is { } balloonSummary && (!isBalloon || balloonSummary.Mechanic != "balloon_dart" ||
            balloonSummary.RoundsCompleted != 3 || balloonSummary.Stars != 3 || results.Count(x => x.Correct) != 3 ||
            balloonSummary.WrongHits != input.WrongAttempts || balloonSummary.HintCount != input.HintCount ||
            balloonSummary.Shots != 3 + balloonSummary.WrongHits + balloonSummary.Misses))
            throw new UserFriendlyException("Tổng kết màn bóng bay không hợp lệ.");
        // Old queued card attempts keep their original scoring. New dart rounds reward completion.
        var stars = isBalloon && input.BalloonDart != null ? 3 : input.WrongAttempts == 0 && input.HintCount == 0 ? 3 : input.WrongAttempts <= 1 ? 2 : 1;
        var duration = (long)(input.CompletedAt - input.StartedAt).TotalMilliseconds;
        await attempts.InsertAsync(new LevelAttempt(input.AttemptId) { ChildProfileId = child.Id, LevelId = level.Id,
            StartedAt = input.StartedAt, CompletedAt = input.CompletedAt, WrongAttempts = input.WrongAttempts,
            HintCount = input.HintCount, Stars = stars, DurationMs = duration, PayloadJson = JsonSerializer.Serialize(input) });
        var p = await progress.FindAsync(x => x.ChildProfileId == child.Id && x.LevelId == level.Id);
        if (p == null)
        {
            p = new PlayerProgress(GuidGenerator.Create()) { ChildProfileId = child.Id, LevelId = level.Id,
                FirstCompletedAt = input.CompletedAt, BestDurationMs = duration };
            await progress.InsertAsync(p);
        }
        p.BestStars = Math.Max(p.BestStars, stars); p.CompletedCount++;
        p.LastCompletedAt = p.LastCompletedAt > input.CompletedAt ? p.LastCompletedAt : input.CompletedAt;
        p.BestDurationMs = Math.Min(p.BestDurationMs, duration);
        await progress.UpdateAsync(p);
        foreach (var group in results.GroupBy(x => x.Term))
        {
            var word = await mastery.FindAsync(x => x.ChildProfileId == child.Id && x.Term == group.Key);
            if (word == null) { word = new WordMastery(GuidGenerator.Create()) { ChildProfileId = child.Id, Term = group.Key }; await mastery.InsertAsync(word); }
            word.Record(group.Count(x => x.Correct), group.Count(x => !x.Correct), Clock.Now);
            await mastery.UpdateAsync(word);
        }
        return ToDto(p);
    }
    public async Task<DashboardDto> GetDashboardAsync(Guid childId)
    {
        await OwnedChild(childId);
        var p = await progress.GetListAsync(x => x.ChildProfileId == childId);
        var words = (await mastery.GetListAsync(x => x.ChildProfileId == childId)).OrderBy(x => x.Term)
            .Select(x => new MasteryDto(x.Term, x.ExposureCount, x.CorrectCount, x.IncorrectCount, x.MasteryScore)).ToList();
        var query = await attempts.GetQueryableAsync();
        var duration = await AsyncExecuter.SumAsync(query.Where(x => x.ChildProfileId == childId).Select(x => (double)x.DurationMs));
        return new DashboardDto(p.Count, p.Sum(x => x.BestStars), Math.Round(duration / 60000, 1), words,
            words.Where(x => x.ExposureCount >= 2 && x.MasteryScore < 70).ToList());
    }
    private void RequireAdmin() { if (!CurrentUser.IsInRole("admin")) throw new AbpAuthorizationException(); }
    public async Task<List<AdminLevelDto>> GetAdminLevelsAsync(string? worldId = null)
    {
        RequireAdmin();
        return (await levels.GetListAsync(x => x.Mechanic != "hide_seek" && (worldId == null || x.WorldId == worldId))).OrderBy(x => x.Id)
            .Select(x => new AdminLevelDto(x.Id, x.WorldId, x.Instruction, x.Difficulty, x.Mechanic, x.IsPublished, x.ContentVersion)).ToList();
    }
    public async Task UpdateLevelAsync(string id, EditLevelInput input)
    {
        RequireAdmin();
        var level = await levels.GetAsync(id);
        var node = JsonNode.Parse(level.DefinitionJson)!;
        if (input.Mechanic == "letter_puzzle" && node["targetVocabulary"]![0]!.GetValue<string>().Length > 8)
            throw new UserFriendlyException("Từ ghép chữ tối đa 8 ký tự.");
        if (string.IsNullOrWhiteSpace(input.Instruction)) throw new UserFriendlyException("Hãy nhập hướng dẫn.");
        level.Instruction = input.Instruction.Trim(); level.Difficulty = input.Difficulty; level.Mechanic = input.Mechanic;
        level.IsBoss = input.Mechanic == "boss_challenge"; level.IsPublished = input.IsPublished; level.ContentVersion++;
        node["instruction"] = level.Instruction; node["difficulty"] = level.Difficulty; node["mechanic"] = level.Mechanic;
        node["isBoss"] = level.IsBoss; node["instructionAudioKey"] = ""; // Edited text must not play an obsolete recording.
        level.DefinitionJson = node.ToJsonString();
        await levels.UpdateAsync(level);
    }
}
