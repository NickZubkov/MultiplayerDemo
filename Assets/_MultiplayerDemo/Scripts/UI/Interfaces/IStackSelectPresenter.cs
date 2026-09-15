using System.Collections.Generic;
using Game.Net;

namespace Game.UI
{
    public interface IStackSelectPresenter
    {
        public IReadOnlyList<NetworkStackDefinition> Stacks { get; }
    }
}
