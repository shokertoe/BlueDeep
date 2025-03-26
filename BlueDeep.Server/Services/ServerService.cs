using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BlueDeep.Core.DataModels;
using BlueDeep.Core.Enums;
using BlueDeep.Core.Models;
using BlueDeep.Server.Broker;
using BlueDeep.Server.Processors;
using BlueDeep.Server.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlueDeep.Server.Services;

public class ServerService : BackgroundService
{
    private readonly ILogger<ServerService> _logger;

    // Хранит подписчиков для каждого топика
    private readonly TopicSubscribersBag _topicSubscribers;
    private readonly MessageBroker _messageBroker;
    private readonly SubscribeProcessor _subscribeProcessor;
    private readonly PublishProcessor _publishProcessor;

    public ServerService(ILogger<ServerService> logger,
        MessageBroker messageBroker,
        SubscribeProcessor subscribeProcessor,
        TopicSubscribersBag topicSubscribers,
        PublishProcessor publishProcessor,
        IConfiguration configuration)
    {
        _logger = logger;
        _topicSubscribers = new();
        _messageBroker = messageBroker;
        _subscribeProcessor = subscribeProcessor;
        _topicSubscribers = topicSubscribers;
        _publishProcessor = publishProcessor;
        _logger.LogInformation("Port: {Port}", configuration["Port"]);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any,9090);
        listener.Start();
        _logger.LogInformation("Сервер запущен по адрес tcp://localhost:9090/");

        // Запуск обработки сообщений в отдельном потоке
        _ = Task.Run(async () => await ProcessMessagesAsync(), stoppingToken);

        _logger.LogInformation("Поток обработки входящих сообщений запущен");

        await Task.Run(async () =>
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client); // Обработка клиента в отдельном потоке
            }
        }, stoppingToken);
    }

    private async Task HandleClientAsync(TcpClient client)
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
                if (messageObj is null)
                {
                    _logger.LogWarning($"Получена неизвестная модель данных от {client.Client.RemoteEndPoint}");
                    continue;
                }

                switch (messageObj.MessageType)
                {
                    case ClientMessageType.Subscribe:
                    {
                        var subscribeData = JsonSerializer.Deserialize<MessageSubscribeModel>(messageObj.MessageData);
                        if (subscribeData is null)
                        {
                            _logger.LogError("Модель данных Subscribe не может быть получена");
                            break;
                        }

                        _subscribeProcessor.ProcessMessage(subscribeData, client);
                        break;
                    }

                    case ClientMessageType.Publish:
                        var publishData = JsonSerializer.Deserialize<MessagePublishModel>(messageObj.MessageData);
                        if (publishData is null)
                        {
                            _logger.LogError("Модель данных Publish не может быть получена");
                            break;
                        }

                        _publishProcessor.ProcessMessage(publishData, client);
                        break;

                    case ClientMessageType.Ack:
                        _logger.LogWarning($"Unknown type {messageObj?.MessageType}  was received.");
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

    private void RemoveClientFromAllTopics(TcpClient client)
    {
        foreach (var topic in _topicSubscribers.Keys)
        {
            if (!_topicSubscribers.TryGetValue(topic, out var subscribers)) continue;

            var newSubscribers = new ConcurrentBag<TcpClient>(subscribers.Where(s => s != client));
            _topicSubscribers[topic] = newSubscribers;
        }

        Console.WriteLine($"Client removed from topics");
    }

    private Task ProcessMessagesAsync()
    {
        while (true)
        {
            try
            {
                //проходим по всем топикам, у кого есть подписчики
                foreach (var topic in _topicSubscribers.Where(x => !x.Value.IsEmpty).Select(x => x.Key))
                {
                    //Получаем сообщение из брокера сообщений
                    var messageObject = _messageBroker.GetMessage(topic);
                    //Если нет сообщений в топике, то ничего не делаем
                    if (messageObject is null) continue;


                    var message = messageObject.Data;
                    var messageId = messageObject.Id;

                    var count = 0; //счетчик, который показывает, сколько раз сообщение было успешно доставлено
                    if (_topicSubscribers.TryGetValue(topic, out var subscribers))
                    {
                        var data = Encoding.UTF8.GetBytes(
                            JsonSerializer.Serialize(new ServerMessage(topic, message, messageId)));
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
                        _messageBroker.DequeueMessage(topic, messageId);
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