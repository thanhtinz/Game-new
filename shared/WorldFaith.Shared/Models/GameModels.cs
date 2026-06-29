using WorldFaith.Shared.Enums;

namespace WorldFaith.Shared.Models;

// ─── God ───────────────────────────────────────────────
public class GodDto
{
    public string Id { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GodArchetype Archetype { get; set; }
    public float Faith { get; set; }
    public float Trust { get; set; }
    public float Fear { get; set; }
    public int FollowerCount { get; set; }
    public List<string> UnlockedMiracles { get; set; } = new();
    public bool IsAlive { get; set; } = true;
    public long LastActionAt { get; set; }
}

// ─── World Tile ─────────────────────────────────────────
public class WorldTileDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public TileType Type { get; set; }
    public float Fertility { get; set; }
    public string? CivilizationId { get; set; }
    public string? ReligionId { get; set; }
    public bool HasTemple { get; set; }
    public int Population { get; set; }
}

// ─── Civilization ───────────────────────────────────────
public class CivilizationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CivilizationPersonality Personality { get; set; }
    public int Population { get; set; }
    public float Economy { get; set; }
    public float Military { get; set; }
    public string? RulingReligionId { get; set; }
    public List<string> ReligionIds { get; set; } = new();
    public List<int[]> ControlledTiles { get; set; } = new();
    public bool IsAtWar { get; set; }
    public CivilizationState State { get; set; }
}

public enum CivilizationState
{
    Tribal,
    Kingdom,
    Empire,
    Collapsing,
    Fallen
}

// ─── Religion ───────────────────────────────────────────
public class ReligionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public int FollowerCount { get; set; }
    public int TempleCount { get; set; }
    public float DevotionLevel { get; set; }
    public bool IsHidden { get; set; }
    public List<string> CivilizationIds { get; set; } = new();
    public List<string> SchismIds { get; set; } = new();
}

// ─── Evolution Entity ───────────────────────────────────
public class EvolutionEntityDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EvolutionStage Stage { get; set; }
    public string? GodInfluenceId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public float Power { get; set; }
}

// ─── World State (snapshot gửi cho client) ──────────────
public class WorldStateDto
{
    public string WorldId { get; set; } = string.Empty;
    public int Cycle { get; set; }
    public long Tick { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<GodDto> Gods { get; set; } = new();
    public List<CivilizationDto> Civilizations { get; set; } = new();
    public List<ReligionDto> Religions { get; set; } = new();
    public List<EvolutionEntityDto> Entities { get; set; } = new();
    public List<WorldTileDto> ChangedTiles { get; set; } = new();
}
