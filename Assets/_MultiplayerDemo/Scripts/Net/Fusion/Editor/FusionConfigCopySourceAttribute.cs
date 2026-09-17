using System;
using Fusion;
using Game.Net.Fusion;

/// Порядок 1000 ставит нас перед обоими источниками Fusion — `Resources` объявлен с 2000,
/// `AssetDatabase` с `int.MaxValue`. Встать последним нельзя: больше `int.MaxValue` порядка
/// нет, а отказ редакторского источника терминален (`AllowFallback = false`), и до нас
/// очередь бы просто не дошла. Чтобы это не превратилось в подмену рабочего пути, `Load`
/// уступает Fusion всегда, когда оригинал читается.
[assembly: FusionConfigCopySource(typeof(NetworkProjectConfigAsset), Order = 1000,
    AllowEditMode = true, AllowFallback = true)]

namespace Game.Net.Fusion
{
    /// Свой источник глобального ассета Fusion — тот самый механизм, на который показывает
    /// сообщение об ошибке «you need to use FusionGlobalScriptableObjectAttribute attribute
    /// to point to a method that will perform the loading». Нужен ровно одному потребителю —
    /// виртуальному игроку MPPM, у которого `.fusion` не импортируется; почему так, разобрано
    /// в [[FusionConfigCopy]].
    ///
    /// Живёт в редакторской сборке `Game.Net.Fusion.Editor`: в собранной игре ассет приходит
    /// из `Resources` штатным источником Fusion, и подменять там нечего (И-28).
    public sealed class FusionConfigCopySourceAttribute : FusionGlobalScriptableObjectSourceAttribute
    {
        public FusionConfigCopySourceAttribute(Type objectType) : base(objectType)
        {
        }

        public override FusionGlobalScriptableObjectLoadResult Load(Type type)
        {
            if (type != typeof(NetworkProjectConfigAsset)) return default;

            var original = FusionConfigCopy.LoadOriginal();

            /// Оригинал на месте — уступаем очередь Fusion. Пустой результат при
            /// `AllowFallback = true` означает «здесь не нашлось, идите дальше».
            if (original != null)
            {
                FusionConfigCopy.WarnIfStale(original);
                return default;
            }

            var copy = FusionConfigCopy.Load();

            /// Копии тоже нет — молчим и уступаем: пусть Fusion падает со своим сообщением,
            /// оно объясняет причину точнее, чем сможем мы.
            if (copy == null) return default;

            /// Выгружать нечего: ассет держит база ассетов редактора, а не мы.
            return new FusionGlobalScriptableObjectLoadResult(copy, instance => { });
        }
    }
}
