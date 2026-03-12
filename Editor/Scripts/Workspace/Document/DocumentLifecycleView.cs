using System;
using Core.UI.Foundation.Tooling;
using UnityEngine;
using UnityEngine.UIElements;

namespace SvgEditor
{
    internal sealed class DocumentLifecycleView
    {
        private static class UssClassName
        {
            private const string Prefix = "svg-editor__";

            public const string TOAST_LAYER = Prefix + "toast-layer";
            public const string TOAST = Prefix + "toast";
            public const string TOAST_SUCCESS = TOAST + "--success";
        }

        internal enum ToastVariant
        {
            Neutral,
            Success
        }

        private Image _previewImage;
        private Label _sourceStatusLabel;
        private Button _saveButton;
        private VisualElement _toastLayer;
        private Label _toastLabel;
        private IVisualElementScheduledItem _toastHideItem;

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

            _saveButton = root.Q<Button>("document-save");
            _sourceStatusLabel = root.Q<Label>("document-status");

            _toastLayer = new VisualElement
            {
                name = "document-toast-layer"
            };
            _toastLayer.AddToClassList(UssClassName.TOAST_LAYER);
            _toastLayer.pickingMode = PickingMode.Ignore;
            _toastLayer.style.display = DisplayStyle.None;

            _toastLabel = new Label
            {
                name = "document-toast"
            };
            _toastLabel.AddToClassList(UssClassName.TOAST);
            _toastLabel.pickingMode = PickingMode.Ignore;
            _toastLayer.Add(_toastLabel);
            root.Add(_toastLayer);

            if (_saveButton != null)
            {
                _saveButton.clicked += OnSaveClicked;
            }
        }

        public void Unbind()
        {
            _toastHideItem?.Pause();
            _toastHideItem = null;
            _toastLayer?.RemoveFromHierarchy();

            if (_saveButton != null)
            {
                _saveButton.clicked -= OnSaveClicked;
            }

            _previewImage = null;
            _sourceStatusLabel = null;
            _saveButton = null;
            _toastLayer = null;
            _toastLabel = null;
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

        public void ShowToast(string message, ToastVariant variant = ToastVariant.Neutral)
        {
            if (_toastLayer == null || _toastLabel == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _toastHideItem?.Pause();
            _toastLabel.text = message;
            _toastLabel.EnableInClassList(UssClassName.TOAST_SUCCESS, variant == ToastVariant.Success);
            _toastLayer.style.display = DisplayStyle.Flex;
            _toastHideItem = _toastLayer.schedule.Execute(() =>
            {
                if (_toastLayer != null)
                {
                    _toastLayer.style.display = DisplayStyle.None;
                }
            }).StartingIn(1800);
        }

        public void ShowLoadFailure(string error)
        {
            SetPreviewVectorImage(null);
            SetStatus($"Load failed: {error}");
        }

        private void OnSaveClicked() => SaveRequested?.Invoke();
    }
}
