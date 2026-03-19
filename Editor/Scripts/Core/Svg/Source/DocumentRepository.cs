using System;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SvgEditor.Core.Svg.Structure.Xml;
using SvgEditor.Core.Shared;
using Core.UI.Extensions;

namespace SvgEditor.Core.Svg.Source
{
    internal sealed class DocumentRepository
    {
        private readonly AssetPathResolver _assetPathResolver = new();
        private readonly DocumentSourceService _documentSourceService = new();

        #region Public Methods

        public IReadOnlyList<string> FindVectorImageAssetPaths(string searchRoot = null)
        {
            return _assetPathResolver.FindEditableSvgAssetPaths(searchRoot);
        }

        public bool TryLoad(string assetPath, out DocumentSession document, out string error)
        {
            document = null;
            Result<LoadOperation> load = ResolveLoad(assetPath);
            error = load.Error ?? string.Empty;
            if (load.IsFailure)
            {
                return false;
            }

            document = CreateSession(load.Value);
            return true;
        }

        public bool Save(DocumentSession document, out string error)
        {
            Result<Unit> save = ResolveSave(document)
                .Bind(operation => WriteDocument(operation, document));
            error = save.Error ?? string.Empty;
            return save.IsSuccess;
        }

        public void RefreshDocumentModel(DocumentSession document)
        {
            _documentSourceService.RefreshDocumentModel(document);
        }

        public bool ValidateXml(string sourceText, out string error)
        {
            return _documentSourceService.TryValidateXml(sourceText, out error);
        }

        internal bool TryResolveSourceTextToPersist(DocumentSession document, out string sourceText, out string error)
        {
            return _documentSourceService.TryResolvePersistedSource(document, out sourceText, out error);
        }

        #endregion Public Methods

        private Result<DocumentPath> ResolveDocumentPath(string assetPath)
        {
            string normalizedAssetPath = assetPath?.Replace('\\', '/').Trim();
            if (string.IsNullOrWhiteSpace(normalizedAssetPath))
            {
                return Result.Failure<DocumentPath>("SVG asset path is empty.");
            }

            return _assetPathResolver.ResolveAbsolutePath(normalizedAssetPath)
                .Map(absolutePath => new DocumentPath(normalizedAssetPath, absolutePath));
        }

        private Result<LoadOperation> ResolveLoad(string assetPath)
        {
            return ResolveDocumentPath(assetPath)
                .Bind(path => ReadDocumentSource(path)
                    .Map(source => new LoadOperation(path, source.SourceText, source.Encoding)));
        }

        private Result<SaveOperation> ResolveSave(DocumentSession document)
        {
            if (document == null)
            {
                return Result.Failure<SaveOperation>("Document is null.");
            }

            return _documentSourceService.ResolvePersistedSource(document)
                .Bind(sourceText => ResolveDocumentPath(document.AssetPath)
                    .Map(path => new SaveOperation(path, sourceText)));
        }

        private Result<Unit> WriteDocument(SaveOperation operation, DocumentSession document)
        {
            Result<Unit> write = SourceEncoding.WriteAllText(
                operation.Path.AbsolutePath,
                operation.SourceText,
                document.SourceEncoding);
            if (write.IsFailure)
            {
                return write;
            }

            try
            {
                AssetDatabase.ImportAsset(operation.Path.AssetPath, ImportAssetOptions.ForceUpdate);
                document.AssetPath = operation.Path.AssetPath;
                document.AbsolutePath = operation.Path.AbsolutePath;
                document.WorkingSourceText = operation.SourceText;
                document.OriginalSourceText = operation.SourceText;
                document.VectorImageAsset = LoadVectorImageAsset(operation.Path.AssetPath);
                _documentSourceService.RefreshDocumentModelSnapshot(document, operation.SourceText);
                return Result.Success(Unit.Default);
            }
            catch (Exception ex)
            {
                return Result.Failure<Unit>($"SVG save failed: {ex.Message}");
            }
        }

        private static Result<ReadSourceText> ReadDocumentSource(DocumentPath path)
        {
            if (!File.Exists(path.AbsolutePath))
            {
                return Result.Failure<ReadSourceText>($"SVG file does not exist: {path.AbsolutePath}");
            }

            Result<ReadSourceText> source = SourceEncoding.ReadAllText(path.AbsolutePath);
            if (source.IsFailure)
            {
                return source;
            }

            if (!MaskArtifactSanitizer.TrySanitize(source.Value.SourceText, out string sanitizedSource, out _, out string error))
            {
                return Result.Failure<ReadSourceText>(error);
            }

            return Result.Success(new ReadSourceText(sanitizedSource, source.Value.Encoding));
        }

        private static VectorImage LoadVectorImageAsset(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
        }

        private DocumentSession CreateSession(LoadOperation operation)
        {
            DocumentSession document = new()
            {
                AssetPath = operation.Path.AssetPath,
                AbsolutePath = operation.Path.AbsolutePath,
                SourceEncoding = operation.SourceEncoding,
                VectorImageAsset = LoadVectorImageAsset(operation.Path.AssetPath),
                OriginalSourceText = operation.SourceText,
                WorkingSourceText = operation.SourceText
            };
            _documentSourceService.RefreshDocumentModelSnapshot(document, operation.SourceText);
            return document;
        }

        private readonly struct DocumentPath
        {
            public DocumentPath(string assetPath, string absolutePath)
            {
                AssetPath = assetPath;
                AbsolutePath = absolutePath;
            }

            public string AssetPath { get; }
            public string AbsolutePath { get; }
        }

        private readonly struct LoadOperation
        {
            public LoadOperation(DocumentPath path, string sourceText, System.Text.Encoding sourceEncoding)
            {
                Path = path;
                SourceText = sourceText;
                SourceEncoding = sourceEncoding;
            }

            public DocumentPath Path { get; }
            public string SourceText { get; }
            public System.Text.Encoding SourceEncoding { get; }
        }

        private readonly struct SaveOperation
        {
            public SaveOperation(DocumentPath path, string sourceText)
            {
                Path = path;
                SourceText = sourceText;
            }

            public DocumentPath Path { get; }
            public string SourceText { get; }
        }
    }
}
