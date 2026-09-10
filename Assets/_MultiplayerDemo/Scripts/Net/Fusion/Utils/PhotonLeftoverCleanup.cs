#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Net.Fusion
{
    /// Уборка служебных объектов Photon, оседающих в открытой сцене после выхода из Play Mode.
    ///
    /// Механика — в `Assets/Photon/PhotonRealtime/Code/ConnectionHandler.cs:100`: объект-носитель
    /// хранится в статическом поле и создаётся заново, если поля нет, а `DontDestroyOnLoad`
    /// вызывается только под `Application.isPlaying`. При выходе из Play Mode объект с
    /// `DontDestroyOnLoad` уничтожается, статика обнуляется — и первая же асинхронная операция
    /// Photon, чьё продолжение приземлилось уже вне игры, создаёт обычный объект прямо в сцене.
    /// `RegionHandler` попадает туда иначе (`RegionHandler.cs:874`): там `DontDestroyOnLoad`
    /// зовётся безусловно, вне игры бросает — и объект остаётся вовсе без компонентов.
    ///
    /// Предотвратить это нельзя: у нас есть асинхронное завершение сессии, а редактор не ждёт
    /// его ни в одном хуке — ни `ExitingPlayMode`, ни `OnDestroy` не дают доиграть. Отказаться
    /// от гашения раннера тоже нельзя, иначе сессия останется висеть в облаке. Поэтому мусор
    /// не предупреждается, а выметается — по факту возвращения в edit mode.
    [InitializeOnLoad]
    public static class PhotonLeftoverCleanup
    {
        /// Все три имени Photon задаёт литералом в местах создания, так что список закрыт.
        /// `EventBetterWorker` в него не входит намеренно: ему ставят `HideAndDontSave`,
        /// в файл сцены он не попадёт и трогать его — лезть в чужую статику.
        private static readonly HashSet<string> Names = new()
        {
            "ConnectionHandler",
            "RegionHandler",
            "MonoBehaviourEmpty",
        };

        static PhotonLeftoverCleanup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                Sweep(SceneManager.GetSceneAt(i));
            }
        }

        /// Пометку «изменена» сцене не возвращаем и не снимаем: объекты создавались в рантайме
        /// и частью сохранённой сцены не были, поэтому их удаление её не пачкает — проверено
        /// замером, `isDirty` остаётся `false` и до, и после. Это здесь важно: лишняя пометка
        /// обернулась бы диалогом «сохранить изменения?», а модальные окна вешают MCP-мост.
        private static void Sweep(Scene scene)
        {
            if (!scene.isLoaded) return;

            var removed = 0;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (!IsPhotonLeftover(root)) continue;

                Object.DestroyImmediate(root);
                removed++;
            }

            if (removed == 0) return;

            Debug.Log($"Убрано служебных объектов Photon из сцены {scene.name}: {removed}");
        }

        /// Совпадения имени мало: так может называться и наш объект. Поэтому требуем ещё,
        /// чтобы объект был пустым снаружи — без детей — и не нёс ничего, кроме `Transform`
        /// и компонентов самого Photon. Голый объект под это тоже подходит, и намеренно:
        /// при упавшем `DontDestroyOnLoad` исключение бьёт до `AddComponent`, и от объекта
        /// остаётся один `Transform`.
        private static bool IsPhotonLeftover(GameObject go)
        {
            if (!Names.Contains(go.name)) return false;
            if (go.transform.childCount > 0) return false;

            foreach (var component in go.GetComponents<Component>())
            {
                if (component is Transform) continue;
                if (component == null) return false;

                var space = component.GetType().Namespace;
                if (space == null || !space.StartsWith("Photon.")) return false;
            }

            return true;
        }
    }
}
#endif
