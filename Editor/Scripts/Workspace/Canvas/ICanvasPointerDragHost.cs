using UnityEngine;

namespace UnitySvgEditor.Editor.Workspace.Canvas
{
    internal interface ICanvasPointerDragHost
    {
        DocumentSession CurrentDocument { get; }
        PreviewSnapshot PreviewSnapshot { get; }
        string SelectedElementKey { get; }
        CanvasSelectionKind SelectionKind { get; set; }
        bool HasDefinitionProxySelection { get; }

        void RefreshLivePreview(bool keepExistingPreviewOnFailure);
        bool TryRefreshTransientPreview(SvgDocumentModel documentModel);
        void RefreshInspector();
        void RefreshInspector(SvgDocumentModel documentModel);
        void ApplyUpdatedSource(string updatedSource, string successStatus);
        void UpdateSourceStatus(string status);
        StructureNode FindStructureNode(string elementKey);
        void SelectFrame();
        void SelectElement(string elementKey, bool syncPatchTarget);
        void ClearSelection();
        void UpdateStructureInteractivity(bool hasDocument);
        void UpdateCanvasVisualState();
        void UpdateSelectionVisual();
        void SetHoveredElement(string elementKey);
        void ClearHover();
        void UpdateHoverVisual();
        bool TryHitTestDefinitionOverlay(Vector2 localPoint, out CanvasDefinitionOverlayVisual overlay);
        bool TryGetSelectedDefinitionProxy(out CanvasDefinitionOverlayVisual overlay);
        void SelectDefinitionProxy(CanvasDefinitionOverlayVisual overlay);
        void ClearDefinitionProxySelection();
    }
}
