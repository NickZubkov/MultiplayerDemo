using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Демка/Контроллер/PlayerMotor", fileName = "Controller_PlayerMotor")]
    public sealed class PlayerMotorProvider : AvatarControllerProvider
    {
        [SerializeField] private float _speedChangeRate = 10f;
        [SerializeField] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -15f;
        [SerializeField] private float _topClamp = 89f;
        [SerializeField] private float _bottomClamp = -89f;

        public override IAvatarController Attach(PlayerRig rig, IPlayerInput input, DemoConfig config)
        {
            var motor = rig.gameObject.AddComponent<PlayerMotor>();
            var tuning = new MotorTuning(config.WalkSpeed, config.SprintSpeed, _speedChangeRate, _jumpHeight, _gravity,
                _topClamp, _bottomClamp);

            motor.Configure(rig.View, input, tuning);
            return motor;
        }
    }
}
