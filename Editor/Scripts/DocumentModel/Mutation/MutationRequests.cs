using UnityEngine;
using Core.UI.Extensions;

namespace SvgEditor.DocumentModel
{
    internal readonly struct MutationResult
    {
        public MutationResult(SvgDocumentModel updatedDocumentModel, string updatedSourceText, string error)
        {
            UpdatedDocumentModel = updatedDocumentModel;
            UpdatedSourceText = updatedSourceText ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public SvgDocumentModel UpdatedDocumentModel { get; }
        public string UpdatedSourceText { get; }
        public string Error { get; }
    }

    internal readonly struct ReorderElementRequest
    {
        public ReorderElementRequest(string elementKey, int targetChildIndex)
        {
            ElementKey = elementKey;
            TargetChildIndex = targetChildIndex;
        }

        public string ElementKey { get; }
        public int TargetChildIndex { get; }
    }

    internal readonly struct MoveElementRequest
    {
        public MoveElementRequest(string elementKey, string targetParentKey, int targetChildIndex)
        {
            ElementKey = elementKey;
            TargetParentKey = targetParentKey;
            TargetChildIndex = targetChildIndex;
        }

        public string ElementKey { get; }
        public string TargetParentKey { get; }
        public int TargetChildIndex { get; }
    }

    internal readonly struct TranslateElementRequest
    {
        public TranslateElementRequest(string elementKey, Vector2 translation)
        {
            ElementKey = elementKey;
            Translation = translation;
        }

        public string ElementKey { get; }
        public Vector2 Translation { get; }
    }

    internal readonly struct ScaleElementRequest
    {
        public ScaleElementRequest(string elementKey, Vector2 scale, Vector2 pivot)
        {
            ElementKey = elementKey;
            Scale = scale;
            Pivot = pivot;
        }

        public string ElementKey { get; }
        public Vector2 Scale { get; }
        public Vector2 Pivot { get; }
    }

    internal readonly struct RotateElementRequest
    {
        public RotateElementRequest(string elementKey, float angle, Vector2 pivot)
        {
            ElementKey = elementKey;
            Angle = angle;
            Pivot = pivot;
        }

        public string ElementKey { get; }
        public float Angle { get; }
        public Vector2 Pivot { get; }
    }
}
