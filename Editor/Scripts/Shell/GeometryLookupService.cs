using System;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Preview;

namespace SvgEditor.Shell
{
    internal sealed class GeometryLookupService
    {
        private readonly Func<PreviewSnapshot> _previewSnapshotAccessor;

        public GeometryLookupService(Func<PreviewSnapshot> previewSnapshotAccessor)
            {
                _previewSnapshotAccessor = previewSnapshotAccessor;
            }

        private PreviewSnapshot PreviewSnapshot => _previewSnapshotAccessor?.Invoke();

        public bool TryGetTargetSceneRect(string targetKey, out Rect sceneRect)
        {
            sceneRect = default;
            if (!TryFindTargetElement(targetKey, out PreviewElementGeometry targetElement))
            {
                return false;
            }

            sceneRect = targetElement.VisualBounds;
            return true;
        }

        public bool TryGetRotationPivotParentSpace(string targetKey, out Vector2 parentPivot)
        {
            parentPivot = default;
            if (!TryFindTargetElement(targetKey, out PreviewElementGeometry targetElement))
            {
                return false;
            }

            parentPivot = targetElement.RotationPivotParentSpace;
            return true;
        }

        public bool TryGetParentWorldTransform(string targetKey, out Matrix2D parentWorldTransform)
        {
            parentWorldTransform = Matrix2D.identity;
            if (!TryFindTargetElement(targetKey, out PreviewElementGeometry targetElement))
            {
                return false;
            }

            parentWorldTransform = targetElement.ParentWorldTransform;
            return true;
        }

        public bool TryGetViewportSceneRect(out Rect sceneRect)
        {
            sceneRect = PreviewSnapshot?.CanvasViewportRect ?? default;
            return sceneRect.width > 0f || sceneRect.height > 0f;
        }

        private bool TryFindTargetElement(string targetKey, out PreviewElementGeometry targetElement)
        {
            targetElement = null;
            var snapshot = PreviewSnapshot;
            if (snapshot?.Elements == null || string.IsNullOrWhiteSpace(targetKey))
            {
                return false;
            }

            for (var index = 0; index < snapshot.Elements.Count; index++)
            {
                PreviewElementGeometry candidate = snapshot.Elements[index];
                if (candidate == null ||
                    !string.Equals(candidate.TargetKey, targetKey, StringComparison.Ordinal))
                {
                    continue;
                }

                targetElement = candidate;
                return true;
            }

            return false;
        }
    }
}
