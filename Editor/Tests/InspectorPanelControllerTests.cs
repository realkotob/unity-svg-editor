using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class InspectorPanelControllerTests
    {
        [Test]
        public void FrameRectChanges_AreCoalescedIntoSingleDeferredApply()
        {
            DeferredScheduler scheduler = new();
            InspectorPanelController controller = new(
                new AttributePatcher(),
                new InspectorPanelState(),
                scheduler.Schedule,
                scheduler.Unschedule);

            VisualElement root = new();
            root.Add(new DropdownField { name = "patch-target" });
            root.Add(new FloatField { name = "inspector-frame-x" });
            root.Add(new FloatField { name = "inspector-frame-y" });
            root.Add(new FloatField { name = "inspector-frame-width" });
            root.Add(new FloatField { name = "inspector-frame-height" });
            root.Add(new TextField { name = "inspector-transform" });

            FakeInspectorPanelHost host = new()
            {
                CurrentDocument = new DocumentSession
                {
                    WorkingSourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" /></svg>"
                },
                SceneRect = new Rect(10f, 20f, 30f, 40f)
            };

            controller.Bind(root, host);
            controller.RefreshTargets(host.CurrentDocument.WorkingSourceText);
            Assert.That(controller.TrySelectTargetByKey("node", out _), Is.True);

            root.Q<FloatField>("inspector-frame-x").value = 100f;
            root.Q<FloatField>("inspector-frame-y").value = 200f;
            root.Q<FloatField>("inspector-frame-width").value = 300f;
            root.Q<FloatField>("inspector-frame-height").value = 400f;

            Assert.That(host.ApplyFrameRectCallCount, Is.EqualTo(0));
            Assert.That(scheduler.ScheduledCount, Is.EqualTo(1));

            scheduler.RunAll();

            Assert.That(host.ApplyFrameRectCallCount, Is.EqualTo(1));
            Assert.That(host.AppliedFrameTargetKey, Is.EqualTo("node"));
            Assert.That(host.AppliedFrameRect, Is.EqualTo(new Rect(100f, 200f, 300f, 400f)));
        }

        private sealed class DeferredScheduler
        {
            private readonly List<Action> _callbacks = new();

            public int ScheduledCount => _callbacks.Count;

            public void Schedule(Action callback)
            {
                _callbacks.Add(callback);
            }

            public void Unschedule(Action callback)
            {
                _callbacks.Remove(callback);
            }

            public void RunAll()
            {
                while (_callbacks.Count > 0)
                {
                    Action callback = _callbacks[0];
                    _callbacks.RemoveAt(0);
                    callback?.Invoke();
                }
            }
        }

        private sealed class FakeInspectorPanelHost : IInspectorPanelHost
        {
            public DocumentSession CurrentDocument { get; set; }
            public Rect SceneRect { get; set; }
            public int ApplyFrameRectCallCount { get; private set; }
            public string AppliedFrameTargetKey { get; private set; }
            public Rect AppliedFrameRect { get; private set; }

            public bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus) => true;

            public bool TryApplyTargetFrameRect(string targetKey, Rect targetSceneRect, string successStatus)
            {
                ApplyFrameRectCallCount++;
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
