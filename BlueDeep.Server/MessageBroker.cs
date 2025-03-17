using System.Collections.Concurrent;
using BlueDeep.Core.DataModels;
using BlueDeep.Server.Models;

namespace BlueDeep.Server;

public class MessageBroker
{
    private readonly object _queueLock = new();
    
    // Очередь сообщений с приоритетами
    private readonly ConcurrentDictionary<string, List<MessageBrokerModel>> _topics  = new ();
    
    public void Enqueue(MessagePublishModel messageObj)
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
            Console.WriteLine($"Message broker added to topic {topicName}");
            ShowStat();        
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
            
            Console.WriteLine($"Message broker message {messageId} dequeued from topic {topic}");
            ShowStat();
        }
    }

    //TODO потом убрать (пока что для отладочных целей)
    private void ShowStat()
    {
        Console.WriteLine("==================");
        foreach (var topic in _topics.Keys)
        {
            Console.WriteLine($"{topic}: {_topics[topic].Count}");
        }
        Console.WriteLine("==================");
    }
}