using System;
using SvgEditor.DocumentModel;

namespace SvgEditor.Workspace.HierarchyPanel
{
    internal sealed class ReorderMutationService
    {
        private readonly SvgDocumentModelMutationService _documentModelMutationService = new();

        public void ApplyMove(
            IHierarchyHost host,
            string elementKey,
            string parentKey,
            int childIndex)
        {
            if (host?.CurrentDocument == null ||
                string.IsNullOrWhiteSpace(elementKey) ||
                string.IsNullOrWhiteSpace(parentKey) ||
                childIndex < 0)
            {
                return;
            }

            if (host.CurrentDocument.DocumentModel == null ||
                !string.IsNullOrWhiteSpace(host.CurrentDocument.DocumentModelLoadError) ||
                !string.Equals(host.CurrentDocument.DocumentModel.SourceText, host.CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                host.UpdateSourceStatus("Reorder failed: document model is unavailable.");
                return;
            }

            if (_documentModelMutationService.TryMoveElement(
                    host.CurrentDocument.DocumentModel,
                    new MoveElementRequest(elementKey, parentKey, childIndex),
                    out MutationResult result))
            {
                if (!string.Equals(result.UpdatedSourceText, host.CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
                {
                    host.ApplyUpdatedSource(result.UpdatedSourceText, $"Moved #{elementKey}.");
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
