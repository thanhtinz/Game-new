using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}

public class BanRequest
{
    public string Reason { get; set; } = string.Empty;
}
