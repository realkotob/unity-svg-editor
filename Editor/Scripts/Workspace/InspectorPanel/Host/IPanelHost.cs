using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using SvgEditor.Document;
using SvgEditor.Workspace.Coordination;
using SvgEditor.Workspace.Document;

using SvgEditor;

namespace SvgEditor.Workspace.InspectorPanel
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
