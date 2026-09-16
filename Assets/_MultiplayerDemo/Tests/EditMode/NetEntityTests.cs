using System.Reflection;
using Game.Net;
using Game.Net.Local;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class NetEntityTests
    {
        [TearDown]
        public void Clean()
        {
            foreach (var entity in Object.FindObjectsByType<NetEntity>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(entity.gameObject);
            }
        }

        /// Носитель отвязывается в конце жизни объекта, и порядок разрушения компонентов
        /// Unity не обещает: у NGO OnNetworkDespawn приходит из OnDestroy сетевого объекта,
        /// то есть уже после того, как эта метка похоронила своё свойство. На выходе хоста
        /// так падали семь ObjectDisposedException — по одной на каждую сетевую сущность.
        ///
        /// Сущность обязательно привязана: снятие привязки с пустой метки R3 гасит сравнением
        /// значений, и проверка прошла бы мимо ошибки.
        [Test]
        public void UnbindAfterDestroyIsSilent()
        {
            var entity = new GameObject("Crate").AddComponent<NetEntity>();
            entity.Bind(new LocalNetwork().Join().CreateEntity(NetEntityId.Dynamic(1), PlayerId.NONE));

            Destroy(entity);

            Assert.DoesNotThrow(() => entity.Bind(null));
        }

        /// Вне Play Mode Unity сообщений жизненного цикла не шлёт вовсе, и DestroyImmediate
        /// до OnDestroy не доходит. Зовём её напрямую — тем же порядком, каким движок зовёт
        /// её при разрушении объекта в игре.
        private static void Destroy(NetEntity entity) =>
            typeof(NetEntity).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)
                             .Invoke(entity, null);
    }
}
