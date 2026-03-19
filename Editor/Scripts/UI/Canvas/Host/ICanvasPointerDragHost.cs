using System.Collections.Generic;
using UnityEngine;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Source;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Core.Preview;

namespace SvgEditor.UI.Canvas
{
    internal interface ICanvasPointerDragHost
    {
        DocumentSession CurrentDocument { get; }
        PreviewSnapshot PreviewSnapshot { get; }
        string SelectedElementKey { get; }
        IReadOnlyList<string> SelectedElementKeys { get; }
        SelectionKind SelectionKind { get; set; }
        bool HasDefinitionProxySelection { get; }

        void RefreshLivePreview(bool keepExistingPreviewOnFailure);
        bool TryRefreshTransientPreview(SvgDocumentModel documentModel);
        void RefreshInspector();
        void RefreshInspector(SvgDocumentModel documentModel);
        void ApplyUpdatedSource(string updatedSource, string successStatus);
        void UpdateSourceStatus(string status);
        HierarchyNode FindHierarchyNode(string elementKey);
        void SelectFrame();
        void SelectElement(string elementKey, bool syncPatchTarget);
        void ToggleElementSelection(string elementKey, bool syncPatchTarget);
        void ReplaceElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget);
        void AddElementSelection(IReadOnlyList<string> elementKeys, bool syncPatchTarget);
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
