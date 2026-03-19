using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal sealed class ToolController
    {
        private static class UssClassName
        {
            private const string Prefix = "svg-editor__";

            public const string TOOL_BUTTON_ACTIVE = Prefix + "tool-btn--active";
            public const string CANVAS_OVERLAY_READONLY = Prefix + "canvas-overlay--readonly";
            public const string CANVAS_OVERLAY_PAN_ARMED = Prefix + "canvas-overlay--pan-armed";
        }

        private const float MIN_CANVAS_ZOOM = 0.25f;
        private const float MAX_CANVAS_ZOOM = 16f;
        private static readonly ViewportFrameLayoutSettings CanvasFrameLayout = new(72f, 0f, 0f);

        private readonly Dictionary<ToolKind, Toggle> _toolButtons = new();
        private Toggle _moveToolToggle;

        public ToolKind ActiveTool { get; private set; } = ToolKind.Move;
        public bool IsSpacePanArmed { get; private set; }

        public void BindMoveTool(Toggle toggle)
        {
            if (toggle == null)
            {
                return;
            }

            _moveToolToggle = toggle;
            toggle.tooltip = "Move";
            toggle.text = string.Empty;
            toggle.UnregisterValueChangedCallback(OnMoveToolToggleValueChanged);
            toggle.RegisterValueChangedCallback(OnMoveToolToggleValueChanged);

            bool isActive = ActiveTool == ToolKind.Move;
            toggle.SetValueWithoutNotify(isActive);
            toggle.EnableClass(UssClassName.TOOL_BUTTON_ACTIVE, isActive);
            _toolButtons[ToolKind.Move] = toggle;
        }

        public void Dispose()
        {
            if (_moveToolToggle != null)
            {
                _moveToolToggle.UnregisterValueChangedCallback(OnMoveToolToggleValueChanged);
                _moveToolToggle = null;
            }

            _toolButtons.Clear();
        }

        public void UpdateVisualState(VisualElement canvasOverlay)
        {
            foreach (KeyValuePair<ToolKind, Toggle> pair in _toolButtons)
            {
                bool isActive = pair.Key == ActiveTool;
                pair.Value.SetValueWithoutNotify(isActive);
                pair.Value.EnableClass(UssClassName.TOOL_BUTTON_ACTIVE, isActive);
            }

            if (canvasOverlay != null)
            {
                canvasOverlay.EnableClass(
                    UssClassName.CANVAS_OVERLAY_READONLY,
                    ActiveTool != ToolKind.Move);
                canvasOverlay.EnableClass(
                    UssClassName.CANVAS_OVERLAY_PAN_ARMED,
                    IsSpacePanArmed);
            }
        }

        public bool IsPanGesture(PointerDownEvent evt)
        {
            return evt.button == (int)MouseButton.MiddleMouse ||
                   (IsSpacePanArmed && evt.button == (int)MouseButton.LeftMouse);
        }

        public void HandleWheel(
            WheelEvent evt,
            VisualElement canvasOverlay,
            PreviewSnapshot previewSnapshot,
            SceneProjector sceneProjector,
            ViewportState viewportState,
            Action updateCanvasVisualState)
        {
            if (canvasOverlay == null || previewSnapshot == null)
            {
                return;
            }

            Vector2 localPosition = evt.localMousePosition;
            if (!canvasOverlay.contentRect.Contains(localPosition) &&
                !sceneProjector.TryGetCanvasLocalPosition(canvasOverlay, evt.mousePosition, out localPosition))
            {
                localPosition = canvasOverlay.contentRect.center;
            }

            viewportState.ZoomAtPoint(
                sceneProjector.GetCanvasBounds(canvasOverlay),
                sceneProjector.GetPreviewSceneRect(previewSnapshot),
                localPosition,
                evt.delta.y,
                MIN_CANVAS_ZOOM,
                MAX_CANVAS_ZOOM,
                CanvasFrameLayout.Padding,
                CanvasFrameLayout.HeaderHeight);

            updateCanvasVisualState?.Invoke();
            evt.StopPropagation();
        }

        public bool HandleKeyDown(
            KeyDownEvent evt,
            VisualElement canvasOverlay,
            PreviewSnapshot previewSnapshot,
            SceneProjector sceneProjector,
            ViewportState viewportState,
            Action updateCanvasVisualState,
            Func<Vector2, bool> nudgeSelectedElement)
        {
            if (evt.keyCode == KeyCode.Space)
            {
                IsSpacePanArmed = true;
                evt.StopPropagation();
                return true;
            }

            Vector2 nudgeDelta = evt.keyCode switch
            {
                KeyCode.LeftArrow => Vector2.left,
                KeyCode.RightArrow => Vector2.right,
                KeyCode.UpArrow => Vector2.down,
                KeyCode.DownArrow => Vector2.up,
                _ => Vector2.zero
            };
            if (nudgeDelta != Vector2.zero)
            {
                float nudgeAmount = (evt.modifiers & EventModifiers.Shift) != 0 ? 10f : 1f;
                if (nudgeSelectedElement?.Invoke(nudgeDelta * nudgeAmount) == true)
                {
                    evt.StopPropagation();
                    return true;
                }
            }

            bool isActionKeyPressed = (evt.modifiers & EventModifiers.Command) != 0 ||
                                      (evt.modifiers & EventModifiers.Control) != 0;
            if (!isActionKeyPressed)
            {
                return false;
            }

            if (canvasOverlay == null || previewSnapshot == null)
            {
                return false;
            }

            if (evt.keyCode == KeyCode.Alpha0)
            {
                viewportState.ResetToFit(
                    sceneProjector.GetCanvasBounds(canvasOverlay),
                    sceneProjector.GetPreviewSceneRect(previewSnapshot),
                    CanvasFrameLayout);
                updateCanvasVisualState?.Invoke();
                evt.StopPropagation();
                return true;
            }

            if (evt.keyCode == KeyCode.Alpha1)
            {
                viewportState.ResetToActualSize(
                    sceneProjector.GetCanvasBounds(canvasOverlay),
                    sceneProjector.GetPreviewSceneRect(previewSnapshot),
                    CanvasFrameLayout.Padding,
                    CanvasFrameLayout.HeaderHeight);
                updateCanvasVisualState?.Invoke();
                evt.StopPropagation();
                return true;
            }

            return false;
        }

        public bool HandleKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Space)
            {
                return false;
            }

            IsSpacePanArmed = false;
            evt.StopPropagation();
            return true;
        }

        private void OnMoveToolToggleValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                ActiveTool = ToolKind.Move;
                return;
            }

            if (ActiveTool == ToolKind.Move)
            {
                _moveToolToggle?.SetValueWithoutNotify(true);
            }
        }
    }
}
