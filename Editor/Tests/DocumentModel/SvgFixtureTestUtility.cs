using System.IO;
using UnityEngine;

namespace UnitySvgEditor.Editor.Tests
{
    internal static class SvgFixtureTestUtility
    {
        private const string FixtureAssetRoot = "Assets/unity-svg-editor/Editor/Tests/Fixtures";
        private static readonly SvgDocumentModelLoader _loader = new();
        private static readonly SvgDocumentModelSerializer _serializer = new();

        public static string LoadFixtureSource(string fileName)
        {
            string assetPath = $"{FixtureAssetRoot}/{fileName}";
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string absolutePath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            return File.ReadAllText(absolutePath);
        }

        public static SvgDocumentModel LoadFixtureModel(string fileName)
        {
            return LoadModel(LoadFixtureSource(fileName));
        }

        public static SvgDocumentModel LoadModel(string sourceText)
        {
            if (!_loader.TryLoad(sourceText, out SvgDocumentModel documentModel, out string error))
                throw new InvalidDataException(error);

            return documentModel;
        }

        public static string SerializeModel(SvgDocumentModel documentModel)
        {
            if (!_serializer.TrySerialize(documentModel, out string sourceText, out string error))
                throw new InvalidDataException(error);

            return sourceText;
        }
    }
}
