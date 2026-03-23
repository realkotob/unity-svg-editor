using System;
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
    }
}
