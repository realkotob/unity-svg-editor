using UnityEngine;
using UnitySvgEditor.Editor.Workspace.Canvas;

namespace UnitySvgEditor.Editor
{
    internal sealed class ElementRotationSession
    {
        private readonly CanvasTransientDocumentModelSession _transientSession = new();

        public bool IsActive { get; private set; }
        public string TargetKey { get; private set; } = string.Empty;
        public Vector2 ParentPivot { get; private set; }

        public bool TryBegin(DocumentSession document, string targetKey, Vector2 parentPivot)
        {
            End();
            if (!_transientSession.TryBegin(document, targetKey))
            {
                return false;
            }

            TargetKey = targetKey ?? string.Empty;
            ParentPivot = parentPivot;
            IsActive = true;
            return true;
        }

        public bool TryBuildTransform(float deltaDegrees, out string transform, out string error)
        {
            transform = string.Empty;
            error = string.Empty;
            if (!IsActive)
            {
                error = "Rotation session is unavailable.";
                return false;
            }

            if (!_transientSession.TryApplyRotation(deltaDegrees, ParentPivot))
            {
                error = "Rotation could not be applied.";
                return false;
            }

            if (!_transientSession.TryGetCurrentTransform(out transform))
            {
                error = "Rotation transform could not be resolved.";
                return false;
            }

            return true;
        }

        public bool TryBuildPreview(float deltaDegrees, out SvgDocumentModel previewDocumentModel, out string error)
        {
            previewDocumentModel = null;
            error = string.Empty;
            if (!IsActive)
            {
                error = "Rotation session is unavailable.";
                return false;
            }

            return _transientSession.TryApplyRotation(deltaDegrees, ParentPivot) &&
                   _transientSession.TryBuildPreviewDocumentModel(out previewDocumentModel, out error);
        }

        public bool TryBuildCommittedSource(out string sourceText, out string error)
        {
            if (!IsActive)
            {
                sourceText = string.Empty;
                error = "Rotation session is unavailable.";
                return false;
            }

            return _transientSession.TryBuildCommittedSource(out sourceText, out error);
        }

        public void End()
        {
            _transientSession.End();
            IsActive = false;
            TargetKey = string.Empty;
            ParentPivot = default;
        }
    }
}
