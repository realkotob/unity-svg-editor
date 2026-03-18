using System;
using System.Collections.Generic;
using UnityEngine;
using SvgEditor.Shared;
using SvgEditor.Document;
using SvgEditor.Document.Structure.Hierarchy;
using Core.UI.Extensions;

using SvgEditor;
using SvgEditor.Preview;

namespace SvgEditor.Workspace.Canvas
{
    internal sealed class InteractionSelectionResolver
    {
        private readonly ICanvasPointerDragHost _host;
        private readonly SceneProjector _sceneProjector;

        public InteractionSelectionResolver(
            ICanvasPointerDragHost host,
            SceneProjector sceneProjector)
        {
            _host = host;
            _sceneProjector = sceneProjector;
        }

        public bool IsDirectElementSelectionModifier(EventModifiers modifiers)
        {
            return (modifiers & (EventModifiers.Command | EventModifiers.Control)) != 0;
        }

        public bool TryResolveInteractionElement(
            Vector2 localPosition,
            EventModifiers modifiers,
            out PreviewElementGeometry interactionElement,
            out string interactionElementKey)
        {
            interactionElement = null;
            interactionElementKey = null;

            if (!IsDirectElementSelectionModifier(modifiers) &&
                TryFindContainingGroupElement(localPosition, out interactionElement))
            {
                interactionElementKey = interactionElement.Key;
                return true;
            }

            if (!_sceneProjector.TryHitTestPreviewElement(_host.PreviewSnapshot, localPosition, out PreviewElementGeometry hitElement))
            {
                return false;
            }

            interactionElementKey = ResolveInteractionElementKey(hitElement.Key, modifiers);
            if (string.IsNullOrWhiteSpace(interactionElementKey))
            {
                return false;
            }

            interactionElement = string.Equals(interactionElementKey, hitElement.Key, StringComparison.Ordinal)
                ? hitElement
                : _sceneProjector.FindPreviewElement(_host.PreviewSnapshot, interactionElementKey);
            return interactionElement != null;
        }

        public IReadOnlyList<string> ResolveAreaSelectionKeys(Rect sceneRect, EventModifiers modifiers)
        {
            List<string> resolvedKeys = new();
            PreviewSnapshot previewSnapshot = _host.PreviewSnapshot;
            if (previewSnapshot?.Elements == null)
            {
                return resolvedKeys;
            }

            for (int index = 0; index < previewSnapshot.Elements.Count; index++)
            {
                PreviewElementGeometry candidate = previewSnapshot.Elements[index];
                if (candidate == null || !candidate.VisualBounds.Overlaps(sceneRect))
                {
                    continue;
                }

                string resolvedKey = ResolveInteractionElementKey(candidate.Key, modifiers);
                if (string.IsNullOrWhiteSpace(resolvedKey) || resolvedKeys.Contains(resolvedKey))
                {
                    continue;
                }

                resolvedKeys.Add(resolvedKey);
            }

            return resolvedKeys;
        }

        private string ResolveInteractionElementKey(string elementKey, EventModifiers modifiers)
        {
            if (string.IsNullOrWhiteSpace(elementKey) || IsDirectElementSelectionModifier(modifiers))
            {
                return elementKey;
            }

            HierarchyNode currentNode = _host.FindHierarchyNode(elementKey);
            while (currentNode != null)
            {
                if (string.Equals(currentNode.TagName, SvgTagName.GROUP, StringComparison.Ordinal))
                {
                    return currentNode.Key;
                }

                if (string.IsNullOrWhiteSpace(currentNode.ParentKey))
                {
                    break;
                }

                currentNode = _host.FindHierarchyNode(currentNode.ParentKey);
            }

            return elementKey;
        }

        private bool TryFindContainingGroupElement(Vector2 localPosition, out PreviewElementGeometry groupElement)
        {
            groupElement = null;
            PreviewSnapshot previewSnapshot = _host.PreviewSnapshot;
            if (previewSnapshot?.Elements == null ||
                !_sceneProjector.TryViewportPointToScenePoint(previewSnapshot, localPosition, out Vector2 scenePoint))
            {
                return false;
            }

            float bestArea = float.MaxValue;
            int bestDrawOrder = int.MinValue;

            for (int index = 0; index < previewSnapshot.Elements.Count; index++)
            {
                PreviewElementGeometry candidate = previewSnapshot.Elements[index];
                if (candidate == null || !candidate.VisualBounds.Contains(scenePoint))
                {
                    continue;
                }

                HierarchyNode candidateNode = _host.FindHierarchyNode(candidate.Key);
                if (candidateNode == null ||
                    !string.Equals(candidateNode.TagName, SvgTagName.GROUP, StringComparison.Ordinal))
                {
                    continue;
                }

                float area = candidate.VisualBounds.width * candidate.VisualBounds.height;
                bool isBetter =
                    groupElement == null ||
                    area < bestArea ||
                    (Mathf.Approximately(area, bestArea) && candidate.DrawOrder > bestDrawOrder);
                if (!isBetter)
                {
                    continue;
                }

                groupElement = candidate;
                bestArea = area;
                bestDrawOrder = candidate.DrawOrder;
            }

            return groupElement != null;
        }
    }
}
