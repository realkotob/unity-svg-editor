using UnityEngine;
using SvgEditor.Workspace.Document;
using Core.UI.Extensions;

namespace SvgEditor.Workspace.Coordination
{
    internal readonly struct TargetFrameRectRequest
    {
        public TargetFrameRectRequest(
            string targetKey,
            Rect targetSceneRect,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate)
        {
            TargetKey = targetKey;
            TargetSceneRect = targetSceneRect;
            SuccessStatus = successStatus;
            RecordingMode = recordingMode;
        }

        public string TargetKey { get; }
        public Rect TargetSceneRect { get; }
        public string SuccessStatus { get; }
        public HistoryRecordingMode RecordingMode { get; }
    }
}
