namespace BlueDeep.Core.Enums;

/// <summary>
/// Тип сообщений от клиента серверу
/// </summary>
public enum ClientMessageType
{
    Subscribe = 1, 
    Publish = 2,
    Ack = 3,
}