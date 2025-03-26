using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using BlueDeep.Core.DataModels;
using BlueDeep.Server.Store;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Processors;

public class SubscribeProcessor
{
    private readonly TopicSubscribersBag _topicSubscribers;
    private readonly ILogger<SubscribeProcessor> _logger;

    public SubscribeProcessor(TopicSubscribersBag topicSubscribers, ILogger<SubscribeProcessor> logger)
    {
        _topicSubscribers = topicSubscribers;
        _logger = logger;
    }

    public void ProcessMessage(MessageSubscribeModel subscribeData, TcpClient client)
    {
        // Добавление клиента в подписчики топика
        var topicName = subscribeData.TopicName;
        if (!_topicSubscribers.TryGetValue(topicName, out ConcurrentBag<TcpClient>? clients))
        {
            clients = [];
            _topicSubscribers[topicName] = clients;
        }

        clients.Add(client);
        _logger.LogInformation("Client IP:{@Address} Port: {Port} subscribed to topic '{topicName}'",
            (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString(),
            (client.Client.RemoteEndPoint as IPEndPoint)?.Port
            , topicName);
    }
}