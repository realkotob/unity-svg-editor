# SVG Editor Master Plan

이 문서는 `unity-svg-editor`의 단일 계획 문서다.

- 기준 날짜: 2026-03-12
- 역할:
  - 현재 제품 상태 기록
  - 완료 범위와 남은 우선순위 정리
  - 구현 원칙과 다음 세션 시작 규칙 고정
- 통합 범위:
  - model-driven 전환 계획
  - inspector 레이아웃 후속 메모

## 1. 현재 상태

현재 편집기는 model-driven SVG editor로 전환 완료됐다.

완료된 큰 축:

- edit-time source-of-truth를 `SvgDocumentModel`로 통일
- edit-time XML patch 흐름 제거
- inspector / structure / canvas interaction model-driven 전환
- save-time serialize + validate + import 경로 정리
- model mutation 기반 `Undo/Redo` 도입
- `Cmd/Ctrl+S` save shortcut 도입
- save success toast 도입
- canvas move / resize / rotate interaction 정리
- direct renderer scene path 도입 및 coverage 확장

현재 남은 큰 축:

- unsupported feature 진단 문구와 fallback 노출 정리
- text 계열 세부 편집 정책 정밀화 여부 판단
- renderer invalidation / rebuild 비용 추가 최적화 여부 판단
- inspector transform helper와 직접 입력 UX 충돌 정리

## 2. 제품 정의

`unity-svg-editor`는 Unity 안에서 SVG를 읽고, 구조를 이해하고, 자주 수정하는 속성을 시각적으로 편집하고, 다시 저장하는 실무형 에디터다.

핵심 목표:

- SVG 자산을 열고 구조를 이해한다
- preview와 원본 구조 차이를 진단한다
- canvas에서 선택 / 이동 / 크기 변경 / 회전을 수행한다
- defs / use / gradient / clipPath / mask / text 같은 구조 요소를 다룬다
- 저장 시점에 안정적으로 SVG를 다시 생성한다

## 3. 고정 아키텍처 원칙

현재 기준의 고정 방향:

- edit-time source-of-truth: `SvgDocumentModel`
- save-time interchange format: SVG XML
- live preview contract: model-driven
- inspector / structure / selection contract: model-driven
- save 시에만 serialize + validate + import

중요:

- edit-time XML patch 흐름으로 돌아가지 않는다.
- XML source editor / code inspector는 제품 계획에 없다.
- 외부 편집 계약에 string/XML fallback을 다시 노출하지 않는다.

## 4. 렌더링 방향

목표는 `SVG 요소마다 VisualElement`를 만드는 것이 아니다.

현재 방향:

- document model
- renderer scene builder
- preview snapshot / geometry / hit-test metadata
- overlay / interaction layer

즉, 본문은 전용 renderer가 담당하고, UI Toolkit은 tool chrome / overlay / inspector UI에 집중한다.

지원 범위 상한:

- 제품 feature는 Unity Vector Image가 처리 가능한 SVG feature 범위를 넘어서지 않는다.
- direct renderer coverage 확대도 Unity Vector Image 상한 안에서만 진행한다.
- Unity Vector Image 바깥의 SVG spec feature는 새 제품 범위로 채택하지 않는다.
- coverage가 부족한 feature는 `SvgCanvasRenderer` 내부 fallback으로만 유지한다.

## 5. 현재 완료 범위

### 5.1 Document / Editing Flow

- document model / loader / serializer
- raw attribute / reference 보존
- inspector / structure read path model 전환
- drag / resize transient model session
- style / reorder / save path model 전환
- save 후 history 유지
- `Cmd/Ctrl+Z`, `Cmd/Ctrl+Shift+Z`, `Cmd/Ctrl+S` shortcut
- save success toast

### 5.2 Canvas Interaction

- selection / hover sync
- move / resize commit
- rotate handle / transient preview / commit
- drag / resize snap modifier

### 5.3 Runtime Cleanup

- legacy edit-time XML patch 경로 제거
- `AttributePatcher*` 제거
- preview live/transient refresh의 model contract 정리

### 5.4 Editor Preview / Interaction Support

현재 editor preview / interaction 기준 지원:

- `rect`
- `circle`
- `ellipse`
- `line`
- `polyline`
- `polygon`
- `path`
  - relative command
  - curved path (`C`, `S`, `Q`, `T`)
- `use`
- `linearGradient`
- `radialGradient`
- `clipPath`
- basic `mask`
- `text`
  - editor overlay로 표시
  - 선택
  - move / resize commit

### 5.5 Preview Works, Editing Is Limited

- `path`
  - 현재 direct `d` / anchor / control point editing 대상은 아님
  - move / resize / rotate 같은 transform-based interaction까지만 현재 범위로 본다
- `tspan`
  - 표시 / 선택 가능
  - 개별 편집은 제한적
- `textPath`
  - direct editing target이 아님
- 복합 조합
  - `use + gradient + clipPath`
  - `mask + shape`

주의:

- 현재 `text`는 `VectorImage` native 렌더링 기능으로 취급하지 않는다.
- editor는 model 기반 text overlay로 preview / hit-test / selection 일부를 보강한다.
- 따라서 `text`는 “VectorImage 지원 완료”가 아니라 “overlay-backed editor support”로 본다.

## 6. 남은 제품 과제

우선순위는 아래 순서를 기본으로 잡는다.

1. fallback / unsupported feature 진단 문구 정리
2. rotate / snap UX polish
3. inspector transform helper와 직접 입력 규칙 정리
4. `tspan` / `text-anchor` 세부 정밀도 개선 여부 판단
5. `text`를 overlay 유지 대상으로 둘지, 범위를 더 축소할지 판단
6. renderer invalidation / rebuild 비용 계측 및 최적화 판단
7. Unity Vector Image 범위 안에서 필요한 추가 fixture 기반 coverage 확장
8. `Path editing`
   - direct `d` / anchor / control point editing은 planned scope로만 유지
   - 착수 전 `path` AST / serializer / mutation / overlay hit-test 계층부터 정리

현재 시점에서 `clipPath`, `mask`, gradient 확장은 “이미 들어간 범위를 유지·보강할 대상”이다.
`text`는 같은 축으로 보기보다, overlay-backed support 범위를 어디까지 유지할지 재판단해야 한다.

## 7. Inspector 원칙

현재 inspector는 patch tool이 아니라 model-driven editor UI다.

유지할 원칙:

- 선택 정보보다 편집 가능한 값을 우선한다
- 필드 apply는 model mutation 기준으로 처리한다
- drag 중 inspector 값도 transient document model 기준으로 실시간 갱신한다
- immediate apply field는 즉시 반영 유지한다
- target/read/apply patch UI는 다시 드러내지 않는다
- target key는 내부 selection sync 용도로만 유지한다

현재 권장 섹션 순서:

1. `Position`
2. `Layout`
3. `Appearance`
4. `Fill`
5. `Stroke`
6. 타입별 섹션
7. `Advanced`
8. `Document`

추가로 정리할 결정 항목:

- `Transform helper`와 직접 입력 충돌 정리
- 타입별 geometry field 확장 여부 판단
- `rect` 계열 corner radius 노출 방식 정리
- `text` / gradient / reference 노드용 inspector 섹션 설계 보강
- unsupported feature를 inspector에서 어떻게 표시할지 결정

## 8. Fixture / Test Rule

- 새 renderer 작업은 fixture 없이 시작하지 않는다.
- fixture는 `Assets/Resources/TestSvg/` 아래에 둔다.
- fixture 파일명은 지원하려는 feature가 드러나게 짓는다.
- fixture 추가 후 EditMode 테스트를 먼저 쓴다.
- 최소 검증:
  - snapshot build 성공
  - target key 존재
  - bounds 또는 projection rect 유효
- 새 feature 후보는 Unity Vector Image 지원 범위 안에서만 고른다.

현재 중요한 fixture 예시:

- `defs-use-basic.svg`
- `path-relative-commands.svg`
- `path-curves-basic.svg`
- `text-tspan-basic.svg`
- `clippath-basic.svg`
- `mask-basic.svg`
- `radial-gradient-basic.svg`
- `polyline-polygon-basic.svg`
- `use-gradient-clip-combo.svg`

기본 검증 기준:

- `UnitySvgEditor.Editor.Tests` EditMode green 유지

## 9. 구현 원칙

- 새 feature는 테스트 없이 구현하지 않는다.
- unsupported feature는 숨기지 말고 명시한다.
- fallback이 필요하면 renderer 내부 한정으로만 유지한다.
- 새 fallback을 넣을 때는 direct path로 못 가는 이유를 테스트 또는 fixture와 함께 남긴다.
- `Undo/Redo` 단위는 XML diff가 아니라 committed model mutation 기준으로 잡는다.
- drag / resize / rotate 중 transient preview는 유지하되 history 적재는 interaction commit 시점 한 번만 한다.
- rotate / snap은 canvas interaction과 inspector 입력이 같은 규칙을 공유해야 한다.
- save는 history를 끊지 않고 유지한다.
- toast는 save success처럼 사용자 가치가 높은 완료 피드백에만 쓴다.

## 10. 비목표

- Illustrator / Figma 급 범용 벡터 툴
- 모든 SVG feature 100% authoring
- edit-time XML patch UI 복귀
- `Read From Target`
- `Apply Patch`
- XML source editor / code inspector 복귀

## 11. 다음 세션 시작 규칙

다음 세션은 아래 순서로 시작한다.

1. 남은 제품 과제 하나를 고른다.
2. 해당 기능의 commit / history / preview 규칙을 먼저 고정한다.
3. renderer 작업이면 대응 fixture를 `Assets/Resources/TestSvg/`에 추가하고 EditMode 테스트를 먼저 쓴다.
4. inspector 작업이면 섹션 배치보다 interaction 규칙 충돌부터 먼저 정리한다.
5. 구현 후 `UnitySvgEditor.Editor.Tests` green을 확인한다.

## 12. 관련 문서

- 상태 설명과 사용자용 요약은 `README.md`를 따른다.
- Unity `VectorImage` 지원 경계는 `docs/unity-vectorimage-support.md`를 따른다.
