using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// Провайдер — ассет, как NetworkStackDefinition: заменить контроллер значит поставить
    /// в поле BootstrapScope другой ассет (спека Ц3). Контроллер ставится только своему
    /// аватару, в момент «этот персонаж мой»; у чужих его нет — их двигает сеть.
    public abstract class AvatarControllerProvider : ScriptableObject
    {
        public abstract IAvatarController Attach(PlayerRig rig, IPlayerInput input, DemoConfig config);
    }
}
