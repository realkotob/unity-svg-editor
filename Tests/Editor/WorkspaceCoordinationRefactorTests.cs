using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SvgEditor.Core.Shared;
using SvgEditor.UI.Workspace.Coordination;
using SvgEditor.UI.Workspace.Document;

namespace SvgEditor.Tests.Editor
{
    public sealed class WorkspaceCoordinationRefactorTests
    {
        [Test]
        public void WorkspaceCoordination_DoesNotExposeWorkspaceViewBinder()
        {
            System.Type binderType = typeof(EditorWorkspaceCoordinator).Assembly.GetType(
                "SvgEditor.UI.Workspace.Coordination.WorkspaceViewBinder");

            Assert.That(binderType, Is.Null);
        }

        [Test]
        public void SelectionCoordinator_DoesNotDependOnWorkspaceViewBinderConstructorParameter()
        {
            ConstructorInfo constructor = typeof(SelectionCoordinator).GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();

            bool hasWorkspaceViewBinderParameter = constructor
                .GetParameters()
                .Any(parameter => parameter.ParameterType.Name == "WorkspaceViewBinder");

            Assert.That(hasWorkspaceViewBinderParameter, Is.False);
        }

        [Test]
        public void LifecycleCommandService_ExposesNamedDocumentLoadHandler()
        {
            MethodInfo loadHandler = typeof(LifecycleCommandService).GetMethod(
                "HandleLoadedDocument",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(loadHandler, Is.Not.Null);
        }

        [Test]
        public void LifecycleCommandService_DoesNotExposePrivateApplyUpdatedSourceOverload()
        {
            MethodInfo privateApplyUpdatedSource = typeof(LifecycleCommandService).GetMethod(
                "ApplyUpdatedSource",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(bool),
                    typeof(HistoryRecordingMode)
                },
                null);

            Assert.That(privateApplyUpdatedSource, Is.Null);
        }

        [Test]
        public void ViewportFrameState_ExposesLayoutSettingsType()
        {
            System.Type layoutSettingsType = typeof(ViewportFrameState).Assembly.GetType(
                "SvgEditor.Core.Shared.ViewportFrameLayoutSettings");

            Assert.That(layoutSettingsType, Is.Not.Null);
        }

        [Test]
        public void ViewportFrameState_DoesNotExposeLegacyHighArityLayoutEntryPoints()
        {
            MethodInfo resetToFit = typeof(ViewportFrameState).GetMethod(
                "ResetToFit",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(UnityEngine.Rect), typeof(UnityEngine.Rect), typeof(float), typeof(float), typeof(float) },
                null);
            MethodInfo ensureFrame = typeof(ViewportFrameState).GetMethod(
                "EnsureFrame",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(UnityEngine.Rect), typeof(UnityEngine.Rect), typeof(float), typeof(float), typeof(float) },
                null);

            Assert.That(resetToFit, Is.Null);
            Assert.That(ensureFrame, Is.Null);
        }
    }
}
