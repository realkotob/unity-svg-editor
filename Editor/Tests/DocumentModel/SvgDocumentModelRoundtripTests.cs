using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnitySvgEditor.Editor.Tests
{
    public sealed class SvgDocumentModelRoundtripTests
    {
        [TestCase("no-viewbox-basic.svg")]
        [TestCase("negative-coordinates.svg")]
        [TestCase("transformed-parent.svg")]
        [TestCase("tiny-stroke-overlap.svg")]
        [TestCase("defs-use-basic.svg")]
        public void LoadSerializeReload_PreservesModelSemantics(string fixtureFileName)
        {
            SvgDocumentModel originalModel = SvgFixtureTestUtility.LoadFixtureModel(fixtureFileName);
            string serialized = SvgFixtureTestUtility.SerializeModel(originalModel);
            SvgDocumentModel roundtrippedModel = SvgFixtureTestUtility.LoadModel(serialized);

            Assert.That(SvgDocumentXmlUtility.TryLoadDocument(serialized, out _, out string error), Is.True, error);
            AssertModelsEquivalent(originalModel, roundtrippedModel);
        }

        [Test]
        public void LoadSerializeReload_PreservesInlinePreserveAspectRatio()
        {
            const string source = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"100\" viewBox=\"0 0 100 100\" preserveAspectRatio=\"none\"><rect id=\"r\" x=\"0\" y=\"0\" width=\"100\" height=\"100\" /></svg>";
            SvgDocumentModel originalModel = SvgFixtureTestUtility.LoadModel(source);
            string serialized = SvgFixtureTestUtility.SerializeModel(originalModel);
            SvgDocumentModel roundtrippedModel = SvgFixtureTestUtility.LoadModel(serialized);

            Assert.That(roundtrippedModel.PreserveAspectRatio, Is.EqualTo("none"));
            AssertModelsEquivalent(originalModel, roundtrippedModel);
        }

        private static void AssertModelsEquivalent(SvgDocumentModel expected, SvgDocumentModel actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.RootId, Is.EqualTo(expected.RootId));
            Assert.That(actual.NodeOrder, Is.EqualTo(expected.NodeOrder));
            Assert.That(actual.DefinitionNodeIds, Is.EqualTo(expected.DefinitionNodeIds));
            AssertStringDictionaryEqual(expected.Namespaces, actual.Namespaces);
            Assert.That(actual.Nodes.Count, Is.EqualTo(expected.Nodes.Count));
            Assert.That(actual.NodeIdsByXmlId.Count, Is.EqualTo(expected.NodeIdsByXmlId.Count));

            foreach (var pair in expected.NodeIdsByXmlId)
            {
                Assert.That(actual.NodeIdsByXmlId.TryGetValue(pair.Key, out SvgNodeId actualNodeId), Is.True, pair.Key);
                Assert.That(actualNodeId, Is.EqualTo(pair.Value));
            }

            foreach (SvgNodeId nodeId in expected.NodeOrder)
            {
                Assert.That(expected.TryGetNode(nodeId, out SvgNodeModel expectedNode), Is.True, nodeId.ToString());
                Assert.That(actual.TryGetNode(nodeId, out SvgNodeModel actualNode), Is.True, nodeId.ToString());

                Assert.That(actualNode.Id, Is.EqualTo(expectedNode.Id));
                Assert.That(actualNode.ParentId, Is.EqualTo(expectedNode.ParentId));
                Assert.That(actualNode.TagName, Is.EqualTo(expectedNode.TagName));
                Assert.That(actualNode.Kind, Is.EqualTo(expectedNode.Kind));
                Assert.That(actualNode.XmlId, Is.EqualTo(expectedNode.XmlId));
                Assert.That(actualNode.LegacyElementKey, Is.EqualTo(expectedNode.LegacyElementKey));
                Assert.That(actualNode.LegacyTargetKey, Is.EqualTo(expectedNode.LegacyTargetKey));
                Assert.That(actualNode.Depth, Is.EqualTo(expectedNode.Depth));
                Assert.That(actualNode.SiblingIndex, Is.EqualTo(expectedNode.SiblingIndex));
                Assert.That(actualNode.IsDefinitionNode, Is.EqualTo(expectedNode.IsDefinitionNode));
                Assert.That(actualNode.Children, Is.EqualTo(expectedNode.Children));
                AssertStringDictionaryEqual(expectedNode.RawAttributes, actualNode.RawAttributes);
                AssertReferencesEquivalent(expectedNode.References, actualNode.References);
            }
        }

        private static void AssertStringDictionaryEqual(
            IReadOnlyDictionary<string, string> expected,
            IReadOnlyDictionary<string, string> actual)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));

            foreach (var pair in expected)
            {
                Assert.That(actual.TryGetValue(pair.Key, out string actualValue), Is.True, pair.Key);
                Assert.That(actualValue, Is.EqualTo(pair.Value), pair.Key);
            }
        }

        private static void AssertReferencesEquivalent(
            IReadOnlyList<SvgNodeReference> expected,
            IReadOnlyList<SvgNodeReference> actual)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));

            string[] expectedKeys = expected
                .Select(reference => $"{reference.AttributeName}|{reference.FragmentId}|{reference.RawValue}")
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();
            string[] actualKeys = actual
                .Select(reference => $"{reference.AttributeName}|{reference.FragmentId}|{reference.RawValue}")
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            Assert.That(actualKeys, Is.EqualTo(expectedKeys));
        }
    }
}
