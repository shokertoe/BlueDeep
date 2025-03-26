using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using BlueDeep.Core.DataModels;
using BlueDeep.Server.Broker;
using BlueDeep.Server.Store;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Processors;

public class PublishProcessor
{
    private readonly ILogger<SubscribeProcessor> _logger;
    private readonly MessageBroker _messageBroker;

    public PublishProcessor(ILogger<SubscribeProcessor> logger,
        MessageBroker broker)
    {
        _logger = logger;
        _messageBroker = broker;
    }

    public void ProcessMessage(MessagePublishModel publishData, TcpClient client)
    {
        // Добавление сообщения в очередь
        _messageBroker.Enqueue(publishData);

        _logger.LogInformation("Client IP:{@Address} Port: {Port} publish a message to topic '{topicName}'",
            (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString(),
            (client.Client.RemoteEndPoint as IPEndPoint)?.Port
            , publishData.TopicName);
    }
}