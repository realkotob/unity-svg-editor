using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;
using SvgEditor.Core.Preview;
using SvgEditor.Core.Preview.Build;
using SvgEditor.UI.AssetLibrary.Browser;
using SvgEditor.UI.Workspace.Coordination;
using SvgEditor.UI.Workspace.Document;
using SvgEditor.UI.Workspace.Host;
using SvgEditor.UI.Inspector;
using SvgEditor.Core.Svg.Model;
using SvgEditor.Core.Svg.Mutation;
using SvgEditor.Core.Svg.Source;
using SvgEditor.Core.Shared;
using SvgEditor.UI.Shell;

namespace SvgEditor.Bootstrap
{
    [MovedFrom(true, sourceNamespace: "SvgEditor.Shell", sourceAssembly: null, sourceClassName: "SvgEditorWindow")]
    public sealed class SvgEditorWindow : EditorWindow, IEditorWorkspaceHost, IPanelHost
    {
        private const string WINDOW_MENU_PATH = "Window/Tools/SVG Editor";

        #region Variables

        private DocumentRepository _documentRepository;
        private SnapshotBuilder _previewSnapshotBuilder;
        private AssetDatabaseVectorImageSourceProvider _vectorImageSourceProvider;
        private AssetBrowser _assetLibraryBrowser;
        private PanelController _inspectorPanelController;
        private LifecycleController _documentLifecycleController;
        private WindowLayoutBinder _layoutBinder;
        private WindowShortcutRouter _shortcutRouter;
        private GeometryLookupService _previewGeometryLookupService;
        private EditorWorkspaceCoordinator _workspaceCoordinator;

        VisualElement IEditorWorkspaceHost.RootVisualElement => rootVisualElement;

        DocumentSession IEditorWorkspaceHost.CurrentDocument
        {
            get
            {
                EnsureInitialized();
                return _documentLifecycleController.CurrentDocument;
            }
        }

        PreviewSnapshot IEditorWorkspaceHost.PreviewSnapshot
        {
            get
            {
                EnsureInitialized();
                return _documentLifecycleController.PreviewSnapshot;
            }
        }

        Image IEditorWorkspaceHost.PreviewImage
        {
            get
            {
                EnsureInitialized();
                return _documentLifecycleController.PreviewImage;
            }
        }

        #endregion Variables

        #region Unity Methods

        [MenuItem(WINDOW_MENU_PATH)]
        public static void Open()
        {
            SvgEditorWindow window = GetWindow<SvgEditorWindow>();
            window.titleContent = new GUIContent("SVG Editor");
            window.minSize = new Vector2(980f, 640f);
            window.Show();
        }

        public SvgEditorWindow()
        {
            EnsureInitialized();
        }

        private void CreateGUI()
        {
            EnsureInitialized();
            RebuildWindowLayout();
        }

        private void OnDisable()
        {
            EnsureInitialized();
            _layoutBinder?.DetachLayout();
            _documentLifecycleController.Dispose();
            _workspaceCoordinator?.Dispose();
        }

        private void OnGUI()
        {
            EnsureInitialized();
            HandleWindowKeyEvent(Event.current);
        }

        #endregion Unity Methods

        #region Help Methods

        private WindowLayoutBinder LayoutBinder => _layoutBinder ??= CreateLayoutBinder();

        private EditorWorkspaceCoordinator WorkspaceCoordinator => _workspaceCoordinator ??= new EditorWorkspaceCoordinator(this);

        private void EnsureInitialized()
        {
            InitializeShell();
        }

        private void InitializeShell()
        {
            EnsureDocumentWorkflowReady();
            EnsureGeometryLookupReady();
            EnsureWindowInteractionsReady();
        }

        private void EnsureDocumentWorkflowReady()
        {
            _documentRepository ??= new DocumentRepository();
            _previewSnapshotBuilder ??= new SnapshotBuilder();
            _vectorImageSourceProvider ??= new AssetDatabaseVectorImageSourceProvider();
            _assetLibraryBrowser ??= new AssetBrowser(_documentRepository, _vectorImageSourceProvider);
            _inspectorPanelController ??= new PanelController(new PanelState());
            _documentLifecycleController ??= new LifecycleController(
                _documentRepository,
                _previewSnapshotBuilder,
                _inspectorPanelController,
                () => WorkspaceCoordinator,
                UpdateEditorInteractivity);
        }

        private void EnsureGeometryLookupReady()
        {
            _previewGeometryLookupService ??= new GeometryLookupService(
                () => _documentLifecycleController.PreviewSnapshot,
                () => WorkspaceCoordinator.SelectedElementKeys);
        }

        private void EnsureWindowInteractionsReady()
        {
            _shortcutRouter ??= CreateShortcutRouter();
        }

        private WindowLayoutBinder CreateLayoutBinder()
        {
            return new WindowLayoutBinder(
                rootVisualElement,
                _assetLibraryBrowser,
                _documentLifecycleController,
                _inspectorPanelController,
                () => WorkspaceCoordinator,
                () => this,
                OnRootKeyDown);
        }

        private WindowShortcutRouter CreateShortcutRouter()
        {
            return new WindowShortcutRouter(
                () => _documentLifecycleController.CurrentDocument,
                () => WorkspaceCoordinator.TryCancelActiveDrag(),
                () => WorkspaceCoordinator.TryDeleteSelectedElements(),
                () => _documentLifecycleController.TryUndo(),
                () => _documentLifecycleController.TryRedo(),
                _documentLifecycleController.SaveCurrentDocument);
        }

        private void RebuildWindowLayout()
        {
            Result<Unit> result = LayoutBinder.RebuildLayout();
            if (result.IsFailure)
            {
                rootVisualElement.Add(new HelpBox(result.Error, HelpBoxMessageType.Error));
            }
        }

        private void HandleWindowKeyEvent(Event currentEvent)
        {
            if (currentEvent == null || currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            if (TryHandleShortcut(currentEvent.keyCode, currentEvent.modifiers))
            {
                currentEvent.Use();
            }
        }

        private bool TryHandleShortcut(KeyCode keyCode, EventModifiers modifiers)
        {
            return _shortcutRouter.TryHandleShortcut(keyCode, modifiers);
        }

        private string ResolveSelectedPatchTargetKey()
        {
            EnsureInitialized();
            return _inspectorPanelController.ResolveSelectedTargetKey();
        }

        bool IEditorWorkspaceHost.TrySelectPatchTargetByKey(string targetKey)
        {
            EnsureInitialized();
            return _inspectorPanelController.TrySelectTargetByKey(targetKey, out _);
        }

        string IEditorWorkspaceHost.ResolveSelectedPatchTargetKey() => ResolveSelectedPatchTargetKey();

        private static string FormatNumber(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        string IEditorWorkspaceHost.FormatNumber(float value) => FormatNumber(value);

        void IEditorWorkspaceHost.RefreshLivePreview(bool keepExistingPreviewOnFailure)
        {
            EnsureInitialized();
            _documentLifecycleController.RefreshLivePreview(keepExistingPreviewOnFailure);
        }

        bool IEditorWorkspaceHost.TryRefreshTransientPreview(SvgDocumentModel documentModel)
        {
            EnsureInitialized();
            return _documentLifecycleController.TryRefreshTransientPreview(documentModel);
        }

        void IEditorWorkspaceHost.RefreshInspector()
        {
            EnsureInitialized();
            _inspectorPanelController.QueueRefreshTargets();
        }

        void IEditorWorkspaceHost.RefreshInspector(SvgDocumentModel documentModel)
        {
            EnsureInitialized();
            _inspectorPanelController.RefreshTargets(documentModel);
        }

        private bool TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode = HistoryRecordingMode.Immediate)
        {
            EnsureInitialized();
            return WorkspaceCoordinator.TryApplyPatchRequest(request, successStatus, recordingMode);
        }

        void IEditorWorkspaceHost.ApplyUpdatedSource(string updatedSource, string successStatus)
        {
            EnsureInitialized();
            _documentLifecycleController.ApplyUpdatedSource(updatedSource, successStatus);
        }

        void IEditorWorkspaceHost.ApplyUpdatedSource(
            string updatedSource,
            string successStatus,
            HistoryRecordingMode recordingMode)
        {
            EnsureInitialized();
            _documentLifecycleController.ApplyUpdatedSource(updatedSource, successStatus, recordingMode);
        }

        private void UpdateEditorInteractivity()
        {
            EnsureInitialized();
            DocumentSession currentDocument = _documentLifecycleController.CurrentDocument;
            bool hasDocument = currentDocument != null;
            bool hasInspectableDocument = currentDocument?.CanUseDocumentModelForEditing == true;

            _documentLifecycleController.UpdateInteractivity();
            _inspectorPanelController.UpdateInteractivity(hasInspectableDocument);
            WorkspaceCoordinator.UpdateStructureInteractivity(hasDocument);
        }

        void IEditorWorkspaceHost.UpdateEditorInteractivity() => UpdateEditorInteractivity();

        private void UpdateSourceStatus(string status)
        {
            EnsureInitialized();
            _documentLifecycleController.UpdateSourceStatus(status);
        }

        void IEditorWorkspaceHost.UpdateSourceStatus(string status) => UpdateSourceStatus(status);

        DocumentSession IPanelHost.CurrentDocument
        {
            get
            {
                EnsureInitialized();
                return _documentLifecycleController.CurrentDocument;
            }
        }

        IReadOnlyList<string> IPanelHost.SelectedElementKeys
        {
            get
            {
                EnsureInitialized();
                return WorkspaceCoordinator.SelectedElementKeys;
            }
        }

        bool IPanelHost.TryApplyPatchRequest(
            AttributePatchRequest request,
            string successStatus,
            HistoryRecordingMode recordingMode) => TryApplyPatchRequest(request, successStatus, recordingMode);

        bool IPanelHost.TryApplyTargetFrameRect(TargetFrameRectRequest request)
        {
            EnsureInitialized();
            return WorkspaceCoordinator.TryApplyTargetFrameRect(request);
        }

        void IPanelHost.ApplyUpdatedSource(
            string updatedSource,
            string successStatus,
            HistoryRecordingMode recordingMode)
        {
            EnsureInitialized();
            _documentLifecycleController.ApplyUpdatedSource(updatedSource, successStatus, recordingMode);
        }

        bool IPanelHost.TryGetTargetSceneRect(string targetKey, out Rect sceneRect)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetTargetSceneRect(targetKey, out sceneRect);
        }

        bool IPanelHost.TryGetCurrentSelectionSceneRect(out Rect sceneRect)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetCurrentSelectionSceneRect(out sceneRect);
        }

        bool IPanelHost.TryGetElementSceneRect(string elementKey, out Rect sceneRect)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetElementSceneRect(elementKey, out sceneRect);
        }

        bool IPanelHost.TryGetRotationPivotParentSpace(string targetKey, out Vector2 parentPivot)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetRotationPivotParentSpace(targetKey, out parentPivot);
        }

        bool IPanelHost.TryGetParentWorldTransform(string targetKey, out Matrix2D parentWorldTransform)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetParentWorldTransform(targetKey, out parentWorldTransform);
        }

        bool IPanelHost.TryGetElementParentWorldTransform(string elementKey, out Matrix2D parentWorldTransform)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetElementParentWorldTransform(elementKey, out parentWorldTransform);
        }

        bool IPanelHost.TryGetViewportSceneRect(out Rect sceneRect)
        {
            EnsureInitialized();
            return _previewGeometryLookupService.TryGetViewportSceneRect(out sceneRect);
        }

        void IPanelHost.SyncSelectionFromInspectorTarget(string targetKey)
        {
            EnsureInitialized();
            WorkspaceCoordinator.SyncSelectionFromInspectorTarget(targetKey);
        }

        void IPanelHost.UpdateSourceStatus(string status) => UpdateSourceStatus(status);

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            EnsureInitialized();
            if (TryHandleShortcut(evt.keyCode, evt.modifiers))
                evt.StopPropagation();
        }

        #endregion Help Methods
    }
}
