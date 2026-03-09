# SVG Editor Master Plan

이 문서는 아래 두 문서를 합친 최종 계획 문서다.

- `docs/svg-product-roadmap.md`
- `docs/canvas-implementation-plan.md`

목적은 두 가지를 한 번에 정리하는 것이다.

1. 이 제품이 SVG 기능 전체 기준으로 어디까지 갈 것인가
2. 실제 구현은 어떤 순서로 진행해야 재작업이 적은가

이 문서는 상위 기준 문서로 사용한다.
세부 구현 메모, 실험 노트, 이슈 대응 문서는 이 문서를 기준으로 하위로 분리한다.

## 1. 제품 정의

`unity-svg-editor`는 Illustrator 대체제가 아니라, Unity 안에서 SVG를 다음 목적에 맞게 다루는 실무형 에디터다.

- SVG 자산을 열고 구조를 파악한다
- Unity 반영 결과와 SVG 원본 간 차이를 진단한다
- XML 직접 수정 없이 자주 바꾸는 속성을 편집한다
- 캔버스에서 선택 / 이동 / 크기 변경 / 회전을 수행한다
- 구조와 참조 관계를 안전하게 다룬다
- 반복 자산 검수와 patch workflow를 지원한다

핵심 원칙:

- source-of-truth는 항상 SVG XML이다
- unsupported feature는 숨기지 않고 드러낸다
- canvas interaction보다 좌표계 / viewport / transform 계약이 먼저다
- geometry 재작성보다 transform patch 누적을 우선한다

## 2. 제품 범위

### 우선 범위

- SVG 문서 로드 / validate / save / reimport
- 구조 트리 / feature scan / 진단
- 스타일 속성 편집
- transform 편집
- 캔버스 selection / move / scale / rotate
- viewBox / preserveAspectRatio / viewport fitting
- defs / symbol / use
- gradient / clipPath / mask
- text 기본 속성

### 후순위 범위

- filter authoring UI
- animation authoring
- foreignObject 편집
- 완전한 path node 편집
- illustrator급 범용 벡터 드로잉 기능

## 3. 현재 해석 기준

현재 저장소와 기존 문서 기준으로는 아래 정도까지 진행된 것으로 본다.

- 문서 로드 / XML 편집 / validate / save
- asset library
- preview snapshot pipeline
- feature scan
- 구조 패널
- 단일 선택
- move
- 일부 resize / transform

반대로 아직 최종적으로 닫히지 않았거나 본격 구현 전인 축은 아래다.

- projection / preview rect 계약 안정화
- preserveAspectRatio 실제 지원
- rotate
- resize modifier 완성
- snapping
- marquee / multi-select
- defs/use, gradient, clipPath/mask 편집
- workflow layer

즉, 제품 로드맵 기준으로는 `1차 완료 + 2차 대부분 완료 + 3차 일부 완료` 수준으로 해석한다.

## 4. 최종 제품 로드맵

### 1차. Inspect / Diagnose Foundation

목표:

- SVG를 읽고 이해하고 문제를 진단하는 기본 에디터를 만든다

포함 기능:

- SVG 로드 / XML 파싱 / validate / save / reimport
- asset 목록 / 검색 / 선택
- 구조 트리
- 기본 태그 인식
  - `svg`
  - `g`
  - `path`
  - `rect`
  - `circle`
  - `ellipse`
  - `line`
  - `polyline`
  - `polygon`
  - `text`
  - `defs`
  - `symbol`
  - `use`
- 문서 메타 표시
  - `width`
  - `height`
  - `viewBox`
  - `preserveAspectRatio`
- feature scan
  - `gradient`
  - `clipPath`
  - `mask`
  - `filter`
  - `text`
  - `symbol/use`

완료 기준:

- SVG 구조와 주요 feature를 UI에서 진단 가능
- 저장 전 XML 에러를 확인 가능
- unsupported feature를 식별 가능

### 2차. Style Patch Editor

목표:

- 실무에서 가장 자주 바꾸는 시각 속성을 빠르게 수정한다

포함 기능:

- 루트 / 선택 노드 기준 patch
- 스타일 속성 편집
  - `fill`
  - `stroke`
  - `stroke-width`
  - `opacity`
  - `fill-opacity`
  - `stroke-opacity`
- 선 속성 편집
  - `stroke-linecap`
  - `stroke-linejoin`
  - `stroke-dasharray`
  - `stroke-dashoffset`
- transform 문자열 기본 편집
  - `translate`
  - `scale`
  - `rotate`
- 현재 속성 읽기 / 적용 / 반영 확인

완료 기준:

- 간단한 수정 요청의 다수를 XML 직접 편집 없이 처리 가능
- patch preview와 saved result가 충분히 일치

### 3차. Geometry Interaction Editor

목표:

- 캔버스에서 직접 선택하고 변형할 수 있는 편집기 경험을 완성한다

포함 기능:

- hover / single select
- selection overlay
- move
- scale
  - 8 handle resize
  - edge / corner resize
  - `Shift` uniform scale
  - `Alt/Option` center anchor scale
- rotate
  - rotate handle
  - center pivot rotate
  - 15 degree snap
- structure panel selection sync

완료 기준:

- preview와 commit 결과가 일치
- 작은 요소와 겹친 요소도 합리적으로 선택 가능
- overlay와 actual preview의 위치가 안정적

### 4차. Structural SVG Features

목표:

- SVG의 구조와 재사용 관계를 실제 편집 대상으로 확장한다

포함 기능:

- `defs` / `symbol` / `use` 시각화
- 참조 추적
- gradient 편집
  - linear / radial
  - stop color
  - stop offset
- `clipPath` / `mask` 상태 표시와 교체
- 구조 편집
  - reorder
  - visibility
  - lock
  - group
  - ungroup
- text 속성 편집
  - `font-size`
  - `font-family`
  - `font-weight`
  - `text-anchor`
  - baseline / spacing 계열

완료 기준:

- production SVG의 구조 요소를 안전하게 파악하고 편집 가능
- 참조형 요소 수정 영향 범위를 추적 가능

### 5차. Production Workflow Layer

목표:

- 단일 문서 편집기를 팀 단위 반복 작업용 툴로 확장한다

포함 기능:

- snapping
  - edge snap
  - center snap
  - canvas center snap
- guides / alignment indicator
- marquee select
- multi-select
- group transform
- keyboard UX
  - delete / backspace
  - arrow nudge
  - `Esc` cancel
- undo / redo
- patch history
- diff view
- preset / template export
- batch validation
- asset quality rule check
- pipeline integration

완료 기준:

- 여러 SVG 자산을 반복 검수 / 보정 / 패치하는 workflow에 투입 가능
- 이력과 결과 비교가 가능
- preset과 규칙을 팀 단위로 재사용 가능

## 5. 엔지니어링 구현 전략

제품 단계와 실제 구현 순서는 완전히 같지 않다.
현재 저장소 기준으로는 1차, 2차 기능 일부가 이미 들어와 있어서, 구현은 아래 순서로 가야 한다.

### 구현 원칙

- 작은 단계로 나누고 각 단계마다 fixture와 회귀 체크를 추가한다
- bounds / projection / transform-space 계약이 흔들리면 기능 추가보다 계약 정리를 먼저 한다
- preview와 overlay는 같은 rect source를 사용해야 한다
- drag 중 delta 변환 기준은 interaction 시작 시점에 고정해야 한다
- rotate를 넣기 전에 resize modifier와 transform-space conversion을 먼저 닫는다

## 6. 최종 구현 단계

### 구현 1단계. Foundation Stabilization

이 단계는 제품 3차 이전의 필수 선행 작업이다.

범위:

- `PreviewSnapshot` rect 계약 정리
- preview image rect와 overlay rect source 통일
- drag-start projection 고정
- canvas active source 정리
- fixture 기반 bbox / hit test / projection 회귀 샘플 추가
- negative coordinates / root transform / transformed parent 검증

완료 기준:

- bbox drift와 jump-back 문제가 재현되지 않음
- move delta 계산 기준이 drag 중 바뀌지 않음
- selection overlay와 preview가 안정적으로 일치

### 구현 2단계. Navigation / Viewport Completion

이 단계는 제품 3차의 기반이며, SVG 좌표계 해석을 닫는 단계다.

범위:

- pan / zoom / fit-to-document 정리
- `viewBox` 해석 고정
- `preserveAspectRatio` 최소 지원
- document viewport와 visual content bounds 계약 정리

완료 기준:

- 같은 SVG가 zoom level과 무관하게 일관된 mapping을 가짐
- fit 결과가 항상 동일
- no-`viewBox`, `meet`, negative coordinate 케이스가 안정적

### 구현 3단계. Selection / Resize Completion

이 단계는 제품 3차의 절반 이상을 차지한다.

범위:

- hover / single select 안정화
- resize handle hit test 정리
- 8 handle resize
- edge / corner resize
- `Shift` uniform scale
- `Alt/Option` center anchor scale
- structure panel selection sync 마감

완료 기준:

- tiny / stroke-only / overlap 요소 선택이 납득 가능
- scale preview와 commit 결과가 일치
- selection sync가 깨지지 않음

### 구현 4단계. Rotate / Transform-Space Completion

이 단계가 끝나야 제품 3차가 마감된다.

범위:

- rotate handle
- center pivot rotate
- 15 degree snap
- scene pivot를 SVG transform space로 변환하는 로직 정리
- nested transform 하위 요소 회전 검증

완료 기준:

- rotation preview와 commit 결과가 일치
- 회전 후 재선택 overlay가 안정적
- move / scale / rotate 연속 적용 시 drift 없음

### 구현 5단계. Structural Editing

이 단계는 제품 4차 구현 단계다.

범위:

- `defs` / `symbol` / `use` 시각화와 추적
- gradient 기본 편집
- `clipPath` / `mask` 상태 표시
- 구조 reorder / visibility / lock / group / ungroup
- text 기본 속성 편집

완료 기준:

- 구조형 SVG 자산을 안전하게 편집 가능
- 참조형 요소 편집 시 영향 범위를 설명 가능

### 구현 6단계. Workflow Layer

이 단계는 제품 5차 구현 단계다.

범위:

- snapping 체계
- marquee select
- multi-select transform
- keyboard editing UX
- undo / redo 연결
- diff / history / preset / batch validation

완료 기준:

- 반복 작업 흐름에서 사용할 수 있는 생산성 레이어 확보

## 7. 제품 단계와 구현 단계 매핑

### 제품 1차

- 현재 대부분 확보됨
- 남은 보완:
  - unsupported feature 표기 정교화
  - 메타 / 진단 표현 정리

### 제품 2차

- 현재 대부분 확보됨
- 남은 보완:
  - patch preview / saved result 정합성
  - rotate 문자열과 실제 캔버스 동작의 일치성

### 제품 3차

- 현재 미완료 핵심 단계
- 구현 1~4단계를 순서대로 닫아야 함

### 제품 4차

- 제품 3차 안정화 이후 착수
- 구현 5단계에 해당

### 제품 5차

- 제품 4차 이후 착수
- 구현 6단계에 해당

## 8. 권장 릴리즈 기준

### MVP

- 제품 1차 완료
- 제품 2차 완료
- 제품 3차 일부
  - hover
  - single select
  - move
  - basic scale

### 1.0

- 제품 1차 완료
- 제품 2차 완료
- 제품 3차 완료
  - move
  - scale
  - rotate
  - stable selection
- 제품 4차 일부
  - `defs/use`
  - gradient 기본
  - reorder / visibility / lock

### 1.x 이후

- 제품 4차 확대
- 제품 5차 착수
- 고급 backlog 선별 착수

## 9. 테스트 전략

단계별로 아래를 같이 가져간다.

- fixture SVG 추가
- snapshot rect dump 비교
- hit test 회귀 샘플
- transformed parent 케이스
- negative coordinate 케이스
- no-`viewBox` 케이스
- `preserveAspectRatio` 케이스
- preview와 overlay 일치 확인

원칙:

- 한 단계는 가능하면 2~4개 파일 수준의 작은 변경으로 끝낸다
- fixture 없는 단계는 다음 단계로 넘어가지 않는다
- geometry / projection 회귀가 생기면 기능 추가를 멈추고 계약부터 재점검한다

## 10. 명시적 비목표

초기 제품 단계에서는 아래를 목표로 두지 않는다.

- Illustrator / Figma 급의 범용 벡터 툴
- 모든 SVG 스펙 100% authoring
- full filter graph editor
- full animation studio
- 고급 boolean path 편집기

## 11. 실제 작업 시작 순서

바로 다음 작업은 아래 순서가 맞다.

1. 구현 1단계 Foundation Stabilization
2. 구현 2단계 Navigation / Viewport Completion
3. 구현 3단계 Selection / Resize Completion
4. 구현 4단계 Rotate / Transform-Space Completion
5. 구현 5단계 Structural Editing
6. 구현 6단계 Workflow Layer

즉, 최종 계획의 핵심은 아래 한 줄로 요약된다.

- 제품 로드맵은 `진단 -> 스타일 편집 -> 기하 편집 -> 구조 편집 -> workflow`
- 실제 구현 순서는 `좌표 계약 안정화 -> viewport 완성 -> resize -> rotate -> 구조 -> workflow`
