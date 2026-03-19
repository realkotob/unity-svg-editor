using System.Reflection;
using System.Linq;
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

        [Test]
        public void Inspector_UsesStateOwnedAttributeActionType()
        {
            System.Type attributeActionType = typeof(PanelView).Assembly.GetType("SvgEditor.UI.Inspector.State.AttributeAction");
            EventInfo attributeActionRequested = typeof(PanelView).GetEvent(
                "AttributeActionRequested",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(attributeActionType, Is.Not.Null);
            Assert.That(attributeActionRequested, Is.Not.Null);

            System.Type eventArgumentType = attributeActionRequested.EventHandlerType
                .GetGenericArguments()
                .Single();

            Assert.That(eventArgumentType, Is.EqualTo(attributeActionType));
        }

        [Test]
        public void Inspector_UsesStateOwnedImmediateApplyFieldType()
        {
            System.Type immediateApplyFieldType = typeof(PanelView).Assembly.GetType("SvgEditor.UI.Inspector.State.ImmediateApplyField");
            EventInfo immediateApplyRequested = typeof(PanelView).GetEvent(
                "ImmediateApplyRequested",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(immediateApplyFieldType, Is.Not.Null);
            Assert.That(immediateApplyRequested, Is.Not.Null);

            System.Type eventArgumentType = immediateApplyRequested.EventHandlerType
                .GetGenericArguments()
                .Single();

            Assert.That(eventArgumentType, Is.EqualTo(immediateApplyFieldType));
        }

        [Test]
        public void TransformPositionActionService_ExposesUnifiedSelectionTransformSourceBuilder()
        {
            MethodInfo unifiedBuilder = typeof(TransformPositionActionService).GetMethod(
                "TryBuildSelectionTransformSource",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(unifiedBuilder, Is.Not.Null);
        }

        [Test]
        public void TransformPositionActionService_DoesNotExposeSeparateRotationAndScaleSourceBuilders()
        {
            MethodInfo rotationBuilder = typeof(TransformPositionActionService).GetMethod(
                "TryBuildMultiRotationSource",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo scaleBuilder = typeof(TransformPositionActionService).GetMethod(
                "TryBuildMultiScaleSource",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(rotationBuilder, Is.Null);
            Assert.That(scaleBuilder, Is.Null);
        }
    }
}
