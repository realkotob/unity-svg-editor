using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.Foundation;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasToolController
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
        private const float CANVAS_FRAME_PADDING = 0f;
        private const float CANVAS_FRAME_HEADER_HEIGHT = 0f;
        private const float CANVAS_FRAME_MARGIN = 72f;

        private readonly Dictionary<CanvasTool, Toggle> _toolButtons = new();
        private Toggle _moveToolToggle;

        public CanvasTool ActiveTool { get; private set; } = CanvasTool.Move;
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

            bool isActive = ActiveTool == CanvasTool.Move;
            toggle.SetValueWithoutNotify(isActive);
            toggle.EnableClass(UssClassName.TOOL_BUTTON_ACTIVE, isActive);
            _toolButtons[CanvasTool.Move] = toggle;
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
            foreach (KeyValuePair<CanvasTool, Toggle> pair in _toolButtons)
            {
                bool isActive = pair.Key == ActiveTool;
                pair.Value.SetValueWithoutNotify(isActive);
                pair.Value.EnableClass(UssClassName.TOOL_BUTTON_ACTIVE, isActive);
            }

            if (canvasOverlay != null)
            {
                canvasOverlay.EnableClass(
                    UssClassName.CANVAS_OVERLAY_READONLY,
                    ActiveTool != CanvasTool.Move);
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
            CanvasSceneProjector sceneProjector,
            CanvasViewportState viewportState,
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
                CANVAS_FRAME_PADDING,
                CANVAS_FRAME_HEADER_HEIGHT);

            updateCanvasVisualState?.Invoke();
            evt.StopPropagation();
        }

        public bool HandleKeyDown(
            KeyDownEvent evt,
            VisualElement canvasOverlay,
            PreviewSnapshot previewSnapshot,
            CanvasSceneProjector sceneProjector,
            CanvasViewportState viewportState,
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
                    CANVAS_FRAME_MARGIN,
                    CANVAS_FRAME_PADDING,
                    CANVAS_FRAME_HEADER_HEIGHT);
                updateCanvasVisualState?.Invoke();
                evt.StopPropagation();
                return true;
            }

            if (evt.keyCode == KeyCode.Alpha1)
            {
                viewportState.ResetToActualSize(
                    sceneProjector.GetCanvasBounds(canvasOverlay),
                    sceneProjector.GetPreviewSceneRect(previewSnapshot),
                    CANVAS_FRAME_PADDING,
                    CANVAS_FRAME_HEADER_HEIGHT);
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
                ActiveTool = CanvasTool.Move;
                return;
            }

            if (ActiveTool == CanvasTool.Move)
            {
                _moveToolToggle?.SetValueWithoutNotify(true);
            }
        }
    }
}
