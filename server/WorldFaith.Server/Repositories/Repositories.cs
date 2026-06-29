using MongoDB.Driver;
using WorldFaith.Server.Models;

namespace WorldFaith.Server.Repositories;

// ─── Base Repository ─────────────────────────────────────
public abstract class MongoRepository<T>
{
    protected readonly IMongoCollection<T> Collection;

    protected MongoRepository(IMongoDatabase db, string collectionName)
    {
        Collection = db.GetCollection<T>(collectionName);
    }
}

// ─── World Repository ────────────────────────────────────
public interface IWorldRepository
{
    Task<WorldDocument?> GetByIdAsync(string id);
    Task<List<WorldDocument>> GetActiveWorldsAsync();
    Task<List<WorldDocument>> GetAllAsync();
    Task<WorldDocument> CreateAsync(WorldDocument world);
    Task UpdateTickAsync(string worldId, long tick, int cycle);
    Task UpdateTilesAsync(string worldId, List<WorldTileData> tiles);
    Task DeactivateAsync(string worldId);
}

public class WorldRepository : MongoRepository<WorldDocument>, IWorldRepository
{
    public WorldRepository(IMongoDatabase db) : base(db, "worlds") { }

    public async Task<WorldDocument?> GetByIdAsync(string id)
        => await Collection.Find(w => w.Id == id).FirstOrDefaultAsync();

    public async Task<List<WorldDocument>> GetActiveWorldsAsync()
        => await Collection.Find(w => w.IsActive).ToListAsync();

    public async Task<List<WorldDocument>> GetAllAsync()
        => await Collection.Find(_ => true).SortByDescending(w => w.CreatedAt).Limit(50).ToListAsync();

    public async Task<WorldDocument> CreateAsync(WorldDocument world)
    {
        await Collection.InsertOneAsync(world);
        return world;
    }

    public async Task UpdateTickAsync(string worldId, long tick, int cycle)
    {
        var update = Builders<WorldDocument>.Update
            .Set(w => w.Tick, tick)
            .Set(w => w.Cycle, cycle);
        await Collection.UpdateOneAsync(w => w.Id == worldId, update);
    }

    public async Task UpdateTilesAsync(string worldId, List<WorldTileData> tiles)
    {
        var update = Builders<WorldDocument>.Update.Set(w => w.Tiles, tiles);
        await Collection.UpdateOneAsync(w => w.Id == worldId, update);
    }

    public async Task DeactivateAsync(string worldId)
    {
        var update = Builders<WorldDocument>.Update.Set(w => w.IsActive, false);
        await Collection.UpdateOneAsync(w => w.Id == worldId, update);
    }
}

// ─── God Repository ──────────────────────────────────────
public interface IGodRepository
{
    Task<GodDocument?> GetByIdAsync(string id);
    Task<GodDocument?> GetByPlayerAndWorldAsync(string playerId, string worldId);
    Task<List<GodDocument>> GetByWorldAsync(string worldId);
    Task<GodDocument> CreateAsync(GodDocument god);
    Task UpdateFaithAsync(string godId, float faith, float trust, float fear, int followerCount);
    Task UnlockMiracleAsync(string godId, MiracleType miracle);
    Task KillGodAsync(string godId);
}

public class GodRepository : MongoRepository<GodDocument>, IGodRepository
{
    public GodRepository(IMongoDatabase db) : base(db, "gods") { }

    public async Task<GodDocument?> GetByIdAsync(string id)
        => await Collection.Find(g => g.Id == id).FirstOrDefaultAsync();

    public async Task<GodDocument?> GetByPlayerAndWorldAsync(string playerId, string worldId)
        => await Collection.Find(g => g.PlayerId == playerId && g.WorldId == worldId).FirstOrDefaultAsync();

    public async Task<List<GodDocument>> GetByWorldAsync(string worldId)
        => await Collection.Find(g => g.WorldId == worldId && g.IsAlive).ToListAsync();

    public async Task<GodDocument> CreateAsync(GodDocument god)
    {
        await Collection.InsertOneAsync(god);
        return god;
    }

    public async Task UpdateFaithAsync(string godId, float faith, float trust, float fear, int followerCount)
    {
        var update = Builders<GodDocument>.Update
            .Set(g => g.Faith, faith)
            .Set(g => g.Trust, trust)
            .Set(g => g.Fear, fear)
            .Set(g => g.FollowerCount, followerCount)
            .Set(g => g.LastActionAt, DateTime.UtcNow);
        await Collection.UpdateOneAsync(g => g.Id == godId, update);
    }

    public async Task UnlockMiracleAsync(string godId, MiracleType miracle)
    {
        var update = Builders<GodDocument>.Update.AddToSet(g => g.UnlockedMiracles, miracle);
        await Collection.UpdateOneAsync(g => g.Id == godId, update);
    }

    public async Task KillGodAsync(string godId)
    {
        var update = Builders<GodDocument>.Update.Set(g => g.IsAlive, false);
        await Collection.UpdateOneAsync(g => g.Id == godId, update);
    }
}

// ─── Civilization Repository ─────────────────────────────
public interface ICivilizationRepository
{
    Task<List<CivilizationDocument>> GetByWorldAsync(string worldId);
    Task<CivilizationDocument?> GetByIdAsync(string id);
    Task<CivilizationDocument?> GetByCivilizationByIdAsync(string id);
    Task<CivilizationDocument> CreateAsync(CivilizationDocument civ);
    Task UpdateAsync(CivilizationDocument civ);
    Task DeleteByWorldAsync(string worldId);
}

public class CivilizationRepository : MongoRepository<CivilizationDocument>, ICivilizationRepository
{
    public CivilizationRepository(IMongoDatabase db) : base(db, "civilizations") { }

    public async Task<List<CivilizationDocument>> GetByWorldAsync(string worldId)
        => await Collection.Find(c => c.WorldId == worldId).ToListAsync();

    public async Task<CivilizationDocument?> GetByIdAsync(string id)
        => await Collection.Find(c => c.Id == id).FirstOrDefaultAsync();

    // Alias for OrganizationService / NpcInteractionService
    public async Task<CivilizationDocument?> GetByCivilizationByIdAsync(string id)
        => await Collection.Find(c => c.Id == id).FirstOrDefaultAsync();

    public async Task<CivilizationDocument> CreateAsync(CivilizationDocument civ)
    {
        await Collection.InsertOneAsync(civ);
        return civ;
    }

    public async Task UpdateAsync(CivilizationDocument civ)
        => await Collection.ReplaceOneAsync(c => c.Id == civ.Id, civ);

    public async Task DeleteByWorldAsync(string worldId)
        => await Collection.DeleteManyAsync(c => c.WorldId == worldId);
}

// ─── Religion Repository ─────────────────────────────────
public interface IReligionRepository
{
    Task<List<ReligionDocument>> GetByWorldAsync(string worldId);
    Task<List<ReligionDocument>> GetByGodAsync(string godId);
    Task<ReligionDocument?> GetByGodAsync(string worldId, string godId);
    Task<ReligionDocument?> GetByIdAsync(string id);
    Task<ReligionDocument> CreateAsync(ReligionDocument religion);
    Task UpdateAsync(ReligionDocument religion);
    Task EraseAsync(string religionId);
}

public class ReligionRepository : MongoRepository<ReligionDocument>, IReligionRepository
{
    public ReligionRepository(IMongoDatabase db) : base(db, "religions") { }

    public async Task<List<ReligionDocument>> GetByWorldAsync(string worldId)
        => await Collection.Find(r => r.WorldId == worldId).ToListAsync();

    public async Task<List<ReligionDocument>> GetByGodAsync(string godId)
        => await Collection.Find(r => r.GodId == godId).ToListAsync();

    public async Task<ReligionDocument?> GetByGodAsync(string worldId, string godId)
        => await Collection.Find(r => r.WorldId == worldId && r.GodId == godId).FirstOrDefaultAsync();

    public async Task<ReligionDocument?> GetByIdAsync(string id)
        => await Collection.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task<ReligionDocument> CreateAsync(ReligionDocument religion)
    {
        await Collection.InsertOneAsync(religion);
        return religion;
    }

    public async Task UpdateAsync(ReligionDocument religion)
        => await Collection.ReplaceOneAsync(r => r.Id == religion.Id, religion);

    public async Task EraseAsync(string religionId)
        => await Collection.DeleteOneAsync(r => r.Id == religionId);
}

// ─── Miracle Event Repository ────────────────────────────
public interface IMiracleEventRepository
{
    Task<MiracleEventDocument> LogAsync(MiracleEventDocument evt);
    Task<List<MiracleEventDocument>> GetRecentByWorldAsync(string worldId, int limit = 50);
}

public class MiracleEventRepository : MongoRepository<MiracleEventDocument>, IMiracleEventRepository
{
    public MiracleEventRepository(IMongoDatabase db) : base(db, "miracle_events") { }

    public async Task<MiracleEventDocument> LogAsync(MiracleEventDocument evt)
    {
        await Collection.InsertOneAsync(evt);
        return evt;
    }

    public async Task<List<MiracleEventDocument>> GetRecentByWorldAsync(string worldId, int limit = 50)
        => await Collection.Find(e => e.WorldId == worldId)
            .SortByDescending(e => e.OccurredAt)
            .Limit(limit)
            .ToListAsync();
}

// ─── NPC Repository (v3) ─────────────────────────────────
public interface INpcRepository
{
    Task<NpcDocument> CreateAsync(NpcDocument npc);
    Task<List<NpcDocument>> GetByWorldAsync(string worldId);
    Task<List<NpcDocument>> GetByCivilizationAsync(string civId);
    Task<List<NpcDocument>> GetByTierAsync(string worldId, NpcTier tier);
    Task<NpcDocument?> GetByIdAsync(string npcId);
    Task UpdateAsync(NpcDocument npc);
    Task<List<NpcDocument>> GetChampionsAsync(string worldId);
    Task DeleteByWorldAsync(string worldId);
}

public class NpcRepository : MongoRepository<NpcDocument>, INpcRepository
{
    public NpcRepository(IMongoDatabase db) : base(db, "npcs") { }

    public async Task<List<NpcDocument>> GetByWorldAsync(string worldId)
        => await Collection.Find(n => n.WorldId == worldId && n.State == NpcState.Alive).ToListAsync();

    public async Task<List<NpcDocument>> GetByCivilizationAsync(string civId)
        => await Collection.Find(n => n.CivilizationId == civId && n.State == NpcState.Alive).ToListAsync();

    public async Task<List<NpcDocument>> GetByTierAsync(string worldId, NpcTier tier)
        => await Collection.Find(n => n.WorldId == worldId && n.Tier == tier && n.State == NpcState.Alive).ToListAsync();

    public async Task<NpcDocument?> GetByIdAsync(string npcId)
        => await Collection.Find(n => n.Id == npcId).FirstOrDefaultAsync();

    public async Task UpdateAsync(NpcDocument npc)
        => await Collection.ReplaceOneAsync(n => n.Id == npc.Id, npc);

    public async Task<List<NpcDocument>> GetChampionsAsync(string worldId)
        => await Collection.Find(n => n.WorldId == worldId && n.IsChampion).ToListAsync();

    public async Task DeleteByWorldAsync(string worldId)
        => await Collection.DeleteManyAsync(n => n.WorldId == worldId);
}

// ─── Organization Repository (v3) ────────────────────────
public interface IOrganizationRepository
{
    Task<OrganizationDocument> CreateAsync(OrganizationDocument org);
    Task<List<OrganizationDocument>> GetByWorldAsync(string worldId);
    Task<List<OrganizationDocument>> GetByCivilizationAsync(string civId);
    Task<List<OrganizationDocument>> GetByTypeAsync(string worldId, OrganizationType type);
    Task<OrganizationDocument?> GetByIdAsync(string orgId);
    Task UpdateAsync(OrganizationDocument org);
}

public class OrganizationRepository : MongoRepository<OrganizationDocument>, IOrganizationRepository
{
    public OrganizationRepository(IMongoDatabase db) : base(db, "organizations") { }

    public async Task<List<OrganizationDocument>> GetByWorldAsync(string worldId)
        => await Collection.Find(o => o.WorldId == worldId).ToListAsync();

    public async Task<List<OrganizationDocument>> GetByCivilizationAsync(string civId)
        => await Collection.Find(o => o.CivilizationId == civId).ToListAsync();

    public async Task<List<OrganizationDocument>> GetByTypeAsync(string worldId, OrganizationType type)
        => await Collection.Find(o => o.WorldId == worldId && o.Type == type).ToListAsync();

    public async Task<OrganizationDocument?> GetByIdAsync(string orgId)
        => await Collection.Find(o => o.Id == orgId).FirstOrDefaultAsync();

    public async Task UpdateAsync(OrganizationDocument org)
        => await Collection.ReplaceOneAsync(o => o.Id == org.Id, org);
}

// ─── NPC Event Repository (v3) ───────────────────────────
public interface INpcEventRepository
{
    Task<NpcEventDocument> LogAsync(NpcEventDocument evt);
    Task<List<NpcEventDocument>> GetRecentAsync(string worldId, int limit = 50);
    Task<List<NpcEventDocument>> GetByCivilizationAsync(string civId, int limit = 20);
}

public class NpcEventRepository : MongoRepository<NpcEventDocument>, INpcEventRepository
{
    public NpcEventRepository(IMongoDatabase db) : base(db, "npc_events") { }

    public async Task<NpcEventDocument> LogAsync(NpcEventDocument evt)
    {
        await Collection.InsertOneAsync(evt);
        return evt;
    }

    public async Task<List<NpcEventDocument>> GetRecentAsync(string worldId, int limit = 50)
        => await Collection.Find(e => e.WorldId == worldId)
            .SortByDescending(e => e.OccurredAt).Limit(limit).ToListAsync();

    public async Task<List<NpcEventDocument>> GetByCivilizationAsync(string civId, int limit = 20)
        => await Collection.Find(e => e.CivilizationId == civId)
            .SortByDescending(e => e.OccurredAt).Limit(limit).ToListAsync();
}
