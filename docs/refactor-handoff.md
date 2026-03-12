# SVG Editor Refactor Handoff

- 기준 날짜: 2026-03-13
- 브랜치: `main`
- 목적: 다음 세션에서 바로 이어서 전면 리팩토링을 진행할 수 있도록 현재 기준선을 고정한다.

## 1. 현재 기준선

- `unity-guide all fix` 전면 리팩토링 흐름으로 진행 중이다.
- 계약 기준선은 `docs/refactor-contract-inventory.md` 를 우선 본다.
- edit-time source-of-truth 는 계속 `SvgDocumentModel` 이다.
- save-time interchange format 은 계속 SVG XML 이다.
- push 는 사용자 요청 시에만 수행한다.

## 2. 이번 세션까지 완료된 큰 축

- Shell / editor helper 공용화
  - `SvgEditorWindow` 의 theme / inspector section / icon helper 일부를 공용 editor helper 로 정리
- Document / lifecycle 분리
  - `DocumentRepository`
  - `SvgAssetPathResolver`
  - `SvgDocumentSourceService`
  - `DocumentWorkspaceSyncService`
- Inspector 분리
  - `InspectorTargetCatalogService`
  - `InspectorTransformActionService`
  - `InspectorPatchApplyService`
  - `InspectorTargetSyncService` 는 coordinator 수준으로 축소
- Structure reorder 분리
  - `StructureReorderSession`
  - `StructureDropIndicatorPresenter`
  - `StructureReorderMutationService`
  - `StructureHierarchyController` thin wrapper 제거
- Canvas 분리
  - `CanvasViewportLayoutUtility`
  - `CanvasSelectionVisualBuilder`
  - `CanvasResizeMath`
  - `CanvasDefinitionOverlayPresenter`
  - `CanvasTextOverlayPresenter`
  - `CanvasSelectionChromePresenter`
  - `CanvasInteractionSelectionResolver`
  - `CanvasDefinitionProxyCoordinator`
- Renderer / preview shared parsing 분리
  - `SvgTransformParser`
  - `SvgAttributeUtility`
  - `SvgInheritedAttributeResolver`
  - `SvgNodeLookupUtility`
  - `SvgPathGeometryParser`
  - `SvgShapeStyleBuilder`
  - `SvgPrimitiveShapeBuilder`
  - `SvgShapeBuilder`
  - `SvgReferenceSceneBuilder`
- Namespace spine 정리
  - `SvgEditor.Document`
  - `SvgEditor.DocumentModel`
  - `SvgEditor.Shared`
  - `SvgEditor.Preview`
  - `SvgEditor.Renderer`
  - `SvgEditor.RenderModel`
  - `SvgEditor.Workspace`
  - `SvgEditor.Workspace.Document`
  - `SvgEditor.Workspace.Canvas`
  - `SvgEditor.Workspace.InspectorPanel`
  - `SvgEditor.Workspace.StructureInspector`
  - `SvgEditor.Workspace.AssetLibrary`
  - `SvgEditor.Shell`
- UXML namespace 정리
  - `SvgEditorWindow.uxml` 에서 `svg`, `canvas`, `asset`, `structure` alias 기준선 정리
- Canvas 구조 재편
  - `Workspace/Canvas` 를 `Controllers / Gestures / Host / Overlay / Projection / Selection / State / View` 로 재편
- Canvas naming 1차/2차 축소
  - 유지한 semantic floor:
    - `SelectionKind`
    - `ViewportState`
    - `SceneProjector`
    - `ToolKind`
    - `SelectionHandle`
    - `SelectionVisualBuilder`
    - `InteractionController`
    - `OverlayController`
    - `WorkspaceController`
    - `DefinitionProxyCoordinator`
    - `ElementDragController`
    - `GestureRouter`
    - `PointerDragController`
    - `ElementGestureHandler`
    - `ViewportGestureHandler`
    - `DefinitionOverlayBuilder`
    - `DefinitionOverlayPresenter`
    - `SelectionSyncService`
    - `SelectionChromePresenter`
    - `PolylineOverlayElement`
- InspectorPanel 구조 재편
  - `Workspace/InspectorPanel` 을 `Controllers / Host / View / State / Sync / Apply / Actions` 로 재편
- InspectorPanel naming 1차 축소
  - `PanelController`
  - `IPanelHost`
  - `PanelState`
  - `PanelStateValueCodec`
  - `PanelView`
  - `PatchApplyService`
  - `TransformActionService`
  - `TargetCatalogService`
  - `TargetSyncService`
  - `TargetOption`
  - `TargetSelectionState`
  - `DocumentModelReader`
  - `FormControls`
  - `StateBinder`

## 3. 현재 파일 상태 핵심

- `.cs` 파일 수는 `138` 기준이다.
- namespace는 이제 role-based spine 으로 정리된 상태다.
- `Canvas` 와 `InspectorPanel` 은 folder + namespace + naming 1차 정리가 끝난 상태다.
- `SvgModelSceneBuilder.cs` 는 traversal coordinator 성격으로 수렴했고 `218`줄 수준이다.
- 가장 큰 후속 분해 후보:
  - `SvgDocumentModelMutationService.cs`
  - `CanvasViewportLayoutUtility.cs`
  - `PanelView.cs`
  - `TransformActionService.cs`
  - `PreviewSnapshotGeometryBuilder.cs`
  - `SvgEditorWindow.cs`
  - `EditorWorkspaceCoordinator.cs`

## 4. 네이밍 / 상수 규칙

- shared const 는 `unity-guide` 기준대로 `UPPER_SNAKE_CASE` 를 사용한다.
- `SvgTagName` 과 `SvgAttributeName` 는 이 규칙으로 맞춘다.
- local variable 은 타입이 명확하면 `var` 우선이다.
- one-off 문자열은 무리하게 상수화하지 않고, cross-file contract string 만 상수화한다.
- prefix 축소는 `semantic floor` 를 둔다.
  - 줄였을 때 처음 보는 사람이 역할을 바로 추정할 수 있어야 한다.
  - 예: `Tool`, `Handle` 은 과도하게 짧으므로 `ToolKind`, `SelectionHandle` 쪽에서 멈춘다.
- substring 기반 대량 치환은 금지에 가깝게 본다.
  - `Tooling`, `Toolbar`, `Tooltip`, `Handler`, `TestTools` 같은 unrelated token 을 오염시키기 쉽다.
  - symbol rename 또는 단어 경계 기반 치환만 허용한다.

## 5. 다음 세션 첫 우선순위

1. InspectorPanel final pass
- `PanelView.cs` / `TransformActionService.cs` 중심으로 큰 파일 분해
- `semantic floor` 기준 재점검

2. Top-level orchestrator 정리
- `SvgEditorWindow.cs`
- `EditorWorkspaceCoordinator.cs`
- `DocumentLifecycleController.cs`

3. Large file reduction
- `SvgDocumentModelMutationService.cs`
- `PreviewSnapshotGeometryBuilder.cs`
- `CanvasViewportLayoutUtility.cs`

4. 선택 후속
- `StructureInspector` naming final pass
- `AssetLibrary` naming final pass
- 필요 시 `Document/Structure` 하위 재분류 검토

5. 손대지 말 것
- `Svg*` domain prefix 는 무리하게 전역 축소하지 않는다.
- `CanvasStageView` 같은 `[UxmlElement]` 타입은 rename/move와 UXML alias 변경을 같은 배치로만 처리한다.

## 6. 검증 규칙

- 매 배치 후:
  - `dotnet build UnitySvgEditor.Editor.csproj -nologo`
  - Unity Console Error 확인
- Console 은 stale error 를 들고 있을 수 있으므로:
  - 필요 시 `clear`
  - `refresh_unity compile=request`
  - 재조회
- 사용자 수정 파일은 절대 revert 하지 않는다.

## 7. 주의사항

- generated `.csproj` 와 asset database 는 file move/rename 을 즉시 따라오지 않을 수 있다.
  - file move/rename 뒤에는 `refresh_unity compile=request` 또는 동등한 Unity refresh 를 먼저 태우고 나서 `dotnet build` 를 다시 본다.
- `InternalsVisibleTo` 는 namespace가 아니라 assembly 이름 기준이다.
  - namespace rename 때 기계적으로 같이 바꾸지 않는다.
- `SvgTagName.CLIP_PATH` 는 XML local-name 기준이 아니라 SVG tag literal (`clipPath`) 용도다.
- `SvgAttributeName.CLIP_PATH` 는 attribute literal (`clip-path`) 용도다.
- `PreviewSnapshotGeometryBuilder.cs` 쪽 사용자가 건드린 회전 pivot 변경은 존중 상태로 유지한다.
- backlog:
  - inspector `FlipHorizontal` / `FlipVertical` 는 회전이 없는 경우는 동작하지만, 회전 후 flip 에서 여전히 절대축 기준으로 뒤집히는 증상이 남아 있다.
  - 이번 세션의 회전축 보정 시도는 원인 해결에 실패해서 코드 반영 없이 되돌렸다.
  - 다음 착수 때는 ad-hoc transform prepend 수정부터 하지 말고, fixture + EditMode test 로 `rotate + flip` 기대 계약을 먼저 고정할 것.
