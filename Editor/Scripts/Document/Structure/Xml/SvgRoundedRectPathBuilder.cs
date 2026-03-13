using System;
using System.Globalization;

namespace SvgEditor.Document.Structure.Xml
{
    internal static class SvgRoundedRectPathBuilder
    {
        public static string BuildInsetRingPath(SvgRectShape shape, float inset)
        {
            if (inset <= 0f)
            {
                return BuildRoundedRectPath(shape);
            }

            float innerWidth = shape.Width - (inset * 2f);
            float innerHeight = shape.Height - (inset * 2f);
            if (innerWidth <= 0f || innerHeight <= 0f)
            {
                return BuildRoundedRectPath(shape);
            }

            SvgRectShape innerShape = new(
                shape.X + inset,
                shape.Y + inset,
                innerWidth,
                innerHeight,
                Math.Max(0f, shape.Rx - inset),
                Math.Max(0f, shape.Ry - inset));

            return $"{BuildRoundedRectPath(shape)} {BuildRoundedRectPath(innerShape)}";
        }

        private static string BuildRoundedRectPath(SvgRectShape shape)
        {
            float right = shape.X + shape.Width;
            float bottom = shape.Y + shape.Height;
            float rx = Math.Min(shape.Rx, shape.Width * 0.5f);
            float ry = Math.Min(shape.Ry, shape.Height * 0.5f);

            if (rx <= 0f || ry <= 0f)
            {
                return $"M {Format(shape.X)} {Format(shape.Y)} H {Format(right)} V {Format(bottom)} H {Format(shape.X)} Z";
            }

            return
                $"M {Format(shape.X + rx)} {Format(shape.Y)} " +
                $"H {Format(right - rx)} " +
                $"A {Format(rx)} {Format(ry)} 0 0 1 {Format(right)} {Format(shape.Y + ry)} " +
                $"V {Format(bottom - ry)} " +
                $"A {Format(rx)} {Format(ry)} 0 0 1 {Format(right - rx)} {Format(bottom)} " +
                $"H {Format(shape.X + rx)} " +
                $"A {Format(rx)} {Format(ry)} 0 0 1 {Format(shape.X)} {Format(bottom - ry)} " +
                $"V {Format(shape.Y + ry)} " +
                $"A {Format(rx)} {Format(ry)} 0 0 1 {Format(shape.X + rx)} {Format(shape.Y)} Z";
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
