using System.Collections.Generic;
using NUnit.Framework;
using SvgEditor.Core.Svg.Hierarchy;
using SvgEditor.UI.Hierarchy;

namespace SvgEditor.Editor.Tests
{
    public sealed class HierarchyDeletePlannerTests
    {
        [Test]
        public void Plan_WhenParentAndChildAreSelected_DeletesOnlyParentAndUsesNextSiblingFallback()
        {
            var elements = new List<HierarchyNode>
            {
                new() { Key = "group", ParentKey = string.Empty },
                new() { Key = "path", ParentKey = "group" },
                new() { Key = "rect", ParentKey = string.Empty }
            };

            HierarchyDeletePlan plan = HierarchyDeletePlanner.Plan(
                elements,
                new[] { "group", "path" },
                "group");

            Assert.That(plan.DeleteKeys, Is.EqualTo(new[] { "group" }));
            Assert.That(plan.FallbackElementKey, Is.EqualTo("rect"));
        }
    }
}
