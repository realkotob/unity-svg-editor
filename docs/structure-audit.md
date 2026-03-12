# SVG Editor Structure Audit

- 기준 날짜: 2026-03-13
- 범위: `Editor/Scripts` 전체
- 목적: 네이밍, namespace, 파일 경계, coordinator 책임을 전역 기준으로 정리하고 다음 리팩터링 배치를 고정한다

## 1. 전역 진단

- 현재 `.cs` 파일 `137`개가 모두 `UnitySvgEditor.Editor` 단일 namespace에 들어가 있다.
- prefix 밀도는 `Canvas* 34`, `Svg* 32`, `Inspector* 13`, `Preview* 11`, `Structure* 10` 순이다.
- 폴더가 역할을 설명하고 있는데 타입명도 같은 역할을 반복 설명하는 경우가 많다.
- 따라서 지금은 "폴더 구조는 있는데 코드 레벨 namespace 구조가 없다"가 가장 큰 구조 문제다.

## 2. 가장 큰 구조 hot spot

파일 길이와 책임 기준으로 우선순위를 잡으면 아래가 먼저다.

1. `Workspace`
- `EditorWorkspaceCoordinator`
- `CanvasInteractionController`
- `CanvasViewportLayoutUtility`
- `CanvasElementDragController`
- `CanvasGestureRouter`
- `InspectorPanelView`
- `InspectorTransformActionService`

2. `Document / DocumentModel`
- `SvgDocumentModelMutationService`
- `SvgPathGeometryParser`
- `SvgSafeMaskArtifactSanitizer`

3. `Preview / Shell`
- `PreviewSnapshotGeometryBuilder`
- `SvgEditorWindow`

## 3. Workspace 구조 진단

### 3.1 Canvas

- `Workspace/Canvas`는 controller / gesture / overlay / projector / selection / state / view가 한 폴더에 섞여 있다.
- `CanvasWorkspaceController`는 thin wrapper 성격이 강하고, 장기적으로 merge-back 또는 역할명 rename 후보다.
- `CanvasInteractionController`는 아직도 selection, hover, definition proxy, nudge, zoom HUD까지 다루고 있어 실제 interaction shell 이상을 맡고 있다.
- `CanvasGestureRouter`, `CanvasElementDragController`, `CanvasViewportLayoutUtility`는 각각 충분히 응집도가 있는 편이라 파일명보다 namespace 정리가 먼저다.

### 3.2 InspectorPanel

- `InspectorPanelController`는 scheduler, pending apply, target sync, interactivity gating을 모두 들고 있다.
- `InspectorPanelView`, `InspectorFormControls`, `InspectorStateBinder`는 view 계층인데 같은 폴더에서 service/controller와 섞여 있다.
- `InspectorTargetSyncService`는 coordinator 성격이 남아 있어서 후속 분리가 가능하다.

### 3.3 StructureInspector

- `StructureHierarchyInteractionController`, `StructureReorderSession`, `StructureDropIndicatorPresenter`, `StructureReorderMutationService`는 reorder interaction cluster다.
- `AssetHierarchyListView`, `AssetHierarchyTreeRow`, `AssetHierarchyPreviewRenderer`는 view/preview cluster다.
- 지금은 같은 폴더에 있어 읽기 경계가 흐리다.

## 4. 네이밍 진단

- 현재 단계에서 prefix를 바로 대거 줄이면 검색성과 의미가 동시에 흔들릴 수 있다.
- 이유:
  - namespace가 아직 단일하다
  - 폴더 이동만으로는 코드 검색 경험이 개선되지 않는다
  - internal 타입이라도 cross-folder 참조가 많다
- 따라서 순서는 `namespace 먼저`, `prefix 축소 나중`이 맞다.

### 4.1 축소 후보 원칙

- `UnitySvgEditor.Editor.Workspace.Canvas` 같은 namespace가 먼저 생기면 내부 타입은 `CanvasInteractionController` 대신 `InteractionController` 같은 축소를 검토할 수 있다.
- 다만 아래는 당장은 유지한다.
  - public 타입
  - 검색성이 중요한 top-level shell 타입
  - domain 의미가 강한 `Svg*` 타입

## 5. 이번 세션 제약

- `Workspace/Canvas` 물리 폴더 이동을 먼저 시도했지만, 현재 환경에서는 generated `UnitySvgEditor.Editor.csproj` 경로 재생성이 즉시 따라오지 않아 build green을 유지하기 어려웠다.
- 따라서 물리 이동은 롤백했고, 다음 구조 배치는 경로 이동보다 namespace/code-first 방식으로 진행한다.

## 6. 고정 리팩터링 순서

### Batch 1. Namespace Spine

- 목표:
  - `Workspace.Canvas`
  - `Workspace.InspectorPanel`
  - `Workspace.StructureInspector`
  - `Preview`
  - `Renderer`
  - `Document`
  - `DocumentModel`
  영역 namespace를 먼저 세운다.
- 규칙:
  - 파일 경로는 당장 크게 흔들지 않는다.
  - 타입 이름은 우선 유지한다.
  - batch 끝마다 build + Unity Console Error 확인.

### Batch 2. Prefix Reduction

- namespace가 안정화된 뒤 내부 타입의 redundant prefix를 줄인다.
- 1순위 후보:
  - `CanvasWorkspaceController`
  - `CanvasOverlayController`
  - `CanvasInteractionController`
  - `InspectorPanelController`
  - `StructureHierarchyInteractionController`
- public surface와 contract string은 먼저 유지한다.

### Batch 3. Top-Level Orchestrator Cleanup

- 대상:
  - `SvgEditorWindow`
  - `EditorWorkspaceCoordinator`
  - `DocumentLifecycleController`
- 목표:
  - host adapter
  - selection/workspace sync
  - editor shell binding
  책임을 더 분리한다.

### Batch 4. Large File Reduction

- 대상:
  - `SvgDocumentModelMutationService`
  - `CanvasViewportLayoutUtility`
  - `PreviewSnapshotGeometryBuilder`
  - `InspectorPanelView`
- 원칙:
  - `partial` 금지
  - sibling type 또는 응집된 helper로만 분리
  - 부모 파일 줄 수가 실제로 줄어야 한다

### Batch 5. Physical Folder Realignment

- code namespace가 안정화된 뒤에만 실제 폴더 구조를 재정리한다.
- 이 배치는 generated project refresh가 안정적인 타이밍에서만 수행한다.

## 7. 다음 구현 배치

다음 코드 배치는 `Batch 1. Namespace Spine`의 첫 단계로 잡는다.

- 목표: `Workspace/Canvas` namespace 분리
- 범위: 기존 타입명 유지, namespace와 `using`만 정리
- 이유:
  - prefix 축소보다 리스크가 낮다
  - Canvas cluster가 가장 과밀하다
  - 이후 Inspector/Structure에도 같은 패턴을 적용할 수 있다
