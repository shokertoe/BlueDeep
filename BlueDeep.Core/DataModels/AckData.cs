using BlueDeep.Core.Enums;

namespace BlueDeep.Core.DataModels;

public record AckData(Guid MessageId, MessageStatus Status);