using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SvgEditor.UI.Workspace.Coordination;

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
    }
}
