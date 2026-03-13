using UnityEngine;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal static class CanvasNudgeService
    {
        public static bool TryNudgeSelectedElement(
            ICanvasWorkspaceHost host,
            SceneProjector sceneProjector,
            PointerDragController pointerDragController,
            DefinitionProxyCoordinator definitionProxyCoordinator,
            SelectionKind selectionKind,
            Vector2 sceneDelta)
        {
            if (host.PreviewSnapshot == null || selectionKind != SelectionKind.Element)
            {
                return false;
            }

            if (definitionProxyCoordinator.TryGetSelectedDefinitionProxy(host.SelectedElementKey, out CanvasDefinitionOverlayVisual selectedProxy) &&
                selectedProxy != null &&
                pointerDragController.TryBuildNudgedSource(
                    host.CurrentDocument,
                    selectedProxy.DefinitionElementKey,
                    sceneDelta,
                    selectedProxy.ParentWorldTransform,
                    out string proxyUpdatedSource))
            {
                host.ApplyUpdatedSource(proxyUpdatedSource, $"Moved <{host.FindHierarchyNode(selectedProxy.DefinitionElementKey)?.TagName ?? "definition"}>.");
                return true;
            }

            if (string.IsNullOrWhiteSpace(host.SelectedElementKey))
            {
                return false;
            }

            PreviewElementGeometry selectedGeometry = sceneProjector.FindPreviewElement(host.PreviewSnapshot, host.SelectedElementKey);
            if (selectedGeometry == null ||
                !pointerDragController.TryBuildNudgedSource(
                    host.CurrentDocument,
                    host.SelectedElementKey,
                    sceneDelta,
                    selectedGeometry.ParentWorldTransform,
                    out string updatedSource))
            {
                return false;
            }

            host.ApplyUpdatedSource(updatedSource, $"Moved <{host.FindHierarchyNode(host.SelectedElementKey)?.TagName ?? "element"}>.");
            return true;
        }
    }
}
