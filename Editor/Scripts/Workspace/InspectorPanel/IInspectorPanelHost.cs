namespace UnitySvgEditor.Editor
{
    internal interface IInspectorPanelHost
    {
        DocumentSession CurrentDocument { get; }

        bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus);
        void UpdateSourceStatus(string status);
    }
}
