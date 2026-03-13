using System;

namespace SvgEditor.Document.Structure.Xml
{
    internal readonly struct SvgRectShape
    {
        public SvgRectShape(float x, float y, float width, float height, float rx, float ry)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Rx = rx;
            Ry = ry;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public float Rx { get; }
        public float Ry { get; }

        public bool EqualsGeometry(SvgRectShape other)
        {
            return AreEqual(X, other.X) &&
                   AreEqual(Y, other.Y) &&
                   AreEqual(Width, other.Width) &&
                   AreEqual(Height, other.Height) &&
                   AreEqual(Rx, other.Rx) &&
                   AreEqual(Ry, other.Ry);
        }

        private static bool AreEqual(float left, float right)
        {
            return Math.Abs(left - right) <= 0.001f;
        }
    }
}
