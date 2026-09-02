using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Game.Core
{
    /// ExclusiveAddressUse = false обязателен: без него второй инстанс на том же ПК
    /// не займёт порт — то есть ровно наш сценарий «два окна на одной машине».
    public sealed class LanBeaconSocket : IDisposable
    {
        public const int DefaultPort = 47777;

        private readonly UdpClient _socket;
        private readonly IPEndPoint _broadcast;

        public LanBeaconSocket(int port = DefaultPort)
        {
            _socket = new UdpClient();
            _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.ExclusiveAddressUse = false;
            _socket.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            _socket.EnableBroadcast = true;
            _broadcast = new IPEndPoint(IPAddress.Broadcast, port);
        }

        public void Dispose() => _socket?.Dispose();

        public void Send(string payload)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            _socket.Send(bytes, bytes.Length, _broadcast);
        }

        /// Неблокирующее чтение: вызывать, пока возвращает true.
        public bool TryReceive(out string payload)
        {
            payload = null;
            if (_socket.Available <= 0) return false;

            var from = new IPEndPoint(IPAddress.Any, 0);
            payload = Encoding.UTF8.GetString(_socket.Receive(ref from));
            return true;
        }
    }
}
