using System.Reflection;
using NUnit.Framework;
using SvgEditor.Core.Preview.Rendering;
using SvgEditor.Renderer;

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

        [Test]
        public void SvgReferenceSceneBuilder_ExposesUnifiedReferenceClipperAttachment()
        {
            MethodInfo unifiedAttachMethod = typeof(SvgReferenceSceneBuilder).GetMethod(
                "TryAttachReferenceClipper",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[]
                {
                    typeof(SvgEditor.Core.Svg.Model.SvgDocumentModel),
                    typeof(System.Collections.Generic.IReadOnlyDictionary<string, SvgEditor.Core.Svg.Model.SvgNodeModel>),
                    typeof(SvgEditor.Core.Svg.Model.SvgNodeModel),
                    typeof(Unity.VectorGraphics.SceneNode),
                    typeof(string).MakeByRefType()
                },
                null);

            Assert.That(unifiedAttachMethod, Is.Not.Null);
        }

        [Test]
        public void SvgReferenceSceneBuilder_DoesNotExposeSeparateMaskAndClipAttachmentEntryPoints()
        {
            MethodInfo attachMaskMethod = typeof(SvgReferenceSceneBuilder).GetMethod(
                "TryAttachMask",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo attachClipperMethod = typeof(SvgReferenceSceneBuilder).GetMethod(
                "TryAttachClipper",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(attachMaskMethod, Is.Null);
            Assert.That(attachClipperMethod, Is.Null);
        }
    }
}
