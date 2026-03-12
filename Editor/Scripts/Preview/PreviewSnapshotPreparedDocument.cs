using System;
using System.Collections.Generic;
using System.Xml;

using SvgEditor;

namespace SvgEditor.Preview
{
    internal sealed class PreviewSnapshotPreparedDocument
    {
        public XmlDocument Document { get; set; }
        public XmlElement Root { get; set; }
        public SvgPreserveAspectRatioMode PreserveAspectRatioMode { get; set; } = SvgPreserveAspectRatioMode.Meet;
        public IReadOnlyDictionary<string, (string Key, string TargetKey)> KeyByNodeId { get; set; } =
            new Dictionary<string, (string Key, string TargetKey)>(StringComparer.Ordinal);
    }
}
