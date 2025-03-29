using System.Collections.Concurrent;
using System.Net;

namespace BlueDeep.Server.Models;

/// <summary>
/// Stat info about topic
/// </summary>
public class TopicInfo
{
    /// <summary>
    /// Topic name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Message counter with High priority
    /// </summary>
    public required long PriorityHighCount { get; init; }
    
    /// <summary>
    /// Message counter with Low priority
    /// </summary>
    public required long PriorityLowCount { get; init; }
    
    /// <summary>
    /// Total messages counter
    /// </summary>
    public long TotalCount => PriorityHighCount + PriorityLowCount;

    /// <summary>
    /// Subscribers list (Ip:Port)
    /// </summary>
    private List<string>? _subscribers;

    public void SetSubscribers(ConcurrentBag<SubscriberModel>? subscribers)
    {
        _subscribers = subscribers?.Select(x =>
            (x.Client.Client.RemoteEndPoint as IPEndPoint)?.Address + ":" +
            (x.Client.Client.RemoteEndPoint as IPEndPoint)?.Port).ToList() ?? [];
    }
    
    public List<string>? GetSubscribers()=>_subscribers;
}