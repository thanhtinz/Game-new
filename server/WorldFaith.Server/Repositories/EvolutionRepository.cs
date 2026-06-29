using MongoDB.Driver;
using WorldFaith.Server.Models;

namespace WorldFaith.Server.Repositories;

public interface IEvolutionEntityRepository
{
    Task<EvolutionEntityDocument?> GetByIdAsync(string id);
    Task<List<EvolutionEntityDocument>> GetByWorldAsync(string worldId);
    Task<List<EvolutionEntityDocument>> GetByGodInfluenceAsync(string godId);
    Task<EvolutionEntityDocument> CreateAsync(EvolutionEntityDocument entity);
    Task UpdateAsync(EvolutionEntityDocument entity);
    Task DeleteAsync(string entityId);
    Task DeleteByWorldAsync(string worldId);
}

public class EvolutionEntityRepository : IEvolutionEntityRepository
{
    private readonly IMongoCollection<EvolutionEntityDocument> _collection;

    public EvolutionEntityRepository(IMongoDatabase db)
    {
        _collection = db.GetCollection<EvolutionEntityDocument>("evolution_entities");
    }

    public async Task<EvolutionEntityDocument?> GetByIdAsync(string id)
        => await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();

    public async Task<List<EvolutionEntityDocument>> GetByWorldAsync(string worldId)
        => await _collection.Find(e => e.WorldId == worldId).ToListAsync();

    public async Task<List<EvolutionEntityDocument>> GetByGodInfluenceAsync(string godId)
        => await _collection.Find(e => e.GodInfluenceId == godId).ToListAsync();

    public async Task<EvolutionEntityDocument> CreateAsync(EvolutionEntityDocument entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(EvolutionEntityDocument entity)
        => await _collection.ReplaceOneAsync(e => e.Id == entity.Id, entity);

    public async Task DeleteAsync(string entityId)
        => await _collection.DeleteOneAsync(e => e.Id == entityId);

    public async Task DeleteByWorldAsync(string worldId)
        => await _collection.DeleteManyAsync(e => e.WorldId == worldId);
}
