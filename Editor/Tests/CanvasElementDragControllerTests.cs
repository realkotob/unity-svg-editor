using NUnit.Framework;
using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class CanvasElementDragControllerTests
    {
        [Test]
        public void MoveTransientState_RefreshesPreviewAndInspector_AndCommitsOnce()
        {
            CanvasElementDragController controller = CreateController();
            FakeCanvasPointerDragHost host = new()
            {
                CurrentDocument = CreateDocument("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" width=\"20\" height=\"10\" /></svg>"),
                PreviewSnapshot = CreatePreviewSnapshot()
            };

            controller.BeginMove(
                host.CurrentDocument,
                host.PreviewSnapshot,
                "node",
                Vector2.zero,
                new Rect(0f, 0f, 20f, 10f),
                Matrix2D.identity);

            Vector2 viewportDelta = controller.UpdateMove(new Vector2(10f, 5f));
            bool previewUpdated = controller.TryUpdateMoveTransientState(host, viewportDelta);
            bool committed = controller.TryCommitDrag(host, CanvasDragMode.MoveElement, CanvasHandle.None, viewportDelta);

            Assert.That(previewUpdated, Is.True);
            Assert.That(host.TransientPreviewRefreshCount, Is.EqualTo(1));
            Assert.That(host.InspectorRefreshCount, Is.EqualTo(1));
            Assert.That(committed, Is.True);
            Assert.That(host.ApplyUpdatedSourceCount, Is.EqualTo(1));
            Assert.That(host.LastUpdatedSource, Does.Contain("translate(10 5)"));
        }

        [Test]
        public void ResizeTransientState_RefreshesPreviewAndCommitsFromTransientModel()
        {
            CanvasElementDragController controller = CreateController();
            FakeCanvasPointerDragHost host = new()
            {
                CurrentDocument = CreateDocument("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" width=\"20\" height=\"10\" /></svg>"),
                PreviewSnapshot = CreatePreviewSnapshot()
            };

            controller.BeginResize(
                host.CurrentDocument,
                "node",
                host.PreviewSnapshot.CanvasViewportRect,
                host.PreviewSnapshot.PreserveAspectRatioMode,
                new Rect(0f, 0f, 20f, 10f),
                new Rect(0f, 0f, 20f, 10f),
                Matrix2D.identity);

            controller.UpdateResize(new Vector2(10f, 0f), CanvasHandle.Right, uniformScale: false, centerAnchor: false);
            bool previewUpdated = controller.TryUpdateResizeTransientState(host, CanvasHandle.Right);
            bool committed = controller.TryCommitDrag(host, CanvasDragMode.ResizeElement, CanvasHandle.Right, new Vector2(10f, 0f));

            Assert.That(previewUpdated, Is.True);
            Assert.That(host.TransientPreviewRefreshCount, Is.EqualTo(1));
            Assert.That(host.InspectorRefreshCount, Is.EqualTo(1));
            Assert.That(committed, Is.True);
            Assert.That(host.ApplyUpdatedSourceCount, Is.EqualTo(1));
            Assert.That(host.LastUpdatedSource, Does.Contain("scale("));
        }

        [Test]
        public void TryCommitDrag_SkipsApply_WhenCanvasDeltaIsBelowThreshold()
        {
            CanvasElementDragController controller = CreateController();
            FakeCanvasPointerDragHost host = new()
            {
                CurrentDocument = CreateDocument("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" width=\"20\" height=\"10\" /></svg>"),
                PreviewSnapshot = CreatePreviewSnapshot()
            };

            controller.BeginMove(
                host.CurrentDocument,
                host.PreviewSnapshot,
                "node",
                Vector2.zero,
                new Rect(0f, 0f, 20f, 10f),
                Matrix2D.identity);

            Vector2 viewportDelta = controller.UpdateMove(new Vector2(1f, 1f));
            controller.TryUpdateMoveTransientState(host, viewportDelta);
            bool committed = controller.TryCommitDrag(host, CanvasDragMode.MoveElement, CanvasHandle.None, viewportDelta);

            Assert.That(committed, Is.False);
            Assert.That(host.ApplyUpdatedSourceCount, Is.EqualTo(0));
        }

        private static CanvasElementDragController CreateController()
        {
            CanvasViewportState viewportState = new();
            viewportState.SetFrameRect(new Rect(0f, 0f, 200f, 100f));
            CanvasSceneProjector sceneProjector = new(
                viewportState,
                new PreviewElementHitTester(),
                framePadding: 0f,
                frameHeaderHeight: 0f,
                alignmentGuideThreshold: 2f);
            return new CanvasElementDragController(new StructureEditor(), sceneProjector);
        }

        private static PreviewSnapshot CreatePreviewSnapshot()
        {
            return new PreviewSnapshot
            {
                ProjectionRect = new Rect(0f, 0f, 200f, 100f),
                PreserveAspectRatioMode = SvgPreserveAspectRatioMode.Meet
            };
        }

        private static DocumentSession CreateDocument(string sourceText)
        {
            return new DocumentSession
            {
                WorkingSourceText = sourceText,
                DocumentModel = SvgFixtureTestUtility.LoadModel(sourceText)
            };
        }

        private sealed class FakeCanvasPointerDragHost : ICanvasPointerDragHost
        {
            public DocumentSession CurrentDocument { get; set; }
            public PreviewSnapshot PreviewSnapshot { get; set; }
            public string SelectedElementKey { get; set; } = "node";
            public CanvasSelectionKind SelectionKind { get; set; } = CanvasSelectionKind.Element;
            public int TransientPreviewRefreshCount { get; private set; }
            public int InspectorRefreshCount { get; private set; }
            public int ApplyUpdatedSourceCount { get; private set; }
            public string LastUpdatedSource { get; private set; } = string.Empty;

            public void RefreshLivePreview(bool keepExistingPreviewOnFailure)
            {
            }

            public bool TryRefreshTransientPreview(string sourceText)
            {
                TransientPreviewRefreshCount++;
                return true;
            }

            public void RefreshInspectorFromSource(string sourceText)
            {
                InspectorRefreshCount++;
            }

            public void ApplyUpdatedSource(string updatedSource, string successStatus)
            {
                ApplyUpdatedSourceCount++;
                LastUpdatedSource = updatedSource ?? string.Empty;
            }

            public void UpdateSourceStatus(string status)
            {
            }

            public StructureNode FindStructureNode(string elementKey)
            {
                return new StructureNode
                {
                    Key = elementKey,
                    TagName = "rect"
                };
            }

            public void SelectFrame()
            {
            }

            public void SelectElement(string elementKey, bool syncPatchTarget)
            {
            }

            public void ClearSelection()
            {
            }

            public void UpdateStructureInteractivity(bool hasDocument)
            {
            }

            public void UpdateCanvasVisualState()
            {
            }

            public void UpdateSelectionVisual()
            {
            }

            public void SetHoveredElement(string elementKey)
            {
            }

            public void ClearHover()
            {
            }

            public void UpdateHoverVisual()
            {
            }
        }
    }
}
