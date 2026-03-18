using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using Core.UI.Extensions;

namespace SvgEditor.Document.Structure.Geometry
{
    internal static class SvgPathGeometryParser
    {
        public static bool TryParsePathContours(string pathData, out BezierContour[] contours)
        {
            contours = Array.Empty<BezierContour>();
            if (string.IsNullOrWhiteSpace(pathData))
            {
                return false;
            }

            var reader = new SvgPathTokenReader(pathData);
            var state = new SvgPathParseState();
            var currentCommand = '\0';

            while (reader.TryReadCommand(ref currentCommand, out var command))
            {
                if (!state.TryHandleCommand(command, ref reader))
                {
                    return false;
                }
            }

            if (!reader.IsComplete || !state.TryFinalizeContour(closed: false))
            {
                return false;
            }

            contours = state.ToContours();
            return contours.Length > 0;
        }

        public static bool TryParsePoints(string pointsText, out List<Vector2> points)
        {
            points = new List<Vector2>();
            if (string.IsNullOrWhiteSpace(pointsText))
            {
                return false;
            }

            var reader = new SvgPathTokenReader(pointsText);
            while (reader.TryReadFloatToken(out var x))
            {
                if (!reader.TryReadFloatToken(out var y))
                {
                    return false;
                }

                points.Add(new Vector2(x, y));
            }

            return reader.IsComplete && points.Count >= 2;
        }
    }
}
