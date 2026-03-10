using NUnit.Framework;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class EditorWorkspaceCoordinatorMutationTests
    {
        [Test]
        public void TryApplyPatchRequest_AppliesStyleMutationAndUpdatesSource()
        {
            string sourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" fill=\"#ffffff\" /></svg>";
            FakeEditorWorkspaceHost host = new(sourceText);
            EditorWorkspaceCoordinator coordinator = new(host);

            bool success = coordinator.TryApplyPatchRequest(
                new AttributePatchRequest
                {
                    TargetKey = "node",
                    Fill = "#123456",
                    FillOpacity = "0.25"
                },
                "Inspector changes applied.");

            Assert.That(success, Is.True);
            Assert.That(host.ApplyUpdatedSourceCallCount, Is.EqualTo(1));
            Assert.That(host.LastUpdatedSource, Does.Contain("fill=\"#123456\""));
            Assert.That(host.LastUpdatedSource, Does.Contain("fill-opacity=\"0.25\""));
        }

        [Test]
        public void TryApplyTargetFrameRect_AppliesTransformMutationAndUpdatesSource()
        {
            string sourceText = "<svg xmlns=\"http://www.w3.org/2000/svg\"><rect id=\"node\" width=\"10\" height=\"10\" /></svg>";
            FakeEditorWorkspaceHost host = new(sourceText)
            {
                PreviewSnapshot = new PreviewSnapshot
                {
                    Elements = new[]
                    {
                        new PreviewElementGeometry
                        {
                            Key = "node",
                            TargetKey = "node",
                            VisualBounds = new Rect(0f, 0f, 10f, 10f),
                            ParentWorldTransform = Matrix2D.identity
                        }
                    }
                }
            };
            EditorWorkspaceCoordinator coordinator = new(host);

            bool success = coordinator.TryApplyTargetFrameRect(
                "node",
                new Rect(5f, 6f, 20f, 10f),
                "Frame rect updated.");

            Assert.That(success, Is.True);
            Assert.That(host.ApplyUpdatedSourceCallCount, Is.EqualTo(1));
            Assert.That(host.LastUpdatedSource, Does.Contain("scale(2 1)"));
            Assert.That(host.LastUpdatedSource, Does.Contain("translate(5 6)"));
        }

        private sealed class FakeEditorWorkspaceHost : IEditorWorkspaceHost
        {
            public FakeEditorWorkspaceHost(string sourceText)
            {
                CurrentDocument = new DocumentSession
                {
                    WorkingSourceText = sourceText,
                    DocumentModel = SvgFixtureTestUtility.LoadModel(sourceText)
                };
                AttributePatcher = new AttributePatcher();
                RootVisualElement = new VisualElement();
            }

            public VisualElement RootVisualElement { get; }
            public DocumentSession CurrentDocument { get; }
            public PreviewSnapshot PreviewSnapshot { get; set; } = new();
            public Image PreviewImage { get; } = new();
            public AttributePatcher AttributePatcher { get; }
            public int ApplyUpdatedSourceCallCount { get; private set; }
            public string LastUpdatedSource { get; private set; } = string.Empty;

            public void ApplyUpdatedSource(string updatedSource, string successStatus)
            {
                ApplyUpdatedSourceCallCount++;
                LastUpdatedSource = updatedSource ?? string.Empty;
            }

            public void UpdateSourceStatus(string status)
            {
            }

            public void UpdateEditorInteractivity()
            {
            }

            public void RefreshLivePreview(bool keepExistingPreviewOnFailure)
            {
            }

            public bool TryRefreshTransientPreview(string sourceText)
            {
                return true;
            }

            public void RefreshInspectorFromSource(string sourceText)
            {
            }

            public bool TrySelectPatchTargetByKey(string targetKey)
            {
                return true;
            }

            public string ResolveSelectedPatchTargetKey()
            {
                return AttributePatcher.ROOT_TARGET_KEY;
            }

            public string FormatNumber(float value)
            {
                return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
