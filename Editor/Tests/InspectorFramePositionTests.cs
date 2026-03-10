using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class InspectorFramePositionTests
    {
        [Test]
        public void RefreshTargets_WritesFrameRelativePositionIntoDedicatedFields()
        {
            var root = new VisualElement();
            root.Add(new DropdownField { name = "patch-target" });
            root.Add(new FloatField { name = "inspector-frame-x" });
            root.Add(new FloatField { name = "inspector-frame-y" });
            root.Add(new FloatField { name = "inspector-frame-width" });
            root.Add(new FloatField { name = "inspector-frame-height" });
            root.Add(new FloatField { name = "inspector-translate-x" });
            root.Add(new FloatField { name = "inspector-translate-y" });
            root.Add(new FloatField { name = "inspector-rotate" });
            root.Add(new FloatField { name = "inspector-scale-x", value = 1f });
            root.Add(new FloatField { name = "inspector-scale-y", value = 1f });
            root.Add(new TextField { name = "inspector-transform" });

            var host = new FakeInspectorPanelHost
            {
                CurrentDocument = new DocumentSession
                {
                    WorkingSourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" transform=\"translate(1 2)\" /></svg>"
                },
                SceneRect = new Rect(120f, 48f, 30f, 40f)
            };

            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => host, null);

            view.Bind(root);
            service.RefreshTargets(host.CurrentDocument.WorkingSourceText);
            Assert.That(service.TrySelectTargetByKey("node", out _), Is.True);
            service.RefreshTargets(host.CurrentDocument.WorkingSourceText);

            Assert.That(root.Q<FloatField>("inspector-frame-x").value, Is.EqualTo(120f));
            Assert.That(root.Q<FloatField>("inspector-frame-y").value, Is.EqualTo(48f));
            Assert.That(root.Q<FloatField>("inspector-frame-width").value, Is.EqualTo(30f));
            Assert.That(root.Q<FloatField>("inspector-frame-height").value, Is.EqualTo(40f));
            Assert.That(root.Q<FloatField>("inspector-translate-x").value, Is.EqualTo(1f));
            Assert.That(root.Q<FloatField>("inspector-translate-y").value, Is.EqualTo(2f));
        }

        [Test]
        public void ApplyFrameRectFromView_UsesDedicatedFrameFields()
        {
            var root = new VisualElement();
            root.Add(new DropdownField { name = "patch-target" });
            root.Add(new FloatField { name = "inspector-frame-x", value = 200f });
            root.Add(new FloatField { name = "inspector-frame-y", value = 75f });
            root.Add(new FloatField { name = "inspector-frame-width", value = 60f });
            root.Add(new FloatField { name = "inspector-frame-height", value = 90f });
            root.Add(new FloatField { name = "inspector-rotate" });
            root.Add(new TextField { name = "inspector-transform" });

            var host = new FakeInspectorPanelHost
            {
                CurrentDocument = new DocumentSession
                {
                    WorkingSourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" /></svg>"
                },
                SceneRect = new Rect(120f, 48f, 30f, 40f)
            };

            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => host, null);

            view.Bind(root);
            service.RefreshTargets(host.CurrentDocument.WorkingSourceText);
            Assert.That(service.TrySelectTargetByKey("node", out _), Is.True);
            root.Q<FloatField>("inspector-frame-x").SetValueWithoutNotify(200f);
            root.Q<FloatField>("inspector-frame-y").SetValueWithoutNotify(75f);
            root.Q<FloatField>("inspector-frame-width").SetValueWithoutNotify(60f);
            root.Q<FloatField>("inspector-frame-height").SetValueWithoutNotify(90f);

            service.ApplyFrameRectFromView();

            Assert.That(host.AppliedFrameTargetKey, Is.EqualTo("node"));
            Assert.That(host.AppliedFrameRect, Is.EqualTo(new Rect(200f, 75f, 60f, 90f)));
        }

        private sealed class FakeInspectorPanelHost : IInspectorPanelHost
        {
            public DocumentSession CurrentDocument { get; set; }
            public Rect SceneRect { get; set; }
            public string AppliedFrameTargetKey { get; private set; }
            public Rect AppliedFrameRect { get; private set; }

            public bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus) => true;

            public bool TryApplyTargetFrameRect(string targetKey, Rect targetSceneRect, string successStatus)
            {
                AppliedFrameTargetKey = targetKey;
                AppliedFrameRect = targetSceneRect;
                return true;
            }

            public bool TryGetTargetSceneRect(string targetKey, out Rect sceneRect)
            {
                sceneRect = SceneRect;
                return true;
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
