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
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace WebHoanTien.WordyWings;

[Authorize, RemoteService(false)]
public class GoldenBellAppService : WebHoanTienAppService, IGoldenBellAppService
{
    private readonly IRepository<ChildProfile, Guid> children;
    private readonly IRepository<GoldenBellSession, Guid> sessions;
    private readonly IRepository<GoldenBellAttempt, Guid> attempts;
    private readonly IRepository<WordMastery, Guid> mastery;
    private readonly IGoldenBellQuestionBank bank;
    public GoldenBellAppService(IRepository<ChildProfile, Guid> children, IRepository<GoldenBellSession, Guid> sessions,
        IRepository<GoldenBellAttempt, Guid> attempts, IRepository<WordMastery, Guid> mastery, IGoldenBellQuestionBank bank)
    { this.children = children; this.sessions = sessions; this.attempts = attempts; this.mastery = mastery; this.bank = bank; }

    private async Task<ChildProfile> OwnedChild(Guid id)
    {
        var child = await children.FindAsync(id);
        if (child == null || child.ParentUserId != CurrentUser.GetId()) throw new AbpAuthorizationException("Child profile is not accessible.");
        return child;
    }
    private async Task<GoldenBellSession> OwnedSession(Guid id)
    {
        var session = await sessions.FindAsync(id) ?? throw new UserFriendlyException("Không tìm thấy lượt Chuông Sao.");
        await OwnedChild(session.ChildProfileId); return session;
    }
    private static List<JsonElement> Questions(GoldenBellSession s) => JsonSerializer.Deserialize<List<JsonElement>>(s.QuestionsJson)!;
    private static GoldenBellSessionDto Dto(GoldenBellSession s) => new(s.Id, s.ChildProfileId, s.Seed, s.BankVersion, Questions(s), s.CurrentQuestionIndex, s.WrongAttempts, s.HintCount, s.DurationMs, s.BellRung, s.StartedAt, s.CompletedAt);

    [UnitOfWork(isTransactional: true)]
    public virtual async Task<GoldenBellSessionDto> StartAsync(GoldenBellStartInput input)
    {
        await OwnedChild(input.ChildId);
        if (input.SessionId == Guid.Empty) throw new UserFriendlyException("Thiếu mã lượt chơi.");
        var existing = await sessions.FindAsync(input.SessionId);
        if (existing != null)
        {
            if (existing.ChildProfileId != input.ChildId) throw new AbpAuthorizationException("Session is not accessible.");
            if (existing.Seed != input.Seed || (input.QuestionCodes != null && !Questions(existing).Select(q => GoldenBellContent.Text(q, "id")).SequenceEqual(input.QuestionCodes)))
                throw new UserFriendlyException("Mã lượt chơi đã được dùng cho bộ câu khác.");
            return Dto(existing);
        }
        var source = await bank.GetAsync();
        if (input.BankVersion != null && input.BankVersion != source.Version) throw new UserFriendlyException("Phiên bản nội dung trên máy chủ chưa khớp. Hãy cập nhật ngân hàng câu hỏi; lượt chơi vẫn được giữ trên thiết bị.");
        List<JsonElement> selected;
        if (input.QuestionCodes != null)
        {
            var dictionary = source.Questions.ToDictionary(q => GoldenBellContent.Text(q, "id"));
            if (input.QuestionCodes.Count != 12 || input.QuestionCodes.Distinct().Count() != 12 || input.QuestionCodes.Any(code => !dictionary.ContainsKey(code))) throw new UserFriendlyException("Một lượt cần 12 mã câu hỏi hợp lệ, khác nhau.");
            selected = input.QuestionCodes.Select(code => dictionary[code]).ToList();
        }
        else selected = SelectQuestions(source, input.Seed);
        var now = Clock.Now;
        var started = input.StartedAt?.ToUniversalTime() ?? now;
        if (started > now.AddMinutes(5) || started < new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)) throw new UserFriendlyException("Thời gian bắt đầu không hợp lệ.");
        var session = new GoldenBellSession(input.SessionId) { ChildProfileId = input.ChildId, Seed = input.Seed, BankVersion = source.Version,
            QuestionsJson = JsonSerializer.Serialize(selected), StartedAt = started };
        await sessions.InsertAsync(session); return Dto(session);
    }
    private static List<JsonElement> SelectQuestions(GoldenBellBank bank, long seed)
    {
        var profile = new[] { 1, 2, 2, 3, 4, 5, 6, 7, 8, 8, 9, 10 }; var selected = new List<JsonElement>();
        foreach (var difficulty in profile)
        {
            var used = selected.Select(q => GoldenBellContent.Text(q, "id")).ToHashSet();
            var recent = selected.TakeLast(2).ToList();
            var words = recent.SelectMany(q => GoldenBellContent.Strings(q.GetProperty("targetVocabulary"))).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidates = bank.Questions.Where(q => !used.Contains(GoldenBellContent.Text(q, "id")) &&
                !(recent.Count == 2 && recent.All(r => GoldenBellContent.Text(r, "questionType") == GoldenBellContent.Text(q, "questionType"))) &&
                !(recent.Count > 0 && GoldenBellContent.Text(recent.Last(), "questionType") == "memory_scene" && GoldenBellContent.Text(q, "questionType") == "memory_scene") &&
                !GoldenBellContent.Strings(q.GetProperty("targetVocabulary")).Any(words.Contains));
            var chosen = candidates.OrderBy(q => Math.Abs(q.GetProperty("difficulty").GetInt32() - difficulty)).ThenBy(q => StableOrder(seed + selected.Count, GoldenBellContent.Text(q, "id"))).First();
            selected.Add(chosen);
        }
        return selected;
    }
    private static uint StableOrder(long seed, string code) { var value = (uint)seed; foreach (var c in code) value = unchecked((value ^ c) * 16777619); return value; }
    public async Task<GoldenBellSessionDto> GetAsync(Guid id) => Dto(await OwnedSession(id));

    [UnitOfWork(isTransactional: true)]
    public virtual async Task<GoldenBellAnswerDto> AnswerAsync(Guid id, GoldenBellAnswerInput input)
    {
        var session = await OwnedSession(id);
        if (input.Id == Guid.Empty || input.Input.ValueKind != JsonValueKind.Object || input.Input.GetRawText().Length > 4000) throw new UserFriendlyException("Đáp án không hợp lệ.");
        var inputJson = JsonSerializer.Serialize(input.Input);
        var prior = await attempts.FindAsync(input.Id);
        if (prior != null)
        {
            if (prior.SessionId != id || prior.QuestionCode != input.QuestionId || prior.QuestionIndex != input.QuestionIndex || !JsonNode.DeepEquals(JsonNode.Parse(prior.InputJson), JsonNode.Parse(inputJson)) || prior.HintUsed != input.HintUsed || prior.DurationMs != input.DurationMs)
                throw new UserFriendlyException("Mã đáp án đã được sử dụng cho nội dung khác.");
            return new GoldenBellAnswerDto(prior.IsCorrect, session.CurrentQuestionIndex, session.WrongAttempts, prior.HintUsed);
        }
        if (session.CompletedAt != null || session.CurrentQuestionIndex != input.QuestionIndex || input.QuestionIndex >= 12) throw new UserFriendlyException("Câu hỏi không khớp tiến trình hiện tại.");
        var question = Questions(session)[input.QuestionIndex];
        if (GoldenBellContent.Text(question, "id") != input.QuestionId) throw new UserFriendlyException("Mã câu hỏi không thuộc lượt chơi.");
        var previous = await attempts.GetListAsync(a => a.SessionId == id && a.QuestionIndex == input.QuestionIndex);
        if (previous.Count >= 10000) throw new UserFriendlyException("Lượt chơi có quá nhiều đáp án. Hãy mở lượt mới.");
        var previousMs = previous.Select(a => a.DurationMs).DefaultIfEmpty(0).Max();
        if (input.DurationMs < previousMs) throw new UserFriendlyException("Thời lượng câu hỏi không thể giảm.");
        // Re-evaluate against the immutable server snapshot, never the client's claimed result.
        var correct = GoldenBellContent.Evaluate(question, input.Input);
        await attempts.InsertAsync(new GoldenBellAttempt(input.Id) { SessionId = id, QuestionCode = input.QuestionId, QuestionIndex = input.QuestionIndex,
            AttemptNumber = previous.Count + 1, IsCorrect = correct, HintUsed = input.HintUsed, DurationMs = input.DurationMs, InputJson = inputJson,
            CreatedAt = input.CreatedAt == default ? Clock.Now : input.CreatedAt.ToUniversalTime() });
        session.DurationMs += input.DurationMs - previousMs;
        if (input.HintUsed && previous.All(a => !a.HintUsed)) session.HintCount++;
        if (correct) session.CurrentQuestionIndex++; else session.WrongAttempts++;
        // Aggregate concurrency + the unique attempt id make retries safe and serialize writes.
        session.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await sessions.UpdateAsync(session);
        return new GoldenBellAnswerDto(correct, session.CurrentQuestionIndex, session.WrongAttempts, input.HintUsed);
    }

    [UnitOfWork(isTransactional: true)]
    public virtual async Task<GoldenBellSessionDto> CompleteAsync(Guid id, GoldenBellCompleteInput input)
    {
        var session = await OwnedSession(id);
        if (session.CompletedAt != null) return Dto(session);
        var answers = await attempts.GetListAsync(a => a.SessionId == id);
        if (!input.BellRung || session.CurrentQuestionIndex != 12 || answers.Where(a => a.IsCorrect).Select(a => a.QuestionIndex).Distinct().Count() != 12)
            throw new UserFriendlyException("Hãy hoàn thành 12 câu và tự tay rung Chuông Sao.");
        var completedAt = input.CompletedAt?.ToUniversalTime() ?? Clock.Now;
        if (completedAt < session.StartedAt || completedAt > Clock.Now.AddMinutes(5)) throw new UserFriendlyException("Thời gian hoàn thành không hợp lệ.");
        var child = await OwnedChild(session.ChildProfileId);
        var words = (await mastery.GetListAsync(m => m.ChildProfileId == child.Id)).ToDictionary(m => m.Term, StringComparer.OrdinalIgnoreCase);
        var questions = Questions(session);
        for (var index = 0; index < questions.Count; index++)
        {
            var wrong = answers.Count(a => a.QuestionIndex == index && !a.IsCorrect);
            foreach (var term in GoldenBellContent.Strings(questions[index].GetProperty("targetVocabulary")).Select(t => t.ToLowerInvariant()).Distinct())
            {
                if (!words.TryGetValue(term, out var word)) { word = new WordMastery(GuidGenerator.Create()) { ChildProfileId = child.Id, Term = term }; words[term] = word; await mastery.InsertAsync(word); }
                word.Record(1, wrong, completedAt); await mastery.UpdateAsync(word);
            }
        }
        session.BellRung = true; session.CompletedAt = completedAt; session.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        child.LastPlayedAt = child.LastPlayedAt > completedAt ? child.LastPlayedAt : completedAt; child.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        await children.UpdateAsync(child); await sessions.UpdateAsync(session); return Dto(session);
    }
    public async Task<GoldenBellHistoryDto> GetHistoryAsync(Guid childId)
    {
        await OwnedChild(childId);
        var played = await sessions.GetListAsync(s => s.ChildProfileId == childId && s.BellRung);
        var completed = played.SelectMany(s => Questions(s).Select(q => GoldenBellContent.Text(q, "id"))).Distinct().OrderBy(x => x).ToList();
        return new GoldenBellHistoryDto(completed, played.Count, played.Count, played.Sum(s => s.DurationMs) / 60000d);
    }
    [Authorize(Roles = "admin")]
    public async Task<JsonElement> GetQuestionAsync(string code) => (await bank.GetAsync()).Questions.FirstOrDefault(q => GoldenBellContent.Text(q, "id") == code) is var question && question.ValueKind != JsonValueKind.Undefined ? question : throw new UserFriendlyException("Không tìm thấy câu hỏi.");
    [Authorize(Roles = "admin"), UnitOfWork(isTransactional: true)]
    public virtual Task<int> ImportAsync(JsonElement payload) => bank.ImportAsync(payload);
}
