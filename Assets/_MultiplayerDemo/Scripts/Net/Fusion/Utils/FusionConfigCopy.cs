#if UNITY_EDITOR
using System;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace Game.Net.Fusion
{
    /// Копия конфигурации Fusion для виртуальных игроков MPPM.
    ///
    /// Fusion держит настройки в файле `.fusion`, который собирает scripted importer, а
    /// виртуальный игрок MPPM работает с базой ассетов только на чтение и своих артефактов
    /// не делает. Разница видна в цифрах: в основном редакторе `GetAssetDependencyHash`
    /// этого ассета — `501c6776…`, в клоне — нулевой, импортёр там `null`, а на любую попытку
    /// импорта в консоль летит «Asset Database is set to Read Only». Перезапуск клона, снос
    /// его `Library` и форс-реимпорт в основном редакторе не помогают. Соседний
    /// `PhotonAppSettings.asset` при этом читается — то есть дело именно в scripted importer,
    /// а не в сломанном клоне.
    ///
    /// Отсюда обходной путь: рядом лежит обычный `.asset`, такие клон читает без оговорок.
    /// Копия снимается `Instantiate` с живого ассета, а не разбором `.fusion`: в файле лежит
    /// только `Config`, тогда как таблицу префабов и `BehaviourMeta` импортёр собирает из
    /// проекта, и из файла их не восстановить.
    ///
    /// Копия обязана совпадать с оригиналом: идентификатор сетевого префаба — это его место
    /// в таблице, и разъехавшиеся таблицы у хоста и клиента означали бы спавн не того объекта.
    /// Поэтому [[FusionConfigCopySourceAttribute]] сверяет их при каждой загрузке в основном
    /// редакторе, где оригинал доступен, и ругается, если копия отстала.
    public static class FusionConfigCopy
    {
        public const string AssetPath = "Assets/_MultiplayerDemo/Settings/Fusion_ConfigCopy.asset";

        private const string MenuPath = "Tools/MultiplayerDemo/Обновить копию конфигурации Fusion";

        /// Путь оригинала спрашиваем у самого Fusion, а не пишем строкой: он объявлен
        /// атрибутом на типе ассета и при обновлении пакета может переехать.
        private static string OriginalPath
        {
            get
            {
                var attribute = (FusionGlobalScriptableObjectAttribute)Attribute.GetCustomAttribute(
                    typeof(NetworkProjectConfigAsset), typeof(FusionGlobalScriptableObjectAttribute));

                return attribute.DefaultPath;
            }
        }

        [MenuItem(MenuPath)]
        public static void Regenerate()
        {
            var original = LoadOriginal();

            if (original == null)
            {
                Debug.LogError($"Оригинал конфигурации Fusion не читается по пути {OriginalPath} — "
                    + "копию делать не из чего.");
                return;
            }

            var copy = UnityEngine.Object.Instantiate(original);
            AssetDatabase.CreateAsset(copy, AssetPath);

            /// Имя равняем на оригинал уже после создания: CreateAsset переименовывает объект
            /// в имя файла, а по имени идёт сверка копии с оригиналом — иначе она расходилась
            /// бы всегда. Имя объекта и имя файла при этом разные, и это нормально.
            copy.name = original.name;
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssets();

            Debug.Log($"Копия конфигурации Fusion обновлена: {AssetPath}");
        }

        public static NetworkProjectConfigAsset Load() =>
            AssetDatabase.LoadAssetAtPath<NetworkProjectConfigAsset>(AssetPath);

        public static NetworkProjectConfigAsset LoadOriginal() =>
            AssetDatabase.LoadAssetAtPath<NetworkProjectConfigAsset>(OriginalPath);

        /// Зовётся только там, где оригинал прочитался, — то есть в основном редакторе.
        /// Виртуальному игроку сверять не с чем, и предупреждать его бессмысленно.
        public static void WarnIfStale(NetworkProjectConfigAsset original)
        {
            var copy = Load();
            if (copy == null) return;
            if (EditorJsonUtility.ToJson(original) == EditorJsonUtility.ToJson(copy)) return;

            Debug.LogWarning("Копия конфигурации Fusion отстала от оригинала: виртуальные игроки MPPM "
                + $"возьмут старую таблицу префабов. Обновите её через «{MenuPath}».");
        }
    }
}
#endif
