using Game.Core;
using Mirror;
using UnityEngine;
using VContainer;

namespace Game.Net.Mirror
{
    /// Сервер принимает позицию от владельца, но сверяет её с максимально возможной
    /// (решение D4). Своего персонажа хост не проверяет: он и так авторитет.
    ///
    /// Алгоритм тот же, что у NgoSpeedGuard, и сверяется так же только горизонтальное
    /// смещение: вертикаль в порог не влезает — спринт вместе с приземлением после прыжка
    /// на цифрах Starter Assets даёт 8.49 при пороге 9. Цена названа честно: вертикальный
    /// полёт проверка не ловит, и это идёт в README признанным пробелом.
    [RequireComponent(typeof(CharacterController))]
    public sealed class MirrorSpeedGuard : NetworkBehaviour
    {
        /// Пауза после возврата. Поправка доедет до клиента и вернётся обратно только
        /// через RTT, а до тех пор сервер видит всё ту же старую позицию и отклонял бы
        /// её заново каждый физический шаг — вместо одного возврата вышла бы очередь.
        private const double CORRECTION_GRACE = 0.5;

        private CharacterController _controller;
        private DemoConfig _config;
        private IHudMessages _hud;
        private IClock _clock;

        private Vector3 _lastAccepted;
        private double _lastCheck;
        private double _resumeAt;

        [Inject]
        public void Construct(DemoConfig config, IHudMessages hud, IClock clock)
        {
            _config = config;
            _hud = hud;
            _clock = clock;
        }

        private void Awake() => _controller = GetComponent<CharacterController>();

        public override void OnStartServer()
        {
            _lastAccepted = transform.position;
            _lastCheck = _clock.Now;
        }

        private void FixedUpdate()
        {
            if (!isServer || isOwned) return;

            var now = _clock.Now;
            var elapsed = (float)(now - _lastCheck);
            _lastCheck = now;

            if (now < _resumeAt) return;

            var moved = HorizontalDistance(transform.position, _lastAccepted);

            if (SpeedGuard.IsPlausible(moved, elapsed, _config.MaxPlayerSpeed, _config.SpeedTolerance))
            {
                _lastAccepted = transform.position;
                return;
            }

            TargetTeleport(connectionToClient, _lastAccepted);
            _hud.Show("Подозрительное перемещение отклонено");
            _resumeAt = now + CORRECTION_GRACE;
        }

        /// Возврат делает сам владелец: трансформ у него авторитетный (syncDirection
        /// ClientToServer), и попытка сервера подвинуть капсулу была бы перезаписана
        /// следующим же пакетом от клиента.
        [TargetRpc]
        private void TargetTeleport(NetworkConnection target, Vector3 position)
        {
            /// CharacterController на своём шаге перезапишет transform.position,
            /// поэтому на время подстановки его приходится выключать.
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }
    }
}
