using System;
using Core.UI.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitySvgEditor.Editor
{
    [UxmlElement]
    public partial class CanvasStageView : VisualElement
    {
        #region Constants
        internal static class ElementName
        {
            public const string STAGE = "canvas-stage";
            public const string FRAME = "canvas-frame";
            public const string PREVIEW_IMAGE = "preview-image";
            public const string ZOOM_HUD = "canvas-zoom-hud";
            public const string ZOOM_LABEL = "canvas-zoom-label";
            public const string ZOOM_RESET_BUTTON = "canvas-zoom-reset-button";
        }

        internal static class UssClassName
        {
            public const string BASE = "svg-editor__canvas-stage-view";
            public const string STAGE = "svg-editor__canvas-stage";
            public const string FRAME = "svg-editor__canvas-frame";
            public const string PREVIEW_IMAGE = "svg-editor__preview-canvas";
            public const string ZOOM_HUD = "svg-editor__canvas-zoom-hud";
            public const string ZOOM_LABEL = "svg-editor__canvas-zoom-label";
            public const string ZOOM_RESET_BUTTON = "svg-editor__canvas-zoom-reset-button";
        }
        #endregion Constants

        #region Variables
        private readonly VisualElement _stageElement;
        private readonly VisualElement _frameElement;
        private readonly Image _previewImageElement;
        private readonly VisualElement _zoomHudElement;
        private readonly Label _zoomLabelElement;
        private readonly Button _zoomResetButton;
        #endregion Variables

        #region Properties
        internal VisualElement StageElement => _stageElement;
        internal VisualElement FrameElement => _frameElement;
        internal Image PreviewImageElement => _previewImageElement;
        internal event Action ResetRequested;
        #endregion Properties

        #region Constructor
        public CanvasStageView()
        {
            this.AddClass(UssClassName.BASE);

            _stageElement = new VisualElement()
                .SetName(ElementName.STAGE)
                .AddClass(UssClassName.STAGE);
            Add(_stageElement);

            _frameElement = new VisualElement()
                .SetName(ElementName.FRAME)
                .AddClass(UssClassName.FRAME);
            _stageElement.Add(_frameElement);

            _previewImageElement = new Image()
                .SetName(ElementName.PREVIEW_IMAGE)
                .AddClass(UssClassName.PREVIEW_IMAGE);
            _previewImageElement.scaleMode = ScaleMode.ScaleToFit;
            _previewImageElement.pickingMode = PickingMode.Ignore;
            _previewImageElement.style.position = Position.Absolute;
            _previewImageElement.style.left = 0f;
            _previewImageElement.style.top = 0f;
            _previewImageElement.style.width = Length.Percent(100);
            _previewImageElement.style.height = Length.Percent(100);
            _frameElement.Add(_previewImageElement);

            _zoomHudElement = new VisualElement()
                .SetName(ElementName.ZOOM_HUD)
                .AddClass(UssClassName.ZOOM_HUD);

            _zoomLabelElement = new Label()
                .SetName(ElementName.ZOOM_LABEL)
                .AddClass(UssClassName.ZOOM_LABEL);
            _zoomHudElement.Add(_zoomLabelElement);

            _zoomResetButton = new Button(HandleResetButtonClicked)
                .SetName(ElementName.ZOOM_RESET_BUTTON)
                .AddClass(UssClassName.ZOOM_RESET_BUTTON);
            _zoomResetButton.text = "1:1";
            _zoomResetButton.tooltip = "Reset zoom to actual size";
            _zoomHudElement.Add(_zoomResetButton);

            Add(_zoomHudElement);
            SetZoomPercent(1f);
            SetHudEnabled(false);
        }
        #endregion Constructor

        #region Internal Methods
        internal void PrepareRuntime()
        {
            _frameElement.style.display = DisplayStyle.None;
        }

        internal void SetZoomPercent(float zoom)
        {
            int zoomPercent = Mathf.RoundToInt(Mathf.Max(0.01f, zoom) * 100f);
            _zoomLabelElement.text = $"{zoomPercent}%";
        }

        internal void SetHudEnabled(bool enabled)
        {
            _zoomResetButton?.SetEnabled(enabled);
        }

        private void HandleResetButtonClicked()
        {
            ResetRequested?.Invoke();
        }
        #endregion Internal Methods
    }
}
