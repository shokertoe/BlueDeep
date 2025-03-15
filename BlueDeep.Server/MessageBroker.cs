using System.Collections.Concurrent;
using BlueDeep.Core.Models;

namespace BlueDeep.Server;

public class MessageBroker
{
    // Очередь сообщений с приоритетами
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid,(string message, int priority, DateTimeOffset dt)>> _topics  = new ();
    
    public void Enqueue(ClientMessage messageObj)
    {
        if (!_topics.ContainsKey(messageObj.Topic)) _topics.TryAdd(messageObj.Topic, []);
        if (messageObj is not { Priority: not null, Data: not null })
            throw new NullReferenceException("Enqueue some fields has null value");
        
        _topics[messageObj.Topic].TryAdd(messageObj.Id,
            (messageObj.Data, messageObj.Priority.Value, messageObj.Timestamp));
        Console.WriteLine($"Message broker added to topic {messageObj.Topic} with id {messageObj.Id}");
        ShowStat();
    }
    
    public (Guid id, string message)? GetMessage(string topic)
    {
        if (!_topics.TryGetValue(topic, out var messages)) return null;
        if (messages.Count ==0) return null;
        var msg = messages.OrderByDescending(x => x.Value.priority)
            .ThenBy(x => x.Value.dt)
            .First();
        return (msg.Key, msg.Value.message);
    }

    public void DequeueMessage(string topic, Guid messageId)
    {
        if (!_topics.TryGetValue(topic, out var messages)) throw new KeyNotFoundException("DequeueFailed topic not found");
        if (messages.ContainsKey(messageId) && !messages.TryRemove(messageId,out _)) throw new KeyNotFoundException("DequeueFailed messageId not found");
        Console.WriteLine($"Message broker message {messageId} dequeued from topic {topic}");
        ShowStat();
    }

    private void ShowStat()
    {
        Console.WriteLine("==================");
        foreach (var topic in _topics.Keys)
        {
            Console.WriteLine($"{topic}: {_topics[topic].Count()}");
        }
        Console.WriteLine("==================");
    }
}