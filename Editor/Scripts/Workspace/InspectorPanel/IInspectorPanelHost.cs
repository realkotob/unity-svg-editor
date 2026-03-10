namespace UnitySvgEditor.Editor
{
    internal interface IInspectorPanelHost
    {
        DocumentSession CurrentDocument { get; }

        bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus);
        void SyncSelectionFromInspectorTarget(string targetKey);
        void UpdateSourceStatus(string status);
    }
}
