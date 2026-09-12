using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Демка/Конфиг", fileName = "DemoConfig")]
    public sealed class DemoConfig : ScriptableObject
    {
        [SerializeField] private float _maxPlayerSpeed = 5.5f;
        [SerializeField] private float _speedTolerance = SpeedGuard.DEFAULT_TOLERANCE;
        [SerializeField] private float _throwImpulse = 5f;
        [SerializeField] private int _maxPlayers = 4;

        public float MaxPlayerSpeed => _maxPlayerSpeed;
        public float SpeedTolerance => _speedTolerance;
        public float ThrowImpulse => _throwImpulse;
        public int MaxPlayers => _maxPlayers;
    }
}
