# SVG Editor Canvas Selection Backlog

- 기준 날짜: 2026-03-13
- 범위: `Canvas` 기준 멀티 선택, area select, 그룹 기준 정렬
- 목적: 이후 세션에서 바로 구현 착수할 수 있도록 canvas 선택 기능 백로그를 고정한다.

## 1. 범위

- 포함:
  - canvas 멀티 선택 상태 모델
  - `Shift + 클릭` 선택 그룹 추가
  - `Ctrl/Cmd + 클릭` 선택 그룹 제거
  - drag 기반 area select
  - 다중 선택 시 그룹 bounds 기준 정렬
- 제외:
  - asset grid / icon browser selection 동작
  - 멀티 resize / rotate
  - hierarchy 멀티 선택 시각화
  - inspector의 다중 속성 편집 전면 확장

## 2. 현재 기준선

- 현재 canvas 선택 모델은 사실상 단일 `SelectedElementKey` 전제다.
- 선택/입력 핵심 진입점은 아래 파일들이다.
  - `Editor/Scripts/Workspace/Canvas/Selection/CanvasInteractionSelectionResolver.cs`
  - `Editor/Scripts/Workspace/Canvas/Gestures/GestureRouter.cs`
  - `Editor/Scripts/Workspace/Canvas/Controllers/InteractionController.cs`
  - `Editor/Scripts/Workspace/HierarchyPanel/HierarchyState.cs`
  - `Editor/Scripts/Workspace/InspectorPanel/Actions/TransformPositionActionService.cs`
- 정렬 액션은 현재 단일 target을 canvas 기준으로 정렬하는 구조다.

## 3. 기능 결정 기준

- 클릭 선택:
  - 기본 클릭: replace
  - `Shift + 클릭`: add
  - `Ctrl/Cmd + 클릭`: subtract
- drag 선택:
  - 기본 drag: replace
  - `Shift + drag`: add
  - `Ctrl/Cmd + drag`: subtract
- 정렬 기준:
  - 선택된 요소 각각을 개별 정렬하지 않는다.
  - 선택 그룹의 bounding box 를 canvas에 맞추도록 동일 delta를 적용한다.
- 1차 visual rule:
  - 단일 선택은 기존 selection chrome 유지
  - 다중 선택은 group bounds만 우선 표시
  - 멀티 선택에서는 resize / rotate handle을 노출하지 않는다

## 4. 백로그

### P0

#### BL-01. 캔버스 멀티 선택 상태 모델 도입

- 목표: 단일 `SelectedElementKey` 중심 구조를 `primary selection + selected key set` 구조로 확장한다.
- 완료 기준:
  - primary key 와 selected key set 을 읽고 갱신하는 공용 API가 있다.
  - 기존 단일 선택 흐름이 회귀 없이 유지된다.
- 선행 이유:
  - 이후 click / drag / align / overlay 변경의 기반이다.

#### BL-02. 캔버스 modifier 선택 규칙 반영

- 목표: `Shift + 클릭 = add`, `Ctrl/Cmd + 클릭 = subtract` 규칙을 canvas hit-test 선택에 반영한다.
- 완료 기준:
  - canvas element 클릭 시 replace / add / subtract 가 의도대로 동작한다.
  - 그룹 선택 우선 규칙과 modifier 규칙 충돌이 정리된다.
- 리스크:
  - 기존 `Ctrl/Cmd = group bypass direct select` 규칙과 충돌한다.

#### BL-03. Area Select 드래그 선택 구현

- 목표: 캔버스에서 drag rectangle 로 선택 집합을 만들 수 있게 한다.
- 완료 기준:
  - drag rectangle 이 canvas overlay 에 표시된다.
  - drag 종료 시 hit 된 요소 집합으로 선택이 갱신된다.
  - replace / add / subtract modifier 가 drag 에도 적용된다.
- 비고:
  - 포함 기준은 `bounds overlap` 또는 `fully enclosed` 중 하나로 확정 필요.

#### BL-04. 선택 요소 drag 와 area select gesture 충돌 해소

- 목표: 선택 요소 위 drag 는 move, 빈 공간 drag 는 area select 로 안정적으로 분기한다.
- 완료 기준:
  - selected element drag 와 marquee selection 이 서로 오동작하지 않는다.
  - definition proxy selection 과도 치명적 충돌이 없다.

#### BL-05. 멀티 선택용 selection overlay 확장

- 목표: 다중 선택 시 단일 element box 대신 selection group bounds 를 그린다.
- 완료 기준:
  - 멀티 선택에서 group bounds 가 보인다.
  - 단일 선택과 멀티 선택이 시각적으로 구분된다.
  - 1차 범위에서는 멀티 선택 handle 이 비활성화된다.

#### BL-06. 그룹 기준 정렬 동작 정의 및 구현

- 목표: left / center / right / top / middle / bottom 정렬이 선택 그룹 bounds 기준으로 동작하게 한다.
- 완료 기준:
  - 선택된 각 요소에 동일 delta 가 적용된다.
  - 결과적으로 선택 그룹의 bounds 가 canvas 기준 위치에 정렬된다.
- 비고:
  - “선택 요소끼리 상호 정렬”은 별도 백로그로 분리한다.

#### BL-07. 멀티 정렬용 batch mutation 경로 추가

- 목표: 다중 요소 transform 변경을 한 번의 문서 갱신으로 묶는다.
- 완료 기준:
  - multi-align 이 단일 history record 로 남는다.
  - 중간 상태 없이 한 번에 source 가 갱신된다.

#### BL-08. 테스트 추가

- 목표: selection / drag / align 회귀를 막는다.
- 완료 기준:
  - click replace / add / subtract 테스트가 있다.
  - drag replace / add / subtract 테스트가 있다.
  - multi-align left / center / right / top / middle / bottom 테스트가 있다.
  - 기존 single selection / frame selection 회귀 테스트가 유지된다.

### P1

#### BL-09. hierarchy / inspector 최소 동기화 보강

- 목표: 멀티 선택이 있어도 hierarchy 와 inspector 가 깨지지 않게 최소 대응한다.
- 완료 기준:
  - hierarchy 는 primary selection 기준 포커스를 유지한다.
  - inspector 의 정렬 액션은 멀티 선택에서도 실행 가능하다.
  - 단일 선택 전용 필드는 primary selection 기준으로 계속 동작한다.

#### BL-10. direct select 대체 UX 정리

- 목표: 기존 `Ctrl/Cmd` direct select 제거 후 필요한 bypass UX 를 재정의한다.
- 후보:
  - `Alt + 클릭`
  - depth cycling
  - temporary direct-select mode
- 완료 기준:
  - group 내부 직접 선택 UX 가 문서화되고 테스트 가능 상태로 정리된다.

### P2

#### BL-11. 멀티 resize / rotate 지원

- 목표: group transform handle 기반 멀티 resize / rotate 를 후속 배치로 검토한다.
- 비고:
  - 1차 범위에서는 제외한다.

#### BL-12. hierarchy 멀티선택 시각화

- 목표: hierarchy panel 에서 다중 선택 하이라이트를 표현한다.
- 비고:
  - 1차 범위에서는 primary selection 포커스만 유지한다.

#### BL-13. selection HUD / count 표시

- 목표: 멀티 선택 시 선택 개수와 상태를 HUD 또는 inspector 에 노출한다.

## 5. 권장 작업 순서

1. `BL-01` 멀티 선택 상태 모델
2. `BL-02` modifier 선택 규칙
3. `BL-03` area select
4. `BL-04` gesture 충돌 해소
5. `BL-05` overlay 확장
6. `BL-06` 그룹 기준 정렬
7. `BL-07` batch mutation
8. `BL-09` hierarchy / inspector 최소 동기화
9. `BL-08` 테스트 정리

## 6. 열린 결정사항

1. 기존 `Ctrl/Cmd` direct select 를 완전히 제거할지, 다른 modifier 로 옮길지 결정 필요
2. area select 포함 기준을 `bounds overlap` 로 할지 `fully enclosed` 로 할지 결정 필요
3. definition proxy selection 과 marquee selection 의 우선순위 결정 필요
4. 멀티 선택에서 patch target / inspector target 을 primary selection 하나로 고정할지 결정 필요

## 7. 구현 메모

- 이 문서는 backlog 고정용이다.
- 아직 구현 승인이나 세부 설계 확정 문서는 아니다.
- 실제 착수 시에는 아래 순서로 설계 문서를 다시 좁힌다.
  - modifier 규칙 최종 확정
  - selection set API 확정
  - overlay / gesture 경계 확정
  - mutation / undo batching 방식 확정
