# SVG Editor Master Plan

## 현재 상태 메모

- 기준 날짜: 2026-03-11
- 현재 상태:
  - model-driven editor 전환 완료
  - XML 실시간 source-of-truth 구조 제거 완료
  - inspector / structure / canvas interaction cleanup 완료
  - renderer direct scene path 도입 완료
- 현재 남은 일:
  - renderer supported feature 범위 확대
  - fixture-first 검증 체계 강화
  - direct renderer fidelity / 성능 최적화

## 1. 제품 정의

`unity-svg-editor`는 Unity 안에서 SVG를 읽고, 구조를 이해하고, 자주 바꾸는 속성을 시각적으로 수정하고, 다시 저장하는 실무형 에디터다.

핵심 목표:

- SVG 자산을 열고 구조를 이해한다
- Unity 반영 결과와 원본 SVG 차이를 진단한다
- 캔버스에서 선택 / 이동 / 크기 변경 / 회전을 수행한다
- defs / use / gradient / clipPath / mask 같은 구조 요소를 다룬다
- 저장 시점에 안정적으로 SVG를 다시 생성한다

## 2. 아키텍처 상태

현재 기준의 고정 방향:

- edit-time source-of-truth: `SvgDocumentModel`
- save-time interchange format: SVG XML
- live preview contract: model-driven
- inspector / structure contract: model-driven

중요:

- 더 이상 edit-time XML patch 흐름으로 돌아가지 않는다.
- XML source editor / code inspector는 제품 계획에 없다.

## 3. 렌더링 방향

목표는 `SVG 요소마다 VisualElement`를 만드는 것이 아니다.

현재 방향:

- document model
- renderer scene builder
- preview snapshot / geometry / hit-test metadata
- overlay / interaction layer

즉, 본문은 전용 renderer가 담당하고, UI Toolkit은 tool chrome / overlay / inspector UI에 집중한다.

## 4. 현재 완료 범위

- document model / loader / serializer
- inspector / structure read path model 전환
- drag / resize transient model session
- style / reorder / save path model 전환
- legacy XML patch 경로 제거
- `AttributePatcher*` 제거
- direct renderer가 common shape / `use` / basic gradient 일부 지원

## 5. 현재 남은 제품 과제

이제 남은 일은 cleanup이 아니라 feature coverage 확장이다.

우선순위:

1. complex path command
2. gradient variant
3. `text`
4. `clipPath`
5. `mask`
6. renderer invalidation 최적화

## 6. 구현 원칙

- 새 SVG feature는 fixture-first로 추가한다.
- 새 feature는 테스트 없이 구현하지 않는다.
- unsupported feature는 숨기지 말고 명시한다.
- direct renderer coverage가 불충분하면 renderer 내부 fallback은 허용한다.
- 외부 편집 계약에 XML/string fallback을 다시 노출하지 않는다.

## 7. 문서 사용 규칙

- 상위 제품 방향은 이 문서를 기준으로 판단한다.
- 실제 작업 순서는 `docs/svg-model-driven-editor-plan.md`를 따른다.
- inspector 후속 메모는 `docs/inspector-handoff.md`를 참고한다.
