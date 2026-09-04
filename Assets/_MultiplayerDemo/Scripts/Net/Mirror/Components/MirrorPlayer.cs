using Game.Gameplay;
using Mirror;
using UnityEngine;

namespace Game.Net.Mirror
{
    /// Единственный факт, который сеть обязана сообщить игровому слою: «этот персонаж мой».
    /// Без него PlayerRig остаётся в состоянии, которое ставит себе в Awake, — камера, ввод
    /// и контроллер выключены у всех капсул сразу. Смотреть на арену остаётся камерой сцены
    /// Bootstrap: она стоит в начале координат и чистится сплошным цветом, отсюда и половина
    /// чёрного экрана вместо вида от первого лица.
    ///
    /// Взятие и бросок предмета приедут сюда в задаче 13, вместе с MirrorItem.
    ///
    /// isLocalPlayer к моменту OnStartClient уже выставлен: Mirror ставит флаги раньше
    /// коллбэков (NetworkClient.InitializeIdentityFlags), и в host-режиме тоже — реплика
    /// хоста проходит через тот же BootstrapIdentity из OnHostClientSpawn.
    [RequireComponent(typeof(PlayerRig))]
    public sealed class MirrorPlayer : NetworkBehaviour
    {
        private PlayerRig _rig;

        private void Awake() => _rig = GetComponent<PlayerRig>();

        public override void OnStartClient() => _rig.SetLocal(isLocalPlayer);
    }
}
