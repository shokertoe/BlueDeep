using BlueDeep.Core.Models;

namespace BlueDeep.Client
{
    public partial class BlueDeepClient
    {
        //Время повторной попытки подключения
        private const int ConnectionRetryPeriodMillis = 5000;
        
        /// <summary>
        /// Соединение  сервером
        /// </summary>
        private async Task ConnectAsync()
        {
            //Прячем ошибки подключения, потому что мы пытаемся подключиться при создании экземпляра
            try
            {
                await _tcpClient.ConnectAsync(_serverAddress, _serverPort);
            }
            catch
            {
                Console.WriteLine($"Ошибка инициализации подключения к серверу");
            }
            if (!_tcpClient.Connected) await ReConnectAsync();
            _ = Task.Run(async () => await ReceiveMessagesAsync()); // Запуск фоновой задачи для чтения сообщений
        }

        /// <summary>
        /// Восстановление соединения с сервером
        /// </summary>
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
                        await SendMessageAsync(new SubscribeMessage(subscriptionHandler.Key));
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reconnection failed {ex.Message}. Pause {ConnectionRetryPeriodMillis} seconds");
                    await Task.Delay(TimeSpan.FromMilliseconds(ConnectionRetryPeriodMillis));
                }
            }
        }
    }
}