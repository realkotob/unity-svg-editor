namespace UnitySvgEditor.Editor
{
    internal interface IPatchInspectorHost
    {
        DocumentSession CurrentDocument { get; }

        bool TryApplyPatchRequest(AttributePatchRequest request, string successStatus);
        void UpdateSourceStatus(string status);
    }
}
