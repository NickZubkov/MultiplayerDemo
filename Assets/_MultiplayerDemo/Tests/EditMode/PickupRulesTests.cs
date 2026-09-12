using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class PickupRulesTests
    {
        private static PickupQuery Valid() => new PickupQuery(itemFree: true, handsEmpty: true, distance: 1f, hasLineOfSight: true);

        [Test]
        public void AllowsPickupWhenEverythingIsFine() =>
            Assert.AreEqual(PickupDenial.None, PickupRules.Evaluate(Valid()));

        [Test]
        public void DeniesWhenItemAlreadyHeld() =>
            Assert.AreEqual(PickupDenial.ItemHeld, PickupRules.Evaluate(new PickupQuery(false, true, 1f, true)));

        [Test]
        public void DeniesWhenHandsAreBusy() =>
            Assert.AreEqual(PickupDenial.HandsBusy, PickupRules.Evaluate(new PickupQuery(true, false, 1f, true)));

        [Test]
        public void DeniesBeyondMaxDistance() =>
            Assert.AreEqual(PickupDenial.TooFar,
                PickupRules.Evaluate(new PickupQuery(true, true, PickupRules.MAX_DISTANCE + 0.01f, true)));

        [Test]
        public void AllowsExactlyAtMaxDistance() =>
            Assert.AreEqual(PickupDenial.None,
                PickupRules.Evaluate(new PickupQuery(true, true, PickupRules.MAX_DISTANCE, true)));

        [Test]
        public void DeniesWithoutLineOfSight() =>
            Assert.AreEqual(PickupDenial.NoLineOfSight, PickupRules.Evaluate(new PickupQuery(true, true, 1f, false)));

        [Test]
        public void ItemHeldWinsOverOtherViolations() =>
            Assert.AreEqual(PickupDenial.ItemHeld, PickupRules.Evaluate(new PickupQuery(false, false, 99f, false)));
    }
}
