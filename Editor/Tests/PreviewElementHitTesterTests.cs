using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class PreviewElementHitTesterTests
    {
        [Test]
        public void TryHitTest_UsesHitRadius_ForTinyBoundsFallback()
        {
            var hitTester = new PreviewElementHitTester();
            var elements = new List<PreviewElementGeometry>
            {
                new()
                {
                    Key = "tiny",
                    DrawOrder = 1,
                    VisualBounds = new Rect(10f, 10f, 2f, 2f),
                    HitGeometry = new List<Vector2[]>()
                }
            };

            bool success = hitTester.TryHitTest(
                elements,
                new Vector2(13f, 11f),
                2f,
                out PreviewElementGeometry hitElement);

            Assert.That(success, Is.True);
            Assert.That(hitElement, Is.Not.Null);
            Assert.That(hitElement.Key, Is.EqualTo("tiny"));
        }

        [Test]
        public void TryHitTest_PrefersSmallerBounds_WhenMultipleFallbacksOverlapWithinHitRadius()
        {
            var hitTester = new PreviewElementHitTester();
            var elements = new List<PreviewElementGeometry>
            {
                new()
                {
                    Key = "large",
                    DrawOrder = 1,
                    VisualBounds = new Rect(0f, 0f, 20f, 20f),
                    HitGeometry = new List<Vector2[]>()
                },
                new()
                {
                    Key = "small",
                    DrawOrder = 0,
                    VisualBounds = new Rect(10f, 10f, 4f, 4f),
                    HitGeometry = new List<Vector2[]>()
                }
            };

            bool success = hitTester.TryHitTest(
                elements,
                new Vector2(15f, 12f),
                2f,
                out PreviewElementGeometry hitElement);

            Assert.That(success, Is.True);
            Assert.That(hitElement, Is.Not.Null);
            Assert.That(hitElement.Key, Is.EqualTo("small"));
        }
    }
}
