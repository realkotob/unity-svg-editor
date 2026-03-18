using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Workspace.Canvas;
using SvgEditor.DocumentModel;
using SvgEditor.Document;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.Transforms
{
    internal static class ElementRotationUtility
    {
        public static Vector2 ToParentSpacePoint(Matrix2D parentWorldTransform, Vector2 worldPoint)
        {
            return parentWorldTransform.Inverse().MultiplyPoint(worldPoint);
        }

        public static bool TryComputeDeltaFromAbsolute(string currentTransform, float desiredRotate, out float deltaDegrees)
        {
            deltaDegrees = 0f;
            if (!TransformStringBuilder.TryParseSimpleTransform(
                    currentTransform,
                    out _,
                    out _,
                    out float currentRotate,
                    out _,
                    out _))
            {
                return false;
            }

            deltaDegrees = desiredRotate - currentRotate;
            return true;
        }

        public static bool TryBuildRotationTransform(
            TransientDocumentSession session,
            float deltaDegrees,
            Vector2 parentPivot,
            out string transform,
            out string error)
        {
            transform = string.Empty;
            error = string.Empty;

            if (session == null)
            {
                error = "Rotation session is unavailable.";
                return false;
            }

            if (!session.TryApplyRotation(deltaDegrees, parentPivot))
            {
                error = "Rotation could not be applied.";
                return false;
            }

            if (!session.TryGetCurrentTransform(out transform))
            {
                error = "Rotation transform could not be resolved.";
                return false;
            }

            return true;
        }

        public static bool TryBuildRotationPreview(
            TransientDocumentSession session,
            float deltaDegrees,
            Vector2 parentPivot,
            out SvgDocumentModel previewDocumentModel,
            out string error)
        {
            previewDocumentModel = null;
            error = string.Empty;

            if (session == null)
            {
                error = "Rotation session is unavailable.";
                return false;
            }

            return session.TryApplyRotation(deltaDegrees, parentPivot) &&
                   session.TryBuildPreviewDocumentModel(out previewDocumentModel, out error);
        }
    }
}
