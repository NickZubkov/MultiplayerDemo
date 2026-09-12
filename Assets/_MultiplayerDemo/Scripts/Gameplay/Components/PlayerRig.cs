using UnityEngine;

namespace Game.Gameplay
{
    /// Единственный факт, пересекающий границу «игра ↔ сеть»: «этот персонаж мой».
    public sealed class PlayerRig : MonoBehaviour
    {
        [Tooltip("Работает только у владельца: PlayerInput, FirstPersonController, Camera, AudioListener")]
        [SerializeField] private Behaviour[] _ownerOnlyBehaviours;
        [SerializeField] private GameObject[] _ownerOnlyObjects;

        public bool IsLocal { get; private set; }

        private void Awake() => SetLocal(false);

        public void SetLocal(bool isLocal)
        {
            IsLocal = isLocal;

            foreach (var behaviour in _ownerOnlyBehaviours)
            {
                if (behaviour) behaviour.enabled = isLocal;
            }

            foreach (var go in _ownerOnlyObjects)
            {
                if (go) go.SetActive(isLocal);
            }
        }
    }
}
