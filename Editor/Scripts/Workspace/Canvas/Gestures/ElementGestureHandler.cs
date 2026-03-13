using System;
using Core.UI.Foundation;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Shared;
using SvgEditor.Document;
using SvgEditor.Workspace.Transforms;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class ElementGestureHandler
    {
        private readonly ICanvasPointerDragHost _host;
        private readonly SceneProjector _sceneProjector;
        private readonly ElementDragController _elementDragController;
        private readonly SelectionSyncService _selectionSyncService;
        private readonly PointerDragSession _dragSession;
        private readonly Func<VisualElement> _overlayAccessor;

        public ElementGestureHandler(
            ICanvasPointerDragHost host,
            SceneProjector sceneProjector,
            ElementDragController elementDragController,
            SelectionSyncService selectionSyncService,
            PointerDragSession dragSession,
            Func<VisualElement> overlayAccessor)
        {
            _host = host;
            _sceneProjector = sceneProjector;
            _elementDragController = elementDragController;
            _selectionSyncService = selectionSyncService;
            _dragSession = dragSession;
            _overlayAccessor = overlayAccessor;
        }

        public bool TryBeginResizeFromHandle(GestureState state, SelectionHandle handle, Vector2 localPosition, int pointerId)
        {
            if (_host.HasDefinitionProxySelection ||
                _host.SelectionKind != SelectionKind.Element ||
                !_sceneProjector.TryResolveSelectedElementSceneRect(_host.PreviewSnapshot, _host.SelectedElementKey, out Rect selectedElementSceneRect) ||
                !_sceneProjector.TryBuildCurrentSelectionViewportRect(_host.PreviewSnapshot, _host.SelectionKind, _host.SelectedElementKey, out Rect selectionViewportRect))
            {
                return false;
            }

            string tagName = _host.FindHierarchyNode(_host.SelectedElementKey)?.TagName;
            if (string.Equals(tagName, SvgTagName.TSPAN, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tagName, SvgTagName.TEXT_PATH, StringComparison.OrdinalIgnoreCase))
            {
                _host.UpdateSourceStatus($"Resize is not supported for <{tagName}>. Move/select is still available.");
                return false;
            }

            PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, _host.SelectedElementKey);
            Matrix2D parentWorldTransform = selectedGeometry?.ParentWorldTransform ?? Matrix2D.identity;
            BeginResize(
                state,
                handle,
                localPosition,
                pointerId,
                new ResizeBeginRequest(
                    _host.CurrentDocument,
                    _host.SelectedElementKey,
                    _host.PreviewSnapshot.CanvasViewportRect,
                    _host.PreviewSnapshot.PreserveAspectRatioMode,
                    selectionViewportRect,
                    selectedElementSceneRect,
                    parentWorldTransform));
            return true;
        }

        public bool TryBeginRotateFromHandle(GestureState state, Vector2 localPosition, int pointerId)
        {
            if (_host.HasDefinitionProxySelection ||
                _host.SelectionKind != SelectionKind.Element ||
                !_sceneProjector.TryResolveSelectedElementSceneRect(_host.PreviewSnapshot, _host.SelectedElementKey, out Rect selectedElementSceneRect))
            {
                return false;
            }

            PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, _host.SelectedElementKey);
            Matrix2D parentWorldTransform = selectedGeometry?.ParentWorldTransform ?? Matrix2D.identity;
            Vector2 rotationPivotWorld = selectedGeometry?.RotationPivotWorld ?? selectedElementSceneRect.center;
            Vector2 rotationPivotParentSpace = selectedGeometry?.RotationPivotParentSpace ??
                                              ElementRotationUtility.ToParentSpacePoint(parentWorldTransform, selectedElementSceneRect.center);
            state.Begin(DragMode.RotateElement, SelectionHandle.Rotate, default, default);
            _elementDragController.BeginRotate(
                _host.CurrentDocument,
                _host.PreviewSnapshot,
                _host.SelectedElementKey,
                localPosition,
                selectedElementSceneRect,
                parentWorldTransform,
                rotationPivotWorld,
                rotationPivotParentSpace);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
            return true;
        }

        public void BeginMove(GestureState state, string elementKey, Vector2 localPosition, int pointerId, Rect elementSceneRect, Matrix2D parentWorldTransform)
        {
            state.Begin(DragMode.MoveElement, SelectionHandle.None, default, default);
            _elementDragController.BeginMove(_host.CurrentDocument, _host.PreviewSnapshot, elementKey, localPosition, elementSceneRect, parentWorldTransform);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }

        public void ApplyElementDelta(
            GestureState state,
            Vector2 localPosition,
            Vector2 viewportDelta,
            bool uniformScale,
            bool centerAnchor,
            bool axisLock,
            bool snapEnabled)
        {
            switch (state.Mode)
            {
                case DragMode.MoveElement:
                    _elementDragController.UpdateMove(localPosition);
                    _host.UpdateSelectionVisual();
                    if (_elementDragController.TryUpdateMoveTransientState(_host, viewportDelta, axisLock, snapEnabled))
                    {
                        _host.UpdateSelectionVisual();
                    }
                    break;
                case DragMode.ResizeElement:
                    _elementDragController.UpdateResize(viewportDelta, state.ActiveHandle, uniformScale, centerAnchor);
                    _host.UpdateSelectionVisual();
                    if (_elementDragController.TryUpdateResizeTransientState(_host, state.ActiveHandle, snapEnabled))
                    {
                        _host.UpdateSelectionVisual();
                    }
                    break;
                case DragMode.RotateElement:
                    if (_elementDragController.TryUpdateRotateTransientState(_host, localPosition, snapEnabled))
                    {
                        _host.UpdateSelectionVisual();
                    }
                    break;
            }
        }

        public void Complete(DragMode dragMode, Vector2 canvasDelta)
        {
            if (_elementDragController.TryCommitDrag(new CommitDragRequest(_host, dragMode, canvasDelta)))
            {
                if (_host.HasDefinitionProxySelection)
                {
                    _host.UpdateSelectionVisual();
                    return;
                }

                _selectionSyncService.SelectCanvasElement(_elementDragController.DragElementKey, syncPatchTarget: false);
                return;
            }

            _host.RefreshLivePreview(keepExistingPreviewOnFailure: true);
            _host.RefreshInspector();
            _host.UpdateSelectionVisual();
        }

        public void End()
        {
            _elementDragController.End();
        }

        private void BeginResize(
            GestureState state,
            SelectionHandle handle,
            Vector2 localPosition,
            int pointerId,
            ResizeBeginRequest request)
        {
            state.Begin(DragMode.ResizeElement, handle, default, default);
            _elementDragController.BeginResize(request);
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
        }
    }
}
