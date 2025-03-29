using System.Text.Json;
using BlueDeep.Core.DataModels;
using BlueDeep.Core.Enums;

namespace BlueDeep.Core.Models;

public class BaseClientMessage
{
    public ClientMessageType MessageType { get; init; }
    public string? MessageData { get; init; }
}

public class SubscribeMessage : BaseClientMessage
{
    public SubscribeMessage(string topicName, int maxHandlers)
    {
        MessageType = ClientMessageType.Subscribe;
        MessageData = JsonSerializer.Serialize(new SubscribeData(topicName,  maxHandlers));
    }
}

public class PublishMessage<T> : BaseClientMessage
{
    public PublishMessage(string topicName, MessagePriority priority, T message)
    {
        MessageType = ClientMessageType.Publish;
        MessageData = JsonSerializer.Serialize(new PublishData(topicName,  priority, JsonSerializer.Serialize(message)));
    }
}

public class AckMessage : BaseClientMessage
{
    public AckMessage(Guid messageId, MessageStatus status)
    {
        MessageType = ClientMessageType.Ack;
        MessageData = JsonSerializer.Serialize(new AckData(messageId,  status));
    }
}