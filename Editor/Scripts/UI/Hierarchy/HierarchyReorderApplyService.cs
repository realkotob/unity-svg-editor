using System;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.UI.Hierarchy
{
    internal sealed class HierarchyReorderApplyService
    {
        private readonly SvgMutator _svgMutator = new();

        public void ApplyMove(IHierarchyHost host, MoveElementRequest request)
        {
            Result<IHierarchyHost> validation = Validate(host, request);
            if (validation.IsFailure)
            {
                return;
            }

            IHierarchyHost hierarchyHost = validation.Value;
            Result<MutationResult> result = Apply(hierarchyHost, request);

            if (result.IsFailure)
            {
                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    hierarchyHost.UpdateSourceStatus($"Move failed: {result.Error}");
                }

                return;
            }

            MutationResult mutation = result.Value;
            if (!string.Equals(mutation.UpdatedSourceText, hierarchyHost.CurrentDocument.WorkingSourceText, StringComparison.Ordinal))
            {
                hierarchyHost.ApplyUpdatedSource(mutation.UpdatedSourceText, $"Moved #{request.ElementKey}.");
            }
        }

        private Result<IHierarchyHost> Validate(IHierarchyHost host, MoveElementRequest request)
        {
            if (host?.CurrentDocument == null)
            {
                return Result.Failure<IHierarchyHost>("Hierarchy host is unavailable.");
            }

            if (string.IsNullOrWhiteSpace(request.ElementKey) ||
                string.IsNullOrWhiteSpace(request.TargetParentKey) ||
                request.TargetChildIndex < 0)
            {
                return Result.Failure<IHierarchyHost>("Hierarchy reorder request is invalid.");
            }

            if (!host.CurrentDocument.CanUseDocumentModelForEditing)
            {
                string error = host.CurrentDocument.ResolveModelEditingFailureReason();
                host.UpdateSourceStatus($"Reorder failed: {error}");
                return Result.Failure<IHierarchyHost>(error);
            }

            return Result.Success(host);
        }

        private Result<MutationResult> Apply(IHierarchyHost host, MoveElementRequest request)
        {
            return _svgMutator.TryMoveElement(
                host.CurrentDocument.DocumentModel,
                request,
                out MutationResult result)
                ? Result.Success(result)
                : Result.Failure<MutationResult>(result.Error);
        }
    }
}
