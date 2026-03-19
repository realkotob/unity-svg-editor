using System;
using System.Collections.Generic;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Mutation
{
    internal static class NodeReferenceBuilder
    {
        public static List<NodeReference> Build(IReadOnlyDictionary<string, string> attributes)
        {
            List<NodeReference> references = new();
            if (attributes == null)
                return references;

            foreach (var pair in attributes)
            {
                if (!SvgFragmentReferenceUtility.TryExtractFragmentId(pair.Value, out string fragmentId))
                    continue;

                references.Add(new NodeReference
                {
                    AttributeName = pair.Key,
                    RawValue = pair.Value,
                    FragmentId = fragmentId
                });
            }

            return references;
        }
    }
}
