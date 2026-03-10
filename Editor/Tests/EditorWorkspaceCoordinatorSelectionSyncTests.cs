using System.Collections.Generic;
using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class EditorWorkspaceCoordinatorSelectionSyncTests
    {
        [Test]
        public void TryResolveSelection_PrefersExistingElementKey()
        {
            var elements = new List<StructureNode>
            {
                new()
                {
                    Key = "group[0]/rect#hero",
                    TargetKey = "hero"
                }
            };

            bool success = EditorWorkspaceCoordinator.TryResolveSelection(
                elements,
                "group[0]/rect#hero",
                AttributePatcher.ROOT_TARGET_KEY,
                out StructureNode selectedItem,
                out CanvasSelectionKind selectionKind);

            Assert.That(success, Is.True);
            Assert.That(selectionKind, Is.EqualTo(CanvasSelectionKind.Element));
            Assert.That(selectedItem, Is.Not.Null);
            Assert.That(selectedItem.Key, Is.EqualTo("group[0]/rect#hero"));
        }

        [Test]
        public void TryResolveSelection_ResolvesElementFromTargetKey()
        {
            var elements = new List<StructureNode>
            {
                new()
                {
                    Key = "group[0]/rect#hero",
                    TargetKey = "hero"
                }
            };

            bool success = EditorWorkspaceCoordinator.TryResolveSelection(
                elements,
                string.Empty,
                "hero",
                out StructureNode selectedItem,
                out CanvasSelectionKind selectionKind);

            Assert.That(success, Is.True);
            Assert.That(selectionKind, Is.EqualTo(CanvasSelectionKind.Element));
            Assert.That(selectedItem, Is.Not.Null);
            Assert.That(selectedItem.TargetKey, Is.EqualTo("hero"));
        }

        [Test]
        public void TryResolveSelection_MapsRootTargetToFrameSelection()
        {
            bool success = EditorWorkspaceCoordinator.TryResolveSelection(
                new List<StructureNode>(),
                string.Empty,
                AttributePatcher.ROOT_TARGET_KEY,
                out StructureNode selectedItem,
                out CanvasSelectionKind selectionKind);

            Assert.That(success, Is.True);
            Assert.That(selectionKind, Is.EqualTo(CanvasSelectionKind.Frame));
            Assert.That(selectedItem, Is.Null);
        }

        [Test]
        public void TryResolveSelection_ReturnsNoneWhenTargetCannotBeResolved()
        {
            bool success = EditorWorkspaceCoordinator.TryResolveSelection(
                new List<StructureNode>(),
                string.Empty,
                "missing",
                out StructureNode selectedItem,
                out CanvasSelectionKind selectionKind);

            Assert.That(success, Is.False);
            Assert.That(selectionKind, Is.EqualTo(CanvasSelectionKind.None));
            Assert.That(selectedItem, Is.Null);
        }
    }
}
