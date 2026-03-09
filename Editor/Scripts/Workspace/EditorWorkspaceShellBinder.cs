using System;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class EditorWorkspaceShellBinder
    {
        public Label SelectionNameLabel { get; private set; }
        public Label SelectionMetaLabel { get; private set; }
        public Label SelectionLayerLabel { get; private set; }
        public Label StructureStatusLabel { get; private set; }
        public Toggle LayerVisibleToggle { get; private set; }
        public Button ApplyLayerVisibilityButton { get; private set; }
        public FloatField QuickTransformTranslateXField { get; private set; }
        public FloatField QuickTransformTranslateYField { get; private set; }
        public FloatField QuickTransformRotateField { get; private set; }
        public FloatField QuickTransformScaleXField { get; private set; }
        public FloatField QuickTransformScaleYField { get; private set; }
        public Button QuickApplyTransformButton { get; private set; }
        public TreeView HierarchyTreeView { get; private set; }

        public bool IsBound => StructureStatusLabel != null || HierarchyTreeView != null;

        public void Bind(
            VisualElement root,
            StructureHierarchyController hierarchyController,
            IStructureHierarchyHost hierarchyHost,
            Action<StructureNode> onSelectionChanged,
            Action onApplyLayerVisibilityClicked)
        {
            Unbind(hierarchyController, onApplyLayerVisibilityClicked);
            if (root == null)
                return;

            SelectionNameLabel = root.Q<Label>("selection-name");
            SelectionMetaLabel = root.Q<Label>("selection-meta");
            SelectionLayerLabel = root.Q<Label>("selection-layer");
            LayerVisibleToggle = root.Q<Toggle>("layer-visible-toggle");
            ApplyLayerVisibilityButton = root.Q<Button>("apply-layer-visibility");
            QuickTransformTranslateXField = root.Q<FloatField>("structure-translate-x");
            QuickTransformTranslateYField = root.Q<FloatField>("structure-translate-y");
            QuickTransformRotateField = root.Q<FloatField>("structure-rotate");
            QuickTransformScaleXField = root.Q<FloatField>("structure-scale-x");
            QuickTransformScaleYField = root.Q<FloatField>("structure-scale-y");
            QuickApplyTransformButton = root.Q<Button>("structure-apply-transform");
            StructureStatusLabel = root.Q<Label>("structure-status");
            HierarchyTreeView = root.Q<TreeView>("asset-hierarchy-list");

            hierarchyController.Bind(HierarchyTreeView, hierarchyHost, onSelectionChanged);
            if (ApplyLayerVisibilityButton != null)
                ApplyLayerVisibilityButton.clicked += onApplyLayerVisibilityClicked;
        }

        public void Unbind(StructureHierarchyController hierarchyController, Action onApplyLayerVisibilityClicked)
        {
            hierarchyController?.Unbind();
            if (ApplyLayerVisibilityButton != null)
                ApplyLayerVisibilityButton.clicked -= onApplyLayerVisibilityClicked;

            SelectionNameLabel = null;
            SelectionMetaLabel = null;
            SelectionLayerLabel = null;
            LayerVisibleToggle = null;
            ApplyLayerVisibilityButton = null;
            QuickTransformTranslateXField = null;
            QuickTransformTranslateYField = null;
            QuickTransformRotateField = null;
            QuickTransformScaleXField = null;
            QuickTransformScaleYField = null;
            QuickApplyTransformButton = null;
            StructureStatusLabel = null;
            HierarchyTreeView = null;
        }
    }
}
