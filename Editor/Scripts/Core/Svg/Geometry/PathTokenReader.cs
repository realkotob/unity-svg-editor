using UnityEngine;
using SvgEditor.Core.Svg.Structure.Lookup;

namespace SvgEditor.Core.Svg.Geometry
{
    internal struct PathTokenReader
    {
        private readonly string _text;
        private int _index;

        public PathTokenReader(string text)
        {
            _text = text;
            _index = 0;
        }

        public bool IsComplete
        {
            get
            {
                SkipSeparators();
                return _index >= _text.Length;
            }
        }

        public int Position => _index;

        public bool TryReadCommand(ref char currentCommand, out char command)
        {
            command = '\0';
            SkipSeparators();
            if (_index >= _text.Length)
            {
                return false;
            }

            var token = _text[_index];
            if (char.IsLetter(token))
            {
                command = token;
                currentCommand = token;
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
            if (!TryReadFloatToken(out var x) || !TryReadFloatToken(out var y))
            {
                return false;
            }

            point = relative ? origin + new Vector2(x, y) : new Vector2(x, y);
            return true;
        }

        public bool TryReadCurvePoints(
            bool relative,
            Vector2 origin,
            out Vector2 c1,
            out Vector2 c2,
            out Vector2 endPoint)
        {
            c1 = Vector2.zero;
            c2 = Vector2.zero;
            endPoint = Vector2.zero;

            return TryReadPoint(relative, origin, out c1) &&
                   TryReadPoint(relative, origin, out c2) &&
                   TryReadPoint(relative, origin, out endPoint);
        }

        public bool TryReadSmoothCurvePoints(
            bool relative,
            Vector2 origin,
            out Vector2 c2,
            out Vector2 endPoint)
        {
            c2 = Vector2.zero;
            endPoint = Vector2.zero;

            return TryReadPoint(relative, origin, out c2) &&
                   TryReadPoint(relative, origin, out endPoint);
        }

        public bool TryReadQuadraticPoints(
            bool relative,
            Vector2 origin,
            out Vector2 controlPoint,
            out Vector2 endPoint)
        {
            controlPoint = Vector2.zero;
            endPoint = Vector2.zero;

            return TryReadPoint(relative, origin, out controlPoint) &&
                   TryReadPoint(relative, origin, out endPoint);
        }

        public bool TryReadArcArguments(
            bool relative,
            Vector2 origin,
            out float rx,
            out float ry,
            out float xAxisRotation,
            out bool largeArc,
            out bool sweep,
            out Vector2 endPoint)
        {
            rx = 0f;
            ry = 0f;
            xAxisRotation = 0f;
            largeArc = false;
            sweep = false;
            endPoint = Vector2.zero;

            return TryReadFloatToken(out rx) &&
                   TryReadFloatToken(out ry) &&
                   TryReadFloatToken(out xAxisRotation) &&
                   TryReadFlag(out largeArc) &&
                   TryReadFlag(out sweep) &&
                   TryReadPoint(relative, origin, out endPoint);
        }

        public bool TryReadFloatToken(out float value)
        {
            value = 0f;
            SkipSeparators();
            if (_index >= _text.Length)
            {
                return false;
            }

            var start = _index;
            var hasExponent = false;
            var hasDecimal = false;

            if (_text[_index] == '+' || _text[_index] == '-')
            {
                _index++;
            }

            while (_index < _text.Length)
            {
                var ch = _text[_index];
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
            while (_index < _text.Length &&
                   (char.IsWhiteSpace(_text[_index]) || _text[_index] == ','))
            {
                _index++;
            }
        }
    }
}
