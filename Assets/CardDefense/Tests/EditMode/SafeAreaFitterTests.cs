using CardDefense.UI;
using NUnit.Framework;
using UnityEngine;

namespace CardDefense.Tests.EditMode
{
    public sealed class SafeAreaFitterTests
    {
        [Test]
        public void PortraitNotchInsetsConvertToNormalizedAnchors()
        {
            Vector2[] anchors = SafeAreaFitter.CalculateAnchors(
                new Rect(0f, 80f, 1080f, 1760f), new Vector2Int(1080, 1920));

            Assert.AreEqual(0f, anchors[0].x, 0.0001f);
            Assert.AreEqual(80f / 1920f, anchors[0].y, 0.0001f);
            Assert.AreEqual(1f, anchors[1].x, 0.0001f);
            Assert.AreEqual(1840f / 1920f, anchors[1].y, 0.0001f);
        }

        [Test]
        public void InvalidSafeAreaIsClampedInsideScreen()
        {
            Vector2[] anchors = SafeAreaFitter.CalculateAnchors(
                new Rect(-20f, -10f, 1200f, 2100f), new Vector2Int(1080, 1920));

            Assert.AreEqual(Vector2.zero, anchors[0]);
            Assert.AreEqual(Vector2.one, anchors[1]);
        }
    }
}
