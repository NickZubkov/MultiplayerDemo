using Fusion.Photon.Realtime;
using Photon.Realtime;
using UnityEngine;

namespace Game.Net.Fusion
{
    /// Регион Photon выбирается пингом всех регионов, и делается это на каждом запуске
    /// заново, если приложение само не хранит сводку прошлого замера. Хранить её больше
    /// некому: ни мы, ни Fusion этого не делали, а комментарий в AppSettings отправляет
    /// клиента складывать сводку в PlayerPrefs. Отсюда и пауза с недорисованной сценой
    /// в первые секунды подъёма хоста.
    ///
    /// Не путать с useCachedRegions у входа в лобби и старта игры: тот кэш живёт внутри
    /// одного запуска и до следующего не доживает.
    public static class PhotonRegionSummary
    {
        private const string KEY = "fusion.best-region-summary";

        /// Настройки строим копией глобальных: в них AppId, протокол и адреса, а собранные
        /// с нуля потеряли бы всё это молча — и подключение ушло бы в никуда.
        public static FusionAppSettings WithStoredRegion()
        {
            var settings = PhotonAppSettings.Global.AppSettings.GetCopy();
            var stored = PlayerPrefs.GetString(KEY, string.Empty);

            settings.BestRegionSummaryFromStorage = string.IsNullOrEmpty(stored) ? null : stored;
            return settings;
        }

        /// Свежую сводку держит клиент Realtime, а раннер своего клиента наружу не отдаёт.
        /// Зато его держит ConnectionHandler — обычный компонент, который Photon кладёт
        /// в DontDestroyOnLoad и открывает публичным свойством.
        public static void Remember()
        {
            var handler = Object.FindAnyObjectByType<ConnectionHandler>();
            var summary = handler == null ? null : handler.Client?.SummaryToCache;

            if (string.IsNullOrEmpty(summary)) return;

            PlayerPrefs.SetString(KEY, summary);
            PlayerPrefs.Save();
        }
    }
}
