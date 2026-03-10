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

## 의도적으로 남겨둔 것

아래는 이름에 `Patch`가 남아 있어도 지금은 의도된 상태다.

- `PatchTarget`
- `AttributePatchRequest`
- `AttributePatcher`

이건 UI 레이어가 아니라 SVG source patch 도메인 타입이다.

## 아직 안 끝난 것

### 1. Inspector 편집 흐름 완성

지금 UI는 inspector처럼 보이지만, 아직 실제 편집 흐름은 완전히 정리되지 않았다.

결정이 필요한 항목:

- 값 변경 시 즉시 반영할지
- 포커스 아웃 시 반영할지
- 엔터 시 반영할지
- `Build Transform` 버튼은 유지할지

추천 방향:

- `Opacity`
- `Fill Color`
- `Stroke Color`
- `Stroke Width`
- `Line Cap / Line Join`
- `Dash Length / Dash Gap`

위 항목부터 `즉시 반영`으로 바꾸는 것이 좋다.

### 2. 내부 target 모델 정리

UI에서는 target/read/apply를 거의 걷어냈지만 내부는 아직 target 기반 state를 유지한다.
다음 단계에서 아래 중 하나를 선택해야 한다.

- 선택된 element를 자동 target으로 간주하고 내부 모델도 단순화
- 혹은 target 모델은 유지하되 UI는 계속 숨김

현재는 후자 상태다.

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

1. Inspector core field 즉시 반영 정책 확정
2. `Opacity / Fill / Stroke / Stroke Width / Dash / Join`에 대해 실제 source patch 연결
3. `Position` 섹션의 `Transform` helper와 직접 입력 충돌 정리
4. target 기반 내부 상태를 계속 유지할지 축소할지 결정

한 줄 요약:

- UI 정리와 이름 정리는 끝났다
- 다음은 “실제 편집이 언제 반영되는가”를 구현하는 단계다
