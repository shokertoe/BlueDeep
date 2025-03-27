using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace BlueDeep.Server.Models;

/// <summary>
/// Stat info about topic
/// </summary>
public class TopicInfo
{
    /// <summary>
    /// Topic name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Message counter with High priority
    /// </summary>
    public required long PriorityHighCount { get; set; }
    
    /// <summary>
    /// Message counter with Low priority
    /// </summary>
    public required long PriorityLowCount { get; set; }
    
    /// <summary>
    /// Total messages counter
    /// </summary>
    public long TotalCount => PriorityHighCount + PriorityLowCount;
    
    /// <summary>
    /// Subscribers list (Ip:Port)
    /// </summary>
    private List<string> _subscribers { get; set; }

    public void SetSubscribers(ConcurrentBag<TcpClient>? subscribers)
    {
        _subscribers = subscribers?.Select(x =>
            (x.Client.RemoteEndPoint as IPEndPoint)?.Address + ":" +
            (x.Client.RemoteEndPoint as IPEndPoint)?.Port).ToList() ?? [];
    }
    
    public List<string> GetSubscribers()=>_subscribers;
}