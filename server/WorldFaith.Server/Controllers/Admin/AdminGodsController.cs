using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Controllers.Admin;

[Route("api/admin/gods")]
public class AdminGodsController : AdminBaseController
{
    private readonly IGodRepository _godRepo;

    public AdminGodsController(IGodRepository godRepo) => _godRepo = godRepo;

    [HttpGet]
    public async Task<IActionResult> GetByWorld([FromQuery] string worldId)
    {
        var gods = await _godRepo.GetByWorldAsync(worldId);
        return Ok(gods.Select(g => new
        {
            id = g.Id,
            name = g.Name,
            archetype = g.Archetype.ToString(),
            faith = g.Faith,
            trust = g.Trust,
            fear = g.Fear,
            followerCount = g.FollowerCount,
            unlockedMiracles = g.UnlockedMiracles.Select(m => m.ToString()),
            isAlive = g.IsAlive,
            isForgotten = g.IsForgotten,
            rank = g.RankData.Rank.ToString(),
            rankMultiplier = g.RankData.RankMultiplier,
            totalFaithEarned = g.RankData.TotalFaithEarned,
            relicCount = g.RelicIds.Count,
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var g = await _godRepo.GetByIdAsync(id);
        return g == null ? NotFound() : Ok(g);
    }

    [HttpPut("{id}/stats")]
    public async Task<IActionResult> UpdateStats(string id, [FromBody] UpdateGodStatsRequest req)
    {
        var god = await _godRepo.GetByIdAsync(id);
        if (god == null) return NotFound();

        if (req.Faith.HasValue) god.Faith = req.Faith.Value;
        if (req.Trust.HasValue) god.Trust = req.Trust.Value;
        if (req.Fear.HasValue) god.Fear = req.Fear.Value;
        if (req.FollowerCount.HasValue) god.FollowerCount = req.FollowerCount.Value;

        await _godRepo.UpdateAsync(god);
        return Ok(new { success = true });
    }

    [HttpPut("{id}/faith")]
    public async Task<IActionResult> UpdateFaith(string id, [FromBody] UpdateFaithRequest req)
    {
        var god = await _godRepo.GetByIdAsync(id);
        if (god == null) return NotFound();

        god.Faith = req.Faith;
        await _godRepo.UpdateAsync(god);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> UnlockMiracle(string id, [FromBody] UnlockMiracleRequest req)
    {
        if (!Enum.TryParse<MiracleType>(req.Miracle, out var miracle))
            return BadRequest(new { error = "Invalid miracle type" });

        await _godRepo.UnlockMiracleAsync(id, miracle);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/eliminate")]
    public async Task<IActionResult> Eliminate(string id)
    {
        await _godRepo.KillGodAsync(id);
        return Ok(new { success = true });
    }
}

public class UpdateGodStatsRequest
{
    public float? Faith { get; set; }
    public float? Trust { get; set; }
    public float? Fear { get; set; }
    public int? FollowerCount { get; set; }
}

public class UpdateFaithRequest
{
    public float Faith { get; set; }
}

public class UnlockMiracleRequest
{
    public string Miracle { get; set; } = string.Empty;
}
