using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class CanvasElementDragController
    {
        private readonly CanvasSceneProjector _sceneProjector;
        private readonly ElementMoveSession _moveSession = new();
        private readonly CanvasTransientDocumentModelSession _transientDocumentModelSession = new();

        private Rect _dragStartSelectionViewportRect;
        private Rect _dragCurrentSelectionViewportRect;
        private Rect _dragStartElementSceneRect;
        private Rect _dragStartProjectionSceneRect;
        private SvgPreserveAspectRatioMode _dragStartPreserveAspectRatioMode = SvgPreserveAspectRatioMode.Meet;
        private bool _dragResizeCenterAnchor;
        private string _dragElementKey = string.Empty;
        private Matrix2D _dragStartParentWorldTransform = Matrix2D.identity;
        private Vector2 _dragStartRotateVector;
        private float _dragCurrentRotationAngle;

        public Rect DragCurrentSelectionViewportRect => _dragCurrentSelectionViewportRect;
        public Rect DragStartElementSceneRect => _dragStartElementSceneRect;
        public Rect DragStartSelectionViewportRect => _dragStartSelectionViewportRect;
        public Rect DragStartProjectionSceneRect => _dragStartProjectionSceneRect;
        public SvgPreserveAspectRatioMode DragStartPreserveAspectRatioMode => _dragStartPreserveAspectRatioMode;
        public bool DragResizeCenterAnchor => _dragResizeCenterAnchor;
        public string DragElementKey => _dragElementKey;

        public CanvasElementDragController(CanvasSceneProjector sceneProjector)
        {
            _sceneProjector = sceneProjector;
        }

        public void BeginMove(
            DocumentSession currentDocument,
            PreviewSnapshot previewSnapshot,
            string elementKey,
            Vector2 localPosition,
            Rect elementSceneRect,
            Matrix2D parentWorldTransform)
        {
            _dragElementKey = elementKey;
            _dragStartParentWorldTransform = parentWorldTransform;
            _dragStartProjectionSceneRect = previewSnapshot?.CanvasViewportRect ?? default;
            _dragStartPreserveAspectRatioMode = previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet;
            _dragStartSelectionViewportRect = _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, elementSceneRect, out Rect selectionViewportRect)
                ? selectionViewportRect
                : default;
            _dragCurrentSelectionViewportRect = _dragStartSelectionViewportRect;
            _dragStartElementSceneRect = elementSceneRect;
            _moveSession.Begin(elementKey, localPosition, _dragStartSelectionViewportRect, elementSceneRect);
            _transientDocumentModelSession.TryBegin(currentDocument, elementKey);
        }

        public void BeginResize(
            DocumentSession currentDocument,
            string elementKey,
            Rect projectionSceneRect,
            SvgPreserveAspectRatioMode preserveAspectRatioMode,
            Rect selectionViewportRect,
            Rect selectionSceneRect,
            Matrix2D parentWorldTransform)
        {
            _dragElementKey = elementKey;
            _dragStartParentWorldTransform = parentWorldTransform;
            _dragStartProjectionSceneRect = projectionSceneRect;
            _dragStartPreserveAspectRatioMode = preserveAspectRatioMode;
            _dragStartSelectionViewportRect = selectionViewportRect;
            _dragCurrentSelectionViewportRect = selectionViewportRect;
            _dragStartElementSceneRect = selectionSceneRect;
            _dragResizeCenterAnchor = false;
            _transientDocumentModelSession.TryBegin(currentDocument, elementKey);
        }

        public void BeginRotate(
            DocumentSession currentDocument,
            PreviewSnapshot previewSnapshot,
            string elementKey,
            Vector2 localPosition,
            Rect elementSceneRect,
            Matrix2D parentWorldTransform)
        {
            _dragElementKey = elementKey;
            _dragStartParentWorldTransform = parentWorldTransform;
            _dragStartProjectionSceneRect = previewSnapshot?.CanvasViewportRect ?? default;
            _dragStartPreserveAspectRatioMode = previewSnapshot?.PreserveAspectRatioMode ?? SvgPreserveAspectRatioMode.Meet;
            _dragStartSelectionViewportRect = _sceneProjector.TrySceneRectToViewportRect(previewSnapshot, elementSceneRect, out Rect selectionViewportRect)
                ? selectionViewportRect
                : default;
            _dragCurrentSelectionViewportRect = _dragStartSelectionViewportRect;
            _dragStartElementSceneRect = elementSceneRect;
            _dragStartRotateVector = localPosition - _dragStartSelectionViewportRect.center;
            _dragCurrentRotationAngle = 0f;
            _transientDocumentModelSession.TryBegin(currentDocument, elementKey);
        }

        public Vector2 UpdateMove(Vector2 localPosition)
        {
            Vector2 viewportDelta = _moveSession.Update(localPosition);
            _dragCurrentSelectionViewportRect = _moveSession.CurrentSelectionViewportRect;
            return viewportDelta;
        }

        public void UpdateResize(Vector2 viewportDelta, CanvasHandle activeHandle, bool uniformScale, bool centerAnchor)
        {
            _dragResizeCenterAnchor = centerAnchor;
            Rect resizedViewportRect = RectResizeUtility.ResizeRect(
                _dragStartSelectionViewportRect,
                activeHandle,
                viewportDelta,
                12f);
            _dragCurrentSelectionViewportRect = CanvasProjectionMath.GetResizeViewportRect(
                _dragStartSelectionViewportRect,
                resizedViewportRect,
                activeHandle,
                uniformScale,
                centerAnchor);
        }

        public Rect BuildScaledSceneRect(CanvasHandle handle)
        {
            return _sceneProjector.BuildScaledSceneRect(
                _dragStartSelectionViewportRect,
                _dragStartElementSceneRect,
                _dragCurrentSelectionViewportRect,
                handle,
                _dragResizeCenterAnchor);
        }

        public void End()
        {
            _dragElementKey = string.Empty;
            _dragStartProjectionSceneRect = default;
            _dragStartPreserveAspectRatioMode = SvgPreserveAspectRatioMode.Meet;
            _dragResizeCenterAnchor = false;
            _dragStartParentWorldTransform = Matrix2D.identity;
            _dragStartRotateVector = Vector2.zero;
            _dragCurrentRotationAngle = 0f;
            _transientDocumentModelSession.End();
            _moveSession.End();
        }

        public bool TryUpdateMoveTransientState(
            ICanvasPointerDragHost host,
            Vector2 viewportDelta,
            bool axisLock,
            bool snapEnabled)
        {
            if (host.CurrentDocument == null || string.IsNullOrWhiteSpace(_dragElementKey))
            {
                return false;
            }

            if (!_sceneProjector.TryConvertViewportDeltaToSceneDelta(
                    _dragStartProjectionSceneRect,
                    _dragStartPreserveAspectRatioMode,
                    viewportDelta,
                    out Vector2 sceneDelta))
            {
                return false;
            }

            if (axisLock)
            {
                sceneDelta = Mathf.Abs(sceneDelta.x) >= Mathf.Abs(sceneDelta.y)
                    ? new Vector2(sceneDelta.x, 0f)
                    : new Vector2(0f, sceneDelta.y);
            }

            Rect movedSceneRect = new(
                _dragStartElementSceneRect.xMin + sceneDelta.x,
                _dragStartElementSceneRect.yMin + sceneDelta.y,
                _dragStartElementSceneRect.width,
                _dragStartElementSceneRect.height);
            if (snapEnabled)
            {
                movedSceneRect = EditorSnapUtility.SnapRect(movedSceneRect, snapPosition: true, snapSize: false);
                sceneDelta = movedSceneRect.position - _dragStartElementSceneRect.position;
            }

            if (host.PreviewSnapshot != null &&
                _sceneProjector.TrySceneRectToViewportRect(host.PreviewSnapshot, movedSceneRect, out Rect viewportRect))
            {
                _dragCurrentSelectionViewportRect = viewportRect;
            }

            Vector2 svgTranslateDelta = ToParentSpaceDelta(sceneDelta);
            if (!_transientDocumentModelSession.TryApplyTranslation(svgTranslateDelta) ||
                !_transientDocumentModelSession.TryBuildPreviewDocumentModel(out SvgDocumentModel previewDocumentModel, out _) ||
                !host.TryRefreshTransientPreview(previewDocumentModel))
            {
                return false;
            }

            host.RefreshInspector(previewDocumentModel);
            return true;
        }

        public bool TryUpdateRotateTransientState(
            ICanvasPointerDragHost host,
            Vector2 localPosition,
            bool snapEnabled)
        {
            if (host.CurrentDocument == null ||
                string.IsNullOrWhiteSpace(_dragElementKey) ||
                _dragStartRotateVector.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            Vector2 currentRotateVector = localPosition - _dragStartSelectionViewportRect.center;
            if (currentRotateVector.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            float rotationAngle = Vector2.SignedAngle(_dragStartRotateVector, currentRotateVector);
            _dragCurrentRotationAngle = snapEnabled
                ? EditorSnapUtility.SnapAngle(rotationAngle)
                : rotationAngle;

            Vector2 svgPivot = ToParentSpacePoint(_dragStartElementSceneRect.center);
            if (!_transientDocumentModelSession.TryApplyRotation(_dragCurrentRotationAngle, svgPivot) ||
                !_transientDocumentModelSession.TryBuildPreviewDocumentModel(out SvgDocumentModel previewDocumentModel, out _) ||
                !host.TryRefreshTransientPreview(previewDocumentModel))
            {
                return false;
            }

            host.RefreshInspector(previewDocumentModel);
            return true;
        }

        public bool TryUpdateResizeTransientState(
            ICanvasPointerDragHost host,
            CanvasHandle activeHandle,
            bool snapEnabled)
        {
            if (host.CurrentDocument == null || string.IsNullOrWhiteSpace(_dragElementKey))
            {
                return false;
            }

            if (snapEnabled && host.PreviewSnapshot != null)
            {
                Rect snappedSceneRect = EditorSnapUtility.SnapRect(
                    _sceneProjector.BuildScaledSceneRect(
                        _dragStartSelectionViewportRect,
                        _dragStartElementSceneRect,
                        _dragCurrentSelectionViewportRect,
                        activeHandle,
                        _dragResizeCenterAnchor));
                if (_sceneProjector.TrySceneRectToViewportRect(host.PreviewSnapshot, snappedSceneRect, out Rect snappedViewportRect))
                {
                    _dragCurrentSelectionViewportRect = snappedViewportRect;
                }
            }

            if (!_sceneProjector.TryBuildScaleTransform(
                    _dragStartSelectionViewportRect,
                    _dragStartElementSceneRect,
                    _dragCurrentSelectionViewportRect,
                    activeHandle,
                    _dragResizeCenterAnchor,
                    out Vector2 scale,
                    out Vector2 pivot))
            {
                return false;
            }

            Vector2 svgPivot = ToParentSpacePoint(pivot);
            if (!_transientDocumentModelSession.TryApplyScale(scale, svgPivot) ||
                !_transientDocumentModelSession.TryBuildPreviewDocumentModel(out SvgDocumentModel previewDocumentModel, out _) ||
                !host.TryRefreshTransientPreview(previewDocumentModel))
            {
                return false;
            }

            host.RefreshInspector(previewDocumentModel);
            return true;
        }

        public bool TryCommitDrag(
            ICanvasPointerDragHost host,
            CanvasDragMode dragMode,
            CanvasHandle activeHandle,
            Vector2 canvasDelta)
        {
            if (string.IsNullOrWhiteSpace(_dragElementKey))
            {
                return false;
            }

            if (dragMode == CanvasDragMode.RotateElement)
            {
                if (Mathf.Approximately(_dragCurrentRotationAngle, 0f))
                {
                    return false;
                }
            }
            else if (canvasDelta.sqrMagnitude < 4f)
            {
                return false;
            }

            Vector2 sceneDelta = Vector2.zero;
            if (dragMode == CanvasDragMode.MoveElement &&
                (!_sceneProjector.TryConvertViewportDeltaToSceneDelta(
                     _dragStartProjectionSceneRect,
                     _dragStartPreserveAspectRatioMode,
                     canvasDelta,
                     out sceneDelta) ||
                 sceneDelta.sqrMagnitude <= Mathf.Epsilon))
            {
                return false;
            }

            if (host.CurrentDocument == null)
            {
                return false;
            }

            string updatedSource;
            string error;
            if (!_transientDocumentModelSession.TryBuildCommittedSource(out updatedSource, out error))
            {
                host.UpdateSourceStatus(
                    string.IsNullOrWhiteSpace(error)
                        ? "Drag commit failed: transient model state is unavailable."
                        : $"Drag commit failed: {error}");
                return false;
            }

            host.ApplyUpdatedSource(
                updatedSource,
                dragMode switch
                {
                    CanvasDragMode.ResizeElement => $"Resized <{host.FindStructureNode(_dragElementKey)?.TagName ?? "element"}>.",
                    CanvasDragMode.RotateElement => $"Rotated <{host.FindStructureNode(_dragElementKey)?.TagName ?? "element"}>.",
                    _ => $"Moved <{host.FindStructureNode(_dragElementKey)?.TagName ?? "element"}>."
                });
            return true;
        }

        public bool TryBuildNudgedSource(
            DocumentSession currentDocument,
            string elementKey,
            Vector2 sceneDelta,
            Matrix2D parentWorldTransform,
            out string updatedSource)
        {
            updatedSource = string.Empty;
            if (currentDocument?.DocumentModel == null ||
                string.IsNullOrWhiteSpace(elementKey) ||
                sceneDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            Vector2 parentDelta = parentWorldTransform.Inverse().MultiplyVector(sceneDelta);
            SvgDocumentModelMutationService mutationService = new();
            return mutationService.TryPrependElementTranslation(
                currentDocument.DocumentModel,
                elementKey,
                parentDelta,
                out _,
                out updatedSource,
                out _);
        }

        // Converts a world-space direction vector to the element's SVG parent coordinate space.
        // The SVG transform attribute operates in parent space, so translate() deltas must be
        // expressed in parent space to achieve the intended world-space movement.
        private Vector2 ToParentSpaceDelta(Vector2 worldDelta)
        {
            return _dragStartParentWorldTransform.Inverse().MultiplyVector(worldDelta);
        }

        // Converts a world-space point (e.g., scale pivot) to the element's SVG parent coordinate space.
        private Vector2 ToParentSpacePoint(Vector2 worldPoint)
        {
            return _dragStartParentWorldTransform.Inverse().MultiplyPoint(worldPoint);
        }
    }
}
