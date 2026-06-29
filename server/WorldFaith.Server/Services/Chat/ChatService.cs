using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WorldFaith.Server.Repositories;

namespace WorldFaith.Server.Services.Chat;

// ─── Document ────────────────────────────────────────────
public class ChatMessageDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public string GodName { get; set; } = string.Empty;
    public string GodArchetype { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ChatMessageType Type { get; set; } = ChatMessageType.Normal;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public enum ChatMessageType
{
    Normal,      // Tin nhắn thường
    Emote,       // /me ...
    Divine,      // Thông báo miracle
    System,      // Server notification
    Whisper      // Nhắn riêng
}

// ─── DTO ─────────────────────────────────────────────────
public class ChatMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public string GodName { get; set; } = string.Empty;
    public string GodArchetype { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Normal";
    public long SentAt { get; set; }
}

public class SendChatMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Normal";
    public string? TargetGodId { get; set; } // null = broadcast
}

// ─── Repository ──────────────────────────────────────────
public interface IChatRepository
{
    Task<ChatMessageDocument> SaveAsync(ChatMessageDocument msg);
    Task<List<ChatMessageDocument>> GetRecentAsync(string worldId, int limit = 50);
    Task<List<ChatMessageDocument>> GetWhispersAsync(string worldId, string godId, string targetGodId, int limit = 20);
}

public class ChatRepository : IChatRepository
{
    private readonly IMongoCollection<ChatMessageDocument> _collection;

    public ChatRepository(IMongoDatabase db)
    {
        _collection = db.GetCollection<ChatMessageDocument>("chat_messages");
        _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<ChatMessageDocument>(
                Builders<ChatMessageDocument>.IndexKeys.Ascending(m => m.WorldId).Descending(m => m.SentAt)));
    }

    public async Task<ChatMessageDocument> SaveAsync(ChatMessageDocument msg)
    {
        await _collection.InsertOneAsync(msg);
        return msg;
    }

    public async Task<List<ChatMessageDocument>> GetRecentAsync(string worldId, int limit = 50)
        => await _collection
            .Find(m => m.WorldId == worldId && m.Type != ChatMessageType.Whisper)
            .SortByDescending(m => m.SentAt)
            .Limit(limit)
            .ToListAsync()
            .ContinueWith(t => { t.Result.Reverse(); return t.Result; });

    public async Task<List<ChatMessageDocument>> GetWhispersAsync(
        string worldId, string godId, string targetGodId, int limit = 20)
        => await _collection
            .Find(m => m.WorldId == worldId
                && m.Type == ChatMessageType.Whisper
                && (m.GodId == godId || m.GodId == targetGodId))
            .SortByDescending(m => m.SentAt)
            .Limit(limit)
            .ToListAsync()
            .ContinueWith(t => { t.Result.Reverse(); return t.Result; });
}

// ─── Service ─────────────────────────────────────────────
public interface IChatService
{
    Task<(ChatMessageDto? msg, string? error)> SendAsync(
        string worldId, string godId, string godName, string archetype,
        string message, string type, string? targetGodId = null);
    Task<List<ChatMessageDto>> GetHistoryAsync(string worldId, int limit = 50);
    Task BroadcastSystemAsync(string worldId, string message);
}

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepo;
    private readonly IGodRepository _godRepo;
    private readonly ILogger<ChatService> _logger;

    private const int MaxLength = 200;
    private const int RateLimitMs = 1000; // 1 tin/giây
    private readonly Dictionary<string, DateTime> _lastSent = new();

    // Danh sách từ was cấm (demo)
    private static readonly HashSet<string> BannedWords = new(StringComparer.OrdinalIgnoreCase)
    { "hack", "cheat", "exploit" };

    public ChatService(IChatRepository chatRepo, IGodRepository godRepo, ILogger<ChatService> logger)
    {
        _chatRepo = chatRepo;
        _godRepo = godRepo;
        _logger = logger;
    }

    public async Task<(ChatMessageDto? msg, string? error)> SendAsync(
        string worldId, string godId, string godName, string archetype,
        string message, string type, string? targetGodId = null)
    {
        // Validate
        message = message.Trim();
        if (string.IsNullOrEmpty(message))
            return (null, "Tin nhắn trống");

        if (message.Length > MaxLength)
            return (null, $"Tin nhắn quá dài (tối đa {MaxLength} ký tự)");

        // Rate limit
        if (_lastSent.TryGetValue(godId, out var last)
            && (DateTime.UtcNow - last).TotalMilliseconds < RateLimitMs)
            return (null, "Gửi quá nhanh");

        // Filter từ cấm
        foreach (var word in BannedWords)
            if (message.Contains(word, StringComparison.OrdinalIgnoreCase))
                return (null, "Tin nhắn chứa nội dung not phù hợp");

        // Parse type
        if (!Enum.TryParse<ChatMessageType>(type, out var msgType))
            msgType = ChatMessageType.Normal;

        // Handle /me emote
        if (message.StartsWith("/me "))
        {
            message = message[4..];
            msgType = ChatMessageType.Emote;
        }

        var doc = new ChatMessageDocument
        {
            WorldId = worldId,
            GodId = godId,
            GodName = godName,
            GodArchetype = archetype,
            Message = message,
            Type = msgType
        };

        await _chatRepo.SaveAsync(doc);
        _lastSent[godId] = DateTime.UtcNow;

        return (MapToDto(doc), null);
    }

    public async Task<List<ChatMessageDto>> GetHistoryAsync(string worldId, int limit = 50)
    {
        var msgs = await _chatRepo.GetRecentAsync(worldId, limit);
        return msgs.Select(MapToDto).ToList();
    }

    public async Task BroadcastSystemAsync(string worldId, string message)
    {
        var doc = new ChatMessageDocument
        {
            WorldId = worldId,
            GodId = "system",
            GodName = "⚡ Server",
            GodArchetype = "System",
            Message = message,
            Type = ChatMessageType.System
        };
        await _chatRepo.SaveAsync(doc);
    }

    private static ChatMessageDto MapToDto(ChatMessageDocument d) => new()
    {
        Id = d.Id,
        GodId = d.GodId,
        GodName = d.GodName,
        GodArchetype = d.GodArchetype,
        Message = d.Message,
        Type = d.Type.ToString(),
        SentAt = new DateTimeOffset(d.SentAt).ToUnixTimeSeconds()
    };
}
