using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Демка/Конфиг", fileName = "DemoConfig")]
    public sealed class DemoConfig : ScriptableObject
    {
        /// Скорости ходьбы живут только здесь: их берут оба провайдера контроллера,
        /// и от спринта же считается порог анти-телепорта (Ц16, И-6).
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 6f;
        [SerializeField] private float _speedTolerance = SpeedGuard.DEFAULT_TOLERANCE;
        [SerializeField] private float _throwImpulse = 5f;
        [SerializeField] private int _maxPlayers = 4;

        public float WalkSpeed => _walkSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float SpeedTolerance => _speedTolerance;
        public float ThrowImpulse => _throwImpulse;
        public int MaxPlayers => _maxPlayers;
    }
}
