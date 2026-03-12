using System;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

namespace SvgEditor.Document
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

            var builtContours = new List<BezierContour>();
            List<BezierSegment> currentSegments = null;
            var currentPoint = Vector2.zero;
            var subpathStart = Vector2.zero;
            var lastCubicControl = Vector2.zero;
            var lastQuadraticControl = Vector2.zero;
            var hasLastCubicControl = false;
            var hasLastQuadraticControl = false;
            var currentCommand = '\0';
            var index = 0;

            while (index < pathData.Length)
            {
                SkipPathSeparators(pathData, ref index);
                if (index >= pathData.Length)
                {
                    break;
                }

                var token = pathData[index];
                if (char.IsLetter(token))
                {
                    currentCommand = token;
                    index++;
                }
                else if (currentCommand == '\0')
                {
                    return false;
                }

                switch (currentCommand)
                {
                    case 'M':
                    case 'm':
                    {
                        if (!TryReadPoint(pathData, ref index, currentCommand == 'm', currentPoint, out var movePoint))
                        {
                            return false;
                        }

                        if (!TryFinalizeContour(currentSegments, closed: false, builtContours, ref currentPoint, subpathStart))
                        {
                            return false;
                        }

                        currentSegments = new List<BezierSegment>();
                        currentPoint = movePoint;
                        subpathStart = movePoint;
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        currentCommand = currentCommand == 'm' ? 'l' : 'L';

                        while (TryReadPoint(pathData, ref index, currentCommand == 'l', currentPoint, out var implicitLinePoint))
                        {
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, implicitLinePoint));
                            currentPoint = implicitLinePoint;
                        }
                        break;
                    }
                    case 'L':
                    case 'l':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                        {
                            return false;
                        }

                        while (TryReadPoint(pathData, ref index, currentCommand == 'l', currentPoint, out var linePoint))
                        {
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, linePoint));
                            currentPoint = linePoint;
                        }
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        break;
                    }
                    case 'H':
                    case 'h':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                        {
                            return false;
                        }

                        while (TryReadFloatToken(pathData, ref index, out var xValue))
                        {
                            var nextPoint = new Vector2(
                                currentCommand == 'h' ? currentPoint.x + xValue : xValue,
                                currentPoint.y);
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, nextPoint));
                            currentPoint = nextPoint;
                        }
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        break;
                    }
                    case 'V':
                    case 'v':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                        {
                            return false;
                        }

                        while (TryReadFloatToken(pathData, ref index, out var yValue))
                        {
                            var nextPoint = new Vector2(
                                currentPoint.x,
                                currentCommand == 'v' ? currentPoint.y + yValue : yValue);
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, nextPoint));
                            currentPoint = nextPoint;
                        }
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        break;
                    }
                    case 'C':
                    case 'c':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                        {
                            return false;
                        }

                        while (TryReadCurvePoints(pathData, ref index, currentCommand == 'c', currentPoint, out var c1, out var c2, out var endPoint))
                        {
                            currentSegments.Add(new BezierSegment
                            {
                                P0 = currentPoint,
                                P1 = c1,
                                P2 = c2,
                                P3 = endPoint
                            });
                            currentPoint = endPoint;
                            lastCubicControl = c2;
                            hasLastCubicControl = true;
                            hasLastQuadraticControl = false;
                        }
                        break;
                    }
                    case 'S':
                    case 's':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                        {
                            return false;
                        }

                        while (TryReadSmoothCurvePoints(pathData, ref index, currentCommand == 's', currentPoint, out var c2, out var endPoint))
                        {
                            var c1 = hasLastCubicControl
                                ? (currentPoint * 2f) - lastCubicControl
                                : currentPoint;
                            currentSegments.Add(new BezierSegment
                            {
                                P0 = currentPoint,
                                P1 = c1,
                                P2 = c2,
                                P3 = endPoint
                            });
                            currentPoint = endPoint;
                            lastCubicControl = c2;
                            hasLastCubicControl = true;
                            hasLastQuadraticControl = false;
                        }
                        break;
                    }
                    case 'Q':
                    case 'q':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                        {
                            return false;
                        }

                        while (TryReadQuadraticPoints(pathData, ref index, currentCommand == 'q', currentPoint, out var controlPoint, out var endPoint))
                        {
                            currentSegments.Add(VectorUtils.QuadraticToCubic(currentPoint, controlPoint, endPoint));
                            currentPoint = endPoint;
                            lastQuadraticControl = controlPoint;
                            hasLastQuadraticControl = true;
                            hasLastCubicControl = false;
                        }
                        break;
                    }
                    case 'T':
                    case 't':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                        {
                            return false;
                        }

                        while (TryReadPoint(pathData, ref index, currentCommand == 't', currentPoint, out var endPoint))
                        {
                            var controlPoint = hasLastQuadraticControl
                                ? (currentPoint * 2f) - lastQuadraticControl
                                : currentPoint;
                            currentSegments.Add(VectorUtils.QuadraticToCubic(currentPoint, controlPoint, endPoint));
                            currentPoint = endPoint;
                            lastQuadraticControl = controlPoint;
                            hasLastQuadraticControl = true;
                            hasLastCubicControl = false;
                        }
                        break;
                    }
                    case 'Z':
                    case 'z':
                    {
                        if (!TryEnsurePathStarted(currentSegments, subpathStart, ref currentPoint))
                        {
                            return false;
                        }

                        if ((currentPoint - subpathStart).sqrMagnitude > Mathf.Epsilon)
                        {
                            currentSegments.Add(VectorUtils.MakeLine(currentPoint, subpathStart));
                        }

                        if (!TryFinalizeContour(currentSegments, closed: true, builtContours, ref currentPoint, subpathStart))
                        {
                            return false;
                        }

                        currentSegments = null;
                        currentPoint = subpathStart;
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        break;
                    }
                    default:
                        return false;
                }
            }

            if (!TryFinalizeContour(currentSegments, closed: false, builtContours, ref currentPoint, subpathStart))
            {
                return false;
            }

            contours = builtContours.ToArray();
            return contours.Length > 0;
        }

        public static bool TryParsePoints(string pointsText, out List<Vector2> points)
        {
            points = new List<Vector2>();
            if (string.IsNullOrWhiteSpace(pointsText))
            {
                return false;
            }

            var index = 0;
            while (index < pointsText.Length)
            {
                if (!TryReadFloatToken(pointsText, ref index, out var x))
                {
                    break;
                }

                if (!TryReadFloatToken(pointsText, ref index, out var y))
                {
                    return false;
                }

                points.Add(new Vector2(x, y));
            }

            return points.Count >= 2;
        }

        private static bool TryEnsurePathStarted(
            List<BezierSegment> currentSegments,
            Vector2 subpathStart,
            ref Vector2 currentPoint)
        {
            if (currentSegments == null)
            {
                return false;
            }

            if (currentSegments.Count == 0)
            {
                currentPoint = subpathStart;
            }

            return true;
        }

        private static bool TryFinalizeContour(
            List<BezierSegment> currentSegments,
            bool closed,
            List<BezierContour> builtContours,
            ref Vector2 currentPoint,
            Vector2 subpathStart)
        {
            if (currentSegments == null)
            {
                return true;
            }

            if (currentSegments.Count == 0)
            {
                currentPoint = subpathStart;
                return true;
            }

            builtContours.Add(new BezierContour
            {
                Segments = VectorUtils.BezierSegmentsToPath(currentSegments.ToArray()),
                Closed = closed
            });
            currentPoint = subpathStart;
            return true;
        }

        private static bool TryReadPoint(
            string pathData,
            ref int index,
            bool relative,
            Vector2 origin,
            out Vector2 point)
        {
            point = Vector2.zero;
            if (!TryReadFloatToken(pathData, ref index, out var x) ||
                !TryReadFloatToken(pathData, ref index, out var y))
            {
                return false;
            }

            point = relative ? origin + new Vector2(x, y) : new Vector2(x, y);
            return true;
        }

        private static bool TryReadCurvePoints(
            string pathData,
            ref int index,
            bool relative,
            Vector2 origin,
            out Vector2 c1,
            out Vector2 c2,
            out Vector2 endPoint)
        {
            c1 = Vector2.zero;
            c2 = Vector2.zero;
            endPoint = Vector2.zero;

            return TryReadPoint(pathData, ref index, relative, origin, out c1) &&
                   TryReadPoint(pathData, ref index, relative, origin, out c2) &&
                   TryReadPoint(pathData, ref index, relative, origin, out endPoint);
        }

        private static bool TryReadSmoothCurvePoints(
            string pathData,
            ref int index,
            bool relative,
            Vector2 origin,
            out Vector2 c2,
            out Vector2 endPoint)
        {
            c2 = Vector2.zero;
            endPoint = Vector2.zero;

            return TryReadPoint(pathData, ref index, relative, origin, out c2) &&
                   TryReadPoint(pathData, ref index, relative, origin, out endPoint);
        }

        private static bool TryReadQuadraticPoints(
            string pathData,
            ref int index,
            bool relative,
            Vector2 origin,
            out Vector2 controlPoint,
            out Vector2 endPoint)
        {
            controlPoint = Vector2.zero;
            endPoint = Vector2.zero;

            return TryReadPoint(pathData, ref index, relative, origin, out controlPoint) &&
                   TryReadPoint(pathData, ref index, relative, origin, out endPoint);
        }

        private static void SkipPathSeparators(string pathData, ref int index)
        {
            while (index < pathData.Length &&
                   (char.IsWhiteSpace(pathData[index]) || pathData[index] == ','))
            {
                index++;
            }
        }

        private static bool TryReadFloatToken(string text, ref int index, out float value)
        {
            value = 0f;
            SkipPathSeparators(text, ref index);
            if (index >= text.Length)
            {
                return false;
            }

            var start = index;
            var hasExponent = false;
            var hasDecimal = false;

            if (text[index] == '+' || text[index] == '-')
            {
                index++;
            }

            while (index < text.Length)
            {
                var ch = text[index];
                if (char.IsDigit(ch))
                {
                    index++;
                    continue;
                }

                if (ch == '.' && !hasDecimal)
                {
                    hasDecimal = true;
                    index++;
                    continue;
                }

                if ((ch == 'e' || ch == 'E') && !hasExponent)
                {
                    hasExponent = true;
                    index++;
                    if (index < text.Length && (text[index] == '+' || text[index] == '-'))
                    {
                        index++;
                    }
                    continue;
                }

                break;
            }

            return index > start &&
                   SvgAttributeUtility.TryParseFloat(text.Substring(start, index - start), out value);
        }
    }
}
