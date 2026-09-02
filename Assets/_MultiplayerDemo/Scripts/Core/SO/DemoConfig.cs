using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Демка/Конфиг", fileName = "DemoConfig")]
    public sealed class DemoConfig : ScriptableObject
    {
        [SerializeField] private float maxPlayerSpeed = 5.5f;
        [SerializeField] private float speedTolerance = SpeedGuard.DefaultTolerance;
        [SerializeField] private float throwImpulse = 5f;
        [SerializeField] private int maxPlayers = 4;

        public float MaxPlayerSpeed => maxPlayerSpeed;
        public float SpeedTolerance => speedTolerance;
        public float ThrowImpulse => throwImpulse;
        public int MaxPlayers => maxPlayers;
    }
}
