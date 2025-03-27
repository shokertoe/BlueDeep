using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BlueDeep.Core.DataModels;
using BlueDeep.Core.Enums;
using BlueDeep.Core.Models;
using BlueDeep.Server.Processors;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Services;

/// <summary>
/// Client Interaction service
/// </summary>
public class ClientService
{
    private readonly ILogger<ClientService> _logger;
    private readonly TopicService _topicService;
    
    //Message processors
    private readonly SubscribeMessageProcessor _subscribeMessageProcessor;
    private readonly PublishMessageProcessor _publishMessageProcessor;
    
    public ClientService(ILogger<ClientService> logger,
        TopicService topicService,
        SubscribeMessageProcessor subscribeMessageProcessor,
        PublishMessageProcessor publishMessageProcessor)
    {
        _logger = logger;
        _topicService = topicService;
        _subscribeMessageProcessor = subscribeMessageProcessor;
        _publishMessageProcessor = publishMessageProcessor;
    }
    
    /// <summary>
    /// Receive client data worker
    /// </summary>
    /// <param name="client">Tcp client</param>
    public async Task StartReceiveDataAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new byte[4]; // Message buffer
        try
        {
            while (client.Connected)
            {
                // Reading message from stream to buffer
                var bytesRead = client.Client.Receive(buffer);
                if (bytesRead == 0) //client disconnect recognition
                {
                    break;
                }
                
                var messageLength = BitConverter.ToInt32(buffer, 0);
                var messageBuffer = new byte[messageLength];
                await stream.ReadExactlyAsync(messageBuffer, 0, messageLength);
                
                //Deserialize message to BaseClientMessage model
                var message = Encoding.UTF8.GetString(messageBuffer);
                var messageObj = JsonSerializer.Deserialize<BaseClientMessage>(message);
                ParseClientMessage(messageObj, client);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Client data worker failure. Client {Client}, exception {Exception}", client.Client.RemoteEndPoint, ex);
        }
        finally
        {
            //remove client on disconnect or failure
            await client.Client.DisconnectAsync(true);
            _topicService.RemoveClientFromAllTopics(client);
            _logger.LogInformation("Client disconnected {Client}", client.Client.RemoteEndPoint);
        }
    }
    
    /// <summary>
    /// Parse client messages by types
    /// </summary>
    /// <param name="messageObj">BaseClientMessage model</param>
    /// <param name="client">Tcp client</param>
    private void ParseClientMessage(BaseClientMessage? messageObj, TcpClient client)
    {
        switch (messageObj?.MessageType)
        {
            case ClientMessageType.Subscribe:
            {
                var subscribeData = JsonSerializer.Deserialize<MessageSubscribeModel>(messageObj.MessageData);
                if (subscribeData is null)
                {
                    _logger.LogError("Invalid Subscribe data from {Client}", client.Client.RemoteEndPoint);
                    break;
                }
                _subscribeMessageProcessor.ProcessMessage(subscribeData, client);
                break;
            }

            case ClientMessageType.Publish:
                var publishData = JsonSerializer.Deserialize<MessagePublishModel>(messageObj.MessageData);
                if (publishData is null)
                {
                    _logger.LogError("Invalid Publish data  from {Client} was received", client.Client.RemoteEndPoint);
                    break;
                }
                _publishMessageProcessor.ProcessMessage(publishData, client);
                break;

            case ClientMessageType.Ack:
                _logger.LogWarning("Unknown type {Type}  was received.",messageObj?.MessageType);
                break;
                    
            default:
                _logger.LogWarning("Unknown message type was received from {Client}",client.Client.RemoteEndPoint);
                break;
        }
    }
}