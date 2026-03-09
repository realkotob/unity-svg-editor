# SVG Editor Product Roadmap

이 문서는 `unity-svg-editor`의 현재 구현 상태와 별개로, SVG 기능 전체를 기준으로 제품 로드맵을 정리한 기획 문서다.

- 목적: "Unity 안에서 SVG를 읽고, 수정하고, 검수하고, 재사용 가능한 자산으로 관리하는 에디터"의 단계별 확장 범위를 정의한다.
- 기준: SVG 스펙 전체를 무리하게 한 번에 구현하지 않고, 실무 효용과 fidelity를 기준으로 우선순위를 둔다.
- 원칙: source-of-truth는 항상 SVG XML이다.

## 1. 제품 목표

이 프로젝트의 목표는 Illustrator 대체제가 아니라, 아래 범위에 집중한 실무형 SVG 에디터다.

- Unity 내부에서 SVG 자산을 바로 열고 검수할 수 있어야 한다.
- XML 직접 수정 없이 자주 바꾸는 속성을 편집할 수 있어야 한다.
- 구조와 참조 관계를 시각적으로 파악할 수 있어야 한다.
- 편집 후 저장과 reimport를 통해 Unity 결과를 즉시 확인할 수 있어야 한다.
- unsupported feature가 있으면 숨기지 말고 드러내야 한다.

## 2. 범위 원칙

### 우선 구현

- 도형 구조
- 스타일 속성
- transform
- 좌표계 / viewBox / preserveAspectRatio
- text 기본 속성
- defs / use / symbol
- gradient / clipPath / mask
- selection / move / scale / rotate

### 후순위

- filter authoring UI
- SMIL animation
- script / event authoring
- foreignObject
- 완전한 path boolean 편집

## 3. 사용자 가치 기준

로드맵 우선순위는 아래 질문으로 판단한다.

1. 실제 SVG 자산에서 자주 등장하는가
2. XML 직접 수정 없이 툴에서 바꾸고 싶은가
3. Unity 반영 결과 확인과 직결되는가
4. 잘못 수정했을 때 회귀 위험이 큰가

## 4. 단계별 로드맵

### 1차. Inspect / Diagnose Foundation

목표:

- SVG를 "열고 이해하고 진단"하는 단계

핵심 기능:

- SVG 로드, XML 파싱, validate, save, reimport
- asset 목록 / 검색 / 선택
- 구조 트리 표시
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

- SVG가 어떤 구조와 기능으로 구성됐는지 UI에서 파악 가능
- 저장 전 XML 에러를 확인 가능
- unsupported 또는 고난도 feature를 식별 가능

대표 사용자 시나리오:

- "이 SVG가 왜 Unity에서 다르게 보이는지 확인한다"
- "문제 있는 feature가 있는지 먼저 진단한다"

### 2차. Style Patch Editor

목표:

- 실무에서 가장 자주 바꾸는 시각 속성을 빠르게 수정하는 단계

핵심 기능:

- 루트 또는 선택 노드 기준 속성 패치
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

- 간단한 디자인 수정 요청의 다수를 XML 직접 편집 없이 처리 가능
- patch preview와 saved result가 납득 가능한 수준으로 일치

대표 사용자 시나리오:

- "아이콘 색상과 선 굵기를 바꾼다"
- "선 스타일만 바꿔서 변형 없이 결과를 확인한다"

### 3차. Geometry Interaction Editor

목표:

- 캔버스에서 직접 선택하고 이동하고 크기/회전을 조정하는 단계

핵심 기능:

- hover / single select
- selection overlay
- hit test 안정화
- move
- scale
  - 8 handle resize
  - edge resize
  - corner resize
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
- 이동 / 크기 변경 / 회전이 반복되어도 overlay가 안정적

대표 사용자 시나리오:

- "SVG 내부 도형 위치를 약간만 옮긴다"
- "비율 유지한 채 아이콘 일부를 키운다"
- "로고 요소 각도를 조금 보정한다"

### 4차. Structural SVG Features

목표:

- 단순 스타일 편집을 넘어 SVG 구조와 참조 관계를 관리하는 단계

핵심 기능:

- `defs` / `symbol` / `use` 시각화
- 참조 추적과 대상 식별
- gradient 편집
  - linear gradient
  - radial gradient
  - stop color
  - stop offset
- `clipPath` / `mask` 연결 상태 표시와 교체
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
  - baseline 계열
  - spacing 계열

완료 기준:

- 실제 production SVG에서 자주 나오는 구조 요소를 안전하게 파악하고 수정 가능
- 참조형 요소를 편집해도 어떤 영향이 나는지 추적 가능

대표 사용자 시나리오:

- "여러 곳에서 재사용되는 심볼 중 원본을 찾아 수정한다"
- "gradient stop만 바꿔 전체 톤을 맞춘다"
- "클리핑 구조 때문에 잘리는 요소를 추적한다"

### 5차. Production Workflow Layer

목표:

- 에디터를 단일 편집 툴이 아니라 실무 파이프라인 툴로 확장하는 단계

핵심 기능:

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
- external pipeline integration
  - cursor builder
  - theme builder
  - preset exchange format

완료 기준:

- 한 개 파일 편집보다 여러 SVG 자산을 반복 검수/보정하는 흐름에 투입 가능
- 편집 이력과 결과 검증이 가능
- 팀 단위로 preset과 규칙을 재사용 가능

대표 사용자 시나리오:

- "에셋 폴더 전체에서 규칙 위반 SVG를 찾는다"
- "여러 커서 asset에 공통 patch를 적용한다"
- "수정 전후 차이를 비교하고 되돌린다"

## 5. 고급 확장 Backlog

아래는 구현 가치가 있지만 1.0 이전 필수 범위는 아니다.

- path anchor point / bezier handle 편집
- subpath 선택
- path simplify / optimize
- filter inspection 상세화
- filter authoring UI
- SVG animation inspection
- SMIL / CSS animation preview
- `foreignObject` 경고 및 대체 가이드
- accessibility metadata 편집
  - `title`
  - `desc`
  - `aria-*`

## 6. 1.0 기준 제안

`1.0`은 아래 범위를 만족할 때로 본다.

- 1차 전체 완료
- 2차 전체 완료
- 3차 핵심 완료
  - move
  - scale
  - rotate
  - stable selection
- 4차 일부 완료
  - `defs/use`
  - gradient 기본 편집
  - 구조 reorder / visibility / lock

즉, `1.0`은 "실무에서 SVG를 읽고, 자주 바꾸는 속성과 transform을 수정하고, 구조 문제를 진단할 수 있는 수준"이다.

## 7. MVP 기준 제안

가장 먼저 배포 가능한 MVP는 아래 범위다.

- 1차 전체
- 2차 전체
- 3차 일부
  - hover
  - single select
  - move
  - basic scale

이 MVP만 되어도 "Unity 내 SVG 검수 + 빠른 패치 툴"로 가치는 충분하다.

## 8. 구현 우선순위 제안

제품 전체 기준 우선순위는 아래 순서가 적절하다.

1. Inspect / Diagnose Foundation
2. Style Patch Editor
3. Geometry Interaction Editor
4. Structural SVG Features
5. Production Workflow Layer

이 순서를 유지하는 이유는 다음과 같다.

- 진단 없이 편집 기능부터 늘리면 unsupported feature에서 오동작 원인 파악이 어렵다.
- 스타일 편집은 구현 대비 사용자 체감 가치가 크다.
- geometry interaction은 좌표 계약이 흔들리면 회귀가 크므로 foundation 다음에 와야 한다.
- 구조/참조 기능은 단순 스타일 편집보다 늦게 넣어도 실사용 가치가 유지된다.
- workflow 기능은 핵심 편집 안정화 이후에 붙이는 편이 낫다.

## 9. 명시적 비목표

초기 제품 단계에서 아래 목표는 잡지 않는다.

- Illustrator / Figma 급의 범용 벡터 드로잉 툴
- 완전한 SVG 스펙 100% authoring
- 모든 browser-specific SVG behavior 재현
- 복잡한 filter graph visual editor
- full animation studio

## 10. 현재 저장소와의 관계

현재 저장소에는 이미 아래 축의 일부가 들어와 있을 수 있다.

- 문서 로드 / XML 편집 / validate / save
- asset library
- preview
- quick patch
- feature scan
- selection / move / 일부 transform

하지만 이 문서는 현재 구현 상태를 기록하는 문서가 아니다.
이 문서는 "SVG 기능 전체를 기준으로 앞으로 어디까지 갈 것인가"를 정의하는 상위 기획 문서다.

추후 실제 구현 계획 문서는 별도로 분리한다.

- 기술 구현 순서
- phase별 파일 소유권
- 테스트 fixture
- 회귀 체크리스트
- release milestone
