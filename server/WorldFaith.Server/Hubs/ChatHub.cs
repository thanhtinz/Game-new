using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Chat;
using WorldFaith.Server.Services.Leaderboard;

namespace WorldFaith.Server.Hubs;

// ─── Chat Hub Interface ───────────────────────────────────
public interface IChatHubClient
{
    Task OnChatMessage(ChatMessageDto msg);
    Task OnChatHistory(List<ChatMessageDto> history);
    Task OnWhisper(ChatMessageDto msg);
    Task OnChatError(string error);
}

// ─── Chat Hub ─────────────────────────────────────────────
[Authorize]
public class ChatHub : Hub<IChatHubClient>
{
    private readonly IChatService _chatService;
    private readonly IGodRepository _godRepo;
    private readonly ILogger<ChatHub> _logger;

    // connectionId → (worldId, godId, godName, archetype)
    private static readonly Dictionary<string, (string worldId, string godId, string godName, string archetype)> ConnMap = new();

    public ChatHub(IChatService chatService, IGodRepository godRepo, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _godRepo = godRepo;
        _logger = logger;
    }

    private string PlayerId => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Context.User?.FindFirst("sub")?.Value ?? string.Empty;

    // ─── Join/Leave ──────────────────────────────────────────

    public async Task JoinWorldChat(string worldId)
    {
        var god = await _godRepo.GetByPlayerAndWorldAsync(PlayerId, worldId);
        if (god == null) { await Clients.Caller.OnChatError("Chưa có god trong world này"); return; }

        ConnMap[Context.ConnectionId] = (worldId, god.Id, god.Name, god.Archetype.ToString());
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:{worldId}");

        // Gửi lịch sử chat
        var history = await _chatService.GetHistoryAsync(worldId, 50);
        await Clients.Caller.OnChatHistory(history);

        // Thông báo join
        await _chatService.BroadcastSystemAsync(worldId, $"⚡ {god.Name} has tham gia thế giới");
        var sysMsg = await _chatService.GetHistoryAsync(worldId, 1);
        if (sysMsg.Any())
            await Clients.Group($"chat:{worldId}").OnChatMessage(sysMsg.Last());
    }

    public async Task LeaveWorldChat()
    {
        if (!ConnMap.TryGetValue(Context.ConnectionId, out var info)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{info.worldId}");
        ConnMap.Remove(Context.ConnectionId);
    }

    // ─── Send Message ────────────────────────────────────────

    public async Task SendMessage(string message, string type = "Normal")
    {
        if (!ConnMap.TryGetValue(Context.ConnectionId, out var info)) return;

        var (msg, error) = await _chatService.SendAsync(
            info.worldId, info.godId, info.godName, info.archetype, message, type);

        if (error != null) { await Clients.Caller.OnChatError(error); return; }

        await Clients.Group($"chat:{info.worldId}").OnChatMessage(msg!);
    }

    public async Task SendWhisper(string targetGodId, string message)
    {
        if (!ConnMap.TryGetValue(Context.ConnectionId, out var info)) return;

        var (msg, error) = await _chatService.SendAsync(
            info.worldId, info.godId, info.godName, info.archetype,
            message, "Whisper", targetGodId);

        if (error != null) { await Clients.Caller.OnChatError(error); return; }

        // Gửi for sender and receiver
        await Clients.Caller.OnWhisper(msg!);

        // Tìm connection of target god
        var targetConn = ConnMap.FirstOrDefault(c => c.Value.godId == targetGodId).Key;
        if (targetConn != null)
            await Clients.Client(targetConn).OnWhisper(msg!);
    }

    // ─── Quick Reactions ─────────────────────────────────────

    public async Task SendReaction(string reaction)
    {
        // Reactions nhanh: 😂 ⚡ 🔥 💀 🙏 😈
        var allowed = new HashSet<string> { "😂", "⚡", "🔥", "💀", "🙏", "😈", "👑", "🌍" };
        if (!allowed.Contains(reaction)) return;
        if (!ConnMap.TryGetValue(Context.ConnectionId, out var info)) return;

        var (msg, _) = await _chatService.SendAsync(
            info.worldId, info.godId, info.godName, info.archetype, reaction, "Normal");
        if (msg != null)
            await Clients.Group($"chat:{info.worldId}").OnChatMessage(msg);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnMap.TryGetValue(Context.ConnectionId, out var info))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{info.worldId}");
            ConnMap.Remove(Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
