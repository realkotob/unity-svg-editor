using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal interface IEditorWorkspaceHost
    {
        VisualElement RootVisualElement { get; }
        DocumentSession CurrentDocument { get; }
        PreviewSnapshot PreviewSnapshot { get; }
        Image PreviewImage { get; }
        AttributePatcher AttributePatcher { get; }

        void ApplyUpdatedSource(string updatedSource, string successStatus);
        void UpdateSourceStatus(string status);
        void UpdateEditorInteractivity();
        void RefreshLivePreview(bool keepExistingPreviewOnFailure);
        bool TryRefreshTransientPreview(string sourceText);
        bool TrySelectPatchTargetByKey(string targetKey);
        string ResolveSelectedPatchTargetKey();
        string FormatNumber(float value);
    }
}
