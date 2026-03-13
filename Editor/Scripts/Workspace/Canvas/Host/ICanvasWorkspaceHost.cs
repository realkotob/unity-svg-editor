using UnityEngine.UIElements;
using System.Collections.Generic;
using SvgEditor.DocumentModel;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal interface ICanvasWorkspaceHost
    {
        DocumentSession CurrentDocument { get; }
        PreviewSnapshot PreviewSnapshot { get; }
        Image PreviewImage { get; }
        string SelectedElementKey { get; }
        IReadOnlyList<string> SelectedElementKeys { get; }
        HierarchyNode SelectedHierarchyNode { get; }

        string FormatNumber(float value);
        HierarchyNode FindHierarchyNode(string elementKey);
        void ClearStructureSelectionFromCanvas();
        void SelectFrameFromCanvas();
        void SelectStructureElementFromCanvas(string elementKey, bool syncPatchTarget);
        void ToggleStructureElementSelectionFromCanvas(string elementKey, bool syncPatchTarget);
        void UpdateStructureInteractivity(bool hasDocument);
        void UpdateSourceStatus(string status);
        void RefreshLivePreview(bool keepExistingPreviewOnFailure);
        bool TryRefreshTransientPreview(SvgDocumentModel documentModel);
        void RefreshInspector();
        void RefreshInspector(SvgDocumentModel documentModel);
        void ApplyUpdatedSource(string updatedSource, string successStatus);
    }
}
