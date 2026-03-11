using Unity.VectorGraphics;
using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal interface IInspectorPanelHost
    {
        DocumentSession CurrentDocument { get; }

        bool TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate);
        bool TryApplyTargetFrameRect(
            string targetKey,
            Rect targetSceneRect,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate);
        bool TryGetTargetSceneRect(string targetKey, out Rect sceneRect);
        bool TryGetTargetParentWorldTransform(string targetKey, out Matrix2D parentWorldTransform);
        bool TryGetCanvasViewportSceneRect(out Rect sceneRect);
        void SyncSelectionFromInspectorTarget(string targetKey);
        void UpdateSourceStatus(string status);
    }
}
