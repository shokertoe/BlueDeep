using System.Net.Sockets;

namespace BlueDeep.Server.Models;

public class SubscriberModel
{
    /// <summary>
    /// New subscribe model
    /// </summary>
    /// <param name="client">Tcp client</param>
    /// <param name="maxHandlers">Max concurrent handlers</param>
    public SubscriberModel(TcpClient client, int? maxHandlers)
    {
        Client = client;
        MaxHandlers = maxHandlers ?? 1;
    }

    /// <summary>
    /// Tcp client
    /// </summary>
    public TcpClient Client { get; init; }
    
    /// <summary>
    /// Max concurrent handlers by subscriber
    /// </summary>
    public int MaxHandlers { get; init; }
}