using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldFaith.Server.Services.Chat;
using WorldFaith.Server.Services.Leaderboard;

namespace WorldFaith.Server.Controllers;

// ─── Leaderboard ─────────────────────────────────────────
[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboard;

    public LeaderboardController(ILeaderboardService leaderboard)
        => _leaderboard = leaderboard;

    [HttpGet("top")]
    public async Task<IActionResult> GetTop([FromQuery] int limit = 50)
        => Ok(await _leaderboard.GetTopPlayersAsync(Math.Clamp(limit, 1, 100)));

    [HttpGet("by/{stat}")]
    public async Task<IActionResult> GetByStat(string stat, [FromQuery] int limit = 20)
        => Ok(await _leaderboard.GetLeaderboardByStatAsync(stat, Math.Clamp(limit, 1, 50)));

    [HttpGet("player/{playerId}")]
    public async Task<IActionResult> GetPlayerStats(string playerId)
    {
        var stats = await _leaderboard.GetPlayerStatsAsync(playerId);
        return stats != null ? Ok(stats) : NotFound();
    }
}

// ─── Chat History ─────────────────────────────────────────
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
        => _chatService = chatService;

    [HttpGet("{worldId}/history")]
    public async Task<IActionResult> GetHistory(string worldId, [FromQuery] int limit = 50)
        => Ok(await _chatService.GetHistoryAsync(worldId, Math.Clamp(limit, 1, 100)));
}
