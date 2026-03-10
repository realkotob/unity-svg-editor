using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class InspectorImmediateApplyTests
    {
        [Test]
        public void ChangingOpacitySlider_AppliesOnlyOpacityPatch()
        {
            var host = new FakeInspectorPanelHost();
            var root = CreateInspectorRoot();
            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => host, null);

            view.Bind(root);

            root.Q<Slider>("inspector-opacity").SetValueWithoutNotify(0.25f);
            service.ApplyImmediatePatch(InspectorPanelView.ImmediateApplyField.Opacity);

            Assert.That(host.ApplyCallCount, Is.EqualTo(1));
            Assert.That(host.LastRequest, Is.Not.Null);
            Assert.That(host.LastRequest.TargetKey, Is.EqualTo(AttributePatcher.ROOT_TARGET_KEY));
            Assert.That(host.LastRequest.Opacity, Is.EqualTo("0.25"));
            Assert.That(host.LastRequest.Fill, Is.Null);
            Assert.That(host.LastRequest.Stroke, Is.Null);
            Assert.That(host.LastRequest.StrokeWidth, Is.Null);
            Assert.That(host.LastRequest.StrokeDasharray, Is.Null);
        }

        [Test]
        public void ChangingFillColor_AppliesFillAndFillOpacityOnly()
        {
            var host = new FakeInspectorPanelHost();
            var root = CreateInspectorRoot();
            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => host, null);

            view.Bind(root);

            root.Q<ColorField>("inspector-fill-color").SetValueWithoutNotify(new Color32(0x12, 0x34, 0x56, 0x40));
            service.ApplyImmediatePatch(InspectorPanelView.ImmediateApplyField.FillColor);

            Assert.That(host.ApplyCallCount, Is.EqualTo(1));
            Assert.That(host.LastRequest, Is.Not.Null);
            Assert.That(host.LastRequest.Fill, Is.EqualTo("#123456"));
            Assert.That(host.LastRequest.FillOpacity, Is.EqualTo("0.251"));
            Assert.That(host.LastRequest.Stroke, Is.Null);
            Assert.That(host.LastRequest.StrokeOpacity, Is.Null);
            Assert.That(host.LastRequest.Opacity, Is.Null);
        }

        [Test]
        public void ChangingDashGap_AppliesCombinedDasharrayPatch()
        {
            var host = new FakeInspectorPanelHost();
            var root = CreateInspectorRoot();
            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => host, null);

            view.Bind(root);

            var dashLengthField = root.Q<FloatField>("inspector-dash-length");
            var dashGapField = root.Q<FloatField>("inspector-dash-gap");
            dashLengthField.SetValueWithoutNotify(6f);
            dashGapField.SetValueWithoutNotify(3f);

            service.ApplyImmediatePatch(InspectorPanelView.ImmediateApplyField.StrokeDasharray);

            Assert.That(host.ApplyCallCount, Is.EqualTo(1));
            Assert.That(host.LastRequest, Is.Not.Null);
            Assert.That(host.LastRequest.StrokeDasharray, Is.EqualTo("6 3"));
            Assert.That(host.LastRequest.Fill, Is.Null);
            Assert.That(host.LastRequest.Stroke, Is.Null);
            Assert.That(host.LastRequest.StrokeWidth, Is.Null);
        }

        [Test]
        public void ChangingLinejoin_AppliesOnlyLinejoinPatch()
        {
            var host = new FakeInspectorPanelHost();
            var root = CreateInspectorRoot();
            var view = new InspectorPanelView();
            var state = new InspectorPanelState();
            var service = new InspectorTargetSyncService(new AttributePatcher(), state, view, () => host, null);

            view.Bind(root);

            root.Q<DropdownField>("inspector-linejoin").SetValueWithoutNotify("round");
            service.ApplyImmediatePatch(InspectorPanelView.ImmediateApplyField.StrokeLinejoin);

            Assert.That(host.ApplyCallCount, Is.EqualTo(1));
            Assert.That(host.LastRequest, Is.Not.Null);
            Assert.That(host.LastRequest.StrokeLinejoin, Is.EqualTo("round"));
            Assert.That(host.LastRequest.Fill, Is.Null);
            Assert.That(host.LastRequest.Stroke, Is.Null);
            Assert.That(host.LastRequest.StrokeDasharray, Is.Null);
        }

        private static VisualElement CreateInspectorRoot()
        {
            var root = new VisualElement();
            root.Add(new ColorField { name = "inspector-fill-color" });
            root.Add(new ColorField { name = "inspector-stroke-color" });
            root.Add(new FloatField { name = "inspector-stroke-width" });
            root.Add(new Slider(0f, 1f) { name = "inspector-opacity", value = 1f });
            root.Add(new DropdownField { name = "inspector-linecap" });
            root.Add(new DropdownField { name = "inspector-linejoin" });
            root.Add(new FloatField { name = "inspector-dash-length", value = 4f });
            root.Add(new FloatField { name = "inspector-dash-gap", value = 2f });
            root.Add(new TextField { name = "inspector-transform" });
            return root;
        }

        private sealed class FakeInspectorPanelHost : IInspectorPanelHost
        {
            public FakeInspectorPanelHost()
            {
                CurrentDocument = new DocumentSession
                {
                    OriginalSourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\" />",
                    WorkingSourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\" />"
                };
            }

            public DocumentSession CurrentDocument { get; }
            public AttributePatchRequest LastRequest { get; private set; }
            public int ApplyCallCount { get; private set; }

            public bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus)
            {
                LastRequest = request;
                ApplyCallCount++;
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
