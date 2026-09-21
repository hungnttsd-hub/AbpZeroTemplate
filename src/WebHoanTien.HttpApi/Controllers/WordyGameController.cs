using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebHoanTien.WordyWings;

namespace WebHoanTien.Controllers;

[Authorize, Route("api/game")]
public class WordyGameController : WebHoanTienController
{
    private readonly IWordyGameAppService game;
    public WordyGameController(IWordyGameAppService game) => this.game = game;
    [HttpGet("worlds")] public Task<List<WorldDto>> Worlds() => game.GetWorldsAsync();
    [HttpGet("levels")] public Task<List<JsonElement>> Levels([FromQuery] string? worldId) => game.GetLevelsAsync(worldId);
    [HttpGet("worlds/{worldId}/levels")] public Task<List<JsonElement>> WorldLevels(string worldId) => game.GetLevelsAsync(worldId);
    [HttpGet("levels/{id}")] public Task<JsonElement> Level(string id) => game.GetLevelAsync(id);
    [HttpGet("children")] public Task<List<ChildDto>> Children() => game.GetChildrenAsync();
    [HttpPost("children")] public Task<ChildDto> CreateChild(CreateChildInput input) => game.CreateChildAsync(input);
    [HttpGet("children/{childId:guid}/progress")] public Task<List<ProgressDto>> Progress(Guid childId) => game.GetProgressAsync(childId);
    [HttpPost("attempts")]
    [HttpPost("progress/sync")] public Task<ProgressDto> Attempt(AttemptInput input) => game.SaveAttemptAsync(input);
    [HttpGet("children/{childId:guid}/dashboard")] public Task<DashboardDto> Dashboard(Guid childId) => game.GetDashboardAsync(childId);
    [HttpGet("children/{childId:guid}/review-queue")] public async Task<List<MasteryDto>> Review(Guid childId) => (await game.GetDashboardAsync(childId)).Review;
    [HttpGet("admin/levels")] public Task<List<AdminLevelDto>> AdminLevels([FromQuery] string? worldId) => game.GetAdminLevelsAsync(worldId);
    [HttpPut("admin/levels/{id}")] public Task EditLevel(string id, EditLevelInput input) => game.UpdateLevelAsync(id, input);
}
