using System;
using Core.UI.Foundation.Tooling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    internal sealed class DocumentLifecycleView
    {
        private Image _previewImage;
        private Label _sourceStatusLabel;
        private TextField _sourceEditorField;
        private Button _reloadButton;
        private Button _validateButton;
        private Button _saveButton;
        private bool _isUpdatingSourceField;

        public event Action ReloadRequested;
        public event Action ValidateRequested;
        public event Action SaveRequested;
        public event Action<string> SourceChanged;

        public Image PreviewImage => _previewImage;
        public VisualElement SourceEditorControl => _sourceEditorField;
        public VisualElement ReloadButtonControl => _reloadButton;
        public VisualElement ValidateButtonControl => _validateButton;
        public VisualElement SaveButtonControl => _saveButton;

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
            {
                return;
            }

            _previewImage = root.Q<Image>("preview-image");
            if (_previewImage != null)
            {
                _previewImage.scaleMode = ScaleMode.ScaleToFit;
                _previewImage.pickingMode = PickingMode.Ignore;
                _previewImage.style.position = Position.Absolute;
                _previewImage.style.left = 0f;
                _previewImage.style.top = 0f;
                _previewImage.style.width = Length.Percent(100);
                _previewImage.style.height = Length.Percent(100);
            }

            _reloadButton = root.Q<Button>("source-reload");
            _validateButton = root.Q<Button>("source-validate");
            _saveButton = root.Q<Button>("source-save");
            _sourceEditorField = root.Q<TextField>("source-editor");
            _sourceStatusLabel = root.Q<Label>("source-status");

            if (_reloadButton != null)
            {
                _reloadButton.clicked += OnReloadClicked;
            }

            if (_validateButton != null)
            {
                _validateButton.clicked += OnValidateClicked;
            }

            if (_saveButton != null)
            {
                _saveButton.clicked += OnSaveClicked;
            }

            if (_sourceEditorField != null)
            {
                _sourceEditorField.multiline = true;
                _sourceEditorField.RegisterValueChangedCallback(OnSourceEditorChanged);
            }
        }

        public void Unbind()
        {
            if (_reloadButton != null)
            {
                _reloadButton.clicked -= OnReloadClicked;
            }

            if (_validateButton != null)
            {
                _validateButton.clicked -= OnValidateClicked;
            }

            if (_saveButton != null)
            {
                _saveButton.clicked -= OnSaveClicked;
            }

            if (_sourceEditorField != null)
            {
                _sourceEditorField.UnregisterValueChangedCallback(OnSourceEditorChanged);
            }

            _previewImage = null;
            _sourceStatusLabel = null;
            _sourceEditorField = null;
            _reloadButton = null;
            _validateButton = null;
            _saveButton = null;
            _isUpdatingSourceField = false;
        }

        public void SetSourceText(string sourceText)
        {
            if (_sourceEditorField == null)
            {
                return;
            }

            _isUpdatingSourceField = true;
            _sourceEditorField.value = sourceText ?? string.Empty;
            _isUpdatingSourceField = false;
        }

        public void SetPreviewVectorImage(VectorImage vectorImage)
        {
            if (_previewImage == null)
            {
                return;
            }

            PreviewImageHelper.Apply(_previewImage, PreviewImageSource.FromVectorImage(vectorImage));
        }

        public void SetStatus(string status)
        {
            if (_sourceStatusLabel != null)
            {
                _sourceStatusLabel.text = status ?? string.Empty;
            }
        }

        public void ShowLoadFailure(string error)
        {
            SetPreviewVectorImage(null);
            SetStatus($"Load failed: {error}");
        }

        private void OnReloadClicked() => ReloadRequested?.Invoke();

        private void OnValidateClicked() => ValidateRequested?.Invoke();

        private void OnSaveClicked() => SaveRequested?.Invoke();

        private void OnSourceEditorChanged(ChangeEvent<string> evt)
        {
            if (_isUpdatingSourceField)
            {
                return;
            }

            SourceChanged?.Invoke(evt.newValue ?? string.Empty);
        }
    }
}
