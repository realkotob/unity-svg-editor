using System;
using System.IO;
using System.Text;
using Core.UI.Extensions;
using SvgEditor.Core.Shared;

namespace SvgEditor.Core.Svg.Source
{
    internal static class SourceEncoding
    {
        private static readonly UTF8Encoding UTF8_NO_BOM = new(false);
        private static readonly byte[] UTF8_BOM = { 0xEF, 0xBB, 0xBF };

        public static Result<ReadSourceText> ReadAllText(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return Result.Failure<ReadSourceText>("SVG file path is empty.");
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(absolutePath);
                using var stream = new MemoryStream(bytes, writable: false);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string sourceText = reader.ReadToEnd();

                Encoding detectedEncoding = reader.CurrentEncoding ?? UTF8_NO_BOM;
                Encoding encoding = IsUtf8WithoutBom(bytes, detectedEncoding)
                    ? UTF8_NO_BOM
                    : detectedEncoding;
                return Result.Success(new ReadSourceText(sourceText, encoding));
            }
            catch (Exception ex)
            {
                return Result.Failure<ReadSourceText>($"Failed to read SVG source: {ex.Message}");
            }
        }

        public static bool TryReadAllText(string absolutePath, out string sourceText, out Encoding encoding, out string error)
        {
            Result<ReadSourceText> result = ReadAllText(absolutePath);
            sourceText = result.IsSuccess ? result.Value.SourceText : string.Empty;
            encoding = result.IsSuccess ? result.Value.Encoding : UTF8_NO_BOM;
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }

        public static Result<Unit> WriteAllText(string absolutePath, string sourceText, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return Result.Failure<Unit>("SVG file path is empty.");
            }

            try
            {
                File.WriteAllText(absolutePath, sourceText ?? string.Empty, encoding ?? UTF8_NO_BOM);
                return Result.Success(Unit.Default);
            }
            catch (Exception ex)
            {
                return Result.Failure<Unit>(ex.Message);
            }
        }

        public static bool TryWriteAllText(string absolutePath, string sourceText, Encoding encoding, out string error)
        {
            Result<Unit> result = WriteAllText(absolutePath, sourceText, encoding);
            error = result.Error ?? string.Empty;
            return result.IsSuccess;
        }

        private static bool IsUtf8WithoutBom(byte[] bytes, Encoding encoding)
        {
            return encoding is UTF8Encoding && !StartsWith(bytes, UTF8_BOM);
        }

        private static bool StartsWith(byte[] bytes, byte[] prefix)
        {
            if (bytes == null || prefix == null || bytes.Length < prefix.Length)
            {
                return false;
            }

            for (int index = 0; index < prefix.Length; index++)
            {
                if (bytes[index] != prefix[index])
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal readonly struct ReadSourceText
    {
        public ReadSourceText(string sourceText, Encoding encoding)
        {
            SourceText = sourceText ?? string.Empty;
            Encoding = encoding;
        }

        public string SourceText { get; }
        public Encoding Encoding { get; }
    }
}
