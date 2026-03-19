using System;
using UnityEngine;
using UnityEngine.UIElements;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.UI.Inspector
{
    internal sealed class PanelView
    {
        private const string RemovePlaceholder = "none";
        private const string LinecapActualDefaultValue = "butt";
        private const string LinejoinActualDefaultValue = "miter";

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
        public VisualElement LinecapControl => _form.LinecapControl;
        public VisualElement LinejoinControl => _form.LinejoinControl;
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
            if (_form == null || inspectorPanelState == null)
                return;

            inspectorPanelState.FillEnabled = _form.FillEnabled;
            inspectorPanelState.FillColor = _form.FillColorValue;
            inspectorPanelState.StrokeEnabled = _form.StrokeEnabled;
            inspectorPanelState.StrokeColor = _form.StrokeColorValue;
            inspectorPanelState.StrokeWidthEnabled = _form.StrokeEnabled && _form.StrokeWidthField != null;
            inspectorPanelState.StrokeWidth = _form.StrokeWidthField?.value ?? 1f;
            inspectorPanelState.OpacityEnabled = _form.OpacityControl != null;
            float opacityValue = _form.OpacityValue;
            inspectorPanelState.Opacity = _form.IsOpacitySlider
                ? Mathf.Clamp01(opacityValue)
                : Mathf.Clamp01(opacityValue / 100f);
            inspectorPanelState.CornerRadius = Mathf.Max(0f, _form.CornerRadiusField?.value ?? 0f);
            inspectorPanelState.StrokeLinecap = NormalizeLinecapValue(_form.LinecapValue);
            inspectorPanelState.StrokeLinejoin = NormalizeLinejoinValue(_form.LinejoinValue);
            inspectorPanelState.DasharrayEnabled = _form.StrokeEnabled && (_form.DashLengthField != null || _form.DashGapField != null);
            inspectorPanelState.DashLength = _form.DashLengthField?.value ?? 4f;
            inspectorPanelState.DashGap = _form.DashGapField?.value ?? 2f;
            inspectorPanelState.TransformEnabled = _form.TransformField != null;
            inspectorPanelState.Transform = _form.TransformField?.value ?? string.Empty;
            inspectorPanelState.FrameX = _form.FrameXField?.value ?? 0f;
            inspectorPanelState.FrameY = _form.FrameYField?.value ?? 0f;
            inspectorPanelState.FrameWidth = _form.FrameWidthField?.value ?? 0f;
            inspectorPanelState.FrameHeight = _form.FrameHeightField?.value ?? 0f;
            if (_form.TranslateXField != null)
                inspectorPanelState.TranslateX = _form.TranslateXField.value;
            if (_form.TranslateYField != null)
                inspectorPanelState.TranslateY = _form.TranslateYField.value;
            if (_form.RotateField != null)
                inspectorPanelState.Rotate = _form.RotateField.value;
            if (_form.ScaleXField != null)
                inspectorPanelState.ScaleX = _form.ScaleXField.value;
            if (_form.ScaleYField != null)
                inspectorPanelState.ScaleY = _form.ScaleYField.value;
        }

        public void ApplyState(PanelState inspectorPanelState)
        {
            if (_form == null || inspectorPanelState == null)
                return;

            _form.SetFillVisible(inspectorPanelState.FillEnabled);
            _form.SetStrokeVisible(inspectorPanelState.StrokeEnabled);
            _form.SetFillColorWithoutNotify(inspectorPanelState.FillColor);
            _form.SetStrokeColorWithoutNotify(inspectorPanelState.StrokeColor);
            _form.StrokeWidthField?.SetValueWithoutNotify(inspectorPanelState.StrokeWidth);
            _form.OpacityField?.SetValueWithoutNotify(_form.IsOpacitySlider
                ? inspectorPanelState.Opacity
                : inspectorPanelState.Opacity * 100f);
            _form.CornerRadiusField?.SetValueWithoutNotify(inspectorPanelState.CornerRadius);
            _form.SetLinecapWithoutNotify(string.IsNullOrEmpty(inspectorPanelState.StrokeLinecap) ? LinecapActualDefaultValue : inspectorPanelState.StrokeLinecap);
            _form.SetLinejoinWithoutNotify(string.IsNullOrEmpty(inspectorPanelState.StrokeLinejoin) ? LinejoinActualDefaultValue : inspectorPanelState.StrokeLinejoin);
            _form.DashLengthField?.SetValueWithoutNotify(inspectorPanelState.DashLength);
            _form.DashGapField?.SetValueWithoutNotify(inspectorPanelState.DashGap);
            _form.TransformField?.SetValueWithoutNotify(inspectorPanelState.Transform);
            _form.FrameXField?.SetValueWithoutNotify(inspectorPanelState.FrameX);
            _form.FrameYField?.SetValueWithoutNotify(inspectorPanelState.FrameY);
            _form.FrameWidthField?.SetValueWithoutNotify(inspectorPanelState.FrameWidth);
            _form.FrameHeightField?.SetValueWithoutNotify(inspectorPanelState.FrameHeight);
            _form.TranslateXField?.SetValueWithoutNotify(inspectorPanelState.TranslateX);
            _form.TranslateYField?.SetValueWithoutNotify(inspectorPanelState.TranslateY);
            _form.RotateField?.SetValueWithoutNotify(inspectorPanelState.Rotate);
            _form.ScaleXField?.SetValueWithoutNotify(inspectorPanelState.ScaleX);
            _form.ScaleYField?.SetValueWithoutNotify(inspectorPanelState.ScaleY);
        }

        public void SetTransformText(string transform)
        {
            _form.SetTransformText(transform);
        }

        private static string NormalizePopupValue(string value)
        {
            return string.Equals(value, RemovePlaceholder, System.StringComparison.Ordinal)
                ? string.Empty
                : value ?? string.Empty;
        }

        private static string NormalizeLinecapValue(string value)
        {
            return string.Equals(value, LinecapActualDefaultValue, System.StringComparison.Ordinal)
                ? string.Empty
                : NormalizePopupValue(value);
        }

        private static string NormalizeLinejoinValue(string value)
        {
            return string.Equals(value, LinejoinActualDefaultValue, System.StringComparison.Ordinal)
                ? string.Empty
                : NormalizePopupValue(value);
        }
    }
}
