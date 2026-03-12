# SVG Editor Structure Audit

- 기준 날짜: 2026-03-13
- 범위: `Editor/Scripts` 전체
- 목적: 네이밍, namespace, 파일 경계, coordinator 책임을 전역 기준으로 정리하고 다음 리팩터링 배치를 고정한다

## 1. 전역 진단

- 현재 `.cs` 파일 수는 `138` 이다.
- namespace spine 은 정리된 상태다.
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
- 가장 큰 구조 문제는 이제 namespace 부재가 아니라, 일부 큰 파일과 일부 flat cluster 에 남아 있는 책임 과밀이다.
- prefix 밀도는 여전히 높지만, 이제는 “무조건 축소”가 아니라 `semantic floor` 기준으로 다뤄야 한다.

## 2. 가장 큰 구조 hot spot

파일 길이와 책임 기준으로 우선순위를 잡으면 아래가 먼저다.

1. `Workspace`
- `EditorWorkspaceCoordinator`
- `CanvasViewportLayoutUtility`
- `PanelView`
- `TransformActionService`

2. `Document / DocumentModel`
- `SvgDocumentModelMutationService`
- `SvgPathGeometryParser`
- `SvgSafeMaskArtifactSanitizer`

3. `Preview / Shell`
- `PreviewSnapshotGeometryBuilder`
- `SvgEditorWindow`

## 3. Workspace 구조 진단

### 3.1 Canvas

- `Workspace/Canvas`는 아래 subcluster 로 정리됐다.
  - `Controllers`
  - `Gestures`
  - `Host`
  - `Overlay`
  - `Projection`
  - `Selection`
  - `State`
  - `View`
- naming 1차/2차 축소가 이미 들어갔다.
  - `SelectionKind`
  - `ViewportState`
  - `SceneProjector`
  - `ToolKind`
  - `SelectionHandle`
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
- 현재 `Canvas`는 structure/naming 모두 어느 정도 안정화된 상태로 본다.
- 추가 축소는 `semantic floor`를 넘을 가능성이 높아 후순위다.

### 3.2 InspectorPanel

- `Workspace/InspectorPanel`은 아래 subcluster 로 정리됐다.
  - `Controllers`
  - `Host`
  - `View`
  - `State`
  - `Sync`
  - `Apply`
  - `Actions`
- naming 1차 축소가 들어갔다.
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
- 현재 가장 큰 후속 이슈는 naming 이 아니라 large file reduction 이다.
  - `PanelView`
  - `TransformActionService`

### 3.3 StructureInspector

- namespace spine 은 정리됐지만, 물리 폴더는 아직 flat 하다.
- reorder / view / core cluster 는 여전히 분리 가치가 높다.

## 4. 네이밍 진단

- 현재 단계에서 prefix 축소는 가능하지만 `semantic floor`를 둬야 한다.
- 기준:
  - cluster path + namespace 만으로도 역할이 추정되는가
  - 줄였을 때 generic helper처럼 보이지 않는가
  - unrelated token 오염 없이 안전하게 바꿀 수 있는가

### 4.1 축소 후보 원칙

- 유지:
  - `Svg*` domain core
  - top-level shell / product 타입
  - 축소 시 의미가 흐려지는 타입
- 축소 허용:
  - `Canvas`, `InspectorPanel`, `StructureInspector`, `AssetLibrary` 내부 helper/controller/presenter/state 계층

## 5. 이번 세션 제약

- generated `.csproj` 와 asset database 는 file move/rename 을 즉시 따라오지 않을 수 있다.
- 따라서 file move/rename 뒤에는 Unity refresh/compile 를 먼저 태우고 `dotnet build` 를 다시 보는 것을 기본 규칙으로 둔다.

## 6. 고정 리팩터링 순서

### Batch 1. Top-Level Orchestrator Cleanup

- 대상:
  - `SvgEditorWindow`
  - `EditorWorkspaceCoordinator`
  - `DocumentLifecycleController`
- 목표:
  - host adapter
  - selection/workspace sync
  - editor shell binding
  책임을 더 분리한다.

### Batch 2. Large File Reduction

- 대상:
  - `SvgDocumentModelMutationService`
  - `PreviewSnapshotGeometryBuilder`
  - `CanvasViewportLayoutUtility`
  - `PanelView`
  - `TransformActionService`
- 원칙:
  - `partial` 금지
  - sibling type 또는 응집된 helper로만 분리
  - 부모 파일 줄 수가 실제로 줄어야 한다

### Batch 3. StructureInspector / AssetLibrary Physical Folder Realignment

- `StructureInspector`는 `View / Reorder / Core / State` 정도로 나눌 수 있다.
- `AssetLibrary`는 `Browser / Grid / Preview / Model` 축으로 가볍게 나눌 수 있다.

## 7. 다음 구현 배치

다음 코드 배치는 아래 둘 중 하나로 잡는다.

1. `PanelView` / `TransformActionService` large file reduction
2. `SvgEditorWindow` / `EditorWorkspaceCoordinator` / `DocumentLifecycleController` top-level orchestration 정리
