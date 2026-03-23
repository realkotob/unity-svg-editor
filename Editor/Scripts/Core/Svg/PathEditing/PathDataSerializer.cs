using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using SvgEditor.Core.Shared;

namespace SvgEditor.Core.Svg.PathEditing
{
    internal static class PathDataSerializer
    {
        public static string Serialize(PathData pathData)
        {
            Result<string> result = SerializeResult(pathData);
            if (result.IsFailure)
            {
                throw new InvalidOperationException(result.Error);
            }

            return result.Value;
        }

        public static bool TrySerialize(PathData pathData, out string pathText, out string error)
        {
            Result<string> result = SerializeResult(pathData);
            pathText = result.GetValueOrDefault(string.Empty);
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }

        public static Result<string> SerializeResult(PathData pathData)
        {
            Result<PathData> validation = Validate(pathData);
            if (validation.IsFailure)
            {
                return Result.Failure<string>(validation.Error);
            }

            StringBuilder builder = new();
            bool hasWrittenSubpath = false;

            foreach (PathSubpath subpath in validation.Value.Subpaths)
            {
                if (subpath == null)
                {
                    return Result.Failure<string>("Path data contains a null subpath.");
                }

                if (hasWrittenSubpath)
                {
                    builder.Append(' ');
                }

                AppendCommand(builder, 'M');
                AppendPair(builder, subpath.Start);

                foreach (PathNode node in subpath.Nodes)
                {
                    AppendCommand(builder, char.ToUpperInvariant(node.Command));
                    switch (char.ToUpperInvariant(node.Command))
                    {
                        case 'L':
                            AppendPair(builder, node.Position);
                            break;
                        case 'H':
                            AppendValue(builder, node.Position.x);
                            break;
                        case 'V':
                            AppendValue(builder, node.Position.y);
                            break;
                        case 'C':
                            AppendPair(builder, node.Control0);
                            AppendPair(builder, node.Control1);
                            AppendPair(builder, node.Position);
                            break;
                        case 'S':
                            AppendPair(builder, node.Control1);
                            AppendPair(builder, node.Position);
                            break;
                        case 'Q':
                            AppendPair(builder, node.Control0);
                            AppendPair(builder, node.Position);
                            break;
                        case 'T':
                            AppendPair(builder, node.Position);
                            break;
                        default:
                            return Result.Failure<string>($"Path data contains unsupported command '{node.Command}'.");
                    }
                }

                if (subpath.IsClosed)
                {
                    AppendCommand(builder, 'Z');
                }

                hasWrittenSubpath = true;
            }

            return Result.Success(builder.ToString());
        }

        private static Result<PathData> Validate(PathData pathData)
        {
            if (pathData == null)
            {
                return Result.Failure<PathData>("Path data is unavailable.");
            }

            if (pathData.IsMalformed)
            {
                return Result.Failure<PathData>($"Path data is malformed. {pathData.ParseError}".Trim());
            }

            if (pathData.HasUnsupportedCommands)
            {
                return Result.Failure<PathData>($"Path data contains unsupported commands: {string.Join(", ", pathData.UnsupportedCommands)}.");
            }

            return Result.Success(pathData);
        }

        private static void AppendCommand(StringBuilder builder, char command)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(command);
        }

        private static void AppendPair(StringBuilder builder, Vector2 value)
        {
            builder.Append(' ');
            builder.Append(FormatFloat(value.x));
            builder.Append(' ');
            builder.Append(FormatFloat(value.y));
        }

        private static void AppendValue(StringBuilder builder, float value)
        {
            builder.Append(' ');
            builder.Append(FormatFloat(value));
        }

        private static string FormatFloat(float value)
        {
            if (Mathf.Abs(value) < 0.0000005f)
            {
                value = 0f;
            }

            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
