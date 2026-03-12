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
  - `SvgEditor.Workspace.Coordination`
  - `SvgEditor.Workspace.Document`
  - `SvgEditor.Workspace.Host`
  - `SvgEditor.Workspace.Canvas`
  - `SvgEditor.Workspace.InspectorPanel`
  - `SvgEditor.Workspace.HierarchyPanel`
  - `SvgEditor.Workspace.AssetLibrary`
  - `SvgEditor.Shell`
- 가장 큰 구조 문제는 이제 namespace 부재가 아니라,
  - 일부 큰 파일
  - 일부 root-level helper의 잔여 배치
  - 물리 폴더와 namespace granularity 불일치
  로 옮겨갔다.
- prefix 밀도는 여전히 높지만, 이제는 “무조건 축소”가 아니라 `semantic floor` 기준으로 다뤄야 한다.

## 2. 가장 큰 구조 hot spot

파일 길이와 책임 기준으로 우선순위를 잡으면 아래가 먼저다.

1. `Workspace`
- `Workspace/Coordination` further rename 여부
- `CanvasViewportLayoutUtility`
- `PanelView`
- `TransformActionService`

2. `Document / DocumentModel`
- `SvgPathGeometryParser`
- `SvgSafeMaskArtifactSanitizer`

3. `Preview / Shell`
- `Preview/Contracts` placement
- `PreviewGeometryLookupService`
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

### 3.3 HierarchyPanel

- `Workspace/HierarchyPanel` 로 물리 폴더와 namespace를 정리했다.
- 내부 타입도 `Hierarchy*`, `Reorder*` 축으로 정리됐다.
- 남은 이슈는 top-level contract인 `HierarchyNode` / `HierarchyOutline` 의 namespace 세분화 여부다.

### 3.4 AssetLibrary

- `Workspace/AssetLibrary` 는 `Browser / Grid / Model / Presentation` 으로 재편됐다.
- 현재 구조는 안정적이고 추가 축소 필요성이 크지 않다.

### 3.5 Preview

- `Preview` 는 `Build / Geometry / Text / Contracts / Research` 로 재편됐다.
- `SnapshotBuilder`, `SnapshotGeometryBuilder`, `SnapshotTextBuilder` 축으로 읽기 경계가 생겼다.
- 추가 정리:
  - `PreviewElementGeometry`, `BoundsQuality` 는 `Geometry` 로 이동
  - `PreviewTextOverlay` 는 `Text` 로 이동
  - empty `Research` catalog 는 제거했다
- 남은 이슈는 `PreviewSnapshot` / `SvgPreserveAspectRatioMode` 의 최종 위치와 `Contracts` 폴더명의 적합성이다.

### 3.6 Workspace Root

- `Workspace` 루트 파일은 `Coordination / Host / Document / Transforms` 로 물리 재배치했다.
- 루트는 이제 folder marker 수준으로 비워진 상태다.
- `Coordination` helper도 `MutationCoordinator`, `SelectionCoordinator`, `ShellBinder` 로 정리했다.
- 남은 이슈는 top-level orchestrator `EditorWorkspaceCoordinator` 이름을 더 줄일지 여부다.

### 3.7 Document Structure

- `Document/Structure` 는 `Hierarchy / Lookup / Xml / Geometry` 로 물리 재배치했다.
- 현재는 namespace를 계속 `SvgEditor.Document` 로 유지 중이다.
- 남은 이슈는 namespace까지 세분화할 실익이 있는지 판단하는 것이다.

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

### Batch 1. Core Structure Realignment

- 대상:
  - `HierarchyPanel`
  - `AssetLibrary`
  - `Preview`
  - `Workspace`
- 목표:
  - 물리 폴더와 역할 경계를 먼저 고정한다.

### Batch 2. Naming Final Pass

- 대상:
  - `Preview/Build`
  - `Preview/Geometry`
  - `Workspace/Coordination`
  - 남은 `Hierarchy*` / `Structure*` contract
- 원칙:
  - semantic floor 유지
  - folder context가 충분할 때만 축소

### Batch 3. Residual Large File Reduction

- `SvgPathGeometryParser`
- `SvgSafeMaskArtifactSanitizer`
- 필요 시 `PanelView`
- 필요 시 `TransformActionService`

## 7. 다음 구현 배치

다음 코드 배치는 아래 둘 중 하나로 잡는다.

1. `PreviewSnapshot` / `SvgPreserveAspectRatioMode` placement final pass
2. `Document/Structure` namespace granularity 검토
3. 남은 large file 후보 정리
