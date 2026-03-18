using System;
using System.Collections.Generic;
using SvgEditor.Shared;
using Core.UI.Extensions;

namespace SvgEditor.DocumentModel
{
    internal static class SvgDocumentModelReferenceUtility
    {
        public static List<SvgNodeReference> RebuildReferences(IReadOnlyDictionary<string, string> attributes)
        {
            List<SvgNodeReference> references = new();
            if (attributes == null)
                return references;

            foreach (var pair in attributes)
            {
                if (!SvgFragmentReferenceUtility.TryExtractFragmentId(pair.Value, out string fragmentId))
                    continue;

                references.Add(new SvgNodeReference
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
