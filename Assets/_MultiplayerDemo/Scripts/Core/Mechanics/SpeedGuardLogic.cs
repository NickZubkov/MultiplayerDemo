using UnityEngine;

namespace Game.Core
{
    /// Анти-телепорт одного чужого аватара у судьи (Ц14). Правило — SpeedGuard, здесь то,
    /// что раньше жило в двух сетевых компонентах и не было покрыто ничем: база, пауза после
    /// возврата и что считать возвратом.
    ///
    /// Возврат исполняет сам владелец, поэтому честность одинакова на всех стеках: нечестный
    /// клиент может проигнорировать поправку — и получит её снова на первой же проверке.
    public sealed class SpeedGuardLogic
    {
        private readonly float _maxSpeed;
        private readonly float _tolerance;
        private readonly double _window;
        private readonly double _grace;

        private Vector3 _lastAccepted;
        private double _lastCheck;
        private double _correctedAt;
        private bool _awaitingReturn;

        public SpeedGuardLogic(float maxSpeed, float tolerance, double window, double grace)
        {
            _maxSpeed = maxSpeed;
            _tolerance = tolerance;
            _window = window;
            _grace = grace;
        }

        public void Reset(Vector3 position, double now)
        {
            _lastAccepted = position;
            _lastCheck = now;
            _awaitingReturn = false;
        }

        /// true — перемещение неправдоподобно, владельца надо вернуть в returnTo.
        ///
        /// Скорость меряется на окне, а не между соседними кадрами: поза чужого игрока приходит
        /// по сети порциями, и на шаге физики каждая порция выглядит рывком сверх любого порога.
        /// У Fusion это видно лучше всего — поза едет через облако, — но окно нужно всем: оно
        /// отделяет дрожание доставки от честно задранной скорости, которая за полсекунды
        /// набирает лишние метры и никуда не девается.
        public bool TryCorrect(Vector3 position, double now, out Vector3 returnTo)
        {
            returnTo = _lastAccepted;

            var elapsed = (float)(now - _lastCheck);
            if (elapsed < _window) return false;

            _lastCheck = now;

            if (_awaitingReturn)
            {
                if (now < _correctedAt + _grace) return false;

                /// Пауза кончилась. Вернувшийся владелец мог пройти от точки возврата не больше,
                /// чем позволяет скорость за всё время с поправки, — такую позицию берём новой
                /// базой (И-8). Дальше — значит, возврат проигнорирован.
                _awaitingReturn = false;
                var sinceCorrection = (float)(now - _correctedAt);

                if (IsPlausible(position, sinceCorrection))
                {
                    _lastAccepted = position;
                    return false;
                }

                Correct(now);
                return true;
            }

            if (IsPlausible(position, elapsed))
            {
                _lastAccepted = position;
                return false;
            }

            Correct(now);
            return true;
        }

        private void Correct(double now)
        {
            _correctedAt = now;
            _awaitingReturn = true;
        }

        /// Сверяется только горизонталь: прыжок в беге с приземлением не влезает в порог (D4).
        private bool IsPlausible(Vector3 position, float elapsed)
        {
            var from = _lastAccepted;
            from.y = 0f;
            position.y = 0f;

            return SpeedGuard.IsPlausible(Vector3.Distance(from, position), elapsed, _maxSpeed, _tolerance);
        }
    }
}
