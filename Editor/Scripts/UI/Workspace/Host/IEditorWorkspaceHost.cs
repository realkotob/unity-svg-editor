using UnityEngine.UIElements;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Source;
using SvgEditor.UI.Workspace.Document;
using Core.UI.Extensions;

namespace SvgEditor.UI.Workspace.Host
{
    internal interface IEditorWorkspaceHost
    {
        VisualElement RootVisualElement { get; }
        DocumentSession CurrentDocument { get; }
        PreviewSnapshot PreviewSnapshot { get; }
        Image PreviewImage { get; }

        void ApplyUpdatedSource(string updatedSource, string successStatus);
        void ApplyUpdatedSource(string updatedSource, string successStatus, HistoryRecordingMode recordingMode);
        void UpdateSourceStatus(string status);
        void UpdateEditorInteractivity();
        void RefreshLivePreview(bool keepExistingPreviewOnFailure);
        bool TryRefreshTransientPreview(SvgDocumentModel documentModel);
        void RefreshInspector();
        void RefreshInspector(SvgDocumentModel documentModel);
        bool TrySelectPatchTargetByKey(string targetKey);
        string ResolveSelectedPatchTargetKey();
        string FormatNumber(float value);
    }
}
