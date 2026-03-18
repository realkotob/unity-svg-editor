using System;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.Preview
{
    internal enum SvgPreserveAspectRatioScaleMode
    {
        Meet = 0,
        Slice = 1,
        None = 2
    }

    internal enum SvgPreserveAspectRatioAlignX
    {
        Min = 0,
        Mid = 1,
        Max = 2
    }

    internal enum SvgPreserveAspectRatioAlignY
    {
        Min = 0,
        Mid = 1,
        Max = 2
    }

    internal readonly struct SvgPreserveAspectRatioMode : IEquatable<SvgPreserveAspectRatioMode>
    {
        public static SvgPreserveAspectRatioMode Meet =>
            new(SvgPreserveAspectRatioScaleMode.Meet, SvgPreserveAspectRatioAlignX.Mid, SvgPreserveAspectRatioAlignY.Mid);

        public static SvgPreserveAspectRatioMode Slice =>
            new(SvgPreserveAspectRatioScaleMode.Slice, SvgPreserveAspectRatioAlignX.Mid, SvgPreserveAspectRatioAlignY.Mid);

        public static SvgPreserveAspectRatioMode None =>
            new(SvgPreserveAspectRatioScaleMode.None, SvgPreserveAspectRatioAlignX.Mid, SvgPreserveAspectRatioAlignY.Mid);

        public SvgPreserveAspectRatioMode(
            SvgPreserveAspectRatioScaleMode scaleMode,
            SvgPreserveAspectRatioAlignX alignX,
            SvgPreserveAspectRatioAlignY alignY)
        {
            ScaleMode = scaleMode;
            AlignX = alignX;
            AlignY = alignY;
        }

        public SvgPreserveAspectRatioScaleMode ScaleMode { get; }
        public SvgPreserveAspectRatioAlignX AlignX { get; }
        public SvgPreserveAspectRatioAlignY AlignY { get; }

        public bool IsNone => ScaleMode == SvgPreserveAspectRatioScaleMode.None;
        public bool IsSlice => ScaleMode == SvgPreserveAspectRatioScaleMode.Slice;

        public static SvgPreserveAspectRatioMode Parse(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return Meet;

            string[] tokens = rawValue
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return Meet;

            int index = 0;
            if (string.Equals(tokens[index], "defer", StringComparison.OrdinalIgnoreCase))
            {
                index++;
            }

            if (index >= tokens.Length)
                return Meet;

            if (string.Equals(tokens[index], "none", StringComparison.OrdinalIgnoreCase))
                return None;

            SvgPreserveAspectRatioAlignX alignX = SvgPreserveAspectRatioAlignX.Mid;
            SvgPreserveAspectRatioAlignY alignY = SvgPreserveAspectRatioAlignY.Mid;
            if (TryParseAlignment(tokens[index], out SvgPreserveAspectRatioAlignX parsedAlignX, out SvgPreserveAspectRatioAlignY parsedAlignY))
            {
                alignX = parsedAlignX;
                alignY = parsedAlignY;
                index++;
            }

            SvgPreserveAspectRatioScaleMode scaleMode = SvgPreserveAspectRatioScaleMode.Meet;
            if (index < tokens.Length &&
                string.Equals(tokens[index], "slice", StringComparison.OrdinalIgnoreCase))
            {
                scaleMode = SvgPreserveAspectRatioScaleMode.Slice;
            }

            return new SvgPreserveAspectRatioMode(scaleMode, alignX, alignY);
        }

        public bool Equals(SvgPreserveAspectRatioMode other)
        {
            return ScaleMode == other.ScaleMode &&
                   AlignX == other.AlignX &&
                   AlignY == other.AlignY;
        }

        public override bool Equals(object obj)
        {
            return obj is SvgPreserveAspectRatioMode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)ScaleMode, (int)AlignX, (int)AlignY);
        }

        public static bool operator ==(SvgPreserveAspectRatioMode left, SvgPreserveAspectRatioMode right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SvgPreserveAspectRatioMode left, SvgPreserveAspectRatioMode right)
        {
            return !left.Equals(right);
        }

        private static bool TryParseAlignment(
            string token,
            out SvgPreserveAspectRatioAlignX alignX,
            out SvgPreserveAspectRatioAlignY alignY)
        {
            alignX = SvgPreserveAspectRatioAlignX.Mid;
            alignY = SvgPreserveAspectRatioAlignY.Mid;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (token.Length < 8 || token[0] != 'x')
                return false;

            int yIndex = token.IndexOf('Y');
            if (yIndex <= 1 || yIndex >= token.Length - 1)
                return false;

            string xToken = token.Substring(1, yIndex - 1);
            string yToken = token.Substring(yIndex + 1);
            if (!TryParseAlignX(xToken, out alignX) || !TryParseAlignY(yToken, out alignY))
                return false;

            return true;
        }

        private static bool TryParseAlignX(string token, out SvgPreserveAspectRatioAlignX alignX)
        {
            alignX = token switch
            {
                "Min" => SvgPreserveAspectRatioAlignX.Min,
                "Mid" => SvgPreserveAspectRatioAlignX.Mid,
                "Max" => SvgPreserveAspectRatioAlignX.Max,
                _ => SvgPreserveAspectRatioAlignX.Mid
            };

            return token is "Min" or "Mid" or "Max";
        }

        private static bool TryParseAlignY(string token, out SvgPreserveAspectRatioAlignY alignY)
        {
            alignY = token switch
            {
                "Min" => SvgPreserveAspectRatioAlignY.Min,
                "Mid" => SvgPreserveAspectRatioAlignY.Mid,
                "Max" => SvgPreserveAspectRatioAlignY.Max,
                _ => SvgPreserveAspectRatioAlignY.Mid
            };

            return token is "Min" or "Mid" or "Max";
        }
    }
}
