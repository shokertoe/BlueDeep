﻿using System.Text;
using System.Text.Json;
using BlueDeep.Core.Enums;
using BlueDeep.Core.Models;

namespace BlueDeep.Client
{
    public partial class BlueDeepClient
    {
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
                    Console.WriteLine($"Socket received {bytesRead} bytes.Not proceeded");
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
                                     throw new NullReferenceException("Received message from BlueDeepServer is null");

                    // Вызов обработчика, если он зарегистрирован для обработки топика
                    if (_subscriptionHandlers.TryGetValue(messageObj.Topic, out var handler))
                    {
                        try
                        {
                            handler(messageObj.Data);
                            await SendMessageAsync(new AckMessage(messageObj.Id, MessageStatus.Ok));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error proceeding message: {ex.Message}");
                            await SendMessageAsync(new AckMessage(messageObj.Id, MessageStatus.Failed));
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                }
            }

            Console.WriteLine($"Connection to BlueDeepServer failed.");
            await ReConnectAsync();
        }
    }
}