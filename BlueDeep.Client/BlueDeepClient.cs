using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BlueDeep.Core.Models;

namespace BlueDeep.Client
{
    public class BlueDeepClient : IDisposable
    {
        private readonly TcpClient _tcpClient = new();
        private readonly string _serverAddress;
        private readonly int _serverPort;
        private const int ConnectionRetryPeriodMillis = 5;

        private static readonly SemaphoreSlim SemaphoreSlim = new(1, 1);

        // Потокобезопасный словарь для хранения обработчиков подписок
        private readonly ConcurrentDictionary<string, Action<string>> _subscriptionHandlers = new();

        public BlueDeepClient(string serverAddress, int serverPort)
        {
            _serverAddress = serverAddress;
            _serverPort = serverPort;
        }

        public async Task ConnectAsync()
        {
            await _tcpClient.ConnectAsync(_serverAddress, _serverPort);
            _ = Task.Run(async () => await ReceiveMessagesAsync()); // Запуск фоновой задачи для чтения сообщений
        }

        private async Task ReConnectAsync()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine($"Reconnecting to {_serverAddress}:{_serverPort}");
                    await _tcpClient.ConnectAsync(_serverAddress, _serverPort);
                    Console.WriteLine($"Connected to {_serverAddress}:{_serverPort}");
                    
                    // Запуск фоновой задачи для чтения сообщений
                    _ = Task.Run(async () =>
                        await ReceiveMessagesAsync()); 
                    
                    //Перерегистрируем подписчиков на сервере
                    foreach (var subscriptionHandler in _subscriptionHandlers)
                    {
                        await SendSubscriptionMessageAsync(subscriptionHandler.Key);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reconnection failed {ex.Message}. Pause {ConnectionRetryPeriodMillis} seconds");
                    await Task.Delay(TimeSpan.FromSeconds(ConnectionRetryPeriodMillis));
                }
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            Console.WriteLine($"Socket listen started...");
            var stream = _tcpClient.GetStream();
            var buffer = new byte[4];

            while (_tcpClient.Connected)
            {
                //Проверка на отсоединение
                // если принято 0 байт значит произошел разрыв
                var bytesRead = _tcpClient.Client.Receive(buffer);
                if (bytesRead == 0)
                {
                    Console.WriteLine($"Socket disconnected");
                    await _tcpClient.Client.DisconnectAsync(true);
                    break;
                }

                //Заглушка на всякий случай
                if (bytesRead != 4)
                {
                    Console.WriteLine($"Socket recieved {bytesRead} bytes.Not proceeded");
                    continue;
                }

                try
                {
                    // Чтение длины сообщения
                    var messageLength = BitConverter.ToInt32(buffer, 0);

                    // Чтение самого сообщения
                    var messageBuffer = new byte[messageLength];
                    await stream.ReadExactlyAsync(messageBuffer, 0, messageLength);
                    var message = Encoding.UTF8.GetString(messageBuffer);

                    // Разбор сообщения
                    var messageObj = JsonSerializer.Deserialize<ServerMessage>(message) ??
                                     throw new NullReferenceException("Recieved message from BlueDeepServer is null");

                    // Вызов обработчика, если он зарегистрирован для топика
                    if (_subscriptionHandlers.TryGetValue(messageObj.Topic, out var handler))
                    {
                        handler(messageObj.Data);
                    }

                    // Отправка подтверждения обработки сообщения
                    var ackMessage = new ClientMessage
                    {
                        Type = "ack", 
                        Topic = messageObj.Topic,
                        Id = messageObj.Id
                    };
                    
                    var ackJson = JsonSerializer.Serialize(ackMessage);
                    await SendMessageAsync(ackJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                }
            }

            Console.WriteLine($"Connection to BlueDeepServer failed.");
            await ReConnectAsync();
        }

        public async Task PushAsync<T>(string topic, T message, int priority = 0) where T : class
        {
            var messageObj = new ClientMessage
            {
                Type = "publish",
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Topic = topic,
                Data = JsonSerializer.Serialize(message),
                Priority = priority
            };
            var jsonMessage = JsonSerializer.Serialize(messageObj);
            await SendMessageAsync(jsonMessage);
        }

        public async Task SubscribeAsync<T>(string topic, Action<T> handler) where T : class
        {
            if (_subscriptionHandlers.TryGetValue(topic, out _))
            {
                throw new Exception("Subscription on topic already exists");
            }

            // Регистрация обработчика для топика
            _subscriptionHandlers[topic] = (message) =>
            {
                var receivedMessage = JsonSerializer.Deserialize<T>(message) ??
                                      throw new Exception("Received message was null");
                handler(receivedMessage);
            };

            await SendSubscriptionMessageAsync(topic);
        }

        private async Task SendSubscriptionMessageAsync(string topic)
        {
            var messageObj = new ClientMessage
            {
                Type = "subscribe",
                Topic = topic,
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
            };
            var jsonMessage = JsonSerializer.Serialize(messageObj);
            await SendMessageAsync(jsonMessage);
            Console.WriteLine($"Subscription on topic {topic} was sent");
        }

        private async Task SendMessageAsync(string message)
        {
            while (true)
            {
                //Показатель успешной отправки
                bool isSuccess;
                
                //Если сервер не доступен, то ждем до тех пор, пока не отправится
                if (!_tcpClient.Connected)
                {
                    while (!_tcpClient.Connected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(ConnectionRetryPeriodMillis));
                    }
                }

                await SemaphoreSlim.WaitAsync();
                try
                {
                    var stream = _tcpClient.GetStream();
                    var data = Encoding.UTF8.GetBytes(message);
                    var length = BitConverter.GetBytes(data.Length);
                    await stream.WriteAsync(length.AsMemory(0, 4)); // Отправка длины сообщения
                    await stream.WriteAsync(data); // Отправка сообщения
                    isSuccess = true;
                }
                finally
                {
                    SemaphoreSlim.Release();
                }

                if (isSuccess) return;
            }
        }

        public void Dispose()
        {
            _tcpClient.Dispose();
        }
    }
}