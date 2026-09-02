using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using R3;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Game.Net.Ngo
{
    public sealed class NgoSessionControl : ISessionControl, IDisposable
    {
        public const ushort Port = 7777;

        private readonly NetworkManager _manager;
        private readonly ReactiveProperty<SessionState> _state = new(new SessionState(SessionPhase.Idle));

        public ReadOnlyReactiveProperty<SessionState> State => _state;

        public NgoSessionControl(NetworkManager manager)
        {
            _manager = manager;
            _manager.OnClientConnectedCallback += OnConnected;
            _manager.OnClientDisconnectCallback += OnDisconnected;
        }

        /// Отписка при уничтожении scope — то, ради чего сервисы и живут в контейнере.
        public void Dispose()
        {
            _manager.OnClientConnectedCallback -= OnConnected;
            _manager.OnClientDisconnectCallback -= OnDisconnected;
            _state.Dispose();
        }

        public UniTask StartHostAsync(string playerName, CancellationToken token)
        {
            _manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            _state.Value = _manager.StartHost()
                ? new SessionState(SessionPhase.Hosting)
                : new SessionState(SessionPhase.Failed, "Не удалось поднять хост");
            return UniTask.CompletedTask;
        }

        public async UniTask JoinAsync(HostEntry entry, CancellationToken token)
        {
            var parts = entry.JoinToken.Split(':');

            if (parts.Length != 2 || !ushort.TryParse(parts[1], out var port))
            {
                _state.Value = new SessionState(SessionPhase.Failed, $"Непонятный адрес: {entry.JoinToken}");
                return;
            }

            _manager.GetComponent<UnityTransport>().SetConnectionData(parts[0], port);
            _state.Value = new SessionState(SessionPhase.Connecting);

            if (!_manager.StartClient())
            {
                _state.Value = new SessionState(SessionPhase.Failed, "Не удалось начать подключение");
                return;
            }

            /// Таймаут живёт здесь, а не в презентере: молчаливое зависание в лобби —
            /// худший вид ошибки в сетевом приложении.
            try
            {
                await _state.Where(s => s.Phase != SessionPhase.Connecting)
                            .Timeout(TimeSpan.FromSeconds(5))
                            .FirstAsync(token);
            }
            catch (TimeoutException)
            {
                _state.Value = new SessionState(SessionPhase.Failed, "Хост не ответил за 5 секунд");
            }
        }

        public UniTask LeaveAsync()
        {
            _manager.Shutdown();
            _state.Value = new SessionState(SessionPhase.Idle);
            return UniTask.CompletedTask;
        }

        private void OnConnected(ulong clientId)
        {
            if (clientId != _manager.LocalClientId) return;
            _state.Value = new SessionState(SessionPhase.Connected);
        }

        private void OnDisconnected(ulong clientId)
        {
            if (clientId != _manager.LocalClientId) return;

            var reason = _manager.DisconnectReason;
            _state.Value = new SessionState(SessionPhase.Failed,
                string.IsNullOrEmpty(reason) ? "Хост недоступен или отключился" : reason);
        }
    }
}
