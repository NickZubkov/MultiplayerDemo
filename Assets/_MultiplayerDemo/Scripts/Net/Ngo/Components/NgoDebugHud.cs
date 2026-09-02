using Cysharp.Threading.Tasks;
using Game.App;
using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Net.Ngo
{
    /// ВРЕМЕННЫЙ. Существует только потому, что кнопки лобби подключит к сессии
    /// LobbyPresenter из задачи 11, а проверить спавн и владение нужно уже сейчас.
    /// Удаляется в задаче 11 вместе со своим объектом на сцене Net_Ngo.
    ///
    /// Ходит через ISessionControl, а не напрямую в NetworkManager: так проверяется
    /// наш код — установка адреса, StartHost, обработка колбэков и переходы состояния.
    public sealed class NgoDebugHud : MonoBehaviour
    {
        private const string ArenaScene = "Arena";

        /// Порт берём у самой сессии, чтобы отладка не разъехалась с боевым кодом.
        private static readonly string LocalAddress = "127.0.0.1:" + NgoSessionControl.Port;

        private ISessionControl _session;

        [Inject]
        public void Construct(ISessionControl session) => _session = session;

        private void OnGUI()
        {
            if (_session == null) return;

            var state = _session.State.CurrentValue;

            GUILayout.BeginArea(new Rect(12f, 12f, 280f, 170f), GUI.skin.box);
            GUILayout.Label("Отладка NGO — временный HUD");
            GUILayout.Label("Состояние: " + state.Phase + (string.IsNullOrEmpty(state.Reason) ? "" : " — " + state.Reason));

            if (state.Phase == SessionPhase.Idle || state.Phase == SessionPhase.Failed)
            {
                if (GUILayout.Button("Поднять хост")) HostAsync().Forget();
                if (GUILayout.Button("Подключиться к " + LocalAddress)) JoinAsync().Forget();
            }
            else if (GUILayout.Button("Отключиться"))
            {
                _session.LeaveAsync().Forget();
            }

            GUILayout.EndArea();
        }

        /// Арену грузим до старта сессии: NGO спавнит игрока сразу в StartHost,
        /// и без пола он улетает вниз — проверено, Y уходит в минус тысячи.
        private async UniTaskVoid HostAsync()
        {
            await SceneFlow.LoadAdditiveAsync(ArenaScene, destroyCancellationToken);
            await _session.StartHostAsync("Хост", destroyCancellationToken);
        }

        private async UniTaskVoid JoinAsync()
        {
            await SceneFlow.LoadAdditiveAsync(ArenaScene, destroyCancellationToken);
            await _session.JoinAsync(new HostEntry("локальный", 0, 0, "ngo", LocalAddress), destroyCancellationToken);
        }
    }
}
