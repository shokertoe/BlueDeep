using System.Collections.Concurrent;
using System.Net.Sockets;

namespace BlueDeep.Server.Store;

public class TopicSubscribersBag : ConcurrentDictionary<string, ConcurrentBag<TcpClient>>
{
    public TopicSubscribersBag()
    {
        
    }
}