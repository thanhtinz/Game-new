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
    ExpandTerritory,     // Expand territory
    BuildTemple,         // Build temple
    SpreadFaith,         // Spread the faith
    MakeWar,             // Attack another civ
    MakePeace,           // Stop the war
    FocusEconomy,        // Focus on economy
    FocusMilitary,       // Strengthen military
    Worship,             // Increase devotion
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
    public string Message { get; set; } = string.Empty;  // Divine proclamation
    public bool Obeyed { get; set; } = false;
    public float TrustRequired { get; set; } = 50f;      // Civ must have trust >= this to obey
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

    // Sample proclamations per commandment type
    private static readonly Dictionary<CommandmentType, string[]> CommandMessages = new()
    {
        [CommandmentType.ExpandTerritory] = new[]
        {
            "Expand your borders, my child!",
            "The eastern lands await your conquest!",
            "Your power must spread across the world!"
        },
        [CommandmentType.BuildTemple] = new[]
        {
            "Build a place of worship for me at once!",
            "I need a worthy temple in your domain!",
            "The temple will bring blessings to your people!"
        },
        [CommandmentType.SpreadFaith] = new[]
        {
            "Spread my faith to every corner!",
            "The unbelievers must be enlightened!",
            "I want the whole world to know my name!"
        },
        [CommandmentType.MakeWar] = new[]
        {
            "It is time to punish the unbelievers!",
            "Attack now! Victory is my will!",
            "The blood of enemies shall be my offering!"
        },
        [CommandmentType.MakePeace] = new[]
        {
            "Lay down your weapons. This is my command.",
            "Peace is the true strength.",
            "I do not want my followers dying in vain."
        },
        [CommandmentType.FocusEconomy] = new[]
        {
            "Build, do not fight!",
            "Wealth will attract more believers.",
            "Prosperity is the best way to honor me."
        },
        [CommandmentType.FocusMilitary] = new[]
        {
            "Strengthen the army! The enemy is coming!",
            "Military strength protects the faith!",
            "Weak believers are not worthy of me!"
        },
        [CommandmentType.Worship] = new[]
        {
            "Pray more! I am listening!",
            "Stronger faith is what I demand!",
            "Your devotion must be wholehearted!"
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
        if (god == null || !god.IsAlive) return (null!, "God does not exist");

        var civ = await _civRepo.GetByIdAsync(civId);
        if (civ == null) return (null!, "Civilization does not exist");

        float cost = CommandFaithCosts.TryGetValue(type, out var c) ? c : 15f;
        if (god.Faith < cost) return (null!, $"Insufficient Faith (need {cost})");

        // Select message
        string msg = customMessage ?? PickMessage(type);

        // Check if civ obeys (based on GodTrustLevel)
        float trustLevel = civ.AiMemory.GodTrustLevel;
        float trustRequired = type == CommandmentType.MakeWar ? 70f : 40f;
        bool obeyed = trustLevel >= trustRequired;

        // Faith consumed whether civ obeys or not
        var newFaith = MathF.Max(0f, god.Faith - cost);
        await _godRepo.UpdateFaithAsync(godId, newFaith, god.Trust, god.Fear, god.FollowerCount);

        // Apply effect if civ obeys
        string obeyReason;
        if (obeyed)
        {
            await ApplyCommandmentEffectAsync(civ, type);
            obeyReason = $"{civ.Name} obeyed the divine command! (Trust: {trustLevel:F0})";
        }
        else
        {
            // Refusal reaction: trust decreases slightly
            civ.AiMemory.GodTrustLevel = MathF.Max(0f, trustLevel - 5f);
            await _civRepo.UpdateAsync(civ);
            obeyReason = $"{civ.Name} ignored the divine command (Trust too low: {trustLevel:F0}/{trustRequired:F0})";
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
        // Placeholder: handle multi-tick commandment effects
        // (e.g. ExpandTerritory takes multiple ticks to complete)
    }

    // ─── Effects ──────────────────────────────────────────────

    private async Task ApplyCommandmentEffectAsync(CivilizationDocument civ, CommandmentType type)
    {
        switch (type)
        {
            case CommandmentType.ExpandTerritory:
                civ.Military += 15f;
                civ.IsAtWar = false; // reset to trigger expansion behavior
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
        if (!CommandMessages.TryGetValue(type, out var msgs)) return "Follow my will!";
        return msgs[Random.Shared.Next(msgs.Length)];
    }
}
