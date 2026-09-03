using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Ngo
{
    /// Объект, приехавший по сети, создаёт фреймворк — мимо контейнера.
    /// Этот обработчик возвращает создание нам: и локальный спавн, и реплика,
    /// и автоспавн игрока проходят через IObjectResolver и приходят собранными.
    public sealed class NgoObjectFactory : INetworkPrefabInstanceHandler
    {
        private readonly IObjectResolver _resolver;
        private readonly GameObject _prefab;

        public NgoObjectFactory(IObjectResolver resolver, GameObject prefab)
        {
            _resolver = resolver;
            _prefab = prefab;
        }

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation) =>
            _resolver.Instantiate(_prefab, position, rotation).GetComponent<NetworkObject>();

        public void Destroy(NetworkObject networkObject) => Object.Destroy(networkObject.gameObject);
    }
}
