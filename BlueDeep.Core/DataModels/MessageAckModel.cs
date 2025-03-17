using BlueDeep.Core.Enums;

namespace BlueDeep.Core.DataModels;

public record MessageAckModel(Guid MessageId, MessageStatus Status);