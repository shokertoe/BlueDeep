using System.Collections.Concurrent;
using System.Net.Sockets;
using BlueDeep.Server.Exceptions;
using BlueDeep.Server.Models;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Services;

public class TopicService
{
    private static ConcurrentDictionary<string, ConcurrentBag<SubscriberModel>?>? _topicSubscribers;
    private static object? _topicLock;
    private readonly ILogger<TopicService> _logger;

    public TopicService(ILogger<TopicService> logger)
    {
        _logger = logger;
        _topicSubscribers ??= new ConcurrentDictionary<string, ConcurrentBag<SubscriberModel>?>();
        _topicLock ??= new object();
    }

    public void RemoveClientFromAllTopics(TcpClient client)
    {
        lock (_topicLock ?? throw new LockNullReferenceException())
        {
            foreach (var topic in _topicSubscribers?.Keys ?? throw new TopicNullReferenceException())
            {
                if (!_topicSubscribers.TryGetValue(topic, out var subscribers) || subscribers is null) continue;
                var newSubscribers = new ConcurrentBag<SubscriberModel>(subscribers.Where(s => s.Client != client));
                _topicSubscribers[topic] = newSubscribers;
            }
        }

        _logger.LogDebug("Client {Client} was removed from all topics", client.Client.RemoteEndPoint);
    }

    public List<string> GetTopicsWithActiveSubscribers()
    {
        lock (_topicLock ?? throw new LockNullReferenceException())
        {
            if (_topicSubscribers is null) throw new TopicNullReferenceException();
            return _topicSubscribers.Where(x => x.Value is { IsEmpty: false })
                .Select(x => x.Key)
                .ToList();
        }
    }

    public static bool TryGetSubscribers(string topic, out ConcurrentBag<SubscriberModel>? subscribers)
    {
        lock (_topicLock ?? throw new LockNullReferenceException())
        {
            return _topicSubscribers?.TryGetValue(topic, out subscribers) ?? throw new TopicNullReferenceException();
        }
    }

    public void AddSubscriber(string topicName, TcpClient client, int maxHandlers)
    {
        lock (_topicLock ?? throw new LockNullReferenceException())
        {
            if (_topicSubscribers is null) throw new TopicNullReferenceException();
            // Добавление клиента в подписчики топика
            if (!_topicSubscribers.TryGetValue(topicName, out var subscribers) || subscribers is null)
            {
                subscribers = [];
                _topicSubscribers[topicName] = subscribers;
            }

            subscribers.Add(new SubscriberModel(client, maxHandlers));
        }
    }
}