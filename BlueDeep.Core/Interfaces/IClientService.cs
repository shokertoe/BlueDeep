using BlueDeep.Core.Models;

namespace BlueDeep.Core.Interfaces;

/// <summary>
/// Интерфейс для клиентского сервиса, обеспечивающего взаимодействие с сервером.
/// </summary>
public interface IClientService
{
    /// <summary>
    /// Асинхронно устанавливает соединение с сервером по указанному IP-адресу и порту.
    /// </summary>
    /// <param name="ip">IP-адрес сервера.</param>
    /// <param name="port">Порт сервера.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task ConnectAsync(string ip, int port);

    /// <summary>
    /// Асинхронно закрывает текущее соединение с сервером.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task DisconnectAsync();

    /// <summary>
    /// Проверяет, установлено ли текущее соединение с сервером.
    /// </summary>
    /// <returns>true, если соединение активно, иначе false.</returns>
    Task<bool> IsConnected();

    /// <summary>
    /// Асинхронно публикует сообщение в указанном топике.
    /// </summary>
    /// <typeparam name="T">Тип данных сообщения.</typeparam>
    /// <param name="topic">Топик, в который будет опубликовано сообщение.</param>
    /// <param name="data">Данные сообщения.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task PublishAsync<T>(string topic, T data);

    /// <summary>
    /// Подписывается на получение сообщений из указанного топика.
    /// </summary>
    /// <typeparam name="T">Тип данных сообщения.</typeparam>
    /// <param name="topic">Топик, на который осуществляется подписка.</param>
    /// <param name="handler">Обработчик, который будет вызван при получении сообщения.</param>
    /// <returns>Объект, позволяющий отменить подписку.</returns>
    IDisposable Subscribe<T>(string topic, Action<T> handler);
}
