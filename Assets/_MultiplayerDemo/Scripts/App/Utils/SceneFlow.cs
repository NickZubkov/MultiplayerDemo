using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Game.App
{
    /// Аддитивная загрузка и выгрузка. Ничего не знает ни о стеках, ни о сети.
    ///
    /// Статический и не регистрируется в контейнере: состояния нет, поверхность
    /// закрыта двумя методами на весь план, а подменять его никто не собирается —
    /// ни один тест презентеров до вызова сцен не доходит. Шов появится тогда,
    /// когда понадобится, а не заранее.
    public static class SceneFlow
    {
        public static async UniTask LoadAdditiveAsync(string sceneName, CancellationToken token)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded) return;
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).ToUniTask(cancellationToken: token);
        }

        public static async UniTask UnloadAsync(params string[] sceneNames)
        {
            foreach (var name in sceneNames)
            {
                if (SceneManager.GetSceneByName(name).isLoaded)
                {
                    await SceneManager.UnloadSceneAsync(name).ToUniTask();
                }
            }
        }
    }
}
