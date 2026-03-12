using System;
using Core.UI.Foundation;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Shared;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class CanvasViewportGestureHandler
    {
        private const float MinCanvasFrameSize = 96f;

        private readonly ICanvasPointerDragHost _host;
        private readonly CanvasViewportState _viewportState;
        private readonly PointerDragSession _dragSession;
        private readonly Func<VisualElement> _overlayAccessor;

        public CanvasViewportGestureHandler(
            ICanvasPointerDragHost host,
            CanvasViewportState viewportState,
            PointerDragSession dragSession,
            Func<VisualElement> overlayAccessor)
        {
            _host = host;
            _viewportState = viewportState;
            _dragSession = dragSession;
            _overlayAccessor = overlayAccessor;
        }

        public void BeginPan(CanvasGestureState state, Vector2 localPosition, int pointerId)
        {
            state.Begin(CanvasDragMode.PanCanvas, CanvasHandle.None, _viewportState.Pan, default);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }

        public void BeginFrameMove(CanvasGestureState state, Vector2 localPosition, int pointerId)
        {
            state.Begin(CanvasDragMode.MoveFrame, CanvasHandle.None, default, _viewportState.FrameRect);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }

        public void BeginFrameResize(CanvasGestureState state, CanvasHandle handle, Vector2 localPosition, int pointerId)
        {
            state.Begin(CanvasDragMode.ResizeFrame, handle, default, _viewportState.FrameRect);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }

        public bool ApplyViewportDelta(CanvasGestureState state, Vector2 viewportDelta)
        {
            switch (state.Mode)
            {
                case CanvasDragMode.PanCanvas:
                    _viewportState.SetPan(state.DragStartCanvasPan + viewportDelta);
                    _host.UpdateCanvasVisualState();
                    return true;
                case CanvasDragMode.MoveFrame:
                    _viewportState.SetFrameRect(new Rect(
                        state.DragStartFrameRect.position + _viewportState.ViewportToCanvasDelta(viewportDelta),
                        state.DragStartFrameRect.size));
                    _host.UpdateCanvasVisualState();
                    return true;
                case CanvasDragMode.ResizeFrame:
                    _viewportState.SetFrameRect(RectResizeUtility.ResizeRect(
                        state.DragStartFrameRect,
                        state.ActiveHandle,
                        _viewportState.ViewportToCanvasDelta(viewportDelta),
                        MinCanvasFrameSize));
                    _host.UpdateCanvasVisualState();
                    return true;
                default:
                    return false;
            }
        }

        public void Complete()
        {
            _host.UpdateCanvasVisualState();
        }
    }
}
