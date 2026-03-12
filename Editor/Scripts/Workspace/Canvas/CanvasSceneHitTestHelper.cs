using System;
using System.Linq;
using UnityEngine;

namespace UnitySvgEditor.Editor.Workspace.Canvas
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

            PreviewElementGeometry bestMatch = null;
            float bestArea = float.MinValue;
            int bestDrawOrder = int.MinValue;

            for (int index = 0; index < previewSnapshot.Elements.Count; index++)
            {
                PreviewElementGeometry candidate = previewSnapshot.Elements[index];
                if (candidate == null || !string.Equals(candidate.Key, elementKey, StringComparison.Ordinal))
                    continue;

                float area = candidate.VisualBounds.width * candidate.VisualBounds.height;
                bool isBetter =
                    bestMatch == null ||
                    (candidate.IsTextOverlay && !bestMatch.IsTextOverlay) ||
                    (candidate.IsTextOverlay == bestMatch.IsTextOverlay &&
                     (area > bestArea ||
                      (Mathf.Approximately(area, bestArea) && candidate.DrawOrder > bestDrawOrder)));

                if (!isBetter)
                    continue;

                bestMatch = candidate;
                bestArea = area;
                bestDrawOrder = candidate.DrawOrder;
            }

            return bestMatch;
        }

        public bool TryHitTestFrame(CanvasViewportState viewportState, Vector2 canvasLocalPoint, out Rect frameViewportRect)
        {
            if (!CanvasViewportLayoutUtility.TryGetFrameViewportRect(viewportState, out frameViewportRect))
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
            if (!CanvasViewportLayoutUtility.TryGetFrameViewportRect(viewportState, out frameViewportRect) ||
                !frameViewportRect.Contains(canvasLocalPoint))
            {
                return false;
            }

            if (!CanvasViewportLayoutUtility.TryGetFrameVisibleViewportRect(
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

            if (!CanvasViewportLayoutUtility.TryViewportPointToScenePoint(
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
            if (CanvasViewportLayoutUtility.TryConvertViewportDeltaToSceneDelta(
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
                CanvasSelectionKind.Frame => CanvasViewportLayoutUtility.TryGetFrameViewportRect(viewportState, out selectionViewportRect),
                CanvasSelectionKind.Element => TryResolveSelectedElementSceneRect(previewSnapshot, selectedElementKey, out Rect sceneRect) &&
                                              CanvasViewportLayoutUtility.TrySceneRectToViewportRect(
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
