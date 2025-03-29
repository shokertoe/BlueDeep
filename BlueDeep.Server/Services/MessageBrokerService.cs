using System.Collections.Concurrent;
using BlueDeep.Core.DataModels;
using BlueDeep.Core.Enums;
using BlueDeep.Server.Exceptions;
using BlueDeep.Server.Models;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Services;

/// <summary>
/// MessageBroker
/// </summary>
public class MessageBrokerService
{
    private readonly ILogger<MessageBrokerService> _logger;
    private static ConcurrentDictionary<string, List<MessageBrokerModel>>? _messageList;
    private static object? _brokerLock;
    
    public MessageBrokerService(ILogger<MessageBrokerService> logger)
    {
        _logger = logger;
        _messageList ??= new ConcurrentDictionary<string, List<MessageBrokerModel>>();
        _brokerLock ??= new object();
    }
    
    public void EnqueueMessage(PublishData obj)
    {
        var topicName = obj.TopicName;
        var priority = obj.Priority;
        var data = obj.Data;
        
        //Блокируем поток работы с очередями
        lock (_brokerLock ?? throw new LockNullReferenceException())
        {
            if (_messageList is null) throw new MessagesNullReferenceException();
            
            if (!_messageList.ContainsKey(topicName)) 
                _messageList.TryAdd(topicName, []);
        
            _messageList[topicName].Add( new MessageBrokerModel(data, priority));
            _logger.LogDebug("Enqueued message. Data: {@Data}", obj);
        }
    }
    
    /// <summary>
    /// Получение элемента из очереди без удаления из очереди
    /// </summary>
    /// <param name="topic">Название топика</param>
    /// <returns>Модель с сообщением</returns>
    public MessageBrokerModel? GetMessage(string topic)
    {
        lock (_brokerLock ?? throw new LockNullReferenceException())
        {
            if (_messageList is null) throw new MessagesNullReferenceException();
            
            if (!_messageList.TryGetValue(topic, out var messagesBag)) return null;
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
        lock (_brokerLock ?? throw new LockNullReferenceException())
        {
            if (_messageList is null) throw new MessagesNullReferenceException();
            
            if (!_messageList.TryGetValue(topic, out var messagesBag))
                throw new KeyNotFoundException("DequeueFailed topic not found");
            var message = messagesBag.FirstOrDefault(x => x.Id == messageId) ?? throw new KeyNotFoundException("DequeueFailed messageId not found");
            messagesBag.Remove(message);
            
            _logger.LogDebug("Dequeued message {$Message}", message);
        }
    }

    /// <summary>
    /// Get topic list
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetTopics()
    {
        lock (_brokerLock ?? throw new LockNullReferenceException())
        {
            return _messageList?.Keys.Select(x=>x.ToString()) ?? throw new MessagesNullReferenceException();
        }
    }
    
    /// <summary>
    /// Get topic list
    /// </summary>
    /// <returns></returns>
    public List<TopicInfo> GetTopicsInfo()
    {
        lock (_brokerLock ?? throw new LockNullReferenceException())
        {
            return _messageList?.Select(topic => new TopicInfo()
            {
                Name = topic.Key,
                PriorityHighCount = topic.Value.Count(x => x.Priority == MessagePriority.High),
                PriorityLowCount =  topic.Value.Count(x => x.Priority == MessagePriority.Low)
            }) .ToList() ?? throw new MessagesNullReferenceException();
        }
    }
}