using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal interface ICanvasWorkspaceHost
    {
        DocumentSession CurrentDocument { get; }
        PreviewSnapshot PreviewSnapshot { get; }
        Image PreviewImage { get; }
        string SelectedElementKey { get; }

        string FormatNumber(float value);
        StructureNode FindStructureNode(string elementKey);
        void ClearStructureSelectionFromCanvas();
        void SelectFrameFromCanvas();
        void SelectStructureElementFromCanvas(string elementKey, bool syncPatchTarget);
        void UpdateStructureInteractivity(bool hasDocument);
        void UpdateSourceStatus(string status);
        void RefreshLivePreview(bool keepExistingPreviewOnFailure);
        bool TryRefreshTransientPreview(SvgDocumentModel documentModel);
        void RefreshInspector();
        void RefreshInspector(SvgDocumentModel documentModel);
        void ApplyUpdatedSource(string updatedSource, string successStatus);
    }
}
