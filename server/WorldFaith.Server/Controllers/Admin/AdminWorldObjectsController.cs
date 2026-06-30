using Microsoft.AspNetCore.Mvc;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Dungeon;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Controllers.Admin;

// ─── Dungeons ─────────────────────────────────────────────
[Route("api/admin/dungeons")]
public class AdminDungeonsController : AdminBaseController
{
    private readonly IDungeonRepository _dungeonRepo;
    private readonly IDungeonService _dungeonService;

    public AdminDungeonsController(IDungeonRepository dungeonRepo, IDungeonService dungeonService)
    {
        _dungeonRepo = dungeonRepo;
        _dungeonService = dungeonService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByWorld([FromQuery] string worldId)
    {
        var dungeons = await _dungeonRepo.GetByWorldAsync(worldId);
        return Ok(dungeons.Select(d => new
        {
            id = d.Id,
            type = d.Type.ToString(),
            state = d.State.ToString(),
            x = d.X,
            y = d.Y,
            dangerLevel = d.DangerLevel,
            reward = d.Reward,
            originGodId = d.OriginGodId,
            activeMissionId = d.ActiveMissionId,
            relicId = d.RelicId,
        }));
    }

    [HttpPost("spawn")]
    public async Task<IActionResult> Spawn([FromBody] SpawnDungeonRequest req)
    {
        if (!Enum.TryParse<DungeonType>(req.Type, out var type))
            return BadRequest(new { error = "Invalid dungeon type" });

        var dungeon = await _dungeonService.SpawnDungeonAsync(req.WorldId, req.X, req.Y, type, req.GodId);
        return Ok(dungeon);
    }

    [HttpPost("{id}/seal")]
    public async Task<IActionResult> Seal(string id)
    {
        var dungeon = await _dungeonRepo.GetByIdAsync(id);
        if (dungeon == null) return NotFound();

        dungeon.State = DungeonState.Sealed;
        await _dungeonRepo.UpdateAsync(dungeon);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/clear")]
    public async Task<IActionResult> Clear(string id)
    {
        var dungeon = await _dungeonRepo.GetByIdAsync(id);
        if (dungeon == null) return NotFound();

        dungeon.State = DungeonState.Cleared;
        await _dungeonRepo.UpdateAsync(dungeon);
        return Ok(new { success = true });
    }
}

public class SpawnDungeonRequest
{
    public string WorldId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public string? GodId { get; set; }
}

// ─── Relics ───────────────────────────────────────────────
[Route("api/admin/relics")]
public class AdminRelicsController : AdminBaseController
{
    private readonly IRelicRepository _relicRepo;

    public AdminRelicsController(IRelicRepository relicRepo) => _relicRepo = relicRepo;

    [HttpGet]
    public async Task<IActionResult> GetByWorld([FromQuery] string worldId)
    {
        var relics = await _relicRepo.GetByWorldAsync(worldId);
        return Ok(relics.Select(r => new
        {
            id = r.Id,
            type = r.Type.ToString(),
            name = r.Name,
            originGodId = r.OriginGodId,
            currentOwnerId = r.CurrentOwnerId,
            locationDungeonId = r.LocationDungeonId,
            locationCivId = r.LocationCivId,
            memoryPower = r.MemoryPower,
            faithBonus = r.FaithBonus,
            isActive = r.IsActive,
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var relic = await _relicRepo.GetByIdAsync(id);
        return relic == null ? NotFound() : Ok(relic);
    }

    [HttpPost("{id}/transfer")]
    public async Task<IActionResult> Transfer(string id, [FromBody] TransferRelicRequest req)
    {
        var relic = await _relicRepo.GetByIdAsync(id);
        if (relic == null) return NotFound();

        relic.CurrentOwnerId = string.IsNullOrWhiteSpace(req.OwnerNpcId) ? null : req.OwnerNpcId;
        relic.LocationCivId = string.IsNullOrWhiteSpace(req.CivId) ? null : req.CivId;
        // Manual transfer clears dungeon location (relic has been claimed)
        relic.LocationDungeonId = null;

        await _relicRepo.UpdateAsync(relic);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/destroy")]
    public async Task<IActionResult> Destroy(string id)
    {
        var relic = await _relicRepo.GetByIdAsync(id);
        if (relic == null) return NotFound();

        relic.IsActive = false;
        await _relicRepo.UpdateAsync(relic);
        return Ok(new { success = true });
    }
}

public class TransferRelicRequest
{
    public string? OwnerNpcId { get; set; }
    public string? CivId { get; set; }
}

// ─── Evolution Entities / Mobs ─────────────────────────────
[Route("api/admin/entities")]
public class AdminEntitiesController : AdminBaseController
{
    private readonly IEvolutionEntityRepository _entityRepo;

    public AdminEntitiesController(IEvolutionEntityRepository entityRepo) => _entityRepo = entityRepo;

    [HttpGet]
    public async Task<IActionResult> GetByWorld([FromQuery] string worldId)
    {
        var entities = await _entityRepo.GetByWorldAsync(worldId);
        return Ok(entities.Select(e => new
        {
            id = e.Id,
            name = e.Name,
            stage = e.Stage.ToString(),
            x = e.X,
            y = e.Y,
            power = e.Power,
            evolutionPoints = e.EvolutionPoints,
            godInfluenceId = e.GodInfluenceId,
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var entity = await _entityRepo.GetByIdAsync(id);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateEntityRequest req)
    {
        var entity = await _entityRepo.GetByIdAsync(id);
        if (entity == null) return NotFound();

        if (req.X.HasValue) entity.X = req.X.Value;
        if (req.Y.HasValue) entity.Y = req.Y.Value;
        if (req.Power.HasValue) entity.Power = req.Power.Value;
        if (req.EvolutionPoints.HasValue) entity.EvolutionPoints = req.EvolutionPoints.Value;

        await _entityRepo.UpdateAsync(entity);
        return Ok(new { success = true });
    }

    [HttpPost("spawn")]
    public async Task<IActionResult> Spawn([FromBody] SpawnEntityRequest req)
    {
        if (!Enum.TryParse<EvolutionStage>(req.Stage, out var stage))
            return BadRequest(new { error = "Invalid evolution stage" });

        var entity = await _entityRepo.CreateAsync(new EvolutionEntityDocument
        {
            WorldId = req.WorldId,
            Name = req.Name ?? $"{stage}",
            Stage = stage,
            X = req.X,
            Y = req.Y,
            Power = req.Power ?? 10f,
        });
        return Ok(entity);
    }

    [HttpPost("{id}/evolve")]
    public async Task<IActionResult> Evolve(string id, [FromBody] EvolveEntityToRequest req)
    {
        var entity = await _entityRepo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        if (!Enum.TryParse<EvolutionStage>(req.TargetStage, out var stage))
            return BadRequest(new { error = "Invalid evolution stage" });

        entity.Stage = stage;
        await _entityRepo.UpdateAsync(entity);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/kill")]
    public async Task<IActionResult> Kill(string id)
    {
        await _entityRepo.DeleteAsync(id);
        return Ok(new { success = true });
    }
}

public class UpdateEntityRequest
{
    public int? X { get; set; }
    public int? Y { get; set; }
    public float? Power { get; set; }
    public int? EvolutionPoints { get; set; }
}

public class SpawnEntityRequest
{
    public string WorldId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public float? Power { get; set; }
}

public class EvolveEntityToRequest
{
    public string TargetStage { get; set; } = string.Empty;
}
