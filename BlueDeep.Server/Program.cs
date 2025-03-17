using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BlueDeep.Core.DataModels;
using BlueDeep.Core.Enums;
using BlueDeep.Core.Models;

namespace BlueDeep.Server
{
    static class Program
    {
        // Хранит подписчиков для каждого топика
        private static readonly ConcurrentDictionary<string, ConcurrentBag<TcpClient>> TopicSubscribers = new();
        private static readonly MessageBroker MessageBroker = new();

        private static Task Main()
        {
            //TODO выбирать порт из конфигурации
            var listener = new TcpListener(IPAddress.Any, 9090);
            listener.Start();
            Console.WriteLine("Сервер запущен по адрес tcp://localhost:9090/");

            // Запуск обработки сообщений в отдельном потоке
            _ = Task.Run(async () =>await ProcessMessagesAsync());
            Console.WriteLine("Поток обработки входящих сообщений запущен");
            Task.Run(async () =>
            {
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client); // Обработка клиента в отдельном потоке
                }
            });
            
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();

            return Task.CompletedTask;
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

                    var messageObj = JsonSerializer.Deserialize<BaseClientMessage>(message);

                    switch (messageObj?.MessageType)
                    {
                        case ClientMessageType.Subscribe:
                        {
                            var subscribeData = JsonSerializer.Deserialize<MessageSubscribeModel>(messageObj.MessageData);
                            if (subscribeData is null)
                            {
                                Console.WriteLine("Модель данных Subscribe не может быть получена");
                                break;
                            }
                            
                            // Добавление клиента в подписчики топика
                            var topicName = subscribeData.TopicName;
                            if (!TopicSubscribers.ContainsKey(topicName))
                            {
                                TopicSubscribers[topicName] = [];
                            }
                            TopicSubscribers[topicName].Add(client);
                            Console.WriteLine($"Client subscribed to {topicName}");
                            break;
                        }
                        case ClientMessageType.Publish:
                            var publishData = JsonSerializer.Deserialize<MessagePublishModel>(messageObj.MessageData);
                            if (publishData is null)
                            {
                                Console.WriteLine("Модель данных Publish не может быть получена");
                                break;
                            }
                            // Добавление сообщения в очередь
                            MessageBroker.Enqueue(publishData);
                            Console.WriteLine($"Message was published to topic={publishData.TopicName}");
                            break;
                        default:
                            Console.WriteLine($"Unknown type {messageObj?.MessageType}  was received.");
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
                    //проходим по всем топикам, у кого есть подписчики
                    foreach (var topic in TopicSubscribers.Where(x => !x.Value.IsEmpty).Select(x => x.Key))
                    {
                        //Получаем сообщение из брокера сообщений
                        var messageObject = MessageBroker.GetMessage(topic);
                        //Если нет сообщений в топике, то ничего не делаем
                        if (messageObject is null) continue;
                        
                        
                        var message = messageObject.Data;
                        var messageId = messageObject.Id;
                        
                        var count = 0; //счетчик, который показывает, сколько раз сообщение было успешно доставлено
                        if (TopicSubscribers.TryGetValue(topic, out var subscribers))
                        {
                            var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new ServerMessage(topic, message, messageId)));
                            var length = BitConverter.GetBytes(data.Length);
                            
                            //Отправляем сообщение каждому подписчику топика
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
                        
                        //если хотя бы кому-то было доставлено, то норм и убираем сообщение из очереди брокера
                        if (count != 0) 
                            MessageBroker.DequeueMessage(topic, messageId);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Неизвестная ошибка в потоке доставки сообщений");
                    throw;
                }
            }
        }
    }
}