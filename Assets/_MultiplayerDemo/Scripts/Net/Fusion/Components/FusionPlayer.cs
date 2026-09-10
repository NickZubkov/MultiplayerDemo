using Fusion;
using Game.Gameplay;
using UnityEngine;

namespace Game.Net.Fusion
{
    /// Единственный факт, который сеть обязана сообщить игровому слою: «этот персонаж мой».
    /// Без него PlayerRig остаётся в состоянии из своего Awake — камера, ввод и контроллер
    /// выключены у всех капсул сразу.
    ///
    /// «Мой» в Shared Mode — это власть над состоянием: аватар создаёт себе сам владелец,
    /// и StateAuthority остаётся у него. Ни владельца ввода, ни сервера спрашивать не нужно.
    ///
    /// Взятие и бросок предмета приедут сюда в задаче 15, вместе с FusionItem.
    [RequireComponent(typeof(PlayerRig))]
    public sealed class FusionPlayer : NetworkBehaviour
    {
        private PlayerRig _rig;

        private void Awake() => _rig = GetComponent<PlayerRig>();

        public override void Spawned() => _rig.SetLocal(HasStateAuthority);
    }
}
