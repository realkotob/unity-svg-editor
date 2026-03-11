# SVG Editor Master Plan

## 현재 상태 메모

- 기준 날짜: 2026-03-11
- 현재 상태:
  - model-driven editor 전환 완료
  - XML 실시간 source-of-truth 구조 제거 완료
  - inspector / structure / canvas interaction cleanup 완료
  - renderer direct scene path 도입 완료
  - model mutation 기반 `Undo/Redo` 도입 완료
  - `Cmd/Ctrl+S` save shortcut 도입 완료
  - save success toast 도입 완료
  - canvas rotate handle / transient preview / commit 도입 완료
  - canvas drag / resize snap modifier 도입 완료
- 현재 남은 일:
  - Unity Vector Image 지원 범위 안에서 renderer supported feature 확대
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

지원 범위 상한:

- 제품 feature는 Unity Vector Image가 처리 가능한 SVG feature 범위를 넘어서지 않는다.
- direct renderer 확장도 Unity Vector Image 상한 안에서만 진행한다.
- Unity Vector Image 바깥의 SVG spec feature는 새 제품 범위로 채택하지 않는다.

## 4. 현재 완료 범위

- document model / loader / serializer
- inspector / structure read path model 전환
- drag / resize transient model session
- style / reorder / save path model 전환
- save 후 history 유지
- `Cmd/Ctrl+Z`, `Cmd/Ctrl+Shift+Z` shortcut
- `Cmd/Ctrl+S` save shortcut
- save success toast
- canvas rotate handle / rotate commit
- canvas move / resize snap modifier
- legacy XML patch 경로 제거
- `AttributePatcher*` 제거
- direct renderer가 common shape / `use` / basic gradient 일부 지원

## 5. 현재 남은 제품 과제

이제 남은 일은 남은 interaction UX polish와 Unity Vector Image 범위 안에서의 feature coverage 확장이다.

우선순위:

1. rotate UX polish
2. snap UX polish
3. complex path command
4. gradient variant
5. `text`
6. `clipPath`
7. `mask`
8. renderer invalidation 최적화

## 6. 구현 원칙

- 새 SVG feature는 fixture-first로 추가한다.
- 새 feature는 테스트 없이 구현하지 않는다.
- unsupported feature는 숨기지 말고 명시한다.
- direct renderer coverage가 불충분하면 renderer 내부 fallback은 허용한다.
- direct renderer coverage 확대는 Unity Vector Image가 실질 지원하는 feature까지만 한다.
- `Undo/Redo` 단위는 edit-time XML patch가 아니라 committed model mutation 기준으로 잡는다.
- rotate / snap은 canvas interaction과 inspector 입력이 같은 규칙을 공유해야 한다.
- 외부 편집 계약에 XML/string fallback을 다시 노출하지 않는다.

## 7. 문서 사용 규칙

- 상위 제품 방향은 이 문서를 기준으로 판단한다.
- 실제 작업 순서는 `docs/svg-model-driven-editor-plan.md`를 따른다.
- inspector 후속 메모는 `docs/inspector-handoff.md`를 참고한다.
