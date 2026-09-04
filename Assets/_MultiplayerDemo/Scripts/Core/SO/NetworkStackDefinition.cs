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

        /// Подпись к полю ручного ввода: у LAN-стеков это адрес, у Fusion — имя сессии.
        /// Лобби показывает текст как есть и про стеки по-прежнему ничего не знает.
        [SerializeField] private string manualEntryHint = "Адрес хоста (ip:port)";

        public string DisplayName => displayName;
        public string StackId => stackId;
        public string ManagerScene => managerScene;
        public string ManualEntryHint => manualEntryHint;
    }
}
