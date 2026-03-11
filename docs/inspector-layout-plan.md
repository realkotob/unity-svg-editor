# Inspector Layout Plan

이 문서는 현재 구현의 상위 계획이 아니라, 초기 inspector 구조를 정리하던 시점의 레이아웃 메모다.
cleanup 단계 기준의 최종 원칙은 아래 두 가지다.

- inspector는 document model 기반 편집 UI다
- XML source editor / code inspector / edit-time XML patch UX는 다시 도입하지 않는다

기준:

- Figma류 인스펙터처럼 빠르게 읽히는 밀도 높은 UI를 유지한다
- SVG 특성에 맞게 섹션 구조를 재배치한다
- 자주 쓰는 값이 위에 오고, 구조/진단/고급 기능은 아래로 내린다
- 초기 버전은 필드를 과도하게 늘리지 않고 단순한 UX를 우선한다

## 1. 설계 원칙

### 1.1 핵심 방향

- Inspector는 "현재 무엇이 선택됐는지"보다 "지금 바로 어떤 값을 바꿀 수 있는지"를 우선한다
- 별도 `Selection` 섹션은 두지 않는다
- 선택 정보는 상단 헤더 한 줄로만 표시한다
- 본문은 바로 편집 가능한 속성 섹션부터 시작한다

### 1.2 섹션 우선순위

위에서 아래 순서는 다음 원칙을 따른다.

1. 거의 매번 만지는 값
2. 캔버스 편집과 직접 연결되는 값
3. 타입별 조건부 값
4. 구조 / 참조 / 문서 정보
5. 진단 / 원시 속성

### 1.3 SVG 전용 정책

- `opacity`는 별도 필드로 유지한다
- `fill-opacity`와 `stroke-opacity`는 초기 UX에서 개별 필드로 노출하지 않는다
- `Fill alpha`와 `Stroke alpha`는 color picker의 alpha로 통합한다
- 내부 저장 시에는 SVG 친화적으로 정규화한다

## 2. 상단 레이아웃

### 2.1 Header

Inspector 최상단에는 선택 정보만 간단히 노출한다.

표시 정보:

- tag name
- `id`
- parent 또는 breadcrumb 요약
- 상태 배지
  - dirty
  - unsupported feature
  - validation error

예시:

- `rect #handle-left`
- `path #cursor-body`
- `g #root-icons`

이 영역은 읽기 전용이며 별도 섹션처럼 크게 차지하지 않는다.

### 2.2 Quick Actions

이 섹션의 초기 제안 중 아래 항목은 현재 기준에서 폐기됐다.

- `Read From Target`
- `Apply Patch`
- `Reset`
- `Validate`

현재 유지되는 문서 액션은 저장 시점의 `Save & Reimport`만이다.
편집 중 변경은 inspector와 canvas가 document model을 직접 갱신하는 흐름을 따른다.

## 3. 본문 섹션 구조

최종 기본 순서는 아래와 같다.

1. `Position`
2. `Layout`
3. `Appearance`
4. `Fill`
5. `Stroke`
6. `Text` 또는 타입별 섹션
7. `Structure`
8. `Reference`
9. `Raw Attributes`

이 순서는 첨부 레퍼런스 이미지의 익숙한 시각 구조를 유지하면서 SVG에 필요한 섹션을 추가한 형태다.

## 4. 섹션 상세

### 4.1 Position

이 섹션은 사실상 SVG의 transform 편집 영역이다.
레퍼런스 이미지의 `Position` 섹션 구조를 유지하되, 의미는 SVG transform 기준으로 해석한다.

포함 항목:

- `X`
- `Y`
- `Rotation`
- 추후:
  - `Scale X`
  - `Scale Y`
  - `Pivot`

초기 버전 정책:

- `Alignment` 계열 버튼은 보류하거나 숨긴다
- SVG 노드 편집에서 일반 UI 레이아웃 alignment 개념은 우선순위가 낮다
- rotation은 숫자 필드와 reset 정도만 우선 지원한다

의미:

- canvas interaction 결과를 숫자로 읽고 미세 보정하는 섹션
- move / rotate의 수치 보정점

### 4.2 Layout

레퍼런스 이미지의 `Layout`은 SVG에서는 `Geometry` 성격으로 사용한다.
초기 UI 라벨은 익숙함을 위해 `Layout`을 써도 되지만, 내부 개념은 geometry다.

포함 항목:

- `W`
- `H`
- 타입별 직접 편집 가능한 geometry 값

타입별 확장:

- `rect`
  - `x`
  - `y`
  - `width`
  - `height`
  - `rx`
  - `ry`
- `circle`
  - `cx`
  - `cy`
  - `r`
- `ellipse`
  - `cx`
  - `cy`
  - `rx`
  - `ry`
- `line`
  - `x1`
  - `y1`
  - `x2`
  - `y2`

초기 버전 정책:

- 공통적으로는 `W`, `H` 중심으로 시작
- 타입별 geometry 필드는 선택 노드 타입이 명확할 때만 노출

주의:

- 모든 SVG 요소에 `corner radius`를 공통으로 두지 않는다
- `corner radius`는 `rect` 계열에서만 보이게 한다

### 4.3 Appearance

가장 상단에 와야 하는 시각 공통 속성 섹션이다.

포함 항목:

- `Opacity`

추후 확장 가능:

- blend mode
- visibility toggle

정책:

- `opacity`는 요소 전체 투명도이므로 color alpha와 분리한다
- `opacity`는 항상 Appearance에 유지한다

### 4.4 Fill

채우기 속성 섹션이다.

포함 항목:

- color picker
- alpha
- visible toggle
- add/remove slot

정책:

- UI에서는 `Fill alpha`만 보여준다
- `fill-opacity` 필드는 별도 노출하지 않는다
- 저장 시 내부적으로 `fill-opacity`로 normalize 가능해야 한다

추후 확장:

- solid color
- gradient binding
- pattern binding

초기 목표:

- "색상 + alpha"만으로 대부분의 실사용 편집을 커버한다

### 4.5 Stroke

외곽선 속성 섹션이다.

포함 항목:

- color picker
- alpha
- visible toggle
- add/remove slot
- `Position`
  - inside / center / outside 유사 UI는 신중히 사용
- `Weight`

추가 속성:

- `linecap`
- `linejoin`
- `dasharray`
- `dashoffset`

정책:

- UI에서는 `Stroke alpha`만 보여준다
- `stroke-opacity`는 별도 필드로 두지 않는다
- stroke 위치 UI는 Unity importer와 실제 SVG 의미 차이가 있으면 나중으로 미룬다

### 4.6 Text

`text` 노드를 선택했을 때만 노출한다.

포함 항목:

- `font-size`
- `font-family`
- `font-weight`
- `text-anchor`
- baseline 계열
- spacing 계열

정책:

- 초기에는 inline text editing보다 속성 편집 중심

### 4.7 Structure

구조형 편집 섹션이다.

포함 항목:

- tag
- `id`
- `class`
- reorder
- visibility
- lock
- group / ungroup

이 섹션은 상단보다 아래쪽에 둔다.
이유는 자주 만지는 값보다 구조 조작 빈도가 낮기 때문이다.

### 4.8 Reference

SVG 특유의 참조 구조를 위한 섹션이다.

포함 항목:

- `defs`
- `symbol`
- `use`
- `clipPath`
- `mask`
- gradient reference

표시 기능:

- 현재 노드가 참조형인지
- 참조 대상이 무엇인지
- jump to source 가능 여부

정책:

- `use`나 참조 노드를 선택한 경우 이 섹션을 위로 끌어올릴 수 있다

### 4.9 Raw Attributes

고급 사용자를 위한 원시 속성 섹션이다.

포함 항목:

- 현재 node attribute list
- raw string
- patch preview diff

정책:

- 기본은 접힘
- 일반 사용자보다 디버깅 / 고급 조작용

## 5. 선택 상태별 노출 규칙

### 선택 없음

노출:

- Header는 문서 기준 정보
- Quick Actions

숨김:

- Fill
- Stroke
- Text
- Structure 세부 편집

### 일반 shape 선택

노출:

- Position
- Layout
- Appearance
- Fill
- Stroke
- Structure

### group 선택

노출:

- Position
- Appearance
- Structure

축소:

- Layout의 직접 geometry 편집은 최소화

### text 선택

노출:

- Position
- Layout
- Appearance
- Fill
- Stroke
- Text
- Structure

### `use` / 참조 노드 선택

노출:

- Position
- Appearance
- Reference
- Structure

정책:

- 이 경우 `Reference` 섹션을 상단 쪽으로 올린다

## 6. 초기 버전 최소 섹션

초기 MVP에서 꼭 필요한 최소 구조는 아래다.

1. Header
2. Quick Actions
3. Position
4. Appearance
5. Fill
6. Stroke
7. Structure
8. Raw Attributes

이 조합이면 제품 로드맵 기준으로 2차와 3차 초반까지 커버 가능하다.

## 7. 값 표현 정책

### 7.1 Opacity 정책

- `Appearance > Opacity`는 SVG `opacity`와 1:1 대응
- 요소 전체 최종 투명도 제어용

### 7.2 Fill alpha 정책

- UI에서는 `Fill color alpha`만 노출
- 저장 시 내부적으로 `fill-opacity`로 정규화 가능해야 함
- raw color에 alpha를 직접 굽는 방식은 우선 피함

### 7.3 Stroke alpha 정책

- UI에서는 `Stroke color alpha`만 노출
- 저장 시 내부적으로 `stroke-opacity`로 정규화 가능해야 함

### 7.4 예외 케이스

기존 SVG가 아래처럼 혼합 상태일 수 있다.

- 색상 자체 alpha 포함
- `fill-opacity` 또는 `stroke-opacity` 별도 존재
- `opacity`까지 동시에 존재

초기 정책:

- Inspector에서는 최대한 단일 alpha로 보인다
- 내부 해석 규칙은 별도 codec에서 정규화한다
- 손실 가능성이 있는 경우 `Raw Attributes`에서 경고한다

## 8. 레퍼런스 이미지와의 대응

첨부된 레퍼런스 이미지 구조는 적극적으로 활용한다.

유지할 것:

- 섹션 구획 방식
- 밀도 높은 숫자 필드 배치
- `Fill` / `Stroke` 분리 구조
- `Appearance` 상단 배치
- 토글 / 추가 / 제거 아이콘 패턴

조정할 것:

- 별도 `Selection` 섹션은 두지 않음
- 별도 `Patch Context` 섹션도 두지 않음
- `Appearance`에는 `Opacity`만 우선 둠
- `Fill`과 `Stroke`는 alpha를 color picker에 통합
- `Corner radius`는 공통 필드가 아니라 타입별 조건부 필드로 이동
- `Effects`는 후순위로 내리고 `Reference`만 필요 시 확장

## 9. 최종 권장 순서

최종 권장 Inspector 순서는 아래다.

1. Header
2. Quick Actions
3. Position
4. Layout
5. Appearance
6. Fill
7. Stroke
8. 타입별 섹션
  - Text
  - Gradient
  - Clip / Mask
9. Structure
10. Reference
11. Raw Attributes

핵심 요약:

- Selection은 헤더로 축소
- Patch Context, Document, Diagnostics 카드는 두지 않음
- Opacity는 Appearance에 유지
- Fill / Stroke opacity는 color alpha로 통합
- Figma형 섹션 구조를 유지하되 SVG 전용 정보 구조로 재배치
