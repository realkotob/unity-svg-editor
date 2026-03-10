using UnityEngine;

namespace UnitySvgEditor.Editor
{
    internal interface IInspectorPanelHost
    {
        DocumentSession CurrentDocument { get; }

        bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus);
        bool TryApplyTargetFrameRect(string targetKey, Rect targetSceneRect, string successStatus);
        bool TryGetTargetSceneRect(string targetKey, out Rect sceneRect);
        void SyncSelectionFromInspectorTarget(string targetKey);
        void UpdateSourceStatus(string status);
    }
}
