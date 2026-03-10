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
        private Button _saveButton;

        public event Action SaveRequested;

        public Image PreviewImage => _previewImage;
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

            _saveButton = root.Q<Button>("source-save");
            _sourceStatusLabel = root.Q<Label>("source-status");

            if (_saveButton != null)
            {
                _saveButton.clicked += OnSaveClicked;
            }
        }

        public void Unbind()
        {
            if (_saveButton != null)
            {
                _saveButton.clicked -= OnSaveClicked;
            }

            _previewImage = null;
            _sourceStatusLabel = null;
            _saveButton = null;
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

        private void OnSaveClicked() => SaveRequested?.Invoke();
    }
}
