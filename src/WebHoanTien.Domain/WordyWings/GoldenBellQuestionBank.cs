using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace WebHoanTien.WordyWings;

public record GoldenBellBank(string Version, List<JsonElement> Questions);
public interface IGoldenBellQuestionBank
{
    Task<GoldenBellBank> GetAsync();
    Task<int> ImportAsync(JsonElement payload, bool onlyMissing = false);
}

public static class GoldenBellContent
{
    public static readonly string[] Types = "action_recognition can_cannot color_object color_recognition compare_quantity count_objects final_reasoning function_question listen_find_picture memory_scene missing_letter multi_clue_scene odd_one_out picture_to_word preposition_scene select_pair sentence_completion sentence_order shape_recognition short_story simple_inference two_attribute_object two_step_instruction weather_choice word_picture_mismatch".Split(' ');
    public static string Text(JsonElement item, string key) => item.GetProperty(key).GetString()!;
    public static List<string> Strings(JsonElement item) => item.EnumerateArray().Select(x => x.GetString()!).ToList();
    public static JsonElement Embedded()
    {
        using var stream = typeof(GoldenBellContent).Assembly.GetManifestResourceStream("WordyWings.GoldenBell.questions.v1.json")
            ?? throw new InvalidOperationException("Missing Golden Bell content. Run node scripts/import-golden-bell.mjs in react before building.");
        using var document = JsonDocument.Parse(stream); return document.RootElement.Clone();
    }
    public static GoldenBellBank Validate(JsonElement root)
    {
        var checking = "bank";
        try
        {
            var version = Text(root, "version");
            if (string.IsNullOrWhiteSpace(version) || version.Length > 80 || root.GetProperty("count").GetInt32() != 1000) throw new FormatException();
            var questions = root.GetProperty("questions").EnumerateArray().Select(q => q.Clone()).ToList();
            if (questions.Count != 1000 || questions.Select(q => Text(q, "id")).Distinct().Count() != 1000) throw new FormatException();
            if (!Enumerable.Range(1, 1000).Select(n => $"GB-{n:0000}").ToHashSet().SetEquals(questions.Select(q => Text(q, "id")))) throw new FormatException();
            foreach (var difficulty in Enumerable.Range(1, 10)) if (questions.Count(q => q.GetProperty("difficulty").GetInt32() == difficulty) != 100) throw new FormatException();
            foreach (var q in questions)
            {
                checking = Text(q, "id");
                var type = Text(q, "questionType");
                if (!Types.Contains(type) || !new[] { "answer_zone", "raise_board", "listen_run", "quick_match", "bell_choice" }.Contains(Text(q, "mechanic"))) throw new FormatException();
                foreach (var field in new[] { "skill", "promptText", "audioText", "explanation" }) if (string.IsNullOrWhiteSpace(Text(q, field))) throw new FormatException();
                if (!Strings(q.GetProperty("targetVocabulary")).Any() || q.GetProperty("stimulus").ValueKind != JsonValueKind.Object) throw new FormatException();
                var seconds = q.GetProperty("estimatedSeconds").GetInt32();
                if (seconds < 5 || seconds > 60 || q.GetProperty("requiresMemoryPhase").GetBoolean() != (type == "memory_scene")) throw new FormatException();
                var hint = q.GetProperty("hint");
                if (hint.GetProperty("afterWrong").GetInt32() < 1 || string.IsNullOrWhiteSpace(Text(hint, "text"))) throw new FormatException();
                var options = q.GetProperty("options").EnumerateArray().ToArray();
                if (options.Select(o => Text(o, "id")).Distinct().Count() != options.Length) throw new FormatException();
                var answer = q.GetProperty("answer"); var answerType = Text(answer, "type");
                if (answerType == "option")
                {
                    if (options.Length < 2 || options.Length > 4 || !options.Any(o => Text(o, "id") == Text(answer, "value"))) throw new FormatException();
                    var correct = options.Single(o => Text(o, "id") == Text(answer, "value"));
                    if (type == "two_step_instruction" && !Strings(correct.GetProperty("sequence")).SequenceEqual(Strings(answer.GetProperty("sequence")))) throw new FormatException();
                    if (type == "missing_letter")
                    {
                        var word = Text(q.GetProperty("stimulus"), "maskedWord").Replace(" ", "").ToCharArray(); var letters = Text(correct, "label");
                        var positions = answer.TryGetProperty("missingPositions", out var givenPositions)
                            ? givenPositions.EnumerateArray().Select(p => p.GetInt32()).ToArray()
                            : Enumerable.Range(0, word.Length).Where(i => word[i] == '_').ToArray();
                        if (positions.Length != letters.Length || positions.Distinct().Count() != positions.Length) throw new FormatException();
                        for (var i = 0; i < positions.Length; i++) { if (positions[i] < 0 || positions[i] >= word.Length || word[positions[i]] != '_') throw new FormatException(); word[positions[i]] = letters[i]; }
                        if (!string.Equals(new string(word), Strings(q.GetProperty("targetVocabulary"))[0], StringComparison.OrdinalIgnoreCase)) throw new FormatException();
                    }
                }
                else if (answerType != "sequence" || type != "sentence_order" || !Strings(answer.GetProperty("value")).OrderBy(x => x).SequenceEqual(Strings(q.GetProperty("stimulus").GetProperty("tiles")).OrderBy(x => x))) throw new FormatException();
            }
            return new GoldenBellBank(version, questions);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or FormatException or ArgumentException or IndexOutOfRangeException)
        { throw new UserFriendlyException($"Ngân hàng Chuông Sao không hợp lệ tại {checking}: cần GB-0001–GB-1000, 100 câu/mức, đúng dạng câu và đáp án."); }
    }
    public static bool Evaluate(JsonElement question, JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String || !input.TryGetProperty("value", out var value)) return false;
        var answer = question.GetProperty("answer");
        if (Text(question, "questionType") == "two_step_instruction")
            return type.GetString() == "sequence" && value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(v => v.ValueKind == JsonValueKind.String) && Strings(value).SequenceEqual(Strings(answer.GetProperty("sequence")));
        if (type.GetString() != Text(answer, "type")) return false;
        if (type.GetString() == "option") return value.ValueKind == JsonValueKind.String && value.GetString() == Text(answer, "value");
        return value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(v => v.ValueKind == JsonValueKind.String) && Strings(value).SequenceEqual(Strings(answer.GetProperty("value")));
    }
}

public class GoldenBellQuestionBank : IGoldenBellQuestionBank, ITransientDependency
{
    private readonly IRepository<GoldenBellQuestion, string> questions;
    public GoldenBellQuestionBank(IRepository<GoldenBellQuestion, string> questions) => this.questions = questions;
    public async Task<GoldenBellBank> GetAsync()
    {
        var all = await questions.GetListAsync(q => q.IsActive);
        if (all.Count == 0) return GoldenBellContent.Validate(GoldenBellContent.Embedded());
        if (all.Count != 1000) throw new UserFriendlyException("Ngân hàng chưa đủ 1.000 câu. Hãy import lại toàn bộ gói nội dung.");
        return new GoldenBellBank(all[0].ContentVersion, all.OrderBy(q => q.Id).Select(q => JsonSerializer.Deserialize<JsonElement>(q.DefinitionJson)).ToList());
    }
    [UnitOfWork(isTransactional: true)]
    public virtual async Task<int> ImportAsync(JsonElement payload, bool onlyMissing = false)
    {
        // Validate the entire batch before the first write; repeated imports upsert by canonical code.
        var bank = GoldenBellContent.Validate(payload);
        var known = (await questions.GetListAsync()).ToDictionary(q => q.Id);
        var changed = 0;
        foreach (var definition in bank.Questions)
        {
            var code = GoldenBellContent.Text(definition, "id");
            var exists = known.TryGetValue(code, out var entity);
            if (exists && onlyMissing) continue;
            entity ??= new GoldenBellQuestion(code);
            entity.Difficulty = definition.GetProperty("difficulty").GetInt32(); entity.QuestionType = GoldenBellContent.Text(definition, "questionType");
            entity.ContentVersion = bank.Version; entity.DefinitionJson = definition.GetRawText(); entity.IsActive = true;
            if (exists) await questions.UpdateAsync(entity); else await questions.InsertAsync(entity);
            changed++;
        }
        return changed;
    }
}

public class GoldenBellSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IGoldenBellQuestionBank bank;
    public GoldenBellSeedContributor(IGoldenBellQuestionBank bank) => this.bank = bank;
    [UnitOfWork(isTransactional: true)]
    public virtual async Task SeedAsync(DataSeedContext context) => await bank.ImportAsync(GoldenBellContent.Embedded(), onlyMissing: true);
}
