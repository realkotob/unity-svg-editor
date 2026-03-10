using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
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
            Assert.That(state.TranslateX, Is.EqualTo(12f).Within(0.001f));
            Assert.That(state.TranslateY, Is.EqualTo(4f).Within(0.001f));
            Assert.That(state.Rotate, Is.EqualTo(30f).Within(0.001f));
            Assert.That(state.ScaleX, Is.EqualTo(2f).Within(0.001f));
            Assert.That(state.ScaleY, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void SyncFromAttributes_AccumulatesRepeatedTranslateCommands()
        {
            var state = new InspectorPanelState();
            var attributes = new Dictionary<string, string>
            {
                ["transform"] = "translate(10 5) translate(3 2) rotate(30) scale(2 3)"
            };

            state.SyncFromAttributes(attributes);

            Assert.That(state.TranslateX, Is.EqualTo(13f).Within(0.001f));
            Assert.That(state.TranslateY, Is.EqualTo(7f).Within(0.001f));
            Assert.That(state.Rotate, Is.EqualTo(30f).Within(0.001f));
            Assert.That(state.ScaleX, Is.EqualTo(2f).Within(0.001f));
            Assert.That(state.ScaleY, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void SyncFromAttributes_ParsesScaleAroundIntoSimpleHelperValues()
        {
            var state = new InspectorPanelState();
            var attributes = new Dictionary<string, string>
            {
                ["transform"] = "translate(5 7) scale(2 3) translate(-5 -7)"
            };

            state.SyncFromAttributes(attributes);

            Assert.That(state.TranslateX, Is.EqualTo(-5f).Within(0.001f));
            Assert.That(state.TranslateY, Is.EqualTo(-14f).Within(0.001f));
            Assert.That(state.ScaleX, Is.EqualTo(2f).Within(0.001f));
            Assert.That(state.ScaleY, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void RefreshTargets_UsesProvidedSourceTextForTransientInspectorState()
        {
            var root = CreatePositionRoot();
            root.Add(new DropdownField { name = "patch-target" });
            var host = new FakeInspectorPanelHost
            {
                CurrentDocument = new DocumentSession
                {
                    WorkingSourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" transform=\"translate(1 1)\"/></svg>"
                }
            };
            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => host, null);

            view.Bind(root);
            service.RefreshTargets(host.CurrentDocument.WorkingSourceText);
            Assert.That(service.TrySelectTargetByKey("node", out _), Is.True);
            service.RefreshTargets("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" transform=\"translate(9 4)\"/></svg>");

            Assert.That(root.Q<FloatField>("inspector-translate-x").value, Is.EqualTo(9f).Within(0.001f));
            Assert.That(root.Q<FloatField>("inspector-translate-y").value, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void RefreshTargets_UsesCurrentDocumentModelWhenSourceOverrideIsMissing()
        {
            const string source = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" transform=\"translate(7 3)\"/></svg>";
            var root = CreatePositionRoot();
            root.Add(new DropdownField { name = "patch-target" });
            var host = new FakeInspectorPanelHost
            {
                CurrentDocument = new DocumentSession
                {
                    WorkingSourceText = source,
                    DocumentModel = SvgFixtureTestUtility.LoadModel(source)
                }
            };

            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => host, null);

            view.Bind(root);
            service.RefreshTargets(null);
            Assert.That(service.TrySelectTargetByKey("node", out _), Is.True);

            Assert.That(root.Q<FloatField>("inspector-translate-x").value, Is.EqualTo(7f).Within(0.001f));
            Assert.That(root.Q<FloatField>("inspector-translate-y").value, Is.EqualTo(3f).Within(0.001f));
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
            Assert.That(root.Q<FloatField>("inspector-translate-x").value, Is.EqualTo(8f).Within(0.001f));
            Assert.That(root.Q<FloatField>("inspector-translate-y").value, Is.EqualTo(3f).Within(0.001f));
            Assert.That(root.Q<FloatField>("inspector-rotate").value, Is.EqualTo(15f).Within(0.001f));
            Assert.That(root.Q<FloatField>("inspector-scale-x").value, Is.EqualTo(4f).Within(0.001f));
            Assert.That(root.Q<FloatField>("inspector-scale-y").value, Is.EqualTo(4f).Within(0.001f));
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

        private sealed class FakeInspectorPanelHost : IInspectorPanelHost
        {
            public DocumentSession CurrentDocument { get; set; }

            public bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus)
            {
                return true;
            }

            public bool TryApplyTargetFrameRect(string targetKey, Rect targetSceneRect, string successStatus)
            {
                return true;
            }

            public bool TryGetTargetSceneRect(string targetKey, out Rect sceneRect)
            {
                sceneRect = default;
                return false;
            }

            public void SyncSelectionFromInspectorTarget(string targetKey)
            {
            }

            public void UpdateSourceStatus(string status)
            {
            }
        }
    }
}
