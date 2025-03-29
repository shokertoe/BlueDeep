namespace BlueDeep.Core.DataModels;

/// <summary>
/// Subscribe data from client model
/// </summary>
/// <param name="TopicName">Topic name</param>
/// <param name="MaxHandlers">Max concurrent handlers</param>
public record SubscribeData(string TopicName, int MaxHandlers);