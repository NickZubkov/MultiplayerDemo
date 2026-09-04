using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Game.Core
{
    /// Сцену стека грузит и выгружает сервис из scope лобби, а не презентер: презентер
    /// живёт в той самой сцене, которую просит выгрузить, и его продолжение после await
    /// проснулось бы в уже уничтоженном объекте.
    public interface IStackFlow
    {
        /// Сцена стека выгружена — пора снова показать выбор стека.
        public Observable<Unit> BackToSelect { get; }

        public UniTask LoadAsync(NetworkStackDefinition stack, CancellationToken token);

        /// Без токена и без await: тот, кто просит вернуться, умрёт вместе со сценой.
        public void RequestBackToSelect();
    }
}
