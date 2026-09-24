using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace WebHoanTien.WordyWings;

public class GameContentSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<GameWorld, string> worlds;
    private readonly IRepository<GameLevel, string> levels;
    private readonly IRepository<VocabularyTerm, Guid> vocabulary;
    private readonly IGuidGenerator guids;
    public GameContentSeedContributor(IRepository<GameWorld, string> worlds, IRepository<GameLevel, string> levels,
        IRepository<VocabularyTerm, Guid> vocabulary, IGuidGenerator guids)
    { this.worlds = worlds; this.levels = levels; this.vocabulary = vocabulary; this.guids = guids; }

    private static JsonDocument Read(string file)
    {
        using var stream = typeof(GameContentSeedContributor).Assembly.GetManifestResourceStream("WordyWings.Content." + file)
            ?? throw new InvalidOperationException("Missing embedded game content: " + file);
        return JsonDocument.Parse(stream);
    }
    public async Task SeedAsync(DataSeedContext context)
    {
        using var worldData = Read("worlds.json");
        var order = 0;
        foreach (var item in worldData.RootElement.EnumerateArray().Take(3))
        {
            var id = item.GetProperty("id").GetString()!; order++;
            if (await worlds.FindAsync(id) != null) continue;
            await worlds.InsertAsync(new GameWorld(id) { Name = item.GetProperty("name").GetString()!,
                VietnameseName = item.GetProperty("vi").GetString()!, Hero = item.GetProperty("hero").GetString()!,
                Theme = item.GetProperty("theme").GetString()!, Goal = item.GetProperty("goal").GetString()!, DisplayOrder = order });
        }
        using var levelData = Read("mvp_levels_30.json");
        foreach (var item in levelData.RootElement.EnumerateArray())
        {
            var id = item.GetProperty("id").GetString()!;
            if (await levels.FindAsync(id) != null) continue;
            await levels.InsertAsync(new GameLevel(id) { WorldId = item.GetProperty("worldId").GetString()!,
                DisplayOrder = item.GetProperty("order").GetInt32(), Mechanic = item.GetProperty("mechanic").GetString()!,
                Difficulty = item.GetProperty("difficulty").GetInt32(), Instruction = item.GetProperty("instruction").GetString()!,
                IsBoss = item.GetProperty("isBoss").GetBoolean(), DefinitionJson = item.GetRawText() });
        }
        await HideSeekContent.EnsureAsync(worlds, levels);
        using var terms = Read("vocabulary_pre_a1.json");
        var knownTerms = (await vocabulary.GetListAsync()).Select(x => x.Term).ToHashSet(StringComparer.Ordinal);
        foreach (var item in terms.RootElement.EnumerateArray().Where(x => new[] { "W01", "W02", "W03" }.Contains(x.GetProperty("world").GetString())))
        {
            var term = item.GetProperty("term").GetString()!;
            if (!knownTerms.Add(term)) continue;
            await vocabulary.InsertAsync(new VocabularyTerm(guids.Create()) { Term = term,
                Category = item.GetProperty("category").GetString()!, AudioKey = $"audio/en/{term}.mp3", ImageKey = $"vocab/{term}.svg" });
        }
    }
}
