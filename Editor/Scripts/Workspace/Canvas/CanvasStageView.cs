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
        }

        internal static class UssClassName
        {
            public const string BASE = "svg-editor__canvas-stage-view";
            public const string STAGE = "svg-editor__canvas-stage";
            public const string FRAME = "svg-editor__canvas-frame";
            public const string PREVIEW_IMAGE = "svg-editor__preview-canvas";
        }
        #endregion Constants

        #region Variables
        private readonly VisualElement _stageElement;
        private readonly VisualElement _frameElement;
        private readonly Image _previewImageElement;
        #endregion Variables

        #region Properties
        internal VisualElement StageElement => _stageElement;
        internal VisualElement FrameElement => _frameElement;
        internal Image PreviewImageElement => _previewImageElement;
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
        }
        #endregion Internal Methods
    }
}
