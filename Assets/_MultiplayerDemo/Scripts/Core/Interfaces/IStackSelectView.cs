using System.Collections.Generic;
using Game.Net;
using R3;

namespace Game.Core
{
    /// Экраны спрятаны за интерфейсами: иначе презентеры тянут за собой сцену
    /// и перестают быть тестируемыми.
    public interface IStackSelectView
    {
        public Observable<NetworkStackDefinition> StackChosen { get; }

        public void Show(IReadOnlyList<NetworkStackDefinition> stacks);
        public void Hide();
    }
}
