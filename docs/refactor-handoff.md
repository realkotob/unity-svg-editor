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

## 3. 현재 파일 상태 핵심

- `SvgModelSceneBuilder.cs` 는 큰 폭으로 줄었고, traversal coordinator 쪽으로 수렴 중이다.
- `CanvasOverlayController.cs` 는 selection/hover chrome presenter 분리 후 orchestration 위주로 남았다.
- `CanvasInteractionController.cs` 는 definition proxy / live preview policy 분리 후 fan-out 이 줄었다.
- `InspectorTargetSyncService.cs` 는 catalog / transform / patch apply 분리 후 얇아졌다.

## 4. 네이밍 / 상수 규칙

- shared const 는 `unity-guide` 기준대로 `UPPER_SNAKE_CASE` 를 사용한다.
- `SvgTagName` 과 `SvgAttributeName` 는 이 규칙으로 맞춘다.
- local variable 은 타입이 명확하면 `var` 우선이다.
- one-off 문자열은 무리하게 상수화하지 않고, cross-file contract string 만 상수화한다.

## 5. 다음 세션 첫 우선순위

1. `SvgTagName` / `SvgAttributeName` 상수화 패스 마무리
- renderer / preview / canvas / structure 에 남은 literal tag / attribute string 치환
- `UPPER_SNAKE_CASE` 기준 유지

2. `SvgModelSceneBuilder` 잔여 책임 더 축소
- `document viewport / length parse`
- 남아 있는 thin wrapper 제거
- 가능하면 `500`줄 이하 근접

3. Renderer snapshot assemble 중복 재점검
- `SvgCanvasRenderer`
- `PreviewSnapshotBuilder`
- `PreviewSnapshotGeometryBuilder`

4. Canvas 후속
- `CanvasGestureRouter` 추가 축소 여부
- `CanvasInteractionController` 남은 selection/update fan-out 정리

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

- 현재 세션 기준으로 const 이름 변경 중이라, old `SvgTagName` PascalCase 참조가 남아 있으면 우선 그 compile error 를 먼저 정리한다.
- `SvgTagName.CLIP_PATH` 는 XML local-name 기준이 아니라 SVG tag literal (`clipPath`) 용도다.
- `SvgAttributeName.CLIP_PATH` 는 attribute literal (`clip-path`) 용도다.
- `PreviewSnapshotGeometryBuilder.cs` 쪽 사용자가 건드린 회전 pivot 변경은 존중 상태로 유지한다.
