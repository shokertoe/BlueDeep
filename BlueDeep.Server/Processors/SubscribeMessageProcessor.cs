using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using BlueDeep.Core.DataModels;
using BlueDeep.Server.Services;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Processors;

/// <summary>
///  Processor of SubscribeType incoming message 
/// </summary>
public class SubscribeMessageProcessor
{
    private readonly TopicService _topicService;
    private readonly ILogger<SubscribeMessageProcessor> _logger;

    public SubscribeMessageProcessor(TopicService topicService, ILogger<SubscribeMessageProcessor> logger)
    {
        _topicService = topicService;
        _logger = logger;
    }

    /// <summary>
    /// Process Subscribe message
    /// </summary>
    /// <param name="subscribeData">Subscribe model</param>
    /// <param name="client">Tcp client</param>
    public void ProcessMessage(MessageSubscribeModel subscribeData, TcpClient client)
    {
        _topicService.AddSubscriber(subscribeData.TopicName, client);
        _logger.LogInformation("Client IP:{@Address} Port: {Port} subscribed to topic '{Topic}'",
            (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString(),
            (client.Client.RemoteEndPoint as IPEndPoint)?.Port
            , subscribeData.TopicName);
    }
}