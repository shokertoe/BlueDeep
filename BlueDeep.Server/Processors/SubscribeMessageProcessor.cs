using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using BlueDeep.Core.DataModels;
using BlueDeep.Server.Services;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Processors;

public class SubscribeMessageProcessor
{
    private readonly TopicService _topicService;
    private readonly ILogger<SubscribeMessageProcessor> _logger;

    public SubscribeMessageProcessor(TopicService topicService, ILogger<SubscribeMessageProcessor> logger)
    {
        _topicService = topicService;
        _logger = logger;
    }

    public void ProcessMessage(MessageSubscribeModel subscribeData, TcpClient client)
    {
        _topicService.AddSubscriber(subscribeData.TopicName, client);
        _logger.LogInformation("Client IP:{@Address} Port: {Port} subscribed to topic '{Topic}'",
            (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString(),
            (client.Client.RemoteEndPoint as IPEndPoint)?.Port
            , subscribeData.TopicName);
    }
}