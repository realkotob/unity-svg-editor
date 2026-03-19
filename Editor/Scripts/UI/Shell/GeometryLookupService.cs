using System;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Shared;
using SvgEditor.UI.Canvas;
using Core.UI.Extensions;

namespace SvgEditor.UI.Shell
{
    internal sealed class GeometryLookupService
    {
        private readonly Func<PreviewSnapshot> _previewSnapshotAccessor;
        private readonly Func<System.Collections.Generic.IReadOnlyList<string>> _selectedElementKeysAccessor;

        public GeometryLookupService(
            Func<PreviewSnapshot> previewSnapshotAccessor,
            Func<System.Collections.Generic.IReadOnlyList<string>> selectedElementKeysAccessor)
        {
            _previewSnapshotAccessor = previewSnapshotAccessor;
            _selectedElementKeysAccessor = selectedElementKeysAccessor;
        }

        private PreviewSnapshot PreviewSnapshot => _previewSnapshotAccessor?.Invoke();
        private System.Collections.Generic.IReadOnlyList<string> SelectedElementKeys => _selectedElementKeysAccessor?.Invoke();

        public bool TryGetCurrentSelectionSceneRect(out Rect sceneRect)
        {
            sceneRect = default;
            System.Collections.Generic.IReadOnlyList<string> selectedElementKeys = SelectedElementKeys;
            Result<PreviewSnapshot> snapshotResult = ResolveSnapshot();
            if (snapshotResult.IsFailure || selectedElementKeys == null || selectedElementKeys.Count <= 1)
            {
                return false;
            }

            return CanvasProjectionMath.TryGetCombinedSelectionSceneRect(snapshotResult.Value, selectedElementKeys, out sceneRect);
        }

        public bool TryGetElementSceneRect(string elementKey, out Rect sceneRect)
        {
            sceneRect = default;
            Result<PreviewElementGeometry> result = ResolveElementByKey(elementKey);
            if (result.IsFailure)
            {
                return false;
            }

            sceneRect = result.Value.VisualBounds;
            return true;
        }

        public bool TryGetTargetSceneRect(string targetKey, out Rect sceneRect)
        {
            sceneRect = default;
            Result<PreviewElementGeometry> result = ResolveTargetElement(targetKey);
            if (result.IsFailure)
            {
                return false;
            }

            sceneRect = result.Value.VisualBounds;
            return true;
        }

        public bool TryGetRotationPivotParentSpace(string targetKey, out Vector2 parentPivot)
        {
            parentPivot = default;
            Result<PreviewElementGeometry> result = ResolveTargetElement(targetKey);
            if (result.IsFailure)
            {
                return false;
            }

            parentPivot = result.Value.RotationPivotParentSpace;
            return true;
        }

        public bool TryGetParentWorldTransform(string targetKey, out Matrix2D parentWorldTransform)
        {
            parentWorldTransform = Matrix2D.identity;
            Result<PreviewElementGeometry> result = ResolveTargetElement(targetKey);
            if (result.IsFailure)
            {
                return false;
            }

            parentWorldTransform = result.Value.ParentWorldTransform;
            return true;
        }

        public bool TryGetElementParentWorldTransform(string elementKey, out Matrix2D parentWorldTransform)
        {
            parentWorldTransform = Matrix2D.identity;
            Result<PreviewElementGeometry> result = ResolveElementByKey(elementKey);
            if (result.IsFailure)
            {
                return false;
            }

            parentWorldTransform = result.Value.ParentWorldTransform;
            return true;
        }

        public bool TryGetViewportSceneRect(out Rect sceneRect)
        {
            Result<PreviewSnapshot> result = ResolveSnapshot();
            sceneRect = result.IsSuccess ? result.Value.CanvasViewportRect : default;
            return sceneRect.width > 0f || sceneRect.height > 0f;
        }

        private Result<PreviewSnapshot> ResolveSnapshot()
        {
            var snapshot = PreviewSnapshot;
            return snapshot?.Elements != null
                ? Result.Success(snapshot)
                : Result.Failure<PreviewSnapshot>("Preview snapshot is unavailable.");
        }

        private Result<PreviewElementGeometry> ResolveTargetElement(string targetKey)
        {
            return ResolveElement(
                targetKey,
                candidate => candidate.TargetKey,
                "Target preview element could not be found.");
        }

        private Result<PreviewElementGeometry> ResolveElementByKey(string elementKey)
        {
            return ResolveElement(
                elementKey,
                candidate => candidate.Key,
                "Preview element could not be found.");
        }

        private Result<PreviewElementGeometry> ResolveElement(
            string lookupValue,
            Func<PreviewElementGeometry, string> keySelector,
            string error)
        {
            Result<PreviewSnapshot> snapshotResult = ResolveSnapshot();
            if (snapshotResult.IsFailure || string.IsNullOrWhiteSpace(lookupValue))
            {
                return Result.Failure<PreviewElementGeometry>(error);
            }

            PreviewSnapshot snapshot = snapshotResult.Value;
            for (var index = 0; index < snapshot.Elements.Count; index++)
            {
                PreviewElementGeometry candidate = snapshot.Elements[index];
                if (candidate == null ||
                    !string.Equals(keySelector(candidate), lookupValue, StringComparison.Ordinal))
                {
                    continue;
                }

                return Result.Success(candidate);
            }

            return Result.Failure<PreviewElementGeometry>(error);
        }
    }
}
