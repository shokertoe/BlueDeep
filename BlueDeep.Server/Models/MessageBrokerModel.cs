using BlueDeep.Core.Enums;

namespace BlueDeep.Server.Models;

/// <summary>
/// Message model stored in MessageBroker
/// </summary>
public sealed record MessageBrokerModel
{
    public MessageBrokerModel(string data, MessagePriority? priority)
    {
        Id = Guid.NewGuid();
        Data = data;
        Priority = priority ?? MessagePriority.Low;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Message ID
    /// </summary>
    public Guid Id { get;}
    
    /// <summary>
    /// Serialized data
    /// </summary>
    public string Data { get;}
    
    /// <summary>
    /// Priority
    /// </summary>
    public MessagePriority Priority { get;}
    
    /// <summary>
    /// Created date
    /// </summary>
    public DateTime Timestamp { get;}
}