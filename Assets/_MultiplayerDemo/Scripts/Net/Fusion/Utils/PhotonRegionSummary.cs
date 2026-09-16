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

            if (!IsUsable(summary)) return;

            PlayerPrefs.SetString(KEY, summary);
            PlayerPrefs.Save();
        }

        /// Сводка годится только с замером: `RegionHandler.SummaryToCache` отдаёт
        /// «регион;пинг;список», пока лучший регион известен, и голый список регионов, когда
        /// нет. Голый список `PingMinimumOfRegions` отвергает по числу полей и пингует все
        /// регионы заново — то есть кэш из него не ускоряет ничего, зато затирает хорошую
        /// сводку. Так в PlayerPrefs и осела строка «asia,au,cae,…», с которой каждый старт
        /// хоста заново мерил все регионы (ручной прогон 15.11).
        private static bool IsUsable(string summary)
        {
            if (string.IsNullOrEmpty(summary)) return false;

            var parts = summary.Split(';');
            return parts.Length >= 3 && !string.IsNullOrEmpty(parts[0]) && int.TryParse(parts[1], out _);
        }
    }
}
