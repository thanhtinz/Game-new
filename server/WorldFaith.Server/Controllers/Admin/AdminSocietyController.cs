using Microsoft.AspNetCore.Mvc;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Controllers.Admin;

// ─── Civilizations ────────────────────────────────────────
[Route("api/admin/civs")]
public class AdminCivsController : AdminBaseController
{
    private readonly ICivilizationRepository _civRepo;

    public AdminCivsController(ICivilizationRepository civRepo) => _civRepo = civRepo;

    [HttpGet]
    public async Task<IActionResult> GetByWorld([FromQuery] string worldId)
    {
        var civs = await _civRepo.GetByWorldAsync(worldId);
        return Ok(civs.Select(c => new
        {
            id = c.Id,
            name = c.Name,
            personality = c.Personality.ToString(),
            primaryRace = c.PrimaryRace.ToString(),
            government = c.Government.ToString(),
            population = c.Population,
            economy = c.Economy,
            military = c.Military,
            food = c.Food,
            stability = c.Stability,
            corruption = c.Corruption,
            religiousUnity = c.ReligiousUnity,
            happiness = c.Happiness,
            isAtWar = c.IsAtWar,
            state = c.State.ToString(),
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var civ = await _civRepo.GetByIdAsync(id);
        return civ == null ? NotFound() : Ok(civ);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCivRequest req)
    {
        var civ = await _civRepo.GetByIdAsync(id);
        if (civ == null) return NotFound();

        if (req.Population.HasValue) civ.Population = req.Population.Value;
        if (req.Economy.HasValue) civ.Economy = req.Economy.Value;
        if (req.Military.HasValue) civ.Military = req.Military.Value;
        if (req.Food.HasValue) civ.Food = req.Food.Value;
        if (req.Stability.HasValue) civ.Stability = req.Stability.Value;
        if (req.Corruption.HasValue) civ.Corruption = req.Corruption.Value;
        if (req.ReligiousUnity.HasValue) civ.ReligiousUnity = req.ReligiousUnity.Value;
        if (req.Happiness.HasValue) civ.Happiness = req.Happiness.Value;
        if (req.Government != null && Enum.TryParse<GovernmentType>(req.Government, out var gov)) civ.Government = gov;
        if (req.Personality != null && Enum.TryParse<CivilizationPersonality>(req.Personality, out var pers)) civ.Personality = pers;
        if (req.State != null && Enum.TryParse<CivilizationState>(req.State, out var state)) civ.State = state;
        if (req.IsAtWar.HasValue) civ.IsAtWar = req.IsAtWar.Value;

        await _civRepo.UpdateAsync(civ);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/boost")]
    public async Task<IActionResult> Boost(string id, [FromBody] BoostRequest req)
    {
        var civ = await _civRepo.GetByIdAsync(id);
        if (civ == null) return NotFound();

        switch (req.Stat.ToLower())
        {
            case "economy":  civ.Economy  = Math.Min(200f, civ.Economy + req.Amount); break;
            case "military": civ.Military = Math.Min(200f, civ.Military + req.Amount); break;
            case "food":     civ.Food     = Math.Min(200f, civ.Food + req.Amount); break;
            default: return BadRequest(new { error = "Unknown stat" });
        }

        await _civRepo.UpdateAsync(civ);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/collapse")]
    public async Task<IActionResult> Collapse(string id)
    {
        var civ = await _civRepo.GetByIdAsync(id);
        if (civ == null) return NotFound();

        civ.State = CivilizationState.Collapsing;
        civ.Stability = 0f;
        await _civRepo.UpdateAsync(civ);
        return Ok(new { success = true });
    }
}

public class UpdateCivRequest
{
    public int? Population { get; set; }
    public float? Economy { get; set; }
    public float? Military { get; set; }
    public float? Food { get; set; }
    public float? Stability { get; set; }
    public float? Corruption { get; set; }
    public float? ReligiousUnity { get; set; }
    public float? Happiness { get; set; }
    public string? Government { get; set; }
    public string? Personality { get; set; }
    public string? State { get; set; }
    public bool? IsAtWar { get; set; }
}

public class BoostRequest
{
    public string Stat { get; set; } = string.Empty;
    public float Amount { get; set; } = 30f;
}

// ─── Religions ────────────────────────────────────────────
[Route("api/admin/religions")]
public class AdminReligionsController : AdminBaseController
{
    private readonly IReligionRepository _religionRepo;

    public AdminReligionsController(IReligionRepository religionRepo) => _religionRepo = religionRepo;

    [HttpGet]
    public async Task<IActionResult> GetByWorld([FromQuery] string worldId)
    {
        var religions = await _religionRepo.GetByWorldAsync(worldId);
        return Ok(religions.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            godId = r.GodId,
            followerCount = r.FollowerCount,
            templeCount = r.TempleCount,
            devotionLevel = r.DevotionLevel,
            isHidden = r.IsHidden,
            doctrine = r.Doctrine,
            casualCount = r.CasualCount,
            devoutCount = r.DevoutCount,
            fanaticCount = r.FanaticCount,
            cultistCount = r.CultistCount,
            hereticCount = r.HereticCount,
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var religion = await _religionRepo.GetByIdAsync(id);
        return religion == null ? NotFound() : Ok(religion);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateReligionRequest req)
    {
        var religion = await _religionRepo.GetByIdAsync(id);
        if (religion == null) return NotFound();

        if (req.Name != null) religion.Name = req.Name;
        if (req.FollowerCount.HasValue) religion.FollowerCount = req.FollowerCount.Value;
        if (req.TempleCount.HasValue) religion.TempleCount = req.TempleCount.Value;
        if (req.DevotionLevel.HasValue) religion.DevotionLevel = req.DevotionLevel.Value;
        if (req.IsHidden.HasValue) religion.IsHidden = req.IsHidden.Value;
        if (req.Doctrine != null) religion.Doctrine = req.Doctrine;

        await _religionRepo.UpdateAsync(religion);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/schism")]
    public async Task<IActionResult> ForceSchism(string id)
    {
        var religion = await _religionRepo.GetByIdAsync(id);
        if (religion == null) return NotFound();

        // Split roughly 1/3 of followers into a new schismatic religion
        int splitCount = religion.FollowerCount / 3;
        if (splitCount < 1) return BadRequest(new { error = "Not enough followers to schism" });

        var schism = new ReligionDocument
        {
            WorldId = religion.WorldId,
            Name = $"{religion.Name} Reformed",
            GodId = religion.GodId,
            FollowerCount = splitCount,
            DevotionLevel = religion.DevotionLevel * 0.8f,
            Doctrine = religion.Doctrine,
            CasualCount = religion.CasualCount / 3,
            DevoutCount = religion.DevoutCount / 3,
        };
        await _religionRepo.CreateAsync(schism);

        religion.FollowerCount -= splitCount;
        religion.SchismIds.Add(schism.Id);
        await _religionRepo.UpdateAsync(religion);

        return Ok(new { success = true, schismId = schism.Id });
    }

    [HttpPost("{id}/erase")]
    public async Task<IActionResult> Erase(string id)
    {
        await _religionRepo.EraseAsync(id);
        return Ok(new { success = true });
    }
}

public class UpdateReligionRequest
{
    public string? Name { get; set; }
    public int? FollowerCount { get; set; }
    public int? TempleCount { get; set; }
    public float? DevotionLevel { get; set; }
    public bool? IsHidden { get; set; }
    public DoctrineValues? Doctrine { get; set; }
}

// ─── Organizations ────────────────────────────────────────
[Route("api/admin/organizations")]
public class AdminOrganizationsController : AdminBaseController
{
    private readonly IOrganizationRepository _orgRepo;

    public AdminOrganizationsController(IOrganizationRepository orgRepo) => _orgRepo = orgRepo;

    [HttpGet]
    public async Task<IActionResult> GetByWorld([FromQuery] string worldId)
    {
        var orgs = await _orgRepo.GetByWorldAsync(worldId);
        return Ok(orgs.Select(o => new
        {
            id = o.Id,
            name = o.Name,
            type = o.Type.ToString(),
            civilizationId = o.CivilizationId,
            leaderNpcId = o.LeaderNpcId,
            memberCount = o.Members.Count,
            power = o.Power,
            wealth = o.Wealth,
            loyalty = o.Loyalty,
            isHidden = o.IsHidden,
            heatLevel = o.HeatLevel,
            godInfluenceId = o.GodInfluenceId,
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var org = await _orgRepo.GetByIdAsync(id);
        return org == null ? NotFound() : Ok(org);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOrgRequest req)
    {
        var org = await _orgRepo.GetByIdAsync(id);
        if (org == null) return NotFound();

        if (req.Power.HasValue) org.Power = req.Power.Value;
        if (req.Wealth.HasValue) org.Wealth = req.Wealth.Value;
        if (req.Loyalty.HasValue) org.Loyalty = req.Loyalty.Value;
        if (req.HeatLevel.HasValue) org.HeatLevel = req.HeatLevel.Value;

        await _orgRepo.UpdateAsync(org);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/expose")]
    public async Task<IActionResult> Expose(string id)
    {
        var org = await _orgRepo.GetByIdAsync(id);
        if (org == null) return NotFound();

        org.IsHidden = false;
        org.HeatLevel = 100f;
        await _orgRepo.UpdateAsync(org);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/disband")]
    public async Task<IActionResult> Disband(string id)
    {
        var org = await _orgRepo.GetByIdAsync(id);
        if (org == null) return NotFound();

        org.Members.Clear();
        org.Power = 0f;
        await _orgRepo.UpdateAsync(org);
        return Ok(new { success = true });
    }
}

public class UpdateOrgRequest
{
    public float? Power { get; set; }
    public float? Wealth { get; set; }
    public float? Loyalty { get; set; }
    public float? HeatLevel { get; set; }
}
