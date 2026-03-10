using System;
using System.Linq;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasSceneHitTestHelper
    {
        private const float HitViewportRadius = 6f;
        private readonly PreviewElementHitTester _elementHitTester;

        public CanvasSceneHitTestHelper(PreviewElementHitTester elementHitTester)
        {
            _elementHitTester = elementHitTester;
        }

        public PreviewElementGeometry FindPreviewElement(PreviewSnapshot previewSnapshot, string elementKey)
        {
            if (string.IsNullOrWhiteSpace(elementKey) || previewSnapshot?.Elements == null)
                return null;

            return previewSnapshot.Elements.FirstOrDefault(item =>
                string.Equals(item.Key, elementKey, StringComparison.Ordinal));
        }

        public bool TryHitTestFrame(CanvasViewportState viewportState, Vector2 canvasLocalPoint, out Rect frameViewportRect)
        {
            if (!CanvasProjectionMath.TryGetFrameViewportRect(viewportState, out frameViewportRect))
                return false;

            return frameViewportRect.Contains(canvasLocalPoint);
        }

        public bool TryHitTestFrameChrome(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Vector2 canvasLocalPoint,
            out Rect frameViewportRect)
        {
            if (!CanvasProjectionMath.TryGetFrameViewportRect(viewportState, out frameViewportRect) ||
                !frameViewportRect.Contains(canvasLocalPoint))
            {
                return false;
            }

            if (!CanvasProjectionMath.TryGetFrameVisibleViewportRect(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    out Rect contentViewportRect))
            {
                return true;
            }

            return !contentViewportRect.Contains(canvasLocalPoint);
        }

        public bool TryHitTestPreviewElement(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            Vector2 canvasLocalPoint,
            out PreviewElementGeometry hitElement)
        {
            hitElement = null;
            if (previewSnapshot?.Elements == null)
                return false;

            if (!CanvasProjectionMath.TryViewportPointToScenePoint(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    canvasLocalPoint,
                    out Vector2 scenePoint))
            {
                return false;
            }

            float sceneHitRadius = 0f;
            if (CanvasProjectionMath.TryConvertViewportDeltaToSceneDelta(
                    viewportState,
                    previewSnapshot,
                    framePadding,
                    frameHeaderHeight,
                    new Vector2(HitViewportRadius, HitViewportRadius),
                    out Vector2 sceneRadiusDelta))
            {
                sceneHitRadius = Mathf.Max(Mathf.Abs(sceneRadiusDelta.x), Mathf.Abs(sceneRadiusDelta.y));
            }

            return _elementHitTester.TryHitTest(previewSnapshot.Elements, scenePoint, sceneHitRadius, out hitElement);
        }

        public bool TryResolveSelectedElementSceneRect(
            PreviewSnapshot previewSnapshot,
            string selectedElementKey,
            out Rect sceneRect)
        {
            sceneRect = default;
            PreviewElementGeometry previewElement = FindPreviewElement(previewSnapshot, selectedElementKey);
            if (previewElement == null)
                return false;

            sceneRect = previewElement.VisualBounds;
            return true;
        }

        public bool TryBuildCurrentSelectionViewportRect(
            CanvasViewportState viewportState,
            PreviewSnapshot previewSnapshot,
            float framePadding,
            float frameHeaderHeight,
            CanvasSelectionKind selectionKind,
            string selectedElementKey,
            out Rect selectionViewportRect)
        {
            selectionViewportRect = default;
            return selectionKind switch
            {
                CanvasSelectionKind.Frame => CanvasProjectionMath.TryGetFrameViewportRect(viewportState, out selectionViewportRect),
                CanvasSelectionKind.Element => TryResolveSelectedElementSceneRect(previewSnapshot, selectedElementKey, out Rect sceneRect) &&
                                              CanvasProjectionMath.TrySceneRectToViewportRect(
                                                  viewportState,
                                                  previewSnapshot,
                                                  framePadding,
                                                  frameHeaderHeight,
                                                  sceneRect,
                                                  out selectionViewportRect),
                _ => false
            };
        }
    }
}
