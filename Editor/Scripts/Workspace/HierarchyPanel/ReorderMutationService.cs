using System;
using SvgEditor.DocumentModel;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal sealed class ReorderMutationService
    {
        private readonly SvgDocumentModelMutationService _documentModelMutationService = new();

        public void ApplyMove(IHierarchyHost host, MoveElementRequest request)
        {
            if (host?.CurrentDocument == null ||
                string.IsNullOrWhiteSpace(request.ElementKey) ||
                string.IsNullOrWhiteSpace(request.TargetParentKey) ||
                request.TargetChildIndex < 0)
            {
                return;
            }

            if (!host.CurrentDocument.CanUseDocumentModelForEditing)
            {
                host.UpdateSourceStatus($"Reorder failed: {host.CurrentDocument.ResolveModelEditingFailureReason()}");
                return;
            }

            if (_documentModelMutationService.TryMoveElement(
                    host.CurrentDocument.DocumentModel,
                    request,
                    out MutationResult result))
            {
                if (!string.Equals(result.UpdatedSourceText, host.CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
                {
                    host.ApplyUpdatedSource(result.UpdatedSourceText, $"Moved #{request.ElementKey}.");
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                host.UpdateSourceStatus($"Move failed: {result.Error}");
            }
        }
    }
}
