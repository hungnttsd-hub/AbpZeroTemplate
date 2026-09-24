using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebHoanTien.WordyWings;

namespace WebHoanTien.Controllers;
[Authorize, Route("api/game/hide-seek")]
public class HideSeekController : WebHoanTienController
{
    private readonly IHideSeekAppService game;
    public HideSeekController(IHideSeekAppService game) => this.game = game;
    [HttpPost("complete"), RequestSizeLimit(2_000_000)] public Task<HideSeekResultDto> Complete(HideSeekCompleteInput input) => game.CompleteAsync(input);
    [HttpGet("children/{childId:guid}/history")] public Task<List<ProgressDto>> History(Guid childId) => game.GetHistoryAsync(childId);
}
