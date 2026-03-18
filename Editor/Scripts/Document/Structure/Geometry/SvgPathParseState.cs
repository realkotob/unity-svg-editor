using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using Core.UI.Extensions;

namespace SvgEditor.Document.Structure.Geometry
{
    internal sealed class SvgPathParseState
    {
        private readonly List<BezierContour> _builtContours;
        private List<BezierSegment> _currentSegments;
        private Vector2 _currentPoint;
        private Vector2 _subpathStart;
        private Vector2 _lastCubicControl;
        private Vector2 _lastQuadraticControl;
        private bool _hasLastCubicControl;
        private bool _hasLastQuadraticControl;

        public SvgPathParseState()
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

        public bool TryHandleCommand(char command, ref SvgPathTokenReader reader)
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

        private bool TryHandleMoveCommand(char command, ref SvgPathTokenReader reader)
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

        private bool TryHandleLineCommand(char command, ref SvgPathTokenReader reader)
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

        private bool TryHandleHorizontalLineCommand(char command, ref SvgPathTokenReader reader)
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

        private bool TryHandleVerticalLineCommand(char command, ref SvgPathTokenReader reader)
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

        private bool TryHandleCubicCurveCommand(char command, ref SvgPathTokenReader reader)
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

        private bool TryHandleSmoothCurveCommand(char command, ref SvgPathTokenReader reader)
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

        private bool TryHandleQuadraticCurveCommand(char command, ref SvgPathTokenReader reader)
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

        private bool TryHandleSmoothQuadraticCommand(char command, ref SvgPathTokenReader reader)
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
    }
}
