using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.UI.Workspace.Coordination;
using SvgEditor.UI.Workspace.Document;
using Core.UI.Extensions;

using SvgEditor;

namespace SvgEditor.UI.Inspector
{
    internal interface IPanelHost
    {
        DocumentSession CurrentDocument { get; }
        IReadOnlyList<string> SelectedElementKeys { get; }

        bool TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate);
        bool TryApplyTargetFrameRect(TargetFrameRectRequest request);
        void ApplyUpdatedSource(
            string updatedSource,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate);
        bool TryGetCurrentSelectionSceneRect(out Rect sceneRect);
        bool TryGetElementSceneRect(string elementKey, out Rect sceneRect);
        bool TryGetTargetSceneRect(string targetKey, out Rect sceneRect);
        bool TryGetRotationPivotParentSpace(string targetKey, out Vector2 parentPivot);
        bool TryGetParentWorldTransform(string targetKey, out Matrix2D parentWorldTransform);
        bool TryGetElementParentWorldTransform(string elementKey, out Matrix2D parentWorldTransform);
        bool TryGetViewportSceneRect(out Rect sceneRect);
        void SyncSelectionFromInspectorTarget(string targetKey);
        void UpdateSourceStatus(string status);
    }
}
