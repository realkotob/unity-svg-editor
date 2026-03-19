using System.Reflection;
using NUnit.Framework;
using SvgEditor.Core.Preview.Rendering;

namespace SvgEditor.Tests.Editor
{
    public sealed class PreviewRenderingRefactorTests
    {
        [Test]
        public void SvgShapeBuilder_ExposesRenderBuildContext()
        {
            System.Type contextType = typeof(SvgShapeBuilder).GetNestedType(
                "RenderBuildContext",
                BindingFlags.NonPublic);

            Assert.That(contextType, Is.Not.Null);
        }
    }
}
