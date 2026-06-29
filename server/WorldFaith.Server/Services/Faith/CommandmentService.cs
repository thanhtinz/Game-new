using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Contracts;

namespace WorldFaith.Server.Services.Faith;

// ─── Commandment Types ────────────────────────────────────
public enum CommandmentType
{
    ExpandTerritory,     // Mở rộng lãnh thổ
    BuildTemple,         // Xây đền thờ
    SpreadFaith,         // Lan truyền tôn giáo
    MakeWar,             // Tấn công civ khác
    MakePeace,           // Dừng chiến tranh
    FocusEconomy,        // Tập trung kinh tế
    FocusMilitary,       // Tăng cường quân sự
    Worship,             // Tăng devotion
}

// ─── Document ─────────────────────────────────────────────
public class CommandmentDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public string CivilizationId { get; set; } = string.Empty;
    public CommandmentType Type { get; set; }
    public string Message { get; set; } = string.Empty;  // Lời phán của thần
    public bool Obeyed { get; set; } = false;
    public float TrustRequired { get; set; } = 50f;      // Civ phải có trust >= này mới nghe
    public float FaithCost { get; set; } = 15f;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ObeyedAt { get; set; }
}

// ─── SignalR Contract ─────────────────────────────────────
public class CommandmentIssuedEvent
{
    public string CommandmentId { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public string GodName { get; set; } = string.Empty;
    public string CivilizationId { get; set; } = string.Empty;
    public string CivilizationName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Obeyed { get; set; }
    public string ObeyReason { get; set; } = string.Empty;
}

// ─── Repository ───────────────────────────────────────────
public interface ICommandmentRepository
{
    Task<CommandmentDocument> CreateAsync(CommandmentDocument cmd);
    Task<List<CommandmentDocument>> GetByWorldAsync(string worldId, int limit = 20);
    Task MarkObeyedAsync(string commandmentId);
}

public class CommandmentRepository : ICommandmentRepository
{
    private readonly IMongoCollection<CommandmentDocument> _collection;

    public CommandmentRepository(IMongoDatabase db)
    {
        _collection = db.GetCollection<CommandmentDocument>("commandments");
    }

    public async Task<CommandmentDocument> CreateAsync(CommandmentDocument cmd)
    {
        await _collection.InsertOneAsync(cmd);
        return cmd;
    }

    public async Task<List<CommandmentDocument>> GetByWorldAsync(string worldId, int limit = 20)
        => await _collection.Find(c => c.WorldId == worldId)
            .SortByDescending(c => c.IssuedAt).Limit(limit).ToListAsync();

    public async Task MarkObeyedAsync(string commandmentId)
    {
        var update = Builders<CommandmentDocument>.Update
            .Set(c => c.Obeyed, true)
            .Set(c => c.ObeyedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(c => c.Id == commandmentId, update);
    }
}

// ─── Service ──────────────────────────────────────────────
public interface ICommandmentService
{
    Task<(CommandmentIssuedEvent evt, string? error)> IssueCommandmentAsync(
        string worldId, string godId, string civId,
        CommandmentType type, string? customMessage = null);
    Task<List<CommandmentIssuedEvent>> GetRecentAsync(string worldId);
    Task ProcessPendingCommandmentsAsync(string worldId);
}

public class CommandmentService : ICommandmentService
{
    private readonly ICommandmentRepository _commandmentRepo;
    private readonly IGodRepository _godRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IFaithService _faithService;
    private readonly ILogger<CommandmentService> _logger;

    // Lời phán mẫu theo loại commandment
    private static readonly Dictionary<CommandmentType, string[]> CommandMessages = new()
    {
        [CommandmentType.ExpandTerritory] = new[]
        {
            "Hãy mở rộng bờ cõi, con của ta!",
            "Vùng đất phía đông đang chờ ngươi chinh phục!",
            "Sức mạnh của ngươi phải lan rộng khắp thế giới!"
        },
        [CommandmentType.BuildTemple] = new[]
        {
            "Hãy xây dựng nơi thờ phụng ta ngay lập tức!",
            "Ta cần một ngôi đền xứng đáng trong lãnh thổ của ngươi!",
            "Đền thờ sẽ mang lại phước lành cho người dân của ngươi!"
        },
        [CommandmentType.SpreadFaith] = new[]
        {
            "Hãy truyền bá đức tin của ta đến mọi nơi!",
            "Những kẻ không tin phải được khai sáng!",
            "Ta muốn toàn thế giới biết đến danh ta!"
        },
        [CommandmentType.MakeWar] = new[]
        {
            "Đã đến lúc trừng phạt những kẻ không tin!",
            "Tấn công ngay! Chiến thắng là ý ta!",
            "Máu của kẻ thù sẽ là lễ vật dâng ta!"
        },
        [CommandmentType.MakePeace] = new[]
        {
            "Hãy buông vũ khí xuống. Đây là lệnh của ta.",
            "Hòa bình là sức mạnh thực sự.",
            "Ta không muốn tín đồ của ta chết vô nghĩa nữa."
        },
        [CommandmentType.FocusEconomy] = new[]
        {
            "Hãy xây dựng, không phải chiến đấu!",
            "Sự giàu có sẽ thu hút nhiều tín đồ hơn.",
            "Thịnh vượng là cách tốt nhất để vinh danh ta."
        },
        [CommandmentType.FocusMilitary] = new[]
        {
            "Tăng cường quân đội! Kẻ thù đang đến!",
            "Sức mạnh quân sự bảo vệ đức tin!",
            "Những tín đồ yếu đuối không xứng đáng với ta!"
        },
        [CommandmentType.Worship] = new[]
        {
            "Hãy cầu nguyện nhiều hơn! Ta đang lắng nghe!",
            "Đức tin mạnh mẽ hơn là điều ta đòi hỏi!",
            "Devotion của các ngươi cần phải toàn tâm toàn ý!"
        },
    };

    private static readonly Dictionary<CommandmentType, float> CommandFaithCosts = new()
    {
        [CommandmentType.ExpandTerritory] = 20f,
        [CommandmentType.BuildTemple]     = 25f,
        [CommandmentType.SpreadFaith]     = 15f,
        [CommandmentType.MakeWar]         = 30f,
        [CommandmentType.MakePeace]       = 10f,
        [CommandmentType.FocusEconomy]    = 10f,
        [CommandmentType.FocusMilitary]   = 15f,
        [CommandmentType.Worship]         = 5f,
    };

    public CommandmentService(
        ICommandmentRepository commandmentRepo,
        IGodRepository godRepo,
        ICivilizationRepository civRepo,
        IReligionRepository religionRepo,
        IFaithService faithService,
        ILogger<CommandmentService> logger)
    {
        _commandmentRepo = commandmentRepo;
        _godRepo = godRepo;
        _civRepo = civRepo;
        _religionRepo = religionRepo;
        _faithService = faithService;
        _logger = logger;
    }

    public async Task<(CommandmentIssuedEvent evt, string? error)> IssueCommandmentAsync(
        string worldId, string godId, string civId,
        CommandmentType type, string? customMessage = null)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null || !god.IsAlive) return (null!, "God không tồn tại");

        var civ = await _civRepo.GetByIdAsync(civId);
        if (civ == null) return (null!, "Civilization không tồn tại");

        float cost = CommandFaithCosts.TryGetValue(type, out var c) ? c : 15f;
        if (god.Faith < cost) return (null!, $"Không đủ Faith (cần {cost})");

        // Chọn message
        string msg = customMessage ?? PickMessage(type);

        // Kiểm tra civ có nghe lệnh không (dựa vào GodTrustLevel)
        float trustLevel = civ.AiMemory.GodTrustLevel;
        float trustRequired = type == CommandmentType.MakeWar ? 70f : 40f;
        bool obeyed = trustLevel >= trustRequired;

        // Tốn faith dù civ có nghe hay không
        var newFaith = MathF.Max(0f, god.Faith - cost);
        await _godRepo.UpdateFaithAsync(godId, newFaith, god.Trust, god.Fear, god.FollowerCount);

        // Áp dụng hiệu ứng nếu civ nghe lệnh
        string obeyReason;
        if (obeyed)
        {
            await ApplyCommandmentEffectAsync(civ, type);
            obeyReason = $"{civ.Name} tuân lệnh thần! (Trust: {trustLevel:F0})";
        }
        else
        {
            // Phản ứng từ chối: trust giảm nhẹ
            civ.AiMemory.GodTrustLevel = MathF.Max(0f, trustLevel - 5f);
            await _civRepo.UpdateAsync(civ);
            obeyReason = $"{civ.Name} phớt lờ lệnh thần (Trust quá thấp: {trustLevel:F0}/{trustRequired:F0})";
        }

        var doc = new CommandmentDocument
        {
            WorldId = worldId,
            GodId = godId,
            CivilizationId = civId,
            Type = type,
            Message = msg,
            Obeyed = obeyed,
            TrustRequired = trustRequired,
            FaithCost = cost
        };
        await _commandmentRepo.CreateAsync(doc);

        _logger.LogInformation("Commandment {Type} → {Civ}: {Obeyed}", type, civ.Name, obeyed ? "Obeyed" : "Ignored");

        return (new CommandmentIssuedEvent
        {
            CommandmentId    = doc.Id,
            GodId            = godId,
            GodName          = god.Name,
            CivilizationId   = civId,
            CivilizationName = civ.Name,
            Type             = type.ToString(),
            Message          = msg,
            Obeyed           = obeyed,
            ObeyReason       = obeyReason
        }, null);
    }

    public async Task<List<CommandmentIssuedEvent>> GetRecentAsync(string worldId)
    {
        var docs = await _commandmentRepo.GetByWorldAsync(worldId);
        var result = new List<CommandmentIssuedEvent>();

        foreach (var doc in docs)
        {
            var god = await _godRepo.GetByIdAsync(doc.GodId);
            var civ = await _civRepo.GetByIdAsync(doc.CivilizationId);
            result.Add(new CommandmentIssuedEvent
            {
                CommandmentId    = doc.Id,
                GodId            = doc.GodId,
                GodName          = god?.Name ?? "Unknown",
                CivilizationId   = doc.CivilizationId,
                CivilizationName = civ?.Name ?? "Unknown",
                Type             = doc.Type.ToString(),
                Message          = doc.Message,
                Obeyed           = doc.Obeyed
            });
        }
        return result;
    }

    public async Task ProcessPendingCommandmentsAsync(string worldId)
    {
        // Placeholder: xử lý commandments chưa hoàn thành sau nhiều ticks
        // (vd: ExpandTerritory cần nhiều ticks để xảy ra)
    }

    // ─── Effects ──────────────────────────────────────────────

    private async Task ApplyCommandmentEffectAsync(CivilizationDocument civ, CommandmentType type)
    {
        switch (type)
        {
            case CommandmentType.ExpandTerritory:
                civ.Military += 15f;
                civ.IsAtWar = false; // reset để trigger expansion behavior
                break;

            case CommandmentType.BuildTemple:
                civ.Economy += 10f;
                civ.AiMemory.GodTrustLevel += 10f;
                break;

            case CommandmentType.SpreadFaith:
                civ.AiMemory.GodTrustLevel += 5f;
                break;

            case CommandmentType.MakeWar:
                civ.IsAtWar = true;
                civ.Military += 20f;
                break;

            case CommandmentType.MakePeace:
                civ.IsAtWar = false;
                civ.AiMemory.TicksAtWar = 0;
                civ.AiMemory.CurrentTarget = null;
                break;

            case CommandmentType.FocusEconomy:
                civ.Economy += 25f;
                break;

            case CommandmentType.FocusMilitary:
                civ.Military += 25f;
                break;

            case CommandmentType.Worship:
                civ.AiMemory.GodTrustLevel += 15f;
                break;
        }

        civ.AiMemory.GodTrustLevel = MathF.Min(100f, civ.AiMemory.GodTrustLevel);
        await _civRepo.UpdateAsync(civ);
    }

    private static string PickMessage(CommandmentType type)
    {
        if (!CommandMessages.TryGetValue(type, out var msgs)) return "Hãy làm theo ý ta!";
        return msgs[Random.Shared.Next(msgs.Length)];
    }
}
