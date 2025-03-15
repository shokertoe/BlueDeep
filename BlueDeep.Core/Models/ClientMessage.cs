namespace BlueDeep.Core.Models;

public class ClientMessage
{
    public required string Type { get; init; }
    public required string Topic { get; init; }
    public string? Data { get; init; }
    public int? Priority { get; init; }
    public Guid Id { get; init; }
    public DateTime Timestamp { get; init; }
}