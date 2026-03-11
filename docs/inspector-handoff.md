# Inspector Handoff

## 목적

이 문서는 `unity-svg-editor`의 최근 인스펙터 정리 작업을 이어받기 위한 handoff 문서다.
핵심은 다음 두 축이다.

- Inspector UI를 patch 툴 느낌에서 실제 inspector 구조로 정리
- Canvas frame / preview image 시각 정렬 이슈 보정

## 최근 관련 커밋

- `fec8fc4` `Polish inspector panel naming`
- `24d9066` `Refine uniform resize behavior`

현재 `git log` 최상단은 `7004171`이지만, 아래 handoff 내용은 인스펙터 축 작업 기준으로 정리한다.

## 현재 상태 요약

### 완료된 것

- Inspector 표면 UXML/USS 명칭을 `patch-*`에서 `inspector-*`로 전환
- `PatchInspector*` / `PatchPanelState*` 백엔드 타입을 `InspectorPanel*` 계열로 전환
- 관련 폴더를 `Editor/Scripts/Workspace/PatchInspector`에서 `Editor/Scripts/Workspace/InspectorPanel`로 이동
- 불필요한 inspector 섹션 제거
  - selection summary 제거
  - target / read / apply 제거
  - structure 카드 제거
  - document / diagnostics 제거
- `workspace-split`을 `TwoPaneSplitView`에서 일반 컨테이너로 전환
- `inspector-panel` 고정 폭 적용
- `canvas-frame`와 preview image rect mismatch 보정
  - `frameHeaderHeight`를 `0f`로 조정
  - frame visual을 content rect 기준으로 그리도록 수정
- `References` 폴더 삭제
- 테마 파일명을 `SvgEditorTheme.tss`로 정리

### 현재 Inspector 구조

현재 디자인 모드 인스펙터는 아래 4개 섹션만 남긴 최소 구조다.

1. `Position`
2. `Appearance`
3. `Fill`
4. `Stroke`

실제 UXML 경로:

- [SvgEditorWindow.uxml](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Resources/UXML/SvgEditorWindow.uxml)

실제 USS 경로:

- [Inspector.uss](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Resources/UI/USS/Inspector.uss)
- [Layout.uss](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Resources/UI/USS/Layout.uss)
- [Canvas.uss](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Resources/UI/USS/Canvas.uss)

백엔드 경로:

- [IInspectorPanelHost.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/IInspectorPanelHost.cs)
- [InspectorFormControls.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorFormControls.cs)
- [InspectorPanelController.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorPanelController.cs)
- [InspectorPanelState.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorPanelState.cs)
- [InspectorPanelStateValueCodec.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorPanelStateValueCodec.cs)
- [InspectorPanelView.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorPanelView.cs)
- [InspectorStateBinder.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorStateBinder.cs)
- [InspectorTargetOption.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorTargetOption.cs)
- [InspectorTargetSelectionState.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorTargetSelectionState.cs)
- [InspectorTargetSyncService.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorTargetSyncService.cs)

## cleanup 기준 메모

cleanup 단계 기준으로 아래 원칙을 고정한다.

- inspector read/update 경로는 document model만 사용한다
- drag 중 inspector 값도 transient document model 기준으로 실시간 갱신한다
- XML source editor / code inspector는 다시 살리지 않는다
- edit-time XML patch 경로는 늘리지 않는다

## 아직 안 끝난 것

### 1. Inspector 편집 흐름

핵심 필드는 model mutation 기준 즉시 반영으로 수렴한다.

- `Opacity`
- `Fill Color`
- `Stroke Color`
- `Stroke Width`
- `Line Cap / Line Join`
- `Dash Length / Dash Gap`

### 2. 내부 target 모델

UI에서 target/read/apply patch 흐름을 다시 드러내지 않는다.
내부 target key는 selection sync와 root/non-root 구분용 상태로만 유지한다.

### 3. 실제 화면 기준 미세 조정

아직 Unity Editor 실화면 기준 검토가 더 필요하다.

확인 포인트:

- `inspector-panel` 고정 폭 `240px`이 적당한지
- `Stroke` 섹션의 `Color + Weight` 폭이 균형적인지
- separator 간격이 과하거나 좁지 않은지
- `Build Transform` 버튼 폭이 적절한지

## Canvas 관련 메모

`canvas-frame`가 preview image보다 약간 크게 보이던 문제는 두 단계로 수정했다.

1. `frameHeaderHeight`를 `24f -> 0f`로 변경
2. `CanvasSceneProjector.UpdateFrameVisual(...)`에서 frame visual 자체를 `contentViewportRect` 기준으로 그리도록 변경

관련 파일:

- [CanvasWorkspaceController.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/CanvasWorkspaceController.cs)
- [CanvasSceneProjector.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/CanvasSceneProjector.cs)

만약 여전히 1px 수준의 시각 차이가 보이면 다음 후보를 의심하면 된다.

- frame border 자체의 시각 효과
- UI Toolkit `ScaleToFit` 렌더링과 rect 계산 차이

그 경우에는 `canvas-frame` border를 별도 overlay box로 분리하는 접근이 더 안전할 수 있다.

## 현재 워크트리 주의사항

현재 handoff 시점 기준으로 워크트리에는 이번 인스펙터 작업과 별개로 다른 변경이 섞여 있다.

예시:

- [PreviewElementHitTester.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Preview/PreviewElementHitTester.cs)
- [CanvasSceneHitTestHelper.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/CanvasSceneHitTestHelper.cs)

그리고 아래 문서 삭제도 현재 워크트리에 남아 있을 수 있다.

- `docs/canvas-implementation-plan.md`
- `docs/svg-editor-performance-optimization-plan.md`
- `docs/svg-product-roadmap.md`

이 변경들은 handoff 시점 기준으로 인스펙터 정리 작업과 별도 축으로 봐야 한다.

## 바로 다음 추천 작업

우선순위는 이 순서가 맞다.

1. runtime inspector read path에서 XML fallback 제거 유지
2. drag/resize transient model과 inspector 실시간 동기 유지
3. `Position` 섹션의 `Transform` helper와 직접 입력 충돌 정리
4. dead code와 `source/patch` 용어 잔존 정리

한 줄 요약:

- inspector는 patch tool이 아니라 model-driven editor UI다
- 다음 작업은 그 전제를 흔드는 fallback과 용어를 정리하는 것이다
