using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Services;

public class TopicService
{
    private static ConcurrentDictionary<string, ConcurrentBag<TcpClient>?>? _topicSubscribers;
    private readonly ILogger<TopicService> _logger;
    public TopicService(ILogger<TopicService> logger)
    {
        _logger = logger;
        _topicSubscribers ??= new ConcurrentDictionary<string, ConcurrentBag<TcpClient>?>();
    }
    
    public void RemoveClientFromAllTopics(TcpClient client)
    {
        foreach (var topic in _topicSubscribers.Keys)
        {
            if (!_topicSubscribers.TryGetValue(topic, out var subscribers)) continue;

            var newSubscribers = new ConcurrentBag<TcpClient>(subscribers.Where(s => s != client));
            _topicSubscribers[topic] = newSubscribers;
        }
        
        _logger.LogDebug("Client {Client} was removed from all topics",client.Client.RemoteEndPoint);
    }

    public IEnumerable<string> GetTopicsWithSubscribers()
    {
        return _topicSubscribers.Where(x => !x.Value.IsEmpty).Select(x => x.Key);
    }

    public bool TryGetSubscribers(string topic, out ConcurrentBag<TcpClient>? subscribers)
    {
        return _topicSubscribers.TryGetValue(topic, out subscribers);
    }

    public void AddSubscriber(string topicName, TcpClient client)
    {
        // Добавление клиента в подписчики топика
        if (!_topicSubscribers.TryGetValue(topicName, out var subscribers) )
        {
            subscribers = [];
            _topicSubscribers[topicName] = subscribers;
        }
        subscribers.Add(client);
    }
}