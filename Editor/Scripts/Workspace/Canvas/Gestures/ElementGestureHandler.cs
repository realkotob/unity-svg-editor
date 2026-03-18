using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Shared;
using SvgEditor.Document;
using SvgEditor.Workspace.Transforms;
using Core.UI.Extensions;

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
                _host.SelectionKind != SelectionKind.Element)
            {
                return false;
            }

            bool multipleSelection = _host.SelectedElementKeys != null && _host.SelectedElementKeys.Count > 1;
            Rect selectedElementSceneRect;
            Rect selectionViewportRect;
            if (multipleSelection)
            {
                if (!CanvasProjectionMath.TryGetCombinedSelectionSceneRect(_host.PreviewSnapshot, _host.SelectedElementKeys, out selectedElementSceneRect) ||
                    !_sceneProjector.TrySceneRectToViewportRect(_host.PreviewSnapshot, selectedElementSceneRect, out selectionViewportRect))
                {
                    return false;
                }
            }
            else if (!_sceneProjector.TryResolveSelectedElementSceneRect(_host.PreviewSnapshot, _host.SelectedElementKey, out selectedElementSceneRect) ||
                     !_sceneProjector.TryBuildCurrentSelectionViewportRect(_host.PreviewSnapshot, _host.SelectionKind, _host.SelectedElementKey, out selectionViewportRect))
            {
                return false;
            }

            if (multipleSelection ? HasUnsupportedResizeTargets(_host.SelectedElementKeys) : IsResizeUnsupported(_host.SelectedElementKey))
            {
                _host.UpdateSourceStatus("Resize is not supported for the current selection. Move/select is still available.");
                return false;
            }

            PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, _host.SelectedElementKey);
            Matrix2D parentWorldTransform = multipleSelection
                ? Matrix2D.identity
                : selectedGeometry?.ParentWorldTransform ?? Matrix2D.identity;
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
                    parentWorldTransform,
                    multipleSelection ? BuildMoveTargets(_host.SelectedElementKeys) : null));
            return true;
        }

        public bool TryBeginRotateFromHandle(GestureState state, Vector2 localPosition, int pointerId)
        {
            if (_host.HasDefinitionProxySelection ||
                _host.SelectionKind != SelectionKind.Element)
            {
                return false;
            }

            bool multipleSelection = _host.SelectedElementKeys != null && _host.SelectedElementKeys.Count > 1;
            Rect selectedElementSceneRect;
            PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, _host.SelectedElementKey);
            if (multipleSelection)
            {
                if (!CanvasProjectionMath.TryGetCombinedSelectionSceneRect(_host.PreviewSnapshot, _host.SelectedElementKeys, out selectedElementSceneRect))
                {
                    return false;
                }
            }
            else if (!_sceneProjector.TryResolveSelectedElementSceneRect(_host.PreviewSnapshot, _host.SelectedElementKey, out selectedElementSceneRect))
            {
                return false;
            }

            Matrix2D parentWorldTransform = multipleSelection
                ? Matrix2D.identity
                : selectedGeometry?.ParentWorldTransform ?? Matrix2D.identity;
            Vector2 rotationPivotWorld = multipleSelection
                ? selectedElementSceneRect.center
                : selectedGeometry?.RotationPivotWorld ?? selectedElementSceneRect.center;
            Vector2 rotationPivotParentSpace = multipleSelection
                ? rotationPivotWorld
                : selectedGeometry?.RotationPivotParentSpace ??
                  ElementRotationUtility.ToParentSpacePoint(parentWorldTransform, selectedElementSceneRect.center);
            state.Begin(DragMode.RotateElement, SelectionHandle.Rotate, default, default);
            _elementDragController.BeginRotate(
                new RotateBeginRequest(
                    new ElementDragBeginRequest(
                        _host.CurrentDocument,
                        _host.PreviewSnapshot,
                        _host.SelectedElementKey,
                        localPosition,
                        selectedElementSceneRect,
                        parentWorldTransform,
                        multipleSelection ? BuildMoveTargets(_host.SelectedElementKeys) : null),
                    rotationPivotWorld,
                    rotationPivotParentSpace));
            _dragSession.Begin(_overlayAccessor(), pointerId, localPosition);
            return true;
        }

        public void BeginMove(GestureState state, int pointerId, ElementDragBeginRequest request)
        {
            state.Begin(DragMode.MoveElement, SelectionHandle.None, default, default);
            _elementDragController.BeginMove(request);
            _dragSession.Begin(_overlayAccessor(), pointerId, request.LocalPosition);
        }

        public void ApplyElementDelta(GestureState state, ElementDeltaRequest request)
        {
            switch (state.Mode)
            {
                case DragMode.MoveElement:
                    _elementDragController.UpdateMove(request.LocalPosition);
                    _host.UpdateSelectionVisual();
                    if (_elementDragController.TryUpdateMoveTransientState(_host, request.ViewportDelta, request.AxisLock, request.SnapEnabled))
                    {
                        _host.UpdateSelectionVisual();
                    }
                    break;
                case DragMode.ResizeElement:
                    _elementDragController.UpdateResize(request.ViewportDelta, state.ActiveHandle, request.UniformScale, request.CenterAnchor);
                    _host.UpdateSelectionVisual();
                    if (_elementDragController.TryUpdateResizeTransientState(_host, state.ActiveHandle, request.SnapEnabled))
                    {
                        _host.UpdateSelectionVisual();
                    }
                    break;
                case DragMode.RotateElement:
                    if (_elementDragController.TryUpdateRotateTransientState(_host, request.LocalPosition, request.SnapEnabled))
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

                if (_elementDragController.IsGroupMove)
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

        private IReadOnlyList<ElementMoveTarget> BuildMoveTargets(IReadOnlyList<string> selectedElementKeys)
        {
            if (selectedElementKeys == null || selectedElementKeys.Count == 0)
            {
                return Array.Empty<ElementMoveTarget>();
            }

            List<ElementMoveTarget> moveTargets = new(selectedElementKeys.Count);
            foreach (string selectedElementKey in selectedElementKeys)
            {
                PreviewElementGeometry selectedGeometry = _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, selectedElementKey);
                if (selectedGeometry == null)
                {
                    continue;
                }

                moveTargets.Add(new ElementMoveTarget(selectedElementKey, selectedGeometry.ParentWorldTransform));
            }

            return moveTargets;
        }

        private bool HasUnsupportedResizeTargets(IReadOnlyList<string> selectedElementKeys)
        {
            if (selectedElementKeys == null)
            {
                return false;
            }

            foreach (string selectedElementKey in selectedElementKeys)
            {
                if (IsResizeUnsupported(selectedElementKey))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsResizeUnsupported(string elementKey)
        {
            string tagName = _host.FindHierarchyNode(elementKey)?.TagName;
            return string.Equals(tagName, SvgTagName.TSPAN, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tagName, SvgTagName.TEXT_PATH, StringComparison.OrdinalIgnoreCase);
        }
    }
}
