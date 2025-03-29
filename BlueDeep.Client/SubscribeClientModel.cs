namespace BlueDeep.Client;

/// <summary>
/// Client-side subscribe model
/// </summary>
public class SubscribeClientModel
{
    /// <summary>
    /// Action
    /// </summary>
    public required Func<string, Task> Handler { get; init; }
    
    /// <summary>
    /// Max handlers can be executed concurrently by subscriber 
    /// </summary>
    public required int MaxHandlers { get; init; }
}