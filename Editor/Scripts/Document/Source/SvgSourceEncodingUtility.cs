using System;
using System.IO;
using System.Text;
using Core.UI.Extensions;

namespace SvgEditor.Document
{
    internal static class SvgSourceEncodingUtility
    {
        private static readonly UTF8Encoding UTF8_NO_BOM = new(false);
        private static readonly byte[] UTF8_BOM = { 0xEF, 0xBB, 0xBF };

        public static bool TryReadAllText(string absolutePath, out string sourceText, out Encoding encoding, out string error)
        {
            sourceText = string.Empty;
            encoding = UTF8_NO_BOM;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                error = "SVG file path is empty.";
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(absolutePath);
                using var stream = new MemoryStream(bytes, writable: false);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                sourceText = reader.ReadToEnd();

                Encoding detectedEncoding = reader.CurrentEncoding ?? UTF8_NO_BOM;
                encoding = IsUtf8WithoutBom(bytes, detectedEncoding)
                    ? UTF8_NO_BOM
                    : detectedEncoding;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to read SVG source: {ex.Message}";
                return false;
            }
        }

        public static bool TryWriteAllText(string absolutePath, string sourceText, Encoding encoding, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                error = "SVG file path is empty.";
                return false;
            }

            try
            {
                File.WriteAllText(absolutePath, sourceText ?? string.Empty, encoding ?? UTF8_NO_BOM);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
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
}
