using WorldFaith.Server.Models;
using Microsoft.AspNetCore.Mvc;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Achievement;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Controllers.Admin;

[Route("api/admin/npcs")]
public class AdminNpcsController : AdminBaseController
{
    private readonly INpcRepository _npcRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IAchievementService _achievementService;
    private readonly INpcSpawnService _npcSpawnService;

    public AdminNpcsController(
        INpcRepository npcRepo,
        ICivilizationRepository civRepo,
        IAchievementService achievementService,
        INpcSpawnService npcSpawnService)
    {
        _npcRepo = npcRepo;
        _civRepo = civRepo;
        _achievementService = achievementService;
        _npcSpawnService = npcSpawnService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByWorld([FromQuery] string worldId, [FromQuery] string? tier = null)
    {
        var npcs = await _npcRepo.GetByWorldAsync(worldId);

        if (!string.IsNullOrEmpty(tier) && Enum.TryParse<NpcTier>(tier, out var tierEnum))
            npcs = npcs.Where(n => n.Tier == tierEnum).ToList();

        return Ok(npcs.Select(n => new
        {
            id = n.Id,
            name = n.Name,
            tier = n.Tier.ToString(),
            personality = n.Personality.ToString(),
            state = n.State.ToString(),
            loyalty = n.Loyalty,
            ambition = n.Ambition,
            piety = n.Piety,
            wealth = n.Wealth,
            civilizationId = n.CivilizationId,
            organizationId = n.OrganizationId,
            personalReligionId = n.PersonalReligionId,
            devotionLevel = n.DevotionLevel,
            godInfluenceId = n.GodInfluenceId,
            godTrustLevel = n.GodTrustLevel,
            isChampion = n.IsChampion,
            championPath = n.ChampionPath?.ToString(),
            evolutionPoints = n.EvolutionPoints,
            churchRank = n.DivineProfile.ChurchRank.ToString(),
            divineAttentionScore = n.DivineProfile.DivineAttentionScore,
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var npc = await _npcRepo.GetByIdAsync(id);
        return npc == null ? NotFound() : Ok(npc);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateNpcRequest req)
    {
        var npc = await _npcRepo.GetByIdAsync(id);
        if (npc == null) return NotFound();

        if (req.Loyalty.HasValue) npc.Loyalty = req.Loyalty.Value;
        if (req.Ambition.HasValue) npc.Ambition = req.Ambition.Value;
        if (req.Piety.HasValue) npc.Piety = req.Piety.Value;
        if (req.Wealth.HasValue) npc.Wealth = req.Wealth.Value;
        if (req.GodTrustLevel.HasValue) npc.GodTrustLevel = req.GodTrustLevel.Value;

        await _npcRepo.UpdateAsync(npc);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/exile")]
    public async Task<IActionResult> Exile(string id)
    {
        var npc = await _npcRepo.GetByIdAsync(id);
        if (npc == null) return NotFound();

        npc.State = NpcState.Exiled;
        await _npcRepo.UpdateAsync(npc);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/kill")]
    public async Task<IActionResult> Kill(string id)
    {
        var npc = await _npcRepo.GetByIdAsync(id);
        if (npc == null) return NotFound();

        npc.State = NpcState.Dead;
        await _npcRepo.UpdateAsync(npc);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/champion")]
    public async Task<IActionResult> PromoteChampion(string id, [FromBody] PromoteChampionRequest req)
    {
        var npc = await _npcRepo.GetByIdAsync(id);
        if (npc == null) return NotFound();

        var promoted = await _npcSpawnService.PromoteToChampionAsync(npc.WorldId, id, req.GodId);

        return promoted == null
            ? BadRequest(new { error = "NPC must be an Adventurer to become a Champion" })
            : Ok(promoted);
    }

    [HttpPost("spawn")]
    public async Task<IActionResult> SpawnForCiv([FromBody] SpawnNpcsRequest req)
    {
        var civ = await _civRepo.GetByIdAsync(req.CivId);
        if (civ == null) return NotFound(new { error = "Civilization not found" });

        await _npcSpawnService.SpawnForCivilizationAsync(req.WorldId, civ);
        return Ok(new { success = true });
    }

    [HttpGet("{id}/achievements")]
    public async Task<IActionResult> GetAchievements(string id)
    {
        var npc = await _npcRepo.GetByIdAsync(id);
        if (npc == null) return NotFound();

        return Ok(new
        {
            achievements = npc.DivineProfile.Achievements,
            talents = npc.DivineProfile.Talents,
            divineAttentionScore = npc.DivineProfile.DivineAttentionScore,
            churchRank = npc.DivineProfile.ChurchRank.ToString(),
        });
    }

    [HttpPost("{id}/achievements/earn")]
    public async Task<IActionResult> EarnAchievement(string id, [FromBody] EarnAchievementRequest req)
    {
        try
        {
            var achievement = await _achievementService.EarnAchievementAsync(id, req.AchievementKey, tick: 0);
            return Ok(achievement);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/talents/awaken")]
    public async Task<IActionResult> AwakenTalent(string id, [FromBody] AwakenTalentRequest req)
    {
        await _achievementService.AwakentTalentAsync(id, req.TalentName, "Admin action");
        return Ok(new { success = true });
    }
}

public class UpdateNpcRequest
{
    public float? Loyalty { get; set; }
    public float? Ambition { get; set; }
    public float? Piety { get; set; }
    public float? Wealth { get; set; }
    public float? GodTrustLevel { get; set; }
}

public class EarnAchievementRequest
{
    public string AchievementKey { get; set; } = string.Empty;
}

public class AwakenTalentRequest
{
    public string TalentName { get; set; } = string.Empty;
}

public class PromoteChampionRequest
{
    public string GodId { get; set; } = string.Empty;
}

public class SpawnNpcsRequest
{
    public string WorldId { get; set; } = string.Empty;
    public string CivId { get; set; } = string.Empty;
}
