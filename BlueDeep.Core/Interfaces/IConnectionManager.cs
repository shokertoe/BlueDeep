using BlueDeep.Core.Models;

namespace BlueDeep.Core.Interfaces;

/// <summary>
/// Интерфейс для менеджера соединений, отвечающего за управление клиентами и рассылку сообщений.
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Запускает прослушивание входящих соединений на указанном порте.
    /// </summary>
    /// <param name="port">Порт, на котором будет происходить прослушивание.</param>
    void StartListening(int port);

    /// <summary>
    /// Останавливает прослушивание входящих соединений.
    /// </summary>
    void StopListening();

    /// <summary>
    /// Рассылка сообщения всем подписанным клиентам.
    /// </summary>
    /// <param name="clientMessage">Сообщение для рассылки.</param>
    void Broadcast(ClientMessage clientMessage);

    /// <summary>
    /// Удаляет клиента из списка подписчиков указанного топика.
    /// </summary>
    /// <param name="topic">Топик, из которого удаляется клиент.</param>
    /// <param name="clientId">Идентификатор клиента.</param>
    void RemoveSubscriber(string topic, Guid clientId);

    /// <summary>
    /// Проверяет наличие подписчиков на указанный топик.
    /// </summary>
    /// <param name="topic">Топик, для которого проверяются подписчики.</param>
    /// <returns>true, если есть хотя бы один подписчик, иначе false.</returns>
    bool HasSubscribers(string topic);
}