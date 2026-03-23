using System;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal sealed class ViewportGestureHandler
    {
        private const float MinCanvasFrameSize = 96f;

        private readonly ICanvasPointerDragHost _host;
        private readonly ViewportState _viewportState;
        private readonly PointerDragSession _dragSession;
        private readonly Func<VisualElement> _overlayAccessor;

        public ViewportGestureHandler(
            ICanvasPointerDragHost host,
            ViewportState viewportState,
            PointerDragSession dragSession,
            Func<VisualElement> overlayAccessor)
        {
            _host = host;
            _viewportState = viewportState;
            _dragSession = dragSession;
            _overlayAccessor = overlayAccessor;
        }

        public void BeginPan(GestureState state, Vector2 localPosition, int pointerId)
        {
            state.Begin(DragMode.PanCanvas, SelectionHandle.None, _viewportState.Pan, default);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }

        public void BeginFrameMove(GestureState state, Vector2 localPosition, int pointerId)
        {
            state.Begin(DragMode.MoveFrame, SelectionHandle.None, default, _viewportState.FrameRect);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }

        public void BeginFrameResize(GestureState state, SelectionHandle handle, Vector2 localPosition, int pointerId)
        {
            state.Begin(DragMode.ResizeFrame, handle, default, _viewportState.FrameRect);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }

        public bool ApplyViewportDelta(GestureState state, Vector2 viewportDelta)
        {
            switch (state.Mode)
            {
                case DragMode.PanCanvas:
                    _viewportState.SetPan(state.DragStartCanvasPan + viewportDelta);
                    _host.UpdateViewportVisualState();
                    return true;
                case DragMode.MoveFrame:
                    _viewportState.SetFrameRect(new Rect(
                        state.DragStartFrameRect.position + _viewportState.ViewportToCanvasDelta(viewportDelta),
                        state.DragStartFrameRect.size));
                    _host.UpdateViewportVisualState();
                    return true;
                case DragMode.ResizeFrame:
                    _viewportState.SetFrameRect(RectResizeUtility.ResizeRect(
                        state.DragStartFrameRect,
                        state.ActiveHandle,
                        _viewportState.ViewportToCanvasDelta(viewportDelta),
                        MinCanvasFrameSize));
                    _host.UpdateViewportVisualState();
                    return true;
                default:
                    return false;
            }
        }

        public void Complete()
        {
            _host.UpdateViewportVisualState();
        }
    }
}
