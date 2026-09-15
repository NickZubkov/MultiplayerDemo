using Game.Core;
using Game.Gameplay;
using StarterAssets;
using UnityEngine;

namespace Game.Integrations
{
    /// Второй провайдер (Ц3). Лежит в Assembly-CSharp — иначе StarterAssets не видно:
    /// у них нет asmdef. Это единственное исключение из правила «всё своё — в сборках».
    [CreateAssetMenu(menuName = "Демка/Контроллер/StarterAssets", fileName = "Controller_StarterAssets")]
    public sealed class StarterAssetsControllerProvider : AvatarControllerProvider
    {
        [SerializeField] private LayerMask _groundLayers = 1;

        public override IAvatarController Attach(PlayerRig rig, IPlayerInput input, DemoConfig config)
        {
            var root = rig.gameObject;
            var inputs = root.AddComponent<StarterAssetsInputs>();

            /// PlayerInput контроллер добавит сам — он в RequireComponent. Действий у него нет,
            /// значит и рассылки в StarterAssetsInputs нет: туда пишет только наш кадр.
            var controller = root.AddComponent<FirstPersonController>();
            controller.CinemachineCameraTarget = rig.View.gameObject;
            controller.GroundLayers = _groundLayers;
            controller.MoveSpeed = config.WalkSpeed;
            controller.SprintSpeed = config.SprintSpeed;
            controller.RotationSpeed = 1f;

            var feeder = root.AddComponent<StarterAssetsInputFeeder>();
            feeder.Configure(inputs, input);
            return feeder;
        }
    }
}
