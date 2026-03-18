using System;
using System.Collections.Generic;
using UnityEngine;
using SvgEditor.Document;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
{
    internal sealed class PanelState
    {
        private readonly TargetSelectionState _targetSelection = new();

        public PanelState()
        {
            _targetSelection.SetTargets(Array.Empty<PatchTarget>());
            FillColor = Color.black;
            StrokeColor = Color.black;
            StrokeWidth = 1f;
            Opacity = 1f;
            CornerRadius = 0f;
            FillOpacity = 1f;
            StrokeOpacity = 1f;
            DashLength = 4f;
            DashGap = 2f;
            ScaleX = 1f;
            ScaleY = 1f;
            TranslateX = 0f;
            TranslateY = 0f;
            Rotate = 0f;
        }

        public string SelectedTargetKey => _targetSelection.SelectedTargetKey;
        public string SelectedTargetLabel => _targetSelection.SelectedTargetLabel;

        public bool FillEnabled { get; set; }
        public Color FillColor { get; set; }
        public bool StrokeEnabled { get; set; }
        public Color StrokeColor { get; set; }
        public bool StrokeWidthEnabled { get; set; }
        public float StrokeWidth { get; set; }
        public bool OpacityEnabled { get; set; }
        public float Opacity { get; set; }
        public bool CornerRadiusEnabled { get; set; }
        public float CornerRadius { get; set; }
        public bool FillOpacityEnabled { get; set; }
        public float FillOpacity { get; set; }
        public bool StrokeOpacityEnabled { get; set; }
        public float StrokeOpacity { get; set; }
        public string StrokeLinecap { get; set; } = string.Empty;
        public string StrokeLinejoin { get; set; } = string.Empty;
        public bool DasharrayEnabled { get; set; }
        public float DashLength { get; set; }
        public float DashGap { get; set; }
        public bool TransformEnabled { get; set; }
        public string Transform { get; set; } = string.Empty;
        public bool FramePositionEnabled { get; set; }
        public float FrameX { get; set; }
        public float FrameY { get; set; }
        public float FrameWidth { get; set; }
        public float FrameHeight { get; set; }

        public float TranslateX { get; set; }
        public float TranslateY { get; set; }
        public float Rotate { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }

        public void SetTargets(IReadOnlyList<PatchTarget> targets)
        {
            _targetSelection.SetTargets(targets);
        }

        public string ResolveSelectedTargetKey()
        {
            return _targetSelection.ResolveSelectedTargetKey();
        }

        public bool TrySelectTargetByKey(string targetKey, out string label)
        {
            return _targetSelection.TrySelectTargetByKey(targetKey, out label);
        }

        public AttributePatchRequest BuildPatchRequest()
        {
            return PanelPatchBuilder.BuildPatchRequest(this);
        }

        public AttributePatchRequest BuildPatchRequest(PanelView.ImmediateApplyField field)
        {
            return PanelPatchBuilder.BuildPatchRequest(this, field);
        }

        public AttributePatchRequest BuildPatchRequest(PanelView.AttributeAction action)
        {
            return PanelPatchBuilder.BuildPatchRequest(this, action);
        }

        public string BuildTransformFromHelper()
        {
            return PanelPatchBuilder.BuildTransformFromHelper(this);
        }

        public bool TrySyncTransformHelperFromText()
        {
            return PanelAttributeSync.TrySyncTransformHelperFromText(this);
        }

        public void SyncFromAttributes(IReadOnlyDictionary<string, string> attributes, string tagName)
        {
            PanelAttributeSync.SyncFromAttributes(this, attributes, tagName);
        }
    }
}
