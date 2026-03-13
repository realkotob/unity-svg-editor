using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor.Workspace.Canvas
{
    internal static class SelectionChromeGeometry
    {
        internal const float SelectionHandleHalfSize = 4f;

        private const float EdgeZoneThickness = 12f;
        private const float RotateZoneHalfSize = 18f;

        public static Rect BuildHandleRect(VisualElement element)
        {
            float width = element.resolvedStyle.width > 0f
                ? element.resolvedStyle.width
                : SelectionHandleHalfSize * 2f;
            float height = element.resolvedStyle.height > 0f
                ? element.resolvedStyle.height
                : SelectionHandleHalfSize * 2f;
            return new Rect(element.resolvedStyle.left, element.resolvedStyle.top, width, height);
        }

        public static Rect BuildRect(VisualElement element)
        {
            return new Rect(
                element.resolvedStyle.left,
                element.resolvedStyle.top,
                element.resolvedStyle.width,
                element.resolvedStyle.height);
        }

        public static void PositionHandle(VisualElement element, float x, float y)
        {
            if (element == null)
            {
                return;
            }

            element.style.left = x - SelectionHandleHalfSize;
            element.style.top = y - SelectionHandleHalfSize;
        }

        public static void PositionEdgeZones(
            IReadOnlyDictionary<SelectionHandle, VisualElement> edgeZones,
            Rect selectionRect,
            bool visible)
        {
            PositionEdgeZone(
                edgeZones,
                SelectionHandle.Top,
                selectionRect.xMin + SelectionHandleHalfSize,
                selectionRect.yMin - (EdgeZoneThickness * 0.5f),
                Mathf.Max(0f, selectionRect.width - (SelectionHandleHalfSize * 2f)),
                EdgeZoneThickness,
                visible);
            PositionEdgeZone(
                edgeZones,
                SelectionHandle.Right,
                selectionRect.xMax - (EdgeZoneThickness * 0.5f),
                selectionRect.yMin + SelectionHandleHalfSize,
                EdgeZoneThickness,
                Mathf.Max(0f, selectionRect.height - (SelectionHandleHalfSize * 2f)),
                visible);
            PositionEdgeZone(
                edgeZones,
                SelectionHandle.Bottom,
                selectionRect.xMin + SelectionHandleHalfSize,
                selectionRect.yMax - (EdgeZoneThickness * 0.5f),
                Mathf.Max(0f, selectionRect.width - (SelectionHandleHalfSize * 2f)),
                EdgeZoneThickness,
                visible);
            PositionEdgeZone(
                edgeZones,
                SelectionHandle.Left,
                selectionRect.xMin - (EdgeZoneThickness * 0.5f),
                selectionRect.yMin + SelectionHandleHalfSize,
                EdgeZoneThickness,
                Mathf.Max(0f, selectionRect.height - (SelectionHandleHalfSize * 2f)),
                visible);
        }

        public static void PositionRotationZones(
            IReadOnlyList<VisualElement> rotationZones,
            Rect selectionRect,
            bool visible)
        {
            if (rotationZones == null || rotationZones.Count < 4)
            {
                return;
            }

            PositionRotationZone(rotationZones[0], selectionRect.min, new Vector2(-RotateZoneHalfSize, -RotateZoneHalfSize), visible);
            PositionRotationZone(rotationZones[1], new Vector2(selectionRect.xMax, selectionRect.yMin), new Vector2(0f, -RotateZoneHalfSize), visible);
            PositionRotationZone(rotationZones[2], selectionRect.max, Vector2.zero, visible);
            PositionRotationZone(rotationZones[3], new Vector2(selectionRect.xMin, selectionRect.yMax), new Vector2(-RotateZoneHalfSize, 0f), visible);
        }

        public static string ResolveSelectionHandleCursorClass(SelectionHandle handle)
        {
            return handle switch
            {
                SelectionHandle.TopLeft => OverlayClassName.SELECTION_HANDLE_TOP_LEFT,
                SelectionHandle.Top => OverlayClassName.SELECTION_HANDLE_TOP,
                SelectionHandle.TopRight => OverlayClassName.SELECTION_HANDLE_TOP_RIGHT,
                SelectionHandle.Right => OverlayClassName.SELECTION_HANDLE_RIGHT,
                SelectionHandle.BottomRight => OverlayClassName.SELECTION_HANDLE_BOTTOM_RIGHT,
                SelectionHandle.Bottom => OverlayClassName.SELECTION_HANDLE_BOTTOM,
                SelectionHandle.BottomLeft => OverlayClassName.SELECTION_HANDLE_BOTTOM_LEFT,
                SelectionHandle.Left => OverlayClassName.SELECTION_HANDLE_LEFT,
                _ => string.Empty
            };
        }

        public static bool TryHitRotationZone(CanvasSelectionVisual selection, Vector2 localPoint, out SelectionHandle handle)
        {
            handle = SelectionHandle.None;
            if (selection == null || !selection.AllowSelectionHandleInteraction)
            {
                return false;
            }

            Rect selectionRect = selection.Rect;
            if (TryHitCornerRotationZone(localPoint, selectionRect.min, point => point.x <= selectionRect.xMin && point.y <= selectionRect.yMin) ||
                TryHitCornerRotationZone(localPoint, new Vector2(selectionRect.xMax, selectionRect.yMin), point => point.x >= selectionRect.xMax && point.y <= selectionRect.yMin) ||
                TryHitCornerRotationZone(localPoint, selectionRect.max, point => point.x >= selectionRect.xMax && point.y >= selectionRect.yMax) ||
                TryHitCornerRotationZone(localPoint, new Vector2(selectionRect.xMin, selectionRect.yMax), point => point.x <= selectionRect.xMin && point.y >= selectionRect.yMax))
            {
                handle = SelectionHandle.Rotate;
                return true;
            }

            return false;
        }

        private static void PositionEdgeZone(
            IReadOnlyDictionary<SelectionHandle, VisualElement> edgeZones,
            SelectionHandle handle,
            float left,
            float top,
            float width,
            float height,
            bool visible)
        {
            if (edgeZones == null || !edgeZones.TryGetValue(handle, out VisualElement element))
            {
                return;
            }

            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.left = left;
            element.style.top = top;
            element.style.width = width;
            element.style.height = height;
        }

        private static void PositionRotationZone(VisualElement element, Vector2 corner, Vector2 offset, bool visible)
        {
            if (element == null)
            {
                return;
            }

            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.left = corner.x + offset.x;
            element.style.top = corner.y + offset.y;
            element.style.width = RotateZoneHalfSize;
            element.style.height = RotateZoneHalfSize;
        }

        private static bool TryHitCornerRotationZone(Vector2 localPoint, Vector2 corner, Func<Vector2, bool> diagonalPredicate)
        {
            Rect outerZone = new(
                corner.x - RotateZoneHalfSize,
                corner.y - RotateZoneHalfSize,
                RotateZoneHalfSize * 2f,
                RotateZoneHalfSize * 2f);
            Rect resizeZone = new(
                corner.x - SelectionHandleHalfSize,
                corner.y - SelectionHandleHalfSize,
                SelectionHandleHalfSize * 2f,
                SelectionHandleHalfSize * 2f);

            return outerZone.Contains(localPoint) &&
                   !resizeZone.Contains(localPoint) &&
                   diagonalPredicate(localPoint);
        }
    }
}
