using System;
using System.Collections.Generic;
using UnityEngine;
using SvgEditor.Core.Svg.Structure.Lookup;

namespace SvgEditor.Core.Svg.PathEditing
{
    internal static class PathDataParser
    {
        public static PathData Parse(string pathText)
        {
            PathData pathData = new();
            if (string.IsNullOrWhiteSpace(pathText))
            {
                return pathData;
            }

            Reader reader = new(pathText);
            PathSubpath currentSubpath = null;
            Vector2 currentPoint = Vector2.zero;
            Vector2 subpathStart = Vector2.zero;
            Vector2 lastCubicControl = Vector2.zero;
            Vector2 lastQuadraticControl = Vector2.zero;
            bool hasLastCubicControl = false;
            bool hasLastQuadraticControl = false;
            char currentCommand = '\0';

            while (reader.TryReadCommand(ref currentCommand, out char command))
            {
                bool isRelative = char.IsLower(command);
                char normalizedCommand = char.ToUpperInvariant(command);

                switch (normalizedCommand)
                {
                    case 'M':
                        if (!TryReadRequiredPoint(ref reader, normalizedCommand, isRelative, currentPoint, out Vector2 movePoint, out string moveError))
                        {
                            pathData.MarkMalformed(moveError);
                            break;
                        }

                        FinalizeSubpath(pathData, currentSubpath);
                        currentSubpath = new PathSubpath { Start = movePoint };
                        currentPoint = movePoint;
                        subpathStart = movePoint;
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;

                        string moveLineError = string.Empty;
                        while (TryReadOptionalPoint(ref reader, normalizedCommand, isRelative, currentPoint, out Vector2 linePoint, out moveLineError))
                        {
                            currentSubpath.Nodes.Add(new PathNode('L', linePoint));
                            currentPoint = linePoint;
                        }

                        if (!string.IsNullOrWhiteSpace(moveLineError))
                        {
                            pathData.MarkMalformed(moveLineError);
                        }
                        break;

                    case 'L':
                        if (currentSubpath == null)
                        {
                            pathData.MarkMalformed("Command L requires an active subpath.");
                            break;
                        }

                        if (!TryReadRepeatedPoints(ref reader, normalizedCommand, isRelative, ref currentPoint, currentSubpath, out string lineError))
                        {
                            pathData.MarkMalformed(lineError);
                        }
                        else
                        {
                            hasLastCubicControl = false;
                            hasLastQuadraticControl = false;
                        }
                        break;

                    case 'H':
                        if (currentSubpath == null)
                        {
                            pathData.MarkMalformed("Command H requires an active subpath.");
                            break;
                        }

                        if (!TryReadRepeatedHorizontalScalars(ref reader, normalizedCommand, isRelative, ref currentPoint, currentSubpath, out string horizontalError))
                        {
                            pathData.MarkMalformed(horizontalError);
                        }
                        else
                        {
                            hasLastCubicControl = false;
                            hasLastQuadraticControl = false;
                        }
                        break;

                    case 'V':
                        if (currentSubpath == null)
                        {
                            pathData.MarkMalformed("Command V requires an active subpath.");
                            break;
                        }

                        if (!TryReadRepeatedVerticalScalars(ref reader, normalizedCommand, isRelative, ref currentPoint, currentSubpath, out string verticalError))
                        {
                            pathData.MarkMalformed(verticalError);
                        }
                        else
                        {
                            hasLastCubicControl = false;
                            hasLastQuadraticControl = false;
                        }
                        break;

                    case 'C':
                        if (currentSubpath == null)
                        {
                            pathData.MarkMalformed("Command C requires an active subpath.");
                            break;
                        }

                        if (!TryReadRepeatedCubicSegments(
                                ref reader,
                                normalizedCommand,
                                isRelative,
                                ref currentPoint,
                                currentSubpath,
                                ref lastCubicControl,
                                ref hasLastCubicControl,
                                ref hasLastQuadraticControl,
                                out string cubicError))
                        {
                            pathData.MarkMalformed(cubicError);
                        }
                        break;

                    case 'S':
                        if (currentSubpath == null)
                        {
                            pathData.MarkMalformed("Command S requires an active subpath.");
                            break;
                        }

                        if (!TryReadRepeatedSmoothCubicSegments(
                                ref reader,
                                normalizedCommand,
                                isRelative,
                                ref currentPoint,
                                currentSubpath,
                                ref lastCubicControl,
                                ref hasLastCubicControl,
                                ref hasLastQuadraticControl,
                                out string smoothCubicError))
                        {
                            pathData.MarkMalformed(smoothCubicError);
                        }
                        break;

                    case 'Q':
                        if (currentSubpath == null)
                        {
                            pathData.MarkMalformed("Command Q requires an active subpath.");
                            break;
                        }

                        if (!TryReadRepeatedQuadraticSegments(
                                ref reader,
                                normalizedCommand,
                                isRelative,
                                ref currentPoint,
                                currentSubpath,
                                ref lastQuadraticControl,
                                ref hasLastQuadraticControl,
                                ref hasLastCubicControl,
                                out string quadraticError))
                        {
                            pathData.MarkMalformed(quadraticError);
                        }
                        break;

                    case 'A':
                        if (currentSubpath == null)
                        {
                            pathData.MarkMalformed("Command A requires an active subpath.");
                            break;
                        }

                        if (!TryReadRepeatedArcSegments(
                                ref reader,
                                normalizedCommand,
                                isRelative,
                                ref currentPoint,
                                currentSubpath,
                                ref lastCubicControl,
                                ref hasLastCubicControl,
                                ref hasLastQuadraticControl,
                                out string arcError))
                        {
                            pathData.MarkMalformed(arcError);
                        }
                        break;

                    case 'T':
                        if (currentSubpath == null)
                        {
                            pathData.MarkMalformed("Command T requires an active subpath.");
                            break;
                        }

                        if (!TryReadRepeatedSmoothQuadraticSegments(
                                ref reader,
                                normalizedCommand,
                                isRelative,
                                ref currentPoint,
                                currentSubpath,
                                ref lastQuadraticControl,
                                ref hasLastQuadraticControl,
                                ref hasLastCubicControl,
                                out string smoothQuadraticError))
                        {
                            pathData.MarkMalformed(smoothQuadraticError);
                        }
                        break;

                    case 'Z':
                        if (currentSubpath == null)
                        {
                            pathData.MarkMalformed("Command Z requires an active subpath.");
                            break;
                        }

                        currentSubpath.IsClosed = true;
                        currentPoint = subpathStart;
                        hasLastCubicControl = false;
                        hasLastQuadraticControl = false;
                        currentCommand = '\0';
                        break;

                    default:
                        pathData.AddUnsupportedCommand(normalizedCommand);
                        break;
                }

                if (pathData.IsMalformed || pathData.HasUnsupportedCommands)
                {
                    break;
                }
            }

            FinalizeSubpath(pathData, currentSubpath);
            if (!pathData.IsMalformed &&
                !pathData.HasUnsupportedCommands &&
                !reader.IsComplete)
            {
                pathData.MarkMalformed($"Unexpected token at position {reader.Position}.");
            }

            return pathData;
        }

        private static bool TryReadRequiredPoint(
            ref Reader reader,
            char command,
            bool isRelative,
            Vector2 currentPoint,
            out Vector2 point,
            out string error)
        {
            point = Vector2.zero;
            int start = reader.Position;
            if (reader.TryReadPoint(isRelative, currentPoint, out point))
            {
                error = string.Empty;
                return true;
            }

            error = reader.Position != start
                ? $"Command {command} is truncated."
                : $"Command {command} requires coordinates.";
            return false;
        }

        private static bool TryReadOptionalPoint(
            ref Reader reader,
            char command,
            bool isRelative,
            Vector2 currentPoint,
            out Vector2 point,
            out string error)
        {
            point = Vector2.zero;
            error = string.Empty;
            int start = reader.Position;
            if (reader.TryReadPoint(isRelative, currentPoint, out point))
            {
                return true;
            }

            if (reader.Position != start)
            {
                error = $"Command {command} is truncated.";
                return false;
            }

            if (!reader.IsComplete && !reader.IsCommandAhead)
            {
                error = $"Unexpected token after command {command}.";
                return false;
            }

            return false;
        }

        private static bool TryReadRepeatedPoints(
            ref Reader reader,
            char command,
            bool isRelative,
            ref Vector2 currentPoint,
            PathSubpath subpath,
            out string error)
        {
            error = string.Empty;
            bool consumedAny = false;

            while (true)
            {
                int start = reader.Position;
                if (reader.TryReadPoint(isRelative, currentPoint, out Vector2 linePoint))
                {
                    subpath.Nodes.Add(new PathNode('L', linePoint));
                    currentPoint = linePoint;
                    consumedAny = true;
                    continue;
                }

                return FinishRepeatedRead(ref reader, command, consumedAny, start, out error);
            }
        }

        private static bool TryReadRepeatedHorizontalScalars(
            ref Reader reader,
            char command,
            bool isRelative,
            ref Vector2 currentPoint,
            PathSubpath subpath,
            out string error)
        {
            error = string.Empty;
            bool consumedAny = false;

            while (true)
            {
                int start = reader.Position;
                if (reader.TryReadFloat(out float value))
                {
                    currentPoint = new Vector2(isRelative ? currentPoint.x + value : value, currentPoint.y);
                    subpath.Nodes.Add(new PathNode('H', currentPoint));
                    consumedAny = true;
                    continue;
                }

                return FinishRepeatedRead(ref reader, command, consumedAny, start, out error);
            }
        }

        private static bool TryReadRepeatedVerticalScalars(
            ref Reader reader,
            char command,
            bool isRelative,
            ref Vector2 currentPoint,
            PathSubpath subpath,
            out string error)
        {
            error = string.Empty;
            bool consumedAny = false;

            while (true)
            {
                int start = reader.Position;
                if (reader.TryReadFloat(out float value))
                {
                    currentPoint = new Vector2(currentPoint.x, isRelative ? currentPoint.y + value : value);
                    subpath.Nodes.Add(new PathNode('V', currentPoint));
                    consumedAny = true;
                    continue;
                }

                return FinishRepeatedRead(ref reader, command, consumedAny, start, out error);
            }
        }

        private static bool TryReadRepeatedCubicSegments(
            ref Reader reader,
            char command,
            bool isRelative,
            ref Vector2 currentPoint,
            PathSubpath subpath,
            ref Vector2 lastCubicControl,
            ref bool hasLastCubicControl,
            ref bool hasLastQuadraticControl,
            out string error)
        {
            error = string.Empty;
            bool consumedAny = false;

            while (true)
            {
                int start = reader.Position;
                if (reader.TryReadPoint(isRelative, currentPoint, out Vector2 c0) &&
                    reader.TryReadPoint(isRelative, currentPoint, out Vector2 c1) &&
                    reader.TryReadPoint(isRelative, currentPoint, out Vector2 endPoint))
                {
                    subpath.Nodes.Add(new PathNode('C', endPoint, c0, c1, PathHandleMode.Free));
                    currentPoint = endPoint;
                    lastCubicControl = c1;
                    hasLastCubicControl = true;
                    hasLastQuadraticControl = false;
                    consumedAny = true;
                    continue;
                }

                return FinishRepeatedRead(ref reader, command, consumedAny, start, out error);
            }
        }

        private static bool TryReadRepeatedSmoothCubicSegments(
            ref Reader reader,
            char command,
            bool isRelative,
            ref Vector2 currentPoint,
            PathSubpath subpath,
            ref Vector2 lastCubicControl,
            ref bool hasLastCubicControl,
            ref bool hasLastQuadraticControl,
            out string error)
        {
            error = string.Empty;
            bool consumedAny = false;

            while (true)
            {
                int start = reader.Position;
                if (reader.TryReadPoint(isRelative, currentPoint, out Vector2 smoothControl) &&
                    reader.TryReadPoint(isRelative, currentPoint, out Vector2 smoothEndPoint))
                {
                    Vector2 reflectedControl = hasLastCubicControl
                        ? (currentPoint * 2f) - lastCubicControl
                        : currentPoint;
                    subpath.Nodes.Add(new PathNode('C', smoothEndPoint, reflectedControl, smoothControl, PathHandleMode.Free));
                    currentPoint = smoothEndPoint;
                    lastCubicControl = smoothControl;
                    hasLastCubicControl = true;
                    hasLastQuadraticControl = false;
                    consumedAny = true;
                    continue;
                }

                return FinishRepeatedRead(ref reader, command, consumedAny, start, out error);
            }
        }

        private static bool TryReadRepeatedQuadraticSegments(
            ref Reader reader,
            char command,
            bool isRelative,
            ref Vector2 currentPoint,
            PathSubpath subpath,
            ref Vector2 lastQuadraticControl,
            ref bool hasLastQuadraticControl,
            ref bool hasLastCubicControl,
            out string error)
        {
            error = string.Empty;
            bool consumedAny = false;

            while (true)
            {
                int start = reader.Position;
                if (reader.TryReadPoint(isRelative, currentPoint, out Vector2 quadraticControl) &&
                    reader.TryReadPoint(isRelative, currentPoint, out Vector2 quadraticEndPoint))
                {
                    subpath.Nodes.Add(new PathNode('Q', quadraticEndPoint, quadraticControl, Vector2.zero, PathHandleMode.Free));
                    currentPoint = quadraticEndPoint;
                    lastQuadraticControl = quadraticControl;
                    hasLastQuadraticControl = true;
                    hasLastCubicControl = false;
                    consumedAny = true;
                    continue;
                }

                return FinishRepeatedRead(ref reader, command, consumedAny, start, out error);
            }
        }

        private static bool TryReadRepeatedSmoothQuadraticSegments(
            ref Reader reader,
            char command,
            bool isRelative,
            ref Vector2 currentPoint,
            PathSubpath subpath,
            ref Vector2 lastQuadraticControl,
            ref bool hasLastQuadraticControl,
            ref bool hasLastCubicControl,
            out string error)
        {
            error = string.Empty;
            bool consumedAny = false;

            while (true)
            {
                int start = reader.Position;
                if (reader.TryReadPoint(isRelative, currentPoint, out Vector2 smoothQuadraticEndPoint))
                {
                    Vector2 reflectedControl = hasLastQuadraticControl
                        ? (currentPoint * 2f) - lastQuadraticControl
                        : currentPoint;
                    subpath.Nodes.Add(new PathNode('Q', smoothQuadraticEndPoint, reflectedControl, Vector2.zero, PathHandleMode.Free));
                    currentPoint = smoothQuadraticEndPoint;
                    lastQuadraticControl = reflectedControl;
                    hasLastQuadraticControl = true;
                    hasLastCubicControl = false;
                    consumedAny = true;
                    continue;
                }

                return FinishRepeatedRead(ref reader, command, consumedAny, start, out error);
            }
        }

        private static bool TryReadRepeatedArcSegments(
            ref Reader reader,
            char command,
            bool isRelative,
            ref Vector2 currentPoint,
            PathSubpath subpath,
            ref Vector2 lastCubicControl,
            ref bool hasLastCubicControl,
            ref bool hasLastQuadraticControl,
            out string error)
        {
            error = string.Empty;
            bool consumedAny = false;

            while (true)
            {
                int start = reader.Position;
                if (TryReadArcSegment(ref reader, isRelative, currentPoint, out ArcSegmentData arcSegment))
                {
                    if (!TryConvertArcToPathNodes(currentPoint, arcSegment, out List<PathNode> pathNodes))
                    {
                        error = $"Command {command} is truncated.";
                        return false;
                    }

                    for (int index = 0; index < pathNodes.Count; index++)
                    {
                        subpath.Nodes.Add(pathNodes[index]);
                    }

                    currentPoint = arcSegment.EndPoint;
                    if (pathNodes.Count > 0 && pathNodes[^1].Command == 'C')
                    {
                        lastCubicControl = pathNodes[^1].Control1;
                        hasLastCubicControl = true;
                    }
                    else
                    {
                        hasLastCubicControl = false;
                    }

                    hasLastQuadraticControl = false;
                    consumedAny = true;
                    continue;
                }

                return FinishRepeatedRead(ref reader, command, consumedAny, start, out error);
            }
        }

        private static bool TryReadArcSegment(
            ref Reader reader,
            bool isRelative,
            Vector2 currentPoint,
            out ArcSegmentData segment)
        {
            segment = default;
            return reader.TryReadFloat(out float rx) &&
                   reader.TryReadFloat(out float ry) &&
                   reader.TryReadFloat(out float xAxisRotation) &&
                   reader.TryReadFlag(out bool largeArc) &&
                   reader.TryReadFlag(out bool sweep) &&
                   reader.TryReadPoint(isRelative, currentPoint, out Vector2 endPoint) &&
                   AssignArcSegment(rx, ry, xAxisRotation, largeArc, sweep, endPoint, out segment);
        }

        private static bool AssignArcSegment(
            float rx,
            float ry,
            float xAxisRotation,
            bool largeArc,
            bool sweep,
            Vector2 endPoint,
            out ArcSegmentData segment)
        {
            segment = new ArcSegmentData(rx, ry, xAxisRotation, largeArc, sweep, endPoint);
            return true;
        }

        private static bool TryConvertArcToPathNodes(
            Vector2 startPoint,
            ArcSegmentData arcSegment,
            out List<PathNode> pathNodes)
        {
            pathNodes = new List<PathNode>();
            Vector2 endPoint = arcSegment.EndPoint;
            if ((endPoint - startPoint).sqrMagnitude <= 0.000001f)
            {
                return true;
            }

            float rx = Mathf.Abs(arcSegment.Rx);
            float ry = Mathf.Abs(arcSegment.Ry);
            if (rx <= 0.000001f || ry <= 0.000001f)
            {
                pathNodes.Add(new PathNode('L', endPoint));
                return true;
            }

            float phi = arcSegment.XAxisRotation * Mathf.Deg2Rad;
            float cosPhi = Mathf.Cos(phi);
            float sinPhi = Mathf.Sin(phi);

            float dx2 = (startPoint.x - endPoint.x) * 0.5f;
            float dy2 = (startPoint.y - endPoint.y) * 0.5f;
            float x1Prime = (cosPhi * dx2) + (sinPhi * dy2);
            float y1Prime = (-sinPhi * dx2) + (cosPhi * dy2);

            float rxSq = rx * rx;
            float rySq = ry * ry;
            float x1PrimeSq = x1Prime * x1Prime;
            float y1PrimeSq = y1Prime * y1Prime;

            float lambda = (x1PrimeSq / rxSq) + (y1PrimeSq / rySq);
            if (lambda > 1f)
            {
                float scale = Mathf.Sqrt(lambda);
                rx *= scale;
                ry *= scale;
                rxSq = rx * rx;
                rySq = ry * ry;
            }

            float numerator = (rxSq * rySq) - (rxSq * y1PrimeSq) - (rySq * x1PrimeSq);
            float denominator = (rxSq * y1PrimeSq) + (rySq * x1PrimeSq);
            float factor = denominator <= 0.000001f
                ? 0f
                : Mathf.Sqrt(Mathf.Max(0f, numerator / denominator));
            if (arcSegment.LargeArc == arcSegment.Sweep)
            {
                factor = -factor;
            }

            float cxPrime = factor * ((rx * y1Prime) / ry);
            float cyPrime = factor * (-(ry * x1Prime) / rx);

            float cx = (cosPhi * cxPrime) - (sinPhi * cyPrime) + ((startPoint.x + endPoint.x) * 0.5f);
            float cy = (sinPhi * cxPrime) + (cosPhi * cyPrime) + ((startPoint.y + endPoint.y) * 0.5f);

            Vector2 unitStart = new((x1Prime - cxPrime) / rx, (y1Prime - cyPrime) / ry);
            Vector2 unitEnd = new((-x1Prime - cxPrime) / rx, (-y1Prime - cyPrime) / ry);

            float startAngle = VectorAngle(new Vector2(1f, 0f), unitStart);
            float deltaAngle = VectorAngle(unitStart, unitEnd);
            if (!arcSegment.Sweep && deltaAngle > 0f)
            {
                deltaAngle -= Mathf.PI * 2f;
            }
            else if (arcSegment.Sweep && deltaAngle < 0f)
            {
                deltaAngle += Mathf.PI * 2f;
            }

            int segmentCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(deltaAngle) / (Mathf.PI * 0.5f)));
            float step = deltaAngle / segmentCount;
            for (int index = 0; index < segmentCount; index++)
            {
                float angle0 = startAngle + (index * step);
                float angle1 = angle0 + step;
                AppendArcSegment(pathNodes, cx, cy, rx, ry, cosPhi, sinPhi, angle0, angle1);
            }

            if (pathNodes.Count > 0)
            {
                PathNode lastNode = pathNodes[^1];
                pathNodes[^1] = new PathNode(lastNode.Command, endPoint, lastNode.Control0, lastNode.Control1, lastNode.HandleMode);
            }

            return true;
        }

        private static void AppendArcSegment(
            List<PathNode> pathNodes,
            float cx,
            float cy,
            float rx,
            float ry,
            float cosPhi,
            float sinPhi,
            float angle0,
            float angle1)
        {
            float alpha = (4f / 3f) * Mathf.Tan((angle1 - angle0) * 0.25f);
            Vector2 point0 = new(Mathf.Cos(angle0), Mathf.Sin(angle0));
            Vector2 point1 = new(Mathf.Cos(angle1), Mathf.Sin(angle1));
            Vector2 control0 = new(point0.x - (alpha * point0.y), point0.y + (alpha * point0.x));
            Vector2 control1 = new(point1.x + (alpha * point1.y), point1.y - (alpha * point1.x));

            pathNodes.Add(new PathNode(
                'C',
                TransformArcPoint(point1, cx, cy, rx, ry, cosPhi, sinPhi),
                TransformArcPoint(control0, cx, cy, rx, ry, cosPhi, sinPhi),
                TransformArcPoint(control1, cx, cy, rx, ry, cosPhi, sinPhi),
                PathHandleMode.Free));
        }

        private static Vector2 TransformArcPoint(
            Vector2 point,
            float cx,
            float cy,
            float rx,
            float ry,
            float cosPhi,
            float sinPhi)
        {
            float x = (rx * point.x);
            float y = (ry * point.y);
            return new Vector2(
                (cosPhi * x) - (sinPhi * y) + cx,
                (sinPhi * x) + (cosPhi * y) + cy);
        }

        private static float VectorAngle(Vector2 from, Vector2 to)
        {
            float dot = Mathf.Clamp(Vector2.Dot(from, to), -1f, 1f);
            float angle = Mathf.Acos(dot);
            return ((from.x * to.y) - (from.y * to.x)) < 0f ? -angle : angle;
        }

        private static bool FinishRepeatedRead(
            ref Reader reader,
            char command,
            bool consumedAny,
            int start,
            out string error)
        {
            if (reader.Position != start)
            {
                error = $"Command {command} is truncated.";
                return false;
            }

            if (!consumedAny)
            {
                error = reader.IsComplete || reader.IsCommandAhead
                    ? $"Command {command} requires coordinates."
                    : $"Unexpected token after command {command}.";
                return false;
            }

            if (!reader.IsComplete && !reader.IsCommandAhead)
            {
                error = $"Unexpected token after command {command}.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static void FinalizeSubpath(PathData pathData, PathSubpath subpath)
        {
            if (subpath != null)
            {
                pathData.Subpaths.Add(subpath);
            }
        }

        private ref struct Reader
        {
            private readonly string _text;
            private int _index;

            public Reader(string text)
            {
                _text = text ?? string.Empty;
                _index = 0;
            }

            public int Position => SkipSeparators(_index);

            public bool IsComplete => Position >= _text.Length;

            public bool IsCommandAhead
            {
                get
                {
                    int position = Position;
                    return position < _text.Length && char.IsLetter(_text[position]);
                }
            }

            public bool TryReadCommand(ref char currentCommand, out char command)
            {
                command = '\0';
                SkipSeparators();
                if (_index >= _text.Length)
                {
                    return false;
                }

                char token = _text[_index];
                if (char.IsLetter(token))
                {
                    currentCommand = token;
                    command = token;
                    _index++;
                    return true;
                }

                if (currentCommand == '\0')
                {
                    return false;
                }

                command = currentCommand;
                return true;
            }

            public bool TryReadPoint(bool relative, Vector2 origin, out Vector2 point)
            {
                point = Vector2.zero;
                if (!TryReadFloat(out float x) || !TryReadFloat(out float y))
                {
                    return false;
                }

                point = relative ? origin + new Vector2(x, y) : new Vector2(x, y);
                return true;
            }

            public bool TryReadFloat(out float value)
            {
                value = 0f;
                SkipSeparators();
                if (_index >= _text.Length)
                {
                    return false;
                }

                int start = _index;
                bool hasExponent = false;
                bool hasDecimal = false;

                if (_text[_index] == '+' || _text[_index] == '-')
                {
                    _index++;
                }

                while (_index < _text.Length)
                {
                    char ch = _text[_index];
                    if (char.IsDigit(ch))
                    {
                        _index++;
                        continue;
                    }

                    if (ch == '.' && !hasDecimal)
                    {
                        hasDecimal = true;
                        _index++;
                        continue;
                    }

                    if ((ch == 'e' || ch == 'E') && !hasExponent)
                    {
                        hasExponent = true;
                        _index++;
                        if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-'))
                        {
                            _index++;
                        }

                        continue;
                    }

                    break;
                }

                return _index > start &&
                       AttributeUtility.TryParseFloat(_text.Substring(start, _index - start), out value);
            }

            public bool TryReadFlag(out bool flag)
            {
                flag = false;
                SkipSeparators();
                if (_index >= _text.Length)
                {
                    return false;
                }

                char token = _text[_index];
                if (token == '0')
                {
                    flag = false;
                    _index++;
                    return true;
                }

                if (token == '1')
                {
                    flag = true;
                    _index++;
                    return true;
                }

                return false;
            }

            private void SkipSeparators()
            {
                _index = SkipSeparators(_index);
            }

            private int SkipSeparators(int index)
            {
                while (index < _text.Length &&
                       (char.IsWhiteSpace(_text[index]) || _text[index] == ','))
                {
                    index++;
                }

                return index;
            }
        }

        private readonly struct ArcSegmentData
        {
            public ArcSegmentData(float rx, float ry, float xAxisRotation, bool largeArc, bool sweep, Vector2 endPoint)
            {
                Rx = rx;
                Ry = ry;
                XAxisRotation = xAxisRotation;
                LargeArc = largeArc;
                Sweep = sweep;
                EndPoint = endPoint;
            }

            public float Rx { get; }
            public float Ry { get; }
            public float XAxisRotation { get; }
            public bool LargeArc { get; }
            public bool Sweep { get; }
            public Vector2 EndPoint { get; }
        }
    }
}
