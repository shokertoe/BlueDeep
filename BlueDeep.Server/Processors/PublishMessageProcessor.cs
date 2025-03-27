using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using BlueDeep.Core.DataModels;
using BlueDeep.Server.Services;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Processors;

/// <summary>
///  Processor of PublishType incoming message 
/// </summary>
public class PublishMessageProcessor
{
    private readonly ILogger<SubscribeMessageProcessor> _logger;
    private readonly MessageBrokerService _messageBrokerService;

    public PublishMessageProcessor(ILogger<SubscribeMessageProcessor> logger,
        MessageBrokerService brokerService)
    {
        _logger = logger;
        _messageBrokerService = brokerService;
    }

    /// <summary>
    /// Process Publish message
    /// </summary>
    /// <param name="publishData">Publish model</param>
    /// <param name="client">Tcp client</param>
    public void ProcessMessage(MessagePublishModel publishData, TcpClient client)
    {
        _messageBrokerService.EnqueueMessage(publishData);

        _logger.LogInformation("Client IP:{@Address} Port: {Port} publish a message to topic '{topicName}'",
            (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString(),
            (client.Client.RemoteEndPoint as IPEndPoint)?.Port
            , publishData.TopicName);
    }
}