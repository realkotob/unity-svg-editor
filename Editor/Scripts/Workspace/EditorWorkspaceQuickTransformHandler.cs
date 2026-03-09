using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class EditorWorkspaceQuickTransformHandler
    {
        private readonly IEditorWorkspaceHost _host;
        private readonly StructureEditor _structureEditor;
        private readonly StructurePanelState _structurePanelState;

        private FloatField _translateXField;
        private FloatField _translateYField;
        private FloatField _rotateField;
        private FloatField _scaleXField;
        private FloatField _scaleYField;
        private Button _applyButton;

        public EditorWorkspaceQuickTransformHandler(
            IEditorWorkspaceHost host,
            StructureEditor structureEditor,
            StructurePanelState structurePanelState)
        {
            _host = host;
            _structureEditor = structureEditor;
            _structurePanelState = structurePanelState;
        }

        public void Bind(EditorWorkspaceShellBinder shellBinder)
        {
            Unbind();
            if (shellBinder == null)
                return;

            _translateXField = shellBinder.QuickTransformTranslateXField;
            _translateYField = shellBinder.QuickTransformTranslateYField;
            _rotateField = shellBinder.QuickTransformRotateField;
            _scaleXField = shellBinder.QuickTransformScaleXField;
            _scaleYField = shellBinder.QuickTransformScaleYField;
            _applyButton = shellBinder.QuickApplyTransformButton;

            _translateXField?.SetValueWithoutNotify(_structurePanelState.QuickTranslateX);
            _translateYField?.SetValueWithoutNotify(_structurePanelState.QuickTranslateY);
            _rotateField?.SetValueWithoutNotify(_structurePanelState.QuickRotate);
            _scaleXField?.SetValueWithoutNotify(_structurePanelState.QuickScaleX);
            _scaleYField?.SetValueWithoutNotify(_structurePanelState.QuickScaleY);

            _translateXField?.RegisterValueChangedCallback(OnQuickTranslateXChanged);
            _translateYField?.RegisterValueChangedCallback(OnQuickTranslateYChanged);
            _rotateField?.RegisterValueChangedCallback(OnQuickRotateChanged);
            _scaleXField?.RegisterValueChangedCallback(OnQuickScaleXChanged);
            _scaleYField?.RegisterValueChangedCallback(OnQuickScaleYChanged);
            if (_applyButton != null)
                _applyButton.clicked += OnQuickApplyTransformClicked;
        }

        public void Unbind()
        {
            _translateXField?.UnregisterValueChangedCallback(OnQuickTranslateXChanged);
            _translateYField?.UnregisterValueChangedCallback(OnQuickTranslateYChanged);
            _rotateField?.UnregisterValueChangedCallback(OnQuickRotateChanged);
            _scaleXField?.UnregisterValueChangedCallback(OnQuickScaleXChanged);
            _scaleYField?.UnregisterValueChangedCallback(OnQuickScaleYChanged);
            if (_applyButton != null)
                _applyButton.clicked -= OnQuickApplyTransformClicked;

            _translateXField = null;
            _translateYField = null;
            _rotateField = null;
            _scaleXField = null;
            _scaleYField = null;
            _applyButton = null;
        }

        private void OnQuickTranslateXChanged(ChangeEvent<float> evt)
        {
            _structurePanelState.SetQuickTranslateX(evt.newValue);
        }

        private void OnQuickTranslateYChanged(ChangeEvent<float> evt)
        {
            _structurePanelState.SetQuickTranslateY(evt.newValue);
        }

        private void OnQuickRotateChanged(ChangeEvent<float> evt)
        {
            _structurePanelState.SetQuickRotate(evt.newValue);
        }

        private void OnQuickScaleXChanged(ChangeEvent<float> evt)
        {
            _structurePanelState.SetQuickScaleX(evt.newValue);
        }

        private void OnQuickScaleYChanged(ChangeEvent<float> evt)
        {
            _structurePanelState.SetQuickScaleY(evt.newValue);
        }

        private void OnQuickApplyTransformClicked()
        {
            if (_host.CurrentDocument == null)
                return;

            var selectedElementKey = _structurePanelState.SelectedElementKey;
            if (string.IsNullOrWhiteSpace(selectedElementKey))
            {
                _host.UpdateSourceStatus("Select an element first.");
                return;
            }

            var transformValue = _structurePanelState.BuildQuickTransformString(_host.FormatNumber);
            if (!_structureEditor.TrySetElementTransform(
                    _host.CurrentDocument.WorkingSourceText,
                    selectedElementKey,
                    transformValue,
                    out string updatedSource,
                    out string error))
            {
                _host.UpdateSourceStatus($"Transform update failed: {error}");
                return;
            }

            _host.ApplyUpdatedSource(
                updatedSource,
                string.IsNullOrWhiteSpace(transformValue)
                    ? "Transform cleared from selection."
                    : "Transform applied to selection.");
        }
    }
}
