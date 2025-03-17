using System.Net.Sockets;

namespace BlueDeep.Client
{
    public partial class BlueDeepClient
    {
        private readonly TcpClient _tcpClient = new();
        private readonly string _serverAddress;
        private readonly int _serverPort;
        
        //Семафор для асинхронных методов отправки сообщений на сервер
        private static readonly SemaphoreSlim SemaphoreSlim = new(1, 1);
    }
}