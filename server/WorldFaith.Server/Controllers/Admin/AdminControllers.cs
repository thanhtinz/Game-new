using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using WorldFaith.Server.Logging;
using WorldFaith.Server.Services.Admin;

namespace WorldFaith.Server.Controllers.Admin;

// ─── Admin Base ──────────────────────────────────────────
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public abstract class AdminBaseController : ControllerBase { }

// ─── Server Controller ───────────────────────────────────
[Route("api/admin/server")]
public class AdminServerController : AdminBaseController
{
    private readonly IAdminService _adminService;
    private readonly IBalanceConfigService _balanceConfig;

    public AdminServerController(IAdminService adminService, IBalanceConfigService balanceConfig)
    {
        _adminService = adminService;
        _balanceConfig = balanceConfig;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
        => Ok(await _adminService.GetServerStatsAsync());

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig([FromQuery] string? category = null)
        => Ok(await _balanceConfig.GetAllAsync(category));

    [HttpPut("config/{key}")]
    public async Task<IActionResult> UpdateConfig(string key, [FromBody] UpdateConfigRequest req)
    {
        var adminId = User.FindFirst("sub")?.Value ?? "admin";
        await _balanceConfig.SetAsync(key, req.Value, adminId);
        return Ok(new { success = true, key, value = req.Value });
    }

    [HttpPost("config/seed")]
    public async Task<IActionResult> SeedConfig()
    {
        await _balanceConfig.SeedDefaultsAsync();
        return Ok(new { success = true, message = "Default config seeded" });
    }

    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int limit = 100)
    {
        // Lightweight in-memory log tail. For production-grade log search,
        // point this at a centralized logging backend (e.g. Seq, ELK, CloudWatch)
        // instead of reading from the console sink.
        var logs = InMemoryLogSink.GetRecent(limit);
        return Ok(logs);
    }

    [HttpPost("restart")]
    public IActionResult Restart([FromServices] IHostApplicationLifetime lifetime)
    {
        // Triggers a graceful shutdown. The actual process restart is performed
        // by the process supervisor (Docker restart policy, systemd, PM2, etc.) —
        // this endpoint does not relaunch the process itself.
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            lifetime.StopApplication();
        });
        return Ok(new { success = true, message = "Server is shutting down for restart" });
    }
}

public class UpdateConfigRequest
{
    public string Value { get; set; } = string.Empty;
}

// ─── Worlds Controller ───────────────────────────────────
[Route("api/admin/worlds")]
public class AdminWorldsController : AdminBaseController
{
    private readonly IAdminService _adminService;

    public AdminWorldsController(IAdminService adminService) => _adminService = adminService;

    [HttpGet]
    public async Task<IActionResult> GetWorlds()
        => Ok(await _adminService.GetAllWorldsAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorldAdminRequest req)
    {
        var world = await _adminService.CreateWorldAsync(req);
        return Ok(world);
    }

    [HttpGet("{worldId}")]
    public async Task<IActionResult> GetById(string worldId)
    {
        var worlds = await _adminService.GetAllWorldsAsync();
        var world = worlds.FirstOrDefault(w => w.Id == worldId);
        return world == null ? NotFound() : Ok(world);
    }

    [HttpGet("{worldId}/snapshot")]
    public async Task<IActionResult> GetSnapshot(string worldId)
        => Ok(await _adminService.GetWorldSnapshotAsync(worldId));

    [HttpPost("{worldId}/end")]
    public async Task<IActionResult> ForceEnd(string worldId)
    {
        await _adminService.ForceEndWorldAsync(worldId);
        return Ok(new { success = true });
    }

    [HttpPost("{worldId}/rebirth")]
    public async Task<IActionResult> ForceRebirth(string worldId)
    {
        await _adminService.ForceRebirthAsync(worldId);
        return Ok(new { success = true });
    }
}

// ─── Players Controller ──────────────────────────────────
[Route("api/admin/players")]
public class AdminPlayersController : AdminBaseController
{
    private readonly IAdminService _adminService;

    public AdminPlayersController(IAdminService adminService) => _adminService = adminService;

    [HttpGet]
    public async Task<IActionResult> GetPlayers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
        => Ok(await _adminService.GetPlayersAsync(page, pageSize, search));

    [HttpGet("{playerId}")]
    public async Task<IActionResult> GetById(string playerId)
    {
        var player = await _adminService.GetPlayerByIdAsync(playerId);
        return player == null ? NotFound() : Ok(player);
    }

    [HttpPost("{playerId}/ban")]
    public async Task<IActionResult> Ban(string playerId, [FromBody] BanRequest req)
    {
        await _adminService.BanPlayerAsync(playerId, req.Reason);
        return Ok(new { success = true });
    }

    [HttpPost("{playerId}/unban")]
    public async Task<IActionResult> Unban(string playerId)
    {
        await _adminService.UnbanPlayerAsync(playerId);
        return Ok(new { success = true });
    }

    [HttpPost("{playerId}/promote")]
    public async Task<IActionResult> Promote(string playerId)
    {
        await _adminService.PromotePlayerAsync(playerId);
        return Ok(new { success = true });
    }

    [HttpPost("{playerId}/demote")]
    public async Task<IActionResult> Demote(string playerId)
    {
        await _adminService.DemotePlayerAsync(playerId);
        return Ok(new { success = true });
    }

    [HttpPost("{playerId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string playerId, [FromBody] ResetPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.NewPass) || req.NewPass.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters" });

        await _adminService.ResetPasswordAsync(playerId, req.NewPass);
        return Ok(new { success = true });
    }
}

public class ResetPasswordRequest
{
    public string NewPass { get; set; } = string.Empty;
}

public class BanRequest
{
    public string Reason { get; set; } = string.Empty;
}

// ─── Maps Controller ─────────────────────────────────────
[Route("api/admin/maps")]
public class AdminMapsController : AdminBaseController
{
    private readonly IAdminService _adminService;

    public AdminMapsController(IAdminService adminService) => _adminService = adminService;

    [HttpGet("{worldId}/tiles")]
    public async Task<IActionResult> GetTiles(string worldId)
    {
        var result = await _adminService.GetMapTilesAsync(worldId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPut("{worldId}/tiles/{x}/{y}")]
    public async Task<IActionResult> UpdateTile(string worldId, int x, int y, [FromBody] UpdateTileRequest req)
    {
        var ok = await _adminService.UpdateTileAsync(worldId, x, y, req);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("{worldId}/sacred")]
    public async Task<IActionResult> PlaceSacred(string worldId, [FromBody] PlaceSacredRequest req)
    {
        var ok = await _adminService.PlaceSacredAsync(worldId, req.X, req.Y);
        return ok ? Ok(new { success = true }) : NotFound();
    }

    [HttpPost("{worldId}/regen")]
    public async Task<IActionResult> Regen(string worldId, [FromBody] RegenMapRequest req)
    {
        var usedSeed = await _adminService.RegenerateMapAsync(worldId, req.Seed);
        return usedSeed == 0
            ? NotFound()
            : Ok(new { success = true, seed = usedSeed });
    }
}

public class PlaceSacredRequest
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class RegenMapRequest
{
    public int? Seed { get; set; }
}
