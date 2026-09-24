using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace WebHoanTien.WordyWings;

public static class HideSeekContent
{
    private static readonly Lazy<JsonElement> Bank = new(() => {
        using var stream = typeof(HideSeekContent).Assembly.GetManifestResourceStream("WordyWings.HideSeek.levels.v1.json")
            ?? throw new InvalidOperationException("Missing Hide & Seek content.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetString() != "1.0.0") throw new InvalidOperationException("Unsupported Hide & Seek schema.");
        var entities = root.GetProperty("entities").EnumerateArray().Select(e => e.GetProperty("id").GetString()!).ToHashSet();
        var ids = new HashSet<string>();
        foreach (var level in root.GetProperty("levels").EnumerateArray()) {
            var spots = level.GetProperty("spots").EnumerateArray().ToArray();
            var tools = level.GetProperty("tools").EnumerateArray().Select(t => t.GetString()).ToHashSet();
            if (!ids.Add(level.GetProperty("id").GetString()!) || spots.Length is < 2 or > 6 ||
                spots.Count(s => s.GetProperty("entityId").GetString() == level.GetProperty("targetEntityId").GetString()) != 1 ||
                spots.Select(s => s.GetProperty("id").GetString()).Distinct().Count() != spots.Length ||
                spots.Any(s => !entities.Contains(s.GetProperty("entityId").GetString()!) || !s.GetProperty("allowedTools").EnumerateArray().Any(t => tools.Contains(t.GetString()))))
                throw new InvalidOperationException("Invalid Hide & Seek content.");
        }
        return root.Clone();
    });
    public static IEnumerable<JsonElement> Levels => Bank.Value.GetProperty("levels").EnumerateArray();
    public static JsonElement Get(string levelId, string version) => Levels.FirstOrDefault(l =>
        l.GetProperty("id").GetString() == levelId && l.GetProperty("contentVersion").GetString() == version);
    public static Dictionary<string, string> Occupants(JsonElement level, int seed)
    {
        if (seed < 1) throw new ArgumentException("Invalid seed.");
        var spots = level.GetProperty("spots").EnumerateArray().ToArray();
        var entities = spots.Select(s => s.GetProperty("entityId").GetString()!).ToArray();
        var state = (uint)seed;
        if (level.GetProperty("shuffleOccupants").GetBoolean()) for (var i = entities.Length - 1; i > 0; i--) {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            var j = (int)(state % (uint)(i + 1)); (entities[i], entities[j]) = (entities[j], entities[i]);
        }
        return spots.Select((s, i) => (Id: s.GetProperty("id").GetString()!, Entity: entities[i])).ToDictionary(x => x.Id, x => x.Entity);
    }
    public static async Task EnsureAsync(IRepository<GameWorld, string> worlds, IRepository<GameLevel, string> levels)
    {
        // Separate unpublished world keeps this optional gallery out of the ordered 30-level curriculum.
        if (await worlds.FindAsync("H01") == null) await worlds.InsertAsync(new GameWorld("H01") {
            Name = "Momo's Forest", VietnameseName = "Trốn tìm cùng Momo", Hero = "momo", Theme = "forest",
            Goal = "Find, reveal and identify", DisplayOrder = 100, IsPublished = false
        }, autoSave: true);
        foreach (var item in Levels) {
            var id = item.GetProperty("id").GetString()!;
            if (await levels.FindAsync(id) != null) continue;
            var targetId = item.GetProperty("targetEntityId").GetString();
            var target = Bank.Value.GetProperty("entities").EnumerateArray().Single(e => e.GetProperty("id").GetString() == targetId);
            await levels.InsertAsync(new GameLevel(id) { WorldId = "H01", DisplayOrder = int.Parse(id[3..]), Mechanic = "hide_seek",
                Difficulty = item.GetProperty("difficulty").GetInt32(), Instruction = target.GetProperty("quest").GetString()!,
                DefinitionJson = item.GetRawText(), IsPublished = true, ContentVersion = 1 });
        }
    }
}

public record HideSeekDecision(Guid AnswerEventId, Guid RevealId, string SpotId, bool Answer);
public record HideSeekGrade(int Stars, int WrongReveals, int IndependentCorrect, int IndependentWrong, bool Assisted);
public static class HideSeekScoring
{
    public static HideSeekGrade Grade(JsonElement level, int seed, IReadOnlyList<HideSeekDecision> decisions)
    {
        var occupants = HideSeekContent.Occupants(level, seed);
        var target = level.GetProperty("targetEntityId").GetString()!;
        var events = new Dictionary<Guid, HideSeekDecision>();
        var reveals = new Dictionary<Guid, string>(); var resolved = new HashSet<string>();
        var charged = new HashSet<Guid>(); Guid? assistedReveal = null;
        var completed = false; var independentCorrect = 0; var independentWrong = 0; var assisted = false;
        foreach (var decision in decisions) {
            if (decision.AnswerEventId == Guid.Empty || decision.RevealId == Guid.Empty || !occupants.TryGetValue(decision.SpotId, out var entity))
                throw new ArgumentException("Invalid decision.");
            if (events.TryGetValue(decision.AnswerEventId, out var previous)) {
                if (previous != decision) throw new ArgumentException("Conflicting answer event.");
                continue;
            }
            if (completed || resolved.Contains(decision.SpotId) ||
                assistedReveal.HasValue && assistedReveal != decision.RevealId ||
                reveals.TryGetValue(decision.RevealId, out var spot) && (spot != decision.SpotId || assistedReveal != decision.RevealId))
                throw new ArgumentException("Invalid decision order.");
            events.Add(decision.AnswerEventId, decision); reveals.TryAdd(decision.RevealId, decision.SpotId);
            var isTarget = entity == target; var correct = decision.Answer == isTarget; var isAssisted = assistedReveal.HasValue;
            if (!correct) charged.Add(decision.RevealId);
            if (!isAssisted) { if (correct) independentCorrect++; else independentWrong++; }
            if (isTarget && correct) { completed = true; assisted = isAssisted; }
            else if (isTarget) assistedReveal = decision.RevealId;
            else resolved.Add(decision.SpotId);
        }
        if (!completed) throw new ArgumentException("The target has not been confirmed.");
        return new HideSeekGrade(Math.Max(0, 3 - charged.Count), charged.Count, independentCorrect, independentWrong, assisted);
    }
}
