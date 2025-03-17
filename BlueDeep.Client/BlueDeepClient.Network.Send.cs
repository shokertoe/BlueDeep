using System.Text;
using System.Text.Json;
using BlueDeep.Core.Models;

namespace BlueDeep.Client
{
    public partial class BlueDeepClient
    {
        //Время повторной отправки сообщения в миллисекундах
        private const int MessageRetryPeriodMillis = 5000;
        
        /// <summary>
        /// Отправляет сообщение на сервер
        /// </summary>
        /// <param name="clientMessageModel">Модель клиентского сообщения</param>
        private async Task SendMessageAsync(BaseClientMessage clientMessageModel)
        {
            var message = JsonSerializer.Serialize(clientMessageModel);
            
            while (true)
            {
                //Показатель успешной отправки
                bool isSuccess;

                //Если сервер не доступен, то ждем до тех пор, пока не отправится
                if (!_tcpClient.Connected)
                {
                    while (!_tcpClient.Connected)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(MessageRetryPeriodMillis));
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
    }
}