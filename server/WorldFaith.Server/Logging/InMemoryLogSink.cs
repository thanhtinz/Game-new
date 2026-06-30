using Serilog.Core;
using Serilog.Events;

namespace WorldFaith.Server.Logging;

/// <summary>
/// A small in-memory ring buffer of recent log entries, exposed via
/// GET /api/admin/server/logs. This is intentionally minimal — it exists
/// so the Admin Panel has something to show without standing up a full
/// logging backend (Seq/ELK/CloudWatch). For production-scale log search,
/// replace this with a real centralized logging sink instead.
/// </summary>
public static class InMemoryLogSink
{
    private const int Capacity = 500;
    private static readonly LinkedList<LogEntryDto> Buffer = new();
    private static readonly object Lock = new();

    public static void Add(LogEntryDto entry)
    {
        lock (Lock)
        {
            Buffer.AddFirst(entry);
            while (Buffer.Count > Capacity)
                Buffer.RemoveLast();
        }
    }

    public static List<LogEntryDto> GetRecent(int limit = 100)
    {
        lock (Lock)
        {
            return Buffer.Take(Math.Clamp(limit, 1, Capacity)).ToList();
        }
    }
}

public class LogEntryDto
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}

/// <summary>Serilog sink adapter that forwards every log event into the in-memory ring buffer.</summary>
public class InMemorySink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        InMemoryLogSink.Add(new LogEntryDto
        {
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString(),
        });
    }
}
