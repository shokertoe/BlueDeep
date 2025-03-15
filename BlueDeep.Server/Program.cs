using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BlueDeep.Core.Models;

namespace BlueDeep.Server
{
    static class Program
    {
        // Хранит подписчиков для каждого топика
        private static readonly ConcurrentDictionary<string, ConcurrentBag<TcpClient>> TopicSubscribers = new();
        private static readonly MessageBroker MessageBroker = new();
        
        static async Task Main(string[] args)
        {
            var listener = new TcpListener(IPAddress.Any, 9090);
            listener.Start();
            Console.WriteLine("Server started on tcp://localhost:9090/");

            // Запуск обработки сообщений в отдельном потоке
            _ = Task.Run(async () =>await ProcessMessagesAsync());
            Console.WriteLine("ProcessMessagesAsync started");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client); // Обработка клиента в отдельном потоке
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[4]; // Буфер для длины сообщения
            try
            {
                while (client.Connected)
                {
                    // Чтение длины сообщения
                    await stream.ReadExactlyAsync(buffer, 0, 4);
                    var messageLength = BitConverter.ToInt32(buffer, 0);

                    // Чтение самого сообщения
                    var messageBuffer = new byte[messageLength];
                    await stream.ReadExactlyAsync(messageBuffer, 0, messageLength);
                    var message = Encoding.UTF8.GetString(messageBuffer);

                    var messageObj = JsonSerializer.Deserialize<ClientMessage>(message);

                    switch (messageObj?.Type)
                    {
                        case "subscribe":
                        {
                            // Добавление клиента в подписчики топика
                            if (!TopicSubscribers.ContainsKey(messageObj.Topic))
                            {
                                TopicSubscribers[messageObj.Topic] = [];
                            }
                            TopicSubscribers[messageObj.Topic].Add(client);
                            Console.WriteLine($"Client subscribed to {messageObj.Topic}");
                            break;
                        }
                        case "publish":
                            // Добавление сообщения в очередь
                            MessageBroker.Enqueue(messageObj);
                            Console.WriteLine($"Message with id={messageObj.Id} published to topic={messageObj.Topic}");
                            break;
                        default:
                            Console.WriteLine($"Unknown type {messageObj?.Type} with id={messageObj?.Id} was recieved.");
                            break;
                    }
                }
                
                Console.WriteLine("Client disconnected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Удаление клиента из всех топиков при отключении
                RemoveClientFromAllTopics(client);
                client.Dispose();
            }
        }

        private static void RemoveClientFromAllTopics(TcpClient client)
        {
            foreach (var topic in TopicSubscribers.Keys)
            {
                if (!TopicSubscribers.TryGetValue(topic, out var subscribers)) continue;
                
                var newSubscribers = new ConcurrentBag<TcpClient>(subscribers.Where(s => s != client));
                TopicSubscribers[topic] = newSubscribers;
            }
            Console.WriteLine($"Client removed from topics");
        }

        private static Task ProcessMessagesAsync()
        {
            while (true)
            {
                try
                {
                    foreach (var topic in TopicSubscribers.Where(x => !x.Value.IsEmpty).Select(x => x.Key))
                    {
                        var messageObject = MessageBroker.GetMessage(topic);
                        if (messageObject is null) continue;
                        var message = messageObject.Value.message;
                        var messageId = messageObject.Value.id;
                        var count = 0;
                        if (TopicSubscribers.TryGetValue(topic, out var subscribers))
                        {
                            var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new ServerMessage(topic, message, messageId)));
                            var length = BitConverter.GetBytes(data.Length);

                            foreach (var subscriber in subscribers)
                            {
                                if (subscriber.Connected)
                                {
                                    var stream = subscriber.GetStream(); 
                                    stream.Write(length, 0, 4); // Отправка длины сообщения
                                    stream.Write(data); // Отправка сообщения
                                    count++;
                                }
                                else
                                {
                                    RemoveClientFromAllTopics(subscriber);
                                }
                            }
                        }

                        if (count != 0) 
                            MessageBroker.DequeueMessage(topic, messageId);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}