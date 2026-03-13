using System.Linq;
using NUnit.Framework;
using SvgEditor.Document.Structure.Hierarchy;
using SvgEditor.Workspace.HierarchyPanel;

namespace SvgEditor.Tests.Workspace.HierarchyPanel
{
    public sealed class HierarchyStateSelectionTests
    {
        [Test]
        public void SetStructure_SelectsFallbackElementWhenSelectionIsEmpty()
        {
            var state = new HierarchyState();

            state.SetStructure(CreateOutline("a", "b", "c"), "b");

            Assert.That(state.SelectedElementKey, Is.EqualTo("b"));
            Assert.That(state.SelectedElementKeys, Is.EqualTo(new[] { "b" }));
            Assert.That(state.SelectionRangeAnchorKey, Is.EqualTo("b"));
        }

        [Test]
        public void ToggleElementSelection_AppendsPrimaryAndFallsBackWhenPrimaryIsRemoved()
        {
            var state = new HierarchyState();
            state.SetStructure(CreateOutline("a", "b", "c"), string.Empty);
            state.SelectElement("a");

            state.ToggleElementSelection("c");

            Assert.That(state.SelectedElementKey, Is.EqualTo("c"));
            Assert.That(state.SelectedElementKeys, Is.EqualTo(new[] { "a", "c" }));
            Assert.That(state.SelectionRangeAnchorKey, Is.EqualTo("c"));

            state.ToggleElementSelection("c");

            Assert.That(state.SelectedElementKey, Is.EqualTo("a"));
            Assert.That(state.SelectedElementKeys, Is.EqualTo(new[] { "a" }));
            Assert.That(state.SelectionRangeAnchorKey, Is.EqualTo("a"));
        }

        [Test]
        public void AddElementSelectionRange_AddsRangeAndKeepsAnchorOnOriginalExplicitSelection()
        {
            var state = new HierarchyState();
            state.SetStructure(CreateOutline("a", "b", "c", "d", "e"), string.Empty);
            state.SelectElement("b");

            state.AddElementSelectionRange("d");

            Assert.That(state.SelectedElementKey, Is.EqualTo("d"));
            Assert.That(state.SelectedElementKeys, Is.EqualTo(new[] { "b", "c", "d" }));
            Assert.That(state.SelectionRangeAnchorKey, Is.EqualTo("b"));

            state.AddElementSelectionRange("a");

            Assert.That(state.SelectedElementKey, Is.EqualTo("a"));
            Assert.That(state.SelectedElementKeys, Is.EquivalentTo(new[] { "a", "b", "c", "d" }));
            Assert.That(state.SelectionRangeAnchorKey, Is.EqualTo("b"));
        }

        [Test]
        public void SetStructure_PrunesMissingSelectionsAndPromotesLastSurvivingSelection()
        {
            var state = new HierarchyState();
            state.SetStructure(CreateOutline("a", "b", "c", "d"), string.Empty);
            state.SelectElement("b");
            state.ToggleElementSelection("d");

            state.SetStructure(CreateOutline("a", "b", "c"), string.Empty);

            Assert.That(state.SelectedElementKey, Is.EqualTo("b"));
            Assert.That(state.SelectedElementKeys, Is.EqualTo(new[] { "b" }));
            Assert.That(state.SelectionRangeAnchorKey, Is.EqualTo("b"));
        }

        private static HierarchyOutline CreateOutline(params string[] keys)
        {
            return new HierarchyOutline
            {
                Elements = keys.Select(CreateNode).ToArray()
            };
        }

        private static HierarchyNode CreateNode(string key)
        {
            return new HierarchyNode
            {
                Key = key,
                TargetKey = key,
                LayerKey = "layer"
            };
        }
    }
}
