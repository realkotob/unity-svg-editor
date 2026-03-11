# Inspector Handoff

## 현재 상태

기준 날짜: 2026-03-11

현재 inspector는 patch tool이 아니라 model-driven editor UI다.

고정된 사실:

- inspector read path는 document model만 사용한다
- drag 중 inspector 값도 transient document model 기준으로 실시간 갱신한다
- edit-time XML patch 경로는 없다
- XML source editor / code inspector는 다시 살리지 않는다

## 현재 UI 구성

현재 inspector 핵심 섹션:

1. `Position`
2. `Layout`
3. `Appearance`
4. `Fill`
5. `Stroke`
6. `Advanced`
7. `Document`

## 현재 남은 inspector 일

정리되지 않은 축:

- `Transform helper`와 직접 입력 충돌 정리
- 타입별 geometry field 확장 여부 판단
- `rect` 계열 corner radius 노출 방식 정리
- 선택 header / 상태 배지 노출 여부 판단

## 유지할 원칙

- 필드 apply는 model mutation 기준
- immediate apply field는 즉시 반영 유지
- target/read/apply patch UI는 다시 드러내지 않는다
- target key는 내부 selection sync 용도로만 유지한다

## 관련 핵심 파일

- [IInspectorPanelHost.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/IInspectorPanelHost.cs)
- [InspectorPanelController.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorPanelController.cs)
- [InspectorTargetSyncService.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorTargetSyncService.cs)
- [InspectorPanelState.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorPanelState.cs)
- [InspectorStateBinder.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/InspectorPanel/InspectorStateBinder.cs)

## 다음 작업 추천

1. `Transform helper` UX 정리
2. 타입별 geometry field 정책 정리
3. direct renderer coverage가 늘어날 때 inspector field도 해당 shape 기준으로 확장
