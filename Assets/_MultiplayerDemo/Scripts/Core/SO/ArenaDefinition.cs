using UnityEngine;

namespace Game.Core
{
    /// Описание уровня. Уровни от стека не зависят: один и тот же ассет годится
    /// и для NGO, и для Mirror, и для Fusion — арена про сеть ничего не знает.
    ///
    /// Три поля, а не одно имя сцены: в маяк едет короткий arenaId, в списке хостов
    /// игрок видит displayName, а имя сцены остаётся деталью загрузчика.
    [CreateAssetMenu(menuName = "Демка/Арена", fileName = "Arena_")]
    public sealed class ArenaDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Коробка";
        [SerializeField] private string _arenaId = "box";
        [SerializeField] private string _sceneName = "Arena";

        public string DisplayName => _displayName;
        public string ArenaId => _arenaId;
        public string SceneName => _sceneName;
    }
}
