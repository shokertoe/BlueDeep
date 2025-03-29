using BlueDeep.Core.Enums;

namespace BlueDeep.Core.DataModels;

public record PublishData(string TopicName, MessagePriority Priority, string Data);