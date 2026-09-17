using System;
using System.Linq;
using NUnit.Framework;

namespace Game.Tests
{
    /// Компилятор держит границу, пока asmdef правильный; этот тест ловит лишнюю ссылку,
    /// случайно добавленную в asmdef (спека § 9.2). Сборки ищутся по имени в домене, а не
    /// через типы-зонды: тестовая сборка сама не ссылается ни на один стек, иначе сторож
    /// требовал бы ровно той связи, от которой сторожит.
    public sealed class AssemblyBoundaryTests
    {
        private static readonly string[] GAME_ASSEMBLIES = { "Game.Core", "Game.Gameplay", "Game.UI", "Game.App" };

        [TestCase("Game.Net")]
        [TestCase("Game.Net.Local")]
        [TestCase("Game.Net.Ngo")]
        [TestCase("Game.Net.Mirror")]
        [TestCase("Game.Net.Fusion")]
        [TestCase("Game.Net.Fusion.Editor")]
        public void NetworkDoesNotSeeTheGame(string assemblyName)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetName().Name == assemblyName);

            /// Пропавшая сборка — это выключенный из сборки стек (defineConstraints) или
            /// переименование: сторож обязан упасть с её именем, а не промолчать.
            Assert.IsNotNull(assembly, $"Сборка {assemblyName} не загружена");

            var references = assembly.GetReferencedAssemblies().Select(name => name.Name);

            CollectionAssert.IsEmpty(references.Intersect(GAME_ASSEMBLIES));
        }
    }
}
