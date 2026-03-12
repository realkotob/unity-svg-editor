using System;

namespace SvgEditor
{
    internal readonly struct SvgNodeId : IEquatable<SvgNodeId>
    {
        public static readonly SvgNodeId Root = new("root");

        public SvgNodeId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool IsRoot => Equals(Root);

        public static SvgNodeId FromXmlId(string xmlId)
        {
            return string.IsNullOrWhiteSpace(xmlId)
                ? default
                : new SvgNodeId($"id:{xmlId.Trim()}");
        }

        public static SvgNodeId FromStructuralPath(string structuralPath)
        {
            return string.IsNullOrWhiteSpace(structuralPath)
                ? default
                : new SvgNodeId($"path:{structuralPath.Trim()}");
        }

        public bool Equals(SvgNodeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SvgNodeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(SvgNodeId left, SvgNodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SvgNodeId left, SvgNodeId right)
        {
            return !left.Equals(right);
        }
    }
}
