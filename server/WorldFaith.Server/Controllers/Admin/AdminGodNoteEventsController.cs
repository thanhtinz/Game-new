using WorldFaith.Server.Models;
using Microsoft.AspNetCore.Mvc;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Achievement;
using WorldFaith.Server.Services.Chat;
using WorldFaith.Server.Services.Leaderboard;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Controllers.Admin;

// ─── God Note ─────────────────────────────────────────────
[Route("api/admin/god-note")]
public class AdminGodNoteController : AdminBaseController
{
    private readonly IAchievementService _achievementService;

    public AdminGodNoteController(IAchievementService achievementService)
        => _achievementService = achievementService;

    [HttpGet]
    public async Task<IActionResult> GetGodNote(
        [FromQuery] string worldId, [FromQuery] string godId, [FromQuery] string? tab = null)
    {
        GodNoteTab? tabEnum = null;
        if (!string.IsNullOrEmpty(tab) && Enum.TryParse<GodNoteTab>(tab, out var parsed))
            tabEnum = parsed;

        var entries = await _achievementService.GetGodNoteAsync(worldId, godId, tabEnum);
        return Ok(entries);
    }

    [HttpPost("action")]
    public async Task<IActionResult> ApplyAction([FromBody] ApplyDivineActionRequest req)
    {
        if (!Enum.TryParse<DivineAction>(req.Action, out var action))
            return BadRequest(new { error = "Invalid divine action" });

        var applied = await _achievementService.ApplyDivineActionAsync(req.NpcId, req.GodId, action, tick: 0);
        return Ok(new { success = applied });
    }
}

public class ApplyDivineActionRequest
{
    public string NpcId { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

// ─── Events ───────────────────────────────────────────────
[Route("api/admin/events")]
public class AdminEventsController : AdminBaseController
{
    private readonly INpcEventRepository _eventRepo;
    private readonly IChatRepository _chatRepo;

    public AdminEventsController(INpcEventRepository eventRepo, IChatRepository chatRepo)
    {
        _eventRepo = eventRepo;
        _chatRepo = chatRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent(
        [FromQuery] string worldId, [FromQuery] int limit = 50, [FromQuery] string? type = null)
    {
        var events = await _eventRepo.GetRecentAsync(worldId, limit);

        if (!string.IsNullOrEmpty(type) && type != "All")
        {
            events = type switch
            {
                "Crime" => events.Where(e => e.Type is NpcEventType.Theft or NpcEventType.CorruptionScandal
                    or NpcEventType.Assassination or NpcEventType.HeresyTrial
                    or NpcEventType.Extortion or NpcEventType.TaxEvasion).ToList(),
                "Accidents" => events.Where(e => e.Type is NpcEventType.CropFailure or NpcEventType.DiseaseOutbreak
                    or NpcEventType.BuildingCollapse or NpcEventType.TradeRobbery).ToList(),
                _ => events,
            };
        }

        return Ok(events.Select(e => new
        {
            id = e.Id,
            type = e.Type.ToString(),
            description = e.Description,
            faithImpact = e.FaithImpact,
            economyImpact = e.EconomyImpact,
            stabilityImpact = e.StabilityImpact,
            godResponded = e.GodResponded,
            respondingGodId = e.RespondingGodId,
            tick = e.Tick,
            occurredAt = e.OccurredAt,
        }));
    }

    [HttpGet("chat")]
    public async Task<IActionResult> GetChat([FromQuery] string worldId, [FromQuery] int limit = 50)
        => Ok(await _chatRepo.GetRecentAsync(worldId, limit));
}

// ─── Leaderboard (public read) ─────────────────────────────
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
        => _leaderboardService = leaderboardService;

    [HttpGet("top")]
    public async Task<IActionResult> GetTop([FromQuery] string? type = null, [FromQuery] int limit = 50)
    {
        var result = string.IsNullOrEmpty(type) || type == "rating"
            ? await _leaderboardService.GetTopPlayersAsync(limit)
            : await _leaderboardService.GetLeaderboardByStatAsync(type, limit);

        return Ok(result);
    }
}

// ─── Leaderboard (admin write) ─────────────────────────────
[Route("api/admin/leaderboard")]
public class AdminLeaderboardController : AdminBaseController
{
    private readonly ILeaderboardService _leaderboardService;

    public AdminLeaderboardController(ILeaderboardService leaderboardService)
        => _leaderboardService = leaderboardService;

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await _leaderboardService.ResetAllAsync();
        return Ok(new { success = true });
    }
}
