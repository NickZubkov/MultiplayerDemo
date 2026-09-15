using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Net;

namespace Game.Core
{
    /// Сцену стека грузит и выгружает сервис из BootstrapScope, а не презентер: презентер
    /// живёт рядом со сценой, которую просит выгрузить. Просьбы «вернуться» здесь больше нет:
    /// сценарий матча живёт выше выгружаемой сцены и просто ждёт выгрузку.
    public interface IStackFlow
    {
        public UniTask LoadAsync(NetworkStackDefinition stack, CancellationToken token);
        public UniTask UnloadAsync();
    }
}
