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
            public const string DIRTY_BADGE = "canvas-dirty-badge";
            public const string DIRTY_DOT = "canvas-dirty-dot";
            public const string DIRTY_LABEL = "canvas-dirty-label";
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
            public const string DIRTY_BADGE = "svg-editor__canvas-dirty-badge";
            public const string DIRTY_DOT = "svg-editor__canvas-dirty-dot";
            public const string DIRTY_LABEL = "svg-editor__canvas-dirty-label";
        }
        #endregion Constants

        #region Variables
        private readonly VisualElement _stageElement;
        private readonly VisualElement _frameElement;
        private readonly Image _previewImageElement;
        private VisualElement _zoomHudElement;
        private Label _zoomLabelElement;
        private Button _zoomResetButton;
        private VisualElement _dirtyBadgeElement;
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
        }
        #endregion Constructor

        #region Internal Methods
        internal void PrepareRuntime()
        {
            _frameElement.style.display = DisplayStyle.None;
            EnsureHudElements();
            SetZoomPercent(1f);
            SetHudEnabled(false);
            SetDirtyBadgeVisible(false);
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

        internal void SetDirtyBadgeVisible(bool visible)
        {
            EnsureHudElements();
            if (_dirtyBadgeElement == null)
                return;

            _dirtyBadgeElement.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void HandleResetButtonClicked()
        {
            ResetRequested?.Invoke();
        }

        private void EnsureHudElements()
        {
            _zoomHudElement ??= this.Q<VisualElement>(ElementName.ZOOM_HUD);
            _zoomLabelElement ??= this.Q<Label>(ElementName.ZOOM_LABEL);
            _zoomResetButton ??= this.Q<Button>(ElementName.ZOOM_RESET_BUTTON);
            _dirtyBadgeElement ??= this.Q<VisualElement>(ElementName.DIRTY_BADGE);

            if (_zoomResetButton != null)
            {
                _zoomResetButton.tooltip = "Reset zoom to actual size";
                _zoomResetButton.clicked -= HandleResetButtonClicked;
                _zoomResetButton.clicked += HandleResetButtonClicked;
            }
        }
        #endregion Internal Methods
    }
}
