using System.Reflection;
using NUnit.Framework;
using SvgEditor.UI.Inspector;

namespace SvgEditor.Tests.Editor
{
    public sealed class InspectorStateViewRefactorTests
    {
        [Test]
        public void Inspector_DoesNotExposeStateBinderType()
        {
            System.Type stateBinderType = typeof(PanelView).Assembly.GetType("SvgEditor.UI.Inspector.StateBinder");

            Assert.That(stateBinderType, Is.Null);
        }
    }
}
