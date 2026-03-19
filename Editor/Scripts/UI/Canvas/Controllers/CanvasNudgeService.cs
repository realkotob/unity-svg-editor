using UnityEngine;
using SvgEditor.Core.Preview;
using Core.UI.Extensions;

namespace SvgEditor.UI.Canvas
{
    internal static class CanvasNudgeService
    {
        public static bool TryNudgeSelectedElement(CanvasNudgeRequest request)
        {
            ICanvasPointerDragHost host = request.Host;
            if (host.PreviewSnapshot == null || host.SelectionKind != SelectionKind.Element)
            {
                return false;
            }

            if (host.CurrentDocument?.CanUseDocumentModelForEditing != true)
            {
                if (host.CurrentDocument != null)
                {
                    host.UpdateSourceStatus($"Move failed: {host.CurrentDocument.ResolveModelEditingFailureReason()}");
                }

                return false;
            }

            if (host.TryGetSelectedDefinitionProxy(out CanvasDefinitionOverlayVisual selectedProxy) &&
                selectedProxy != null &&
                request.PointerDragController.TryBuildNudgedSource(
                    new NudgeSourceRequest(
                        host.CurrentDocument,
                        selectedProxy.DefinitionElementKey,
                        request.SceneDelta,
                        selectedProxy.ParentWorldTransform),
                    out string proxyUpdatedSource))
            {
                host.ApplyUpdatedSource(proxyUpdatedSource, $"Moved <{host.FindHierarchyNode(selectedProxy.DefinitionElementKey)?.TagName ?? "definition"}>.");
                return true;
            }

            if (string.IsNullOrWhiteSpace(host.SelectedElementKey))
            {
                return false;
            }

            PreviewElementGeometry selectedGeometry = request.SceneProjector.FindPreviewElement(host.PreviewSnapshot, host.SelectedElementKey);
            if (selectedGeometry == null ||
                !request.PointerDragController.TryBuildNudgedSource(
                    new NudgeSourceRequest(
                        host.CurrentDocument,
                        host.SelectedElementKey,
                        request.SceneDelta,
                        selectedGeometry.ParentWorldTransform),
                    out string updatedSource))
            {
                return false;
            }

            host.ApplyUpdatedSource(updatedSource, $"Moved <{host.FindHierarchyNode(host.SelectedElementKey)?.TagName ?? "element"}>.");
            return true;
        }
    }
}
