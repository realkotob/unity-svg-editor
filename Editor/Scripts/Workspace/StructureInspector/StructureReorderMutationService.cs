using System;

namespace SvgEditor
{
    internal sealed class StructureReorderMutationService
    {
        private readonly SvgDocumentModelMutationService _documentModelMutationService = new();

        public void ApplyMove(
            IStructureHierarchyHost host,
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
                    elementKey,
                    parentKey,
                    childIndex,
                    out SvgDocumentModel _,
                    out string reorderedSource,
                    out string error))
            {
                if (!string.Equals(reorderedSource, host.CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
                {
                    host.ApplyUpdatedSource(reorderedSource, $"Moved #{elementKey}.");
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                host.UpdateSourceStatus($"Move failed: {error}");
            }
        }
    }
}
