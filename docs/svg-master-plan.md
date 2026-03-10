# SVG Editor Master Plan

## 현재 상태 메모

- 기준 날짜: 2026-03-10
- 현재 상태:
  - document model / loader / serializer 도입 완료
  - inspector / structure read path model 전환 완료
  - drag / resize / style / reorder / save path model-first 전환 완료
  - preview는 renderer draft + import fallback 구조까지 도입 완료
- 현재 남은 일:
  - legacy XML 실시간 편집 경로 제거
  - dead code 정리
  - 문서 정리
  - renderer draft의 fidelity / invalidation 고도화 여부 판단

이 문서는 `unity-svg-editor`의 상위 기준 문서다.

세부 구현 순서와 마이그레이션 단계는 아래 문서를 따른다.

- `docs/svg-model-driven-editor-plan.md`
- `docs/inspector-layout-plan.md`
- `docs/inspector-handoff.md`

이 문서의 역할은 세 가지다.

1. 제품이 어디까지 가야 하는지 정의한다.
2. 아키텍처 방향을 고정한다.
3. 하위 계획 문서가 따라야 할 상위 원칙을 정리한다.

## 1. 제품 정의

`unity-svg-editor`는 범용 벡터 드로잉 툴이 아니다.
Unity 안에서 SVG를 읽고, 진단하고, 편집하고, 다시 저장하는 실무형 에디터다.

핵심 목표:

- SVG 자산을 열고 구조를 이해한다
- Unity 반영 결과와 원본 SVG 사이 차이를 진단한다
- 자주 바꾸는 속성과 구조를 시각적으로 수정한다
- 캔버스에서 선택 / 이동 / 크기 변경 / 회전을 수행한다
- defs / use / gradient / clipPath / mask 같은 구조 요소를 다룬다
- 저장 시점에 안정적으로 SVG를 다시 생성한다

## 2. 최종 아키텍처 방향

이 프로젝트는 더 이상 `편집 중 XML 실시간 source-of-truth`를 목표로 두지 않는다.

최종 방향:

- 편집 중 source-of-truth는 메모리 문서 모델이다.
- drag / resize / style / structure 수정은 모델에 직접 반영한다.
- 화면은 모델 기반 렌더러가 갱신한다.
- 저장 시에만 모델을 SVG XML로 serialize 한다.

정리:

- edit-time source-of-truth: document model
- save-time interchange format: SVG XML

## 3. 렌더링 방향

목표는 `SVG 요소마다 VisualElement를 하나씩 만드는 것`이 아니다.

기본 방향:

- document model
- render cache / invalidation graph
- canvas renderer
- overlay / interaction layer

즉, 본문 렌더링은 전용 렌더러가 담당하고, `VisualElement`는 tool chrome / overlay / inspector UI 중심으로 사용한다.

이유:

- SVG의 `path`, `group`, `transform`, `gradient`, `clipPath`, `mask`, `defs/use`를
  일반 `VisualElement` 트리로 직접 관리하는 것은 비용이 높고 fidelity 리스크가 크다.

## 4. 제품 로드맵

### 1차. Inspect / Diagnose

목표:

- SVG를 읽고 구조와 문제를 파악하는 기본 에디터

범위:

- load / validate / save / reimport
- asset library
- structure tree
- feature scan
- 문서 메타 / unsupported feature 진단

### 2차. Style / Geometry Editing

목표:

- 자주 수정하는 속성과 변형을 시각적으로 편집

범위:

- fill / stroke / opacity / dash 등 스타일 편집
- selection / hover
- move / resize / rotate
- structure panel selection sync

### 3차. Structural SVG Editing

목표:

- 재사용 / 참조 / 구조 요소를 실제 편집 대상으로 확장

범위:

- defs / symbol / use
- gradient
- clipPath / mask
- reorder / visibility / lock / group / ungroup
- text 속성 기본 편집

### 4차. Workflow Layer

목표:

- 팀 단위 반복 작업용 생산성 계층 확보

범위:

- snapping
- marquee / multi-select
- undo / redo
- diff / history
- preset / batch validation

## 5. 구현 원칙

- 편집 중 XML 재생성과 전체 preview rebuild를 기본 전략으로 삼지 않는다.
- interaction은 transient session과 committed model state를 분리한다.
- invalidation은 전체 문서가 아니라 영향 subtree 기준으로 계산한다.
- 저장 시 serialize 결과와 editor state가 항상 일치해야 한다.
- unsupported feature는 숨기지 않고 표기한다.
- fidelity가 불확실하면 기능 확대보다 검증과 fixture 확보를 우선한다.

## 6. 현재 전환 전략

구현은 한 번에 갈아엎지 않는다.

전환 순서:

1. document model 도입
2. loader / serializer 도입
3. inspector / structure 읽기 경로를 모델 기반으로 전환
4. drag / resize를 모델 기반 interaction으로 전환
5. preview renderer를 모델 기반으로 전환
6. save path를 serialize 중심으로 마감
7. legacy XML 실시간 편집 경로 제거

상세 작업표는 `docs/svg-model-driven-editor-plan.md`를 따른다.

## 7. 현재 기준에서 가장 중요한 결론

현재 병목은 `그리기`보다 `재구성`이다.

즉, 아래를 줄이는 것이 핵심이다.

- XML 재파싱
- `document.OuterXml` 재생성
- SVG scene reimport
- preview 전체 rebuild

따라서 이 프로젝트의 장기 해법은 미세 최적화가 아니라
`model-driven editor`로의 구조 전환이다.

## 8. 명시적 비목표

초기 및 중기 단계에서 아래는 목표가 아니다.

- Illustrator / Figma 급 범용 벡터 드로잉
- 모든 SVG 스펙 100% authoring
- full filter graph editor
- full animation studio
- 고급 boolean path authoring

## 9. 문서 사용 규칙

- 제품 방향 판단은 이 문서를 기준으로 한다.
- 구현 순서와 세부 batch는 `docs/svg-model-driven-editor-plan.md`를 기준으로 한다.
- 하위 계획 문서가 이 문서와 충돌하면, 하위 문서가 아니라 이 문서를 먼저 갱신한다.
