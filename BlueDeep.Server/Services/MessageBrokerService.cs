using System.Collections.Concurrent;
using BlueDeep.Core.DataModels;
using BlueDeep.Server.Models;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Services;

public class MessageBrokerService
{
    private readonly ILogger<MessageBrokerService> _logger;
    public MessageBrokerService(ILogger<MessageBrokerService> logger)
    {
        _logger = logger;
    }
    private readonly object _queueLock = new();
    
    // Очередь сообщений с приоритетами
    private readonly ConcurrentDictionary<string, List<MessageBrokerModel>> _topics  = new ();
    
    public void EnqueueMessage(MessagePublishModel messageObj)
    {
        var topicName = messageObj.TopicName;
        var priority = messageObj.Priority;
        var data = messageObj.Data;
        
        //Блокируем поток работы с очередями
        lock (_queueLock)
        {
            if (!_topics.ContainsKey(topicName)) 
                _topics.TryAdd(topicName, []);
        
            _topics[topicName].Add( new MessageBrokerModel(data, priority));
            _logger.LogDebug("Enqueued message. Data: {@Data}", messageObj);
        }
    }
    
    /// <summary>
    /// Получение элемента из очереди без удаления из очереди
    /// </summary>
    /// <param name="topic">Название топика</param>
    /// <returns>Модель с сообщением</returns>
    public MessageBrokerModel? GetMessage(string topic)
    {
        lock (_queueLock)
        {
            if (!_topics.TryGetValue(topic, out var messagesBag)) return null;
            if (messagesBag.Count == 0 ) return null;
        
            var msg = messagesBag.OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Timestamp)
                .First();
            return msg;
        }
    }

    /// <summary>
    /// Удаление из очереди топика сообщения
    /// </summary>
    /// <param name="topic">Название топика</param>
    /// <param name="messageId">ID сообщения</param>
    /// <exception cref="KeyNotFoundException">Сообщение или топик не найден</exception>
    public void DequeueMessage(string topic, Guid messageId)
    {
        //Блокируем поток работы с очередями
        lock (_queueLock)
        {
            if (!_topics.TryGetValue(topic, out var messagesBag))
                throw new KeyNotFoundException("DequeueFailed topic not found");
            var message = messagesBag.FirstOrDefault(x => x.Id == messageId) ?? throw new KeyNotFoundException("DequeueFailed messageId not found");
            messagesBag.Remove(message);
            
            _logger.LogDebug("Dequeued message {$Message}", message);
        }
    }
}