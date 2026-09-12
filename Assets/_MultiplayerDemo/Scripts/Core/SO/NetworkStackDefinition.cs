using UnityEngine;

namespace Game.Core
{
    /// Описание стека. Наследники живут в Game.Net.*, а Game.App работает только
    /// с базовым типом — поэтому сборка приложения не ссылается ни на один сетевой пакет.
    /// Выбор стека в меню = загрузка его сцены = создание дочернего scope с его сервисами.
    public abstract class NetworkStackDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "NGO";
        [SerializeField] private string _stackId = "ngo";
        [SerializeField] private string _managerScene = "Net_Ngo";

        /// Подпись к полю ручного ввода: у LAN-стеков это адрес, у Fusion — имя сессии.
        /// Лобби показывает текст как есть и про стеки по-прежнему ничего не знает.
        [SerializeField] private string _manualEntryHint = "Адрес хоста (ip:port)";

        public string DisplayName => _displayName;
        public string StackId => _stackId;
        public string ManagerScene => _managerScene;
        public string ManualEntryHint => _manualEntryHint;
    }
}
