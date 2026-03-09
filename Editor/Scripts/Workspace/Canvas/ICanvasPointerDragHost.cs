using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal interface ICanvasPointerDragHost
    {
        DocumentSession CurrentDocument { get; }
        PreviewSnapshot PreviewSnapshot { get; }
        string SelectedElementKey { get; }
        CanvasSelectionKind SelectionKind { get; set; }

        void RefreshLivePreview(bool keepExistingPreviewOnFailure);
        bool TryRefreshTransientPreview(string sourceText);
        void ApplyUpdatedSource(string updatedSource, string successStatus);
        void UpdateSourceStatus(string status);
        StructureNode FindStructureNode(string elementKey);
        void SelectFrame();
        void SelectElement(string elementKey, bool syncPatchTarget);
        void ClearSelection();
        void UpdateStructureInteractivity(bool hasDocument);
        void RefreshSelectionSummary(CanvasSelectionKind selectionKind);
        void UpdateCanvasVisualState();
        void UpdateSelectionVisual();
        void SetHoveredElement(string elementKey);
        void ClearHover();
        void UpdateHoverVisual();
    }
}
