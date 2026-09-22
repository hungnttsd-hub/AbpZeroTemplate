using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebHoanTien.WordyWings;

namespace WebHoanTien.Controllers;

[Authorize, Route("api/game/golden-bell")]
public class GoldenBellController : WebHoanTienController
{
    private readonly IGoldenBellAppService game;
    public GoldenBellController(IGoldenBellAppService game) => this.game = game;
    [HttpPost("session/start")] public Task<GoldenBellSessionDto> Start(GoldenBellStartInput input) => game.StartAsync(input);
    [HttpGet("session/{id:guid}")] public Task<GoldenBellSessionDto> Session(Guid id) => game.GetAsync(id);
    [HttpPost("session/{id:guid}/answer")] public Task<GoldenBellAnswerDto> Answer(Guid id, GoldenBellAnswerInput input) => game.AnswerAsync(id, input);
    [HttpPost("session/{id:guid}/complete")] public Task<GoldenBellSessionDto> Complete(Guid id, GoldenBellCompleteInput input) => game.CompleteAsync(id, input);
    [HttpGet("children/{childId:guid}/history")] public Task<GoldenBellHistoryDto> History(Guid childId) => game.GetHistoryAsync(childId);
    [Authorize(Roles = "admin"), HttpGet("questions/{code}")] public Task<JsonElement> Question(string code) => game.GetQuestionAsync(code);
    [Authorize(Roles = "admin"), HttpPost("admin/import"), RequestSizeLimit(8_000_000)] public Task<int> Import([FromBody] JsonElement payload) => game.ImportAsync(payload);
}
