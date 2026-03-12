# SVG Editor Refactor Contract Inventory

- 기준 날짜: 2026-03-12
- 목적: `unity-guide all fix` 전면 리팩토링 중 유지해야 하는 연결 계약을 명시한다.
- 범위: shell, workspace, canvas, preview, renderer, document lifecycle, naming migration

## 1. Document Session

[`DocumentSession`](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Document/Source/DocumentSession.cs) 는 편집 세션의 단일 진입점이다.

- `OriginalSourceText` 와 `WorkingSourceText` 의 `Ordinal` 비교가 dirty 기준이다.
- `DocumentModel` 과 `DocumentModelLoadError` 는 동시에 소비된다.
- `VectorImageAsset` 는 preview fallback과 asset identity 모두에 연결된다.
- refactor 중에도 `CurrentDocument` 를 받는 consumer는 동일 의미를 유지해야 한다.

## 2. Preview Snapshot

[`PreviewSnapshot`](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Preview/PreviewSnapshot.cs) 은 canvas/inspector/structure가 공유하는 preview contract 이다.

- `PreviewVectorImage` 는 dispose lifecycle 대상이다.
- `DocumentViewportRect` 는 실제 SVG 문서 viewport 이다.
- `ProjectionRect` 는 live/transient refresh 에서 안정적으로 유지되는 resolved viewport 이다.
- `VisualContentBounds` 는 projection 이 비어 있을 때 canvas fallback bounds 로 쓰인다.
- `PreserveAspectRatioMode` 는 projector와 selection visual 계산에 연결된다.
- `Elements` 와 `TextOverlays` 는 hit-test, selection, overlay 표시의 공통 데이터 소스다.
- `CanvasViewportRect` 의 의미는 유지해야 한다. projection 우선, 없으면 visual bounds fallback 이다.

## 3. Preview Element Geometry

[`PreviewElementGeometry`](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Preview/PreviewElementGeometry.cs) 는 selection/hit-test/rotate 의 핵심 계약이다.

- `Key` 는 preview node identity 이다.
- `TargetKey` 는 structure/inspector/canvas selection sync 에 쓰이는 상위 계약이다.
- `VisualBounds` 는 hover/selection box 기준이다.
- `DrawOrder` 는 hit-test 우선순위와 overlay stacking 기준이다.
- `HitGeometry` 는 precise hit-test 용이다.
- `WorldTransform` 과 `ParentWorldTransform` 는 move/resize/rotate delta 환산에 사용된다.
- `RotationPivotWorld` 와 `RotationPivotParentSpace` 는 rotate handle 과 inspector transform helper 모두에 연결된다.
- `IsTextOverlay` 는 text-only overlay 경로 분기에 사용된다.

## 4. Renderer Build Result

[`SvgModelSceneBuildResult`](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Renderer/SvgModelSceneBuildResult.cs) 는 renderer 와 preview builder 사이의 bridge 이다.

- `Scene` 는 Unity Vector Graphics 입력이다.
- `NodeMappings` 의 `(Key, TargetKey)` 쌍은 preview element identity 를 복원하는 유일한 매핑이다.
- `NodeOpacities` 는 geometry/bounds 해석과 visibility 판정에 연결될 수 있으므로 순서와 key 안정성이 중요하다.
- `DocumentViewportRect` 와 `PreserveAspectRatioMode` 는 `PreviewSnapshot` 으로 전달되는 의미를 유지해야 한다.

## 5. Key Naming Contracts

- `LegacyElementKey` 는 아직 transient mutation 과 preview selection 에서 살아 있다.
- `TargetKey` 는 inspector target, structure selection, canvas selection 을 잇는 공통 식별자다.
- [`PatchTarget`](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Document/Patching/PatchTarget.cs) 는 naming 상 legacy 이지만 현재 selection target catalog 의미까지 포함한다.
- 이번 라운드에서는 이름을 바꾸더라도 먼저 adapter 또는 compatibility layer 를 둔다.
- rename 전에 문자열 비교 지점, dictionary key, host interface, docs 용어를 함께 inventory 해야 한다.

## 6. Canvas Host Boundary

[`ICanvasPointerDragHost`](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/ICanvasPointerDragHost.cs) 는 과결합 상태지만 현재 drag pipeline 의 핵심 경계다.

- `RefreshLivePreview`, `TryRefreshTransientPreview`, `ApplyUpdatedSource` 는 mutation 후 preview 갱신 계약이다.
- `RefreshInspector`, `UpdateStructureInteractivity`, `SelectElement`, `SelectFrame` 는 workspace fan-out 계약이다.
- `TryHitTestDefinitionOverlay`, `TryGetSelectedDefinitionProxy`, `SelectDefinitionProxy` 는 definition proxy selection 계약이다.
- 이 인터페이스는 즉시 rename 하지 않고 역할별 분할 전까지 compatibility surface 로 유지한다.

## 7. Refactor Rules For This Project

- `PreviewSnapshot`, `PreviewElementGeometry`, `SvgModelSceneBuildResult`, `DocumentSession` 은 public 이 아니어도 사실상 shared contract 로 취급한다.
- shell 분해, service 분해, Foundation 승격은 가능하지만 위 계약의 의미는 먼저 고정하고 나중에 이동한다.
- SVG domain 의미가 강한 타입은 product 에 남긴다.
- generic interaction, editor shell helper, icon/style/token helper 만 Foundation 승격 후보로 본다.

## 8. Document Lifecycle Boundary

- 현재 document lifecycle 순서는 유지한다.
- 순서:
  1. asset open
  2. raw source load
  3. document model load
  4. preview build
  5. workspace / inspector / structure sync
  6. transient interaction 은 working state 만 갱신
  7. save 시점에만 serialize + validate + import
- `SvgDocumentModel` 은 edit-time source-of-truth 로 유지한다.
- SVG XML 은 save-time interchange format 으로 유지한다.
- transient drag / resize / rotate 는 disk write 를 유발하면 안 된다.
- live preview refresh 가 실패해도 기존 preview 를 보존할 수 있어야 한다.

## 9. Foundation Promotion Gate

- 즉시 Foundation 으로 올리지 않는 대상:
  - SVG domain model
  - document session / preview contract
  - `LegacyElementKey`, `TargetKey`, `PatchTarget` 의미 계층
  - SVG-specific overlay / mutation rule
- local-first 후 승격 후보:
  - pointer drag session shell
  - generic tree path / selection utility
  - inspector section upgrade helper
  - icon attach helper
  - editor chrome semantic token
