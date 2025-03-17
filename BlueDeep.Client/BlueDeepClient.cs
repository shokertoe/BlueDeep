using System.Collections.Concurrent;
using System.Text.Json;
using BlueDeep.Core.Enums;
using BlueDeep.Core.Models;

namespace BlueDeep.Client
{
    public partial class BlueDeepClient : IDisposable
    {
        // Потокобезопасный словарь для хранения обработчиков подписок
        private readonly ConcurrentDictionary<string, Action<string>> _subscriptionHandlers = new();

        public BlueDeepClient(string serverAddress, int serverPort)
        {
            _serverAddress = serverAddress;
            _serverPort = serverPort;
            //При создании экземпляра сразу пытаемся подключиться
            ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Отправка сообщения типа Т на сервер
        /// </summary>
        /// <param name="topicName">Название топика</param>
        /// <param name="message">Сообщение типа Т</param>
        /// <param name="priority">Приоритет (по умолчанию низкий)</param>
        /// <typeparam name="T">Тип передаваемого сообщения</typeparam>
        public async Task PublishAsync<T>(string topicName, T message, MessagePriority priority = MessagePriority.Low) 
            where T : class
        {
            var publishMessage = new  PublishMessage<T>(topicName, priority, message);
            await SendMessageAsync(publishMessage);
        }

        /// <summary>
        /// Подписка на появление сообщения в топике
        /// </summary>
        /// <param name="topic">Название топика</param>
        /// <param name="handler">Выполняемое действие</param>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <exception cref="Exception">Ошибка создания подписки на топик</exception>
        public async Task SubscribeAsync<T>(string topic, Action<T> handler) where T : class
        {
            if (_subscriptionHandlers.TryGetValue(topic, out _))
            {
                throw new Exception("Subscription on topic already exists");
            }

            // Регистрация обработчика для обработки сообщения топика
            _subscriptionHandlers[topic] = (message) =>
            {
                var receivedMessage = JsonSerializer.Deserialize<T>(message) ??
                                      throw new Exception("Received message was null");
                handler(receivedMessage);
            };

            await SendMessageAsync(new SubscribeMessage(topic));
        }
        
        public void Dispose()
        {
            _tcpClient.Dispose();
        }
    }
}