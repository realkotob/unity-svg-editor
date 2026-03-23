using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;

namespace SvgEditor.Core.Svg.Geometry
{
    internal sealed class PathParseState
    {
        private const float HalfPi = Mathf.PI * 0.5f;
        private readonly List<BezierContour> _builtContours;
        private List<BezierSegment> _currentSegments;
        private Vector2 _currentPoint;
        private Vector2 _subpathStart;
        private Vector2 _lastCubicControl;
        private Vector2 _lastQuadraticControl;
        private bool _hasLastCubicControl;
        private bool _hasLastQuadraticControl;

        public PathParseState()
        {
            _builtContours = new List<BezierContour>();
            _currentSegments = null;
            _currentPoint = Vector2.zero;
            _subpathStart = Vector2.zero;
            _lastCubicControl = Vector2.zero;
            _lastQuadraticControl = Vector2.zero;
            _hasLastCubicControl = false;
            _hasLastQuadraticControl = false;
        }

        public BezierContour[] ToContours() => _builtContours.ToArray();

        public bool TryHandleCommand(char command, ref PathTokenReader reader)
        {
            switch (command)
            {
                case 'M':
                case 'm':
                    return TryHandleMoveCommand(command, ref reader);
                case 'L':
                case 'l':
                    return TryHandleLineCommand(command, ref reader);
                case 'H':
                case 'h':
                    return TryHandleHorizontalLineCommand(command, ref reader);
                case 'V':
                case 'v':
                    return TryHandleVerticalLineCommand(command, ref reader);
                case 'C':
                case 'c':
                    return TryHandleCubicCurveCommand(command, ref reader);
                case 'S':
                case 's':
                    return TryHandleSmoothCurveCommand(command, ref reader);
                case 'Q':
                case 'q':
                    return TryHandleQuadraticCurveCommand(command, ref reader);
                case 'T':
                case 't':
                    return TryHandleSmoothQuadraticCommand(command, ref reader);
                case 'A':
                case 'a':
                    return TryHandleArcCommand(command, ref reader);
                case 'Z':
                case 'z':
                    return TryHandleCloseCommand();
                default:
                    return false;
            }
        }

        public bool TryFinalizeContour(bool closed)
        {
            if (_currentSegments == null)
            {
                return true;
            }

            if (_currentSegments.Count == 0)
            {
                _currentPoint = _subpathStart;
                return true;
            }

            _builtContours.Add(new BezierContour
            {
                Segments = VectorUtils.BezierSegmentsToPath(_currentSegments.ToArray()),
                Closed = closed
            });
            _currentPoint = _subpathStart;
            return true;
        }

        private bool TryHandleMoveCommand(char command, ref PathTokenReader reader)
        {
            if (!reader.TryReadPoint(command == 'm', _currentPoint, out var movePoint) ||
                !TryFinalizeContour(closed: false))
            {
                return false;
            }

            _currentSegments = new List<BezierSegment>();
            _currentPoint = movePoint;
            _subpathStart = movePoint;
            ResetControls();

            var lineCommandIsRelative = command == 'm';
            while (reader.TryReadPoint(lineCommandIsRelative, _currentPoint, out var linePoint))
            {
                AddLineSegment(linePoint);
            }

            return true;
        }

        private bool TryHandleLineCommand(char command, ref PathTokenReader reader)
        {
            if (!TryEnsurePathStarted())
            {
                return false;
            }

            int startIndex = reader.Position;
            while (reader.TryReadPoint(command == 'l', _currentPoint, out var linePoint))
            {
                AddLineSegment(linePoint);
            }

            if (reader.Position == startIndex)
            {
                return false;
            }

            ResetControls();
            return true;
        }

        private bool TryHandleHorizontalLineCommand(char command, ref PathTokenReader reader)
        {
            if (!TryEnsurePathStarted())
            {
                return false;
            }

            int startIndex = reader.Position;
            while (reader.TryReadFloatToken(out var xValue))
            {
                var nextPoint = new Vector2(
                    command == 'h' ? _currentPoint.x + xValue : xValue,
                    _currentPoint.y);
                AddLineSegment(nextPoint);
            }

            if (reader.Position == startIndex)
            {
                return false;
            }

            ResetControls();
            return true;
        }

        private bool TryHandleVerticalLineCommand(char command, ref PathTokenReader reader)
        {
            if (!TryEnsurePathStarted())
            {
                return false;
            }

            int startIndex = reader.Position;
            while (reader.TryReadFloatToken(out var yValue))
            {
                var nextPoint = new Vector2(
                    _currentPoint.x,
                    command == 'v' ? _currentPoint.y + yValue : yValue);
                AddLineSegment(nextPoint);
            }

            if (reader.Position == startIndex)
            {
                return false;
            }

            ResetControls();
            return true;
        }

        private bool TryHandleCubicCurveCommand(char command, ref PathTokenReader reader)
        {
            if (!TryEnsurePathStarted())
            {
                return false;
            }

            int startIndex = reader.Position;
            while (reader.TryReadCurvePoints(command == 'c', _currentPoint, out var c1, out var c2, out var endPoint))
            {
                _currentSegments.Add(new BezierSegment
                {
                    P0 = _currentPoint,
                    P1 = c1,
                    P2 = c2,
                    P3 = endPoint
                });
                _currentPoint = endPoint;
                _lastCubicControl = c2;
                _hasLastCubicControl = true;
                _hasLastQuadraticControl = false;
            }

            if (reader.Position == startIndex)
            {
                return false;
            }

            return true;
        }

        private bool TryHandleSmoothCurveCommand(char command, ref PathTokenReader reader)
        {
            if (!TryEnsurePathStarted())
            {
                return false;
            }

            int startIndex = reader.Position;
            while (reader.TryReadSmoothCurvePoints(command == 's', _currentPoint, out var c2, out var endPoint))
            {
                var c1 = _hasLastCubicControl
                    ? (_currentPoint * 2f) - _lastCubicControl
                    : _currentPoint;

                _currentSegments.Add(new BezierSegment
                {
                    P0 = _currentPoint,
                    P1 = c1,
                    P2 = c2,
                    P3 = endPoint
                });
                _currentPoint = endPoint;
                _lastCubicControl = c2;
                _hasLastCubicControl = true;
                _hasLastQuadraticControl = false;
            }

            if (reader.Position == startIndex)
            {
                return false;
            }

            return true;
        }

        private bool TryHandleQuadraticCurveCommand(char command, ref PathTokenReader reader)
        {
            if (!TryEnsurePathStarted())
            {
                return false;
            }

            int startIndex = reader.Position;
            while (reader.TryReadQuadraticPoints(command == 'q', _currentPoint, out var controlPoint, out var endPoint))
            {
                _currentSegments.Add(VectorUtils.QuadraticToCubic(_currentPoint, controlPoint, endPoint));
                _currentPoint = endPoint;
                _lastQuadraticControl = controlPoint;
                _hasLastQuadraticControl = true;
                _hasLastCubicControl = false;
            }

            if (reader.Position == startIndex)
            {
                return false;
            }

            return true;
        }

        private bool TryHandleSmoothQuadraticCommand(char command, ref PathTokenReader reader)
        {
            if (!TryEnsurePathStarted())
            {
                return false;
            }

            int startIndex = reader.Position;
            while (reader.TryReadPoint(command == 't', _currentPoint, out var endPoint))
            {
                var controlPoint = _hasLastQuadraticControl
                    ? (_currentPoint * 2f) - _lastQuadraticControl
                    : _currentPoint;
                _currentSegments.Add(VectorUtils.QuadraticToCubic(_currentPoint, controlPoint, endPoint));
                _currentPoint = endPoint;
                _lastQuadraticControl = controlPoint;
                _hasLastQuadraticControl = true;
                _hasLastCubicControl = false;
            }

            if (reader.Position == startIndex)
            {
                return false;
            }

            return true;
        }

        private bool TryHandleArcCommand(char command, ref PathTokenReader reader)
        {
            if (!TryEnsurePathStarted())
            {
                return false;
            }

            int startIndex = reader.Position;
            while (reader.TryReadArcArguments(
                       command == 'a',
                       _currentPoint,
                       out float rx,
                       out float ry,
                       out float xAxisRotation,
                       out bool largeArc,
                       out bool sweep,
                       out Vector2 endPoint))
            {
                AppendArcSegments(_currentPoint, endPoint, rx, ry, xAxisRotation, largeArc, sweep);
                _currentPoint = endPoint;
                _hasLastQuadraticControl = false;
            }

            if (reader.Position == startIndex)
            {
                return false;
            }

            return true;
        }

        private bool TryHandleCloseCommand()
        {
            if (!TryEnsurePathStarted())
            {
                return false;
            }

            if ((_currentPoint - _subpathStart).sqrMagnitude > Mathf.Epsilon)
            {
                AddLineSegment(_subpathStart);
            }

            if (!TryFinalizeContour(closed: true))
            {
                return false;
            }

            _currentSegments = null;
            _currentPoint = _subpathStart;
            ResetControls();
            return true;
        }

        private bool TryEnsurePathStarted()
        {
            if (_currentSegments == null)
            {
                return false;
            }

            if (_currentSegments.Count == 0)
            {
                _currentPoint = _subpathStart;
            }

            return true;
        }

        private void AddLineSegment(Vector2 nextPoint)
        {
            _currentSegments.Add(VectorUtils.MakeLine(_currentPoint, nextPoint));
            _currentPoint = nextPoint;
        }

        private void ResetControls()
        {
            _hasLastCubicControl = false;
            _hasLastQuadraticControl = false;
        }

        private void AppendArcSegments(
            Vector2 startPoint,
            Vector2 endPoint,
            float rx,
            float ry,
            float xAxisRotation,
            bool largeArc,
            bool sweep)
        {
            if ((endPoint - startPoint).sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            rx = Mathf.Abs(rx);
            ry = Mathf.Abs(ry);
            if (rx <= Mathf.Epsilon || ry <= Mathf.Epsilon)
            {
                AddLineSegment(endPoint);
                return;
            }

            float phi = xAxisRotation * Mathf.Deg2Rad;
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
            float factor = denominator <= Mathf.Epsilon
                ? 0f
                : Mathf.Sqrt(Mathf.Max(0f, numerator / denominator));
            if (largeArc == sweep)
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
            if (!sweep && deltaAngle > 0f)
            {
                deltaAngle -= Mathf.PI * 2f;
            }
            else if (sweep && deltaAngle < 0f)
            {
                deltaAngle += Mathf.PI * 2f;
            }

            int segmentCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(deltaAngle) / HalfPi));
            float step = deltaAngle / segmentCount;
            Vector2 segmentStart = startPoint;
            for (int index = 0; index < segmentCount; index++)
            {
                float angle0 = startAngle + (index * step);
                float angle1 = angle0 + step;
                AppendArcSegment(segmentStart, cx, cy, rx, ry, cosPhi, sinPhi, angle0, angle1, index == segmentCount - 1 ? endPoint : (Vector2?)null);
                segmentStart = _currentSegments[^1].P3;
            }
        }

        private void AppendArcSegment(
            Vector2 segmentStart,
            float cx,
            float cy,
            float rx,
            float ry,
            float cosPhi,
            float sinPhi,
            float angle0,
            float angle1,
            Vector2? forcedEndPoint)
        {
            float alpha = (4f / 3f) * Mathf.Tan((angle1 - angle0) * 0.25f);
            Vector2 point0 = new(Mathf.Cos(angle0), Mathf.Sin(angle0));
            Vector2 point1 = new(Mathf.Cos(angle1), Mathf.Sin(angle1));
            Vector2 control0 = new(point0.x - (alpha * point0.y), point0.y + (alpha * point0.x));
            Vector2 control1 = new(point1.x + (alpha * point1.y), point1.y - (alpha * point1.x));

            BezierSegment segment = new()
            {
                P0 = segmentStart,
                P1 = TransformArcPoint(control0, cx, cy, rx, ry, cosPhi, sinPhi),
                P2 = TransformArcPoint(control1, cx, cy, rx, ry, cosPhi, sinPhi),
                P3 = forcedEndPoint ?? TransformArcPoint(point1, cx, cy, rx, ry, cosPhi, sinPhi)
            };

            _currentSegments.Add(segment);
            _lastCubicControl = segment.P2;
            _hasLastCubicControl = true;
        }

        private static Vector2 TransformArcPoint(Vector2 point, float cx, float cy, float rx, float ry, float cosPhi, float sinPhi)
        {
            float x = rx * point.x;
            float y = ry * point.y;
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
    }
}
