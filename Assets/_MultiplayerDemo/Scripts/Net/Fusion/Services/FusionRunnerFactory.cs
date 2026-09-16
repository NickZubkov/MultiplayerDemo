using System;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Net.Fusion
{
    /// Раннер у Fusion одноразовый, и это не наша догадка: Shutdown по умолчанию
    /// уничтожает объект вместе с собой (destroyGameObject = true), то есть второго
    /// StartGame на том же экземпляре стек не обещает. Поэтому раннер не лежит в сцене,
    /// как менеджеры NGO и Mirror, а создаётся из префаба и выбрасывается после каждой
    /// сессии — а сервисы стека спрашивают текущий у этой фабрики, а не держат ссылку.
    ///
    /// Это первое место, где модель Fusion расходится с двумя другими стеками настолько,
    /// что расхождение видно в форме кода, а не в деталях вызовов.
    public sealed class FusionRunnerFactory : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly GameObject _prefab;

        private NetworkRunner _runner;
        private INetworkRunnerCallbacks _callbacks;

        /// Живой раннер или null. Выключенный за живой не считаем — по нему уже ничего
        /// не спросить, а SessionInfo у него пустой.
        public NetworkRunner Live => _runner != null && !_runner.IsShutdown ? _runner : null;

        public FusionRunnerFactory(IObjectResolver resolver, GameObject prefab)
        {
            _resolver = resolver;
            _prefab = prefab;
        }

        /// Гасим, а не просто уничтожаем объект. Живое соединение с Photon пережило бы scope:
        /// сессия осталась бы висеть в облаке, а недоделанные операции клиента достроились бы
        /// уже после выхода из Play Mode — и тогда Photon кладёт свой ConnectionHandler
        /// в активную сцену вместо DontDestroyOnLoad, потому что Application.isPlaying уже
        /// false (ConnectionHandler.BuildInstance). Три таких объекта нашлись в Bootstrap
        /// после первых прогонов владельца.
        public void Dispose()
        {
            var runner = _runner;
            _runner = null;

            if (runner != null && !runner.IsShutdown)
            {
                /// Задачу не ждём — Dispose синхронен, а объект раннера Shutdown уносит сам.
                runner.Shutdown();
            }
            else if (runner != null)
            {
                Destroy(runner.gameObject);
            }
        }

        /// Мост коллбэков приходит вызовом после сборки контейнера: он сам просит сессию
        /// и каталог, а те просят фабрику — конструктором это кольцо не собрать.
        public void UseCallbacks(INetworkRunnerCallbacks callbacks) => _callbacks = callbacks;

        public NetworkRunner Ensure()
        {
            if (_runner != null && _runner.IsShutdown) Release();
            if (_runner != null) return _runner;

            /// Через контейнер, а не Instantiate: на префабе раннера висит наш провайдер
            /// сетевых объектов, и ему нужен резолвер.
            _runner = _resolver.Instantiate(_prefab).GetComponent<NetworkRunner>();

            if (_callbacks != null)
            {
                _runner.AddCallbacks(_callbacks);
            }

            return _runner;
        }

        public async UniTask ShutdownAsync()
        {
            var runner = _runner;
            _runner = null;

            /// Ссылку снимаем до ожидания: Fusion зовёт OnShutdown изнутри, и разбор
            /// этого события не должен наткнуться на раннер, который мы сами и гасим.
            if (runner != null && !runner.IsShutdown)
            {
                await runner.Shutdown().AsUniTask();
            }
            else if (runner != null)
            {
                Destroy(runner.gameObject);
            }
        }

        private void Release()
        {
            if (_runner == null) return;

            var runner = _runner;
            _runner = null;
            Destroy(runner.gameObject);
        }

        private static void Destroy(GameObject instance)
        {
            if (instance != null) UnityEngine.Object.Destroy(instance);
        }
    }
}
