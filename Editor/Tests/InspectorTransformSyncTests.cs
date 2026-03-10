using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class InspectorTransformSyncTests
    {
        [Test]
        public void SyncFromAttributes_ParsesSimpleTransformIntoHelperState()
        {
            var state = new InspectorPanelState();
            var attributes = new Dictionary<string, string>
            {
                ["transform"] = "translate(12 4) rotate(30) scale(2 3)"
            };

            state.SyncFromAttributes(attributes);

            Assert.That(state.Transform, Is.EqualTo("translate(12 4) rotate(30) scale(2 3)"));
            Assert.That(state.TranslateX, Is.EqualTo(12f));
            Assert.That(state.TranslateY, Is.EqualTo(4f));
            Assert.That(state.Rotate, Is.EqualTo(30f));
            Assert.That(state.ScaleX, Is.EqualTo(2f));
            Assert.That(state.ScaleY, Is.EqualTo(3f));
        }

        [Test]
        public void SyncTransformTextFromHelper_RebuildsTransformField()
        {
            var root = CreatePositionRoot();
            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => null, null);

            view.Bind(root);
            root.Q<FloatField>("inspector-translate-x").SetValueWithoutNotify(10f);
            root.Q<FloatField>("inspector-translate-y").SetValueWithoutNotify(5f);
            root.Q<FloatField>("inspector-rotate").SetValueWithoutNotify(45f);
            root.Q<FloatField>("inspector-scale-x").SetValueWithoutNotify(2f);
            root.Q<FloatField>("inspector-scale-y").SetValueWithoutNotify(3f);

            var transform = service.SyncTransformTextFromHelper();

            Assert.That(transform, Is.EqualTo("translate(10 5) rotate(45) scale(2 3)"));
            Assert.That(root.Q<TextField>("inspector-transform").value, Is.EqualTo(transform));
        }

        [Test]
        public void SyncTransformHelperFromText_ParsesSimpleTransformIntoFields()
        {
            var root = CreatePositionRoot();
            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => null, null);

            view.Bind(root);
            root.Q<TextField>("inspector-transform").SetValueWithoutNotify("translate(8, 3) rotate(15) scale(4)");

            var success = service.SyncTransformHelperFromText();

            Assert.That(success, Is.True);
            Assert.That(root.Q<FloatField>("inspector-translate-x").value, Is.EqualTo(8f));
            Assert.That(root.Q<FloatField>("inspector-translate-y").value, Is.EqualTo(3f));
            Assert.That(root.Q<FloatField>("inspector-rotate").value, Is.EqualTo(15f));
            Assert.That(root.Q<FloatField>("inspector-scale-x").value, Is.EqualTo(4f));
            Assert.That(root.Q<FloatField>("inspector-scale-y").value, Is.EqualTo(4f));
        }

        [Test]
        public void SyncTransformHelperFromText_IgnoresUnsupportedTransform()
        {
            var root = CreatePositionRoot();
            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => null, null);

            view.Bind(root);
            root.Q<FloatField>("inspector-translate-x").SetValueWithoutNotify(2f);
            root.Q<FloatField>("inspector-scale-x").SetValueWithoutNotify(5f);
            root.Q<TextField>("inspector-transform").SetValueWithoutNotify("matrix(1 0 0 1 20 30)");

            var success = service.SyncTransformHelperFromText();

            Assert.That(success, Is.False);
            Assert.That(root.Q<FloatField>("inspector-translate-x").value, Is.EqualTo(2f));
            Assert.That(root.Q<FloatField>("inspector-scale-x").value, Is.EqualTo(5f));
        }

        private static VisualElement CreatePositionRoot()
        {
            var root = new VisualElement();
            root.Add(new FloatField { name = "inspector-translate-x" });
            root.Add(new FloatField { name = "inspector-translate-y" });
            root.Add(new FloatField { name = "inspector-rotate" });
            root.Add(new FloatField { name = "inspector-scale-x", value = 1f });
            root.Add(new FloatField { name = "inspector-scale-y", value = 1f });
            root.Add(new TextField { name = "inspector-transform" });
            return root;
        }
    }
}
