using Game.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace Game.Net.Ngo
{
    /// Вся связь игрового слоя с NGO — одна строка в OnNetworkSpawn.
    /// Подписки на взятие и бросок предмета появятся в задаче 8, вместе с NgoItem.
    [RequireComponent(typeof(PlayerRig))]
    public sealed class NgoPlayer : NetworkBehaviour
    {
        [SerializeField] private InteractionRay ray;

        private PlayerRig _rig;

        public bool HandsBusy => ray != null && ray.HandsBusy;

        private void Awake() => _rig = GetComponent<PlayerRig>();

        public override void OnNetworkSpawn() => _rig.SetLocal(IsOwner);

        /// Ставится предметом при смене держателя — и на сервере, и у клиентов.
        public void SetHandsBusy(bool busy)
        {
            if (ray != null) ray.HandsBusy = busy;
        }
    }
}
