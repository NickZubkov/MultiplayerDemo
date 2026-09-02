using UnityEngine;

namespace Game.Core
{
    /// Описание стека. Наследники живут в Game.Net.*, а Game.App работает только
    /// с базовым типом — поэтому сборка приложения не ссылается ни на один сетевой пакет.
    /// Выбор стека в меню = загрузка его сцены = создание дочернего scope с его сервисами.
    public abstract class NetworkStackDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "NGO";
        [SerializeField] private string stackId = "ngo";
        [SerializeField] private string managerScene = "Net_Ngo";

        public string DisplayName => displayName;
        public string StackId => stackId;
        public string ManagerScene => managerScene;
    }
}
