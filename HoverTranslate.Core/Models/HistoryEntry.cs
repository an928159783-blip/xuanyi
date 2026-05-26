namespace HoverTranslate.Core.Models;

public sealed class HistoryEntry
{
    public long Id { get; init; }
    public string Source { get; init; } = "";
    public string Target { get; init; } = "";
    public string Provider { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}
