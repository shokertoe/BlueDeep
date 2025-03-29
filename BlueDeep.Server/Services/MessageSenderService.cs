using System.Text;
using System.Text.Json;
using BlueDeep.Core.Models;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Services;

/// <summary>
/// Send messages from message broker
/// </summary>
public class MessageSenderService
{
    private readonly ILogger<MessageSenderService> _logger;
    private readonly TopicService _topicService;
    private readonly MessageBrokerService _messageBrokerService;

    public MessageSenderService(ILogger<MessageSenderService> logger, TopicService topicService,
        MessageBrokerService messageBrokerService)
    {
        _logger = logger;
        _topicService = topicService;
        _messageBrokerService = messageBrokerService;
    }

    /// <summary>
    /// Message sender thread
    /// </summary>
    /// <returns></returns>
    public Task MessageSenderStartAsync()
    {
        while (true)
        {
            try
            {
                foreach (var topic in _topicService.GetTopicsWithActiveSubscribers())
                {
                    //Get message from broker
                    var messageObject = _messageBrokerService.GetMessage(topic);
                    if (messageObject is null) continue;

                    var message = messageObject.Data;
                    var messageId = messageObject.Id;

                    var sentCounter = 0;
                    if (TopicService.TryGetSubscribers(topic, out var subscribers) && subscribers?.Count > 0)
                    {
                        var data = Encoding.UTF8.GetBytes(
                            JsonSerializer.Serialize(new ServerMessage(topic, message, messageId)));
                        var length = BitConverter.GetBytes(data.Length);

                        //Send message for each subscribers
                        foreach (var subscriber in subscribers)
                        {
                            if (subscriber.Client.Connected)
                            {
                                var stream = subscriber.Client.GetStream();
                                stream.Write(length, 0, 4); //Send message length
                                stream.Write(data); //Send message
                                sentCounter++;
                            }
                            else
                            {
                                _topicService.RemoveClientFromAllTopics(subscriber.Client);
                            }
                        }
                    }

                    //если хотя бы кому-то было доставлено, то норм и убираем сообщение из очереди брокера
                    if (sentCounter != 0)
                        _messageBrokerService.DequeueMessage(topic, messageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Error in MessageSender thread {Exception}", ex);
                throw;
            }
        }
    }
}