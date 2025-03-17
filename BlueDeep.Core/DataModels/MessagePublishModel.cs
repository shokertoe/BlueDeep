using BlueDeep.Core.Enums;

namespace BlueDeep.Core.DataModels;

public record MessagePublishModel(string TopicName, MessagePriority Priority, string Data);