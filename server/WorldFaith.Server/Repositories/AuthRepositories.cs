using MongoDB.Driver;
using WorldFaith.Server.Models.Auth;

namespace WorldFaith.Server.Repositories;

// ─── Player Repository ────────────────────────────────────
public interface IPlayerRepository
{
    Task<PlayerDocument?> GetByIdAsync(string id);
    Task<PlayerDocument?> GetByEmailAsync(string email);
    Task<PlayerDocument?> GetByUsernameAsync(string username);
    Task<List<PlayerDocument>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<List<PlayerDocument>> GetAllAsync();
    Task<PlayerDocument> CreateAsync(PlayerDocument player);
    Task UpdateLastLoginAsync(string playerId);
    Task AddRefreshTokenAsync(string playerId, RefreshTokenEntry token);
    Task RevokeRefreshTokenAsync(string playerId, string token);
    Task RevokeAllRefreshTokensAsync(string playerId);
    Task<PlayerDocument?> GetByRefreshTokenAsync(string token);
    Task UpdateStatsAsync(string playerId, bool won);
    Task SetActiveAsync(string playerId, bool isActive);
    Task SetAdminAsync(string playerId, bool isAdmin);
}

public class PlayerRepository : IPlayerRepository
{
    private readonly IMongoCollection<PlayerDocument> _collection;

    public PlayerRepository(IMongoDatabase db)
    {
        _collection = db.GetCollection<PlayerDocument>("players");

        // Indexes
        var indexKeys = Builders<PlayerDocument>.IndexKeys;
        _collection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<PlayerDocument>(indexKeys.Ascending(p => p.Email),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<PlayerDocument>(indexKeys.Ascending(p => p.Username),
                new CreateIndexOptions { Unique = true }),
        });
    }

    public async Task<PlayerDocument?> GetByIdAsync(string id)
        => await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<PlayerDocument?> GetByEmailAsync(string email)
        => await _collection.Find(p => p.Email == email.ToLower()).FirstOrDefaultAsync();

    public async Task<PlayerDocument?> GetByUsernameAsync(string username)
        => await _collection.Find(p => p.Username == username.ToLower()).FirstOrDefaultAsync();

    public async Task<PlayerDocument> CreateAsync(PlayerDocument player)
    {
        player.Email = player.Email.ToLower();
        player.Username = player.Username.ToLower();
        await _collection.InsertOneAsync(player);
        return player;
    }

    public async Task UpdateLastLoginAsync(string playerId)
    {
        var update = Builders<PlayerDocument>.Update.Set(p => p.LastLoginAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(p => p.Id == playerId, update);
    }

    public async Task AddRefreshTokenAsync(string playerId, RefreshTokenEntry token)
    {
        // Delete tokens hết hạn trước when thêm mới
        var pullExpired = Builders<PlayerDocument>.Update.PullFilter(
            p => p.RefreshTokens,
            t => t.ExpiresAt < DateTime.UtcNow || t.IsRevoked);
        await _collection.UpdateOneAsync(p => p.Id == playerId, pullExpired);

        var push = Builders<PlayerDocument>.Update.Push(p => p.RefreshTokens, token);
        await _collection.UpdateOneAsync(p => p.Id == playerId, push);
    }

    public async Task RevokeRefreshTokenAsync(string playerId, string token)
    {
        var filter = Builders<PlayerDocument>.Filter.And(
            Builders<PlayerDocument>.Filter.Eq(p => p.Id, playerId),
            Builders<PlayerDocument>.Filter.ElemMatch(p => p.RefreshTokens, t => t.Token == token));

        var update = Builders<PlayerDocument>.Update.Set(
            "RefreshTokens.$.IsRevoked", true);
        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task RevokeAllRefreshTokensAsync(string playerId)
    {
        var update = Builders<PlayerDocument>.Update.Set(p => p.RefreshTokens, new List<RefreshTokenEntry>());
        await _collection.UpdateOneAsync(p => p.Id == playerId, update);
    }

    public async Task<PlayerDocument?> GetByRefreshTokenAsync(string token)
        => await _collection.Find(p =>
            p.RefreshTokens.Any(t => t.Token == token && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync();

    public async Task UpdateStatsAsync(string playerId, bool won)
    {
        var update = Builders<PlayerDocument>.Update
            .Inc(p => p.TotalGames, 1)
            .Inc(p => p.TotalWins, won ? 1 : 0)
            .Inc(p => p.Experience, won ? 100 : 20);
        await _collection.UpdateOneAsync(p => p.Id == playerId, update);
    }

    public async Task<List<PlayerDocument>> GetAllAsync()
        => await _collection.Find(_ => true).ToListAsync();

    public async Task<List<PlayerDocument>> GetAllAsync(int page, int pageSize, string? search = null)
    {
        var filter = search != null
            ? Builders<PlayerDocument>.Filter.Or(
                Builders<PlayerDocument>.Filter.Regex(p => p.Username, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                Builders<PlayerDocument>.Filter.Regex(p => p.Email, new MongoDB.Bson.BsonRegularExpression(search, "i")))
            : Builders<PlayerDocument>.Filter.Empty;

        return await _collection.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task SetActiveAsync(string playerId, bool isActive)
    {
        var update = Builders<PlayerDocument>.Update.Set(p => p.IsActive, isActive);
        await _collection.UpdateOneAsync(p => p.Id == playerId, update);
    }

    public async Task SetAdminAsync(string playerId, bool isAdmin)
    {
        var update = Builders<PlayerDocument>.Update.Set(p => p.IsAdmin, isAdmin);
        await _collection.UpdateOneAsync(p => p.Id == playerId, update);
    }
}

// ─── Room Repository ──────────────────────────────────────
public interface IRoomRepository
{
    Task<RoomDocument?> GetByIdAsync(string id);
    Task<List<RoomDocument>> GetPublicWaitingRoomsAsync();
    Task<RoomDocument?> GetByPlayerIdAsync(string playerId);
    Task<RoomDocument> CreateAsync(RoomDocument room);
    Task UpdateAsync(RoomDocument room);
    Task AddPlayerAsync(string roomId, RoomPlayerEntry player);
    Task RemovePlayerAsync(string roomId, string playerId);
    Task SetPlayerReadyAsync(string roomId, string playerId, bool isReady);
    Task SetStatusAsync(string roomId, RoomStatus status, string? worldId = null);
    Task DisbandAsync(string roomId);
}

public class RoomRepository : IRoomRepository
{
    private readonly IMongoCollection<RoomDocument> _collection;

    public RoomRepository(IMongoDatabase db)
    {
        _collection = db.GetCollection<RoomDocument>("rooms");
    }

    public async Task<RoomDocument?> GetByIdAsync(string id)
        => await _collection.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task<List<RoomDocument>> GetPublicWaitingRoomsAsync()
        => await _collection.Find(r => !r.IsPrivate && r.Status == RoomStatus.Waiting)
            .SortByDescending(r => r.CreatedAt)
            .Limit(20)
            .ToListAsync();

    public async Task<RoomDocument?> GetByPlayerIdAsync(string playerId)
        => await _collection.Find(r =>
            r.Status == RoomStatus.Waiting &&
            r.Players.Any(p => p.PlayerId == playerId))
            .FirstOrDefaultAsync();

    public async Task<RoomDocument> CreateAsync(RoomDocument room)
    {
        await _collection.InsertOneAsync(room);
        return room;
    }

    public async Task UpdateAsync(RoomDocument room)
    {
        room.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(r => r.Id == room.Id, room);
    }

    public async Task AddPlayerAsync(string roomId, RoomPlayerEntry player)
    {
        var update = Builders<RoomDocument>.Update
            .Push(r => r.Players, player)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(r => r.Id == roomId, update);
    }

    public async Task RemovePlayerAsync(string roomId, string playerId)
    {
        var update = Builders<RoomDocument>.Update
            .PullFilter(r => r.Players, p => p.PlayerId == playerId)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(r => r.Id == roomId, update);
    }

    public async Task SetPlayerReadyAsync(string roomId, string playerId, bool isReady)
    {
        var filter = Builders<RoomDocument>.Filter.And(
            Builders<RoomDocument>.Filter.Eq(r => r.Id, roomId),
            Builders<RoomDocument>.Filter.ElemMatch(r => r.Players, p => p.PlayerId == playerId));
        var update = Builders<RoomDocument>.Update
            .Set("Players.$.IsReady", isReady)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task SetStatusAsync(string roomId, RoomStatus status, string? worldId = null)
    {
        var update = Builders<RoomDocument>.Update
            .Set(r => r.Status, status)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);
        if (worldId != null)
            update = update.Set(r => r.WorldId, worldId);
        await _collection.UpdateOneAsync(r => r.Id == roomId, update);
    }

    public async Task DisbandAsync(string roomId)
        => await SetStatusAsync(roomId, RoomStatus.Disbanded);
}
