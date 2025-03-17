using BlueDeep.Core.Enums;

namespace BlueDeep.Server.Models;

public sealed record MessageBrokerModel
{
    public MessageBrokerModel(string data, MessagePriority? priority)
    {
        Id = Guid.NewGuid();
        Data = data;
        Priority = priority ?? MessagePriority.Low;
        Timestamp = DateTime.UtcNow;
    }

    public Guid Id { get;}
    public string Data { get;}
    public MessagePriority Priority { get;}
    public DateTime Timestamp { get;}
}