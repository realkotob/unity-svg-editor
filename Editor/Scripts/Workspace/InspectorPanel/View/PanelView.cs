using System;
using Core.UI.Foundation.Components.ColorPercentField;
using UnityEngine;
using UnityEngine.UIElements;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class PanelView
    {
        internal enum ImmediateApplyField
        {
            Opacity,
            CornerRadius,
            FillColor,
            StrokeColor,
            StrokeWidth,
            StrokeLinecap,
            StrokeLinejoin,
            StrokeDasharray
        }

        internal enum AttributeAction
        {
            AddFill,
            RemoveFill,
            AddStroke,
            RemoveStroke
        }

        internal enum PositionAction
        {
            AlignLeft,
            AlignCenter,
            AlignRight,
            AlignTop,
            AlignMiddle,
            AlignBottom,
            RotateClockwise90,
            FlipHorizontal,
            FlipVertical
        }

        internal enum TransformHelperField
        {
            TranslateX,
            TranslateY,
            Rotate,
            ScaleX,
            ScaleY
        }

        internal readonly struct TransformHelperChange
        {
            public TransformHelperChange(TransformHelperField field, float delta = 0f)
            {
                Field = field;
                Delta = delta;
            }

            public TransformHelperField Field { get; }
            public float Delta { get; }
        }

        private readonly FormControls _form = new();
        private readonly PanelViewEventBinder _eventBinder;

        public event Action<ImmediateApplyField> ImmediateApplyRequested;
        public event Action FrameRectChanged;
        public event Action<TransformHelperChange> TransformHelperChanged;
        public event Action RotationDragStarted;
        public event Action RotationDragEnded;
        public event Action<PositionAction> PositionActionRequested;
        public event Action<AttributeAction> AttributeActionRequested;

        public bool IsBound => _form.IsBound;
        public bool FillEnabled => _form.FillEnabled;
        public bool StrokeEnabled => _form.StrokeEnabled;
        public bool StrokeWidthEnabled => _form.StrokeWidthEnabled;
        public bool OpacityEnabled => _form.OpacityEnabled;
        public bool DasharrayEnabled => _form.DasharrayEnabled;

        public VisualElement FillColorControl => _form.FillColorControl;
        public VisualElement FillAddControl => _form.FillAddButton;
        public VisualElement FillRemoveControl => _form.FillRemoveButton;
        public VisualElement StrokeColorControl => _form.StrokeColorControl;
        public VisualElement StrokeAddControl => _form.StrokeAddButton;
        public VisualElement StrokeRemoveControl => _form.StrokeRemoveButton;
        public VisualElement StrokeWidthControl => _form.StrokeWidthField;
        public VisualElement OpacityControl => _form.OpacityControl;
        public VisualElement CornerRadiusControl => _form.CornerRadiusField;
        public VisualElement DashLengthControl => _form.DashLengthField;
        public VisualElement DashGapControl => _form.DashGapField;
        public VisualElement TransformControl => _form.TransformField;
        public VisualElement FrameXControl => _form.FrameXField;
        public VisualElement FrameYControl => _form.FrameYField;
        public VisualElement FrameWidthControl => _form.FrameWidthField;
        public VisualElement FrameHeightControl => _form.FrameHeightField;
        public VisualElement LinecapControl => (VisualElement)_form.LinecapPopup ?? _form.LinecapLegacyPopup;
        public VisualElement LinejoinControl => (VisualElement)_form.LinejoinPopup ?? _form.LinejoinLegacyPopup;
        public VisualElement TranslateXControl => _form.TranslateXField;
        public VisualElement TranslateYControl => _form.TranslateYField;
        public VisualElement RotateControl => _form.RotateField;
        public VisualElement ScaleXControl => _form.ScaleXField;
        public VisualElement ScaleYControl => _form.ScaleYField;
        public VisualElement PositionAlignLeftControl => _form.PositionAlignLeftButton;
        public VisualElement PositionAlignCenterControl => _form.PositionAlignCenterButton;
        public VisualElement PositionAlignRightControl => _form.PositionAlignRightButton;
        public VisualElement PositionAlignTopControl => _form.PositionAlignTopButton;
        public VisualElement PositionAlignMiddleControl => _form.PositionAlignMiddleButton;
        public VisualElement PositionAlignBottomControl => _form.PositionAlignBottomButton;
        public VisualElement PositionRotateClockwise90Control => _form.PositionRotateClockwise90Button;
        public VisualElement PositionFlipHorizontalControl => _form.PositionFlipHorizontalButton;
        public VisualElement PositionFlipVerticalControl => _form.PositionFlipVerticalButton;

        public PanelView()
        {
            _eventBinder = new PanelViewEventBinder(
                _form,
                field => ImmediateApplyRequested?.Invoke(field),
                () => FrameRectChanged?.Invoke(),
                change => TransformHelperChanged?.Invoke(change),
                () => RotationDragStarted?.Invoke(),
                () => RotationDragEnded?.Invoke(),
                action => PositionActionRequested?.Invoke(action),
                action => AttributeActionRequested?.Invoke(action));
        }

        public void Bind(VisualElement root)
        {
            Unbind();
            if (root == null)
                return;

            _form.Bind(root);
            _eventBinder.Bind();
        }

        public void Unbind()
        {
            _eventBinder.Unbind();
            _form.Unbind();
        }

        public void CaptureState(PanelState inspectorPanelState)
        {
            StateBinder.CaptureState(_form, inspectorPanelState);
        }

        public void ApplyState(PanelState inspectorPanelState)
        {
            StateBinder.ApplyState(_form, inspectorPanelState);
        }

        public void SetTransformText(string transform)
        {
            _form.SetTransformText(transform);
        }
    }
}
