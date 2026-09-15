using System.Linq;
using Game.Net;
using Game.Net.Local;
using NUnit.Framework;

namespace Game.Tests
{
    /// Компилятор держит границу, пока asmdef правильный; этот тест ловит лишнюю ссылку,
    /// случайно добавленную в asmdef (спека § 9.2). Стеки добавятся в список по мере перевода.
    public sealed class AssemblyBoundaryTests
    {
        private static readonly string[] GAME_ASSEMBLIES = { "Game.Core", "Game.Gameplay", "Game.UI", "Game.App" };

        [TestCase(typeof(NetEntityChannel))]
        [TestCase(typeof(LocalNetwork))]
        public void NetworkDoesNotSeeTheGame(System.Type probe)
        {
            var references = probe.Assembly.GetReferencedAssemblies().Select(name => name.Name);

            CollectionAssert.IsEmpty(references.Intersect(GAME_ASSEMBLIES));
        }
    }
}
