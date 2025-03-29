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
public class AckMessageProcessor
{
    private readonly ILogger<AckMessageProcessor> _logger;
    private readonly MessageBrokerService _messageBrokerService;

    public AckMessageProcessor(ILogger<AckMessageProcessor> logger,
        MessageBrokerService brokerService)
    {
        _logger = logger;
        _messageBrokerService = brokerService;
    }

    /// <summary>
    /// Process Ack message
    /// </summary>
    /// <param name="ackData">Ack model</param>
    /// <param name="client">Tcp client</param>
    public void ProcessMessage(AckData ackData, TcpClient client)
    {
        _logger.LogTrace("Client IP:{@Address} Port: {Port} send ack message to topic '{@AckMessage}'",
            (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString(),
            (client.Client.RemoteEndPoint as IPEndPoint)?.Port
            , ackData);
    }
}