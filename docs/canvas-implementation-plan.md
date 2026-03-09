# Canvas 구현 계획

## 1. 문서 목적

이 문서는 `bounding box` 단일 이슈 대응 문서가 아니다.
목표는 `unity-svg-editor`의 canvas를 실제 편집기로 성립시키기 위한 구현 기준을 정하는 것이다.

이번 계획은 아래 원칙을 따른다.

- 기능은 작은 단위로 쪼개고 각 단계마다 테스트 가능한 상태로 끝낸다.
- SVG 편집기의 핵심은 `move`, `scale`, `rotate`를 정확한 좌표계와 hit test 위에서 제공하는 것이다.
- `viewBox`, `preserveAspectRatio`, `transform` 같은 SVG 규약을 canvas 상호작용보다 먼저 존중해야 한다.
- transform은 가능하면 원본 geometry를 다시 쓰지 않고 `transform` patch 중심으로 누적한다.

## 2. 웹 리서치 요약

이번 계획은 아래 레퍼런스를 기준으로 기능 우선순위를 정했다.

### 2.1 편집기 UX 레퍼런스

- Figma
  - 기본 툴은 `Move`, 임시 `Hand`는 `Space`로 진입한다.
  - resize는 반대편 corner/edge를 anchor로 삼고, `Shift`로 비율 고정, `Alt/Option`으로 center anchor resize를 제공한다.
  - rotate는 선택 외곽에서 시작하고 `Shift`로 각도 snap을 제공한다.
  - rotation origin을 별도로 이동할 수 있다.
- Inkscape
  - selector 하나로 `move`, `scale`, `rotate`, `skew`를 수행한다.
  - 회전 중심점이 이동 가능하다.
  - marquee multi-select가 기본이다.
- tldraw
  - selection/transform을 별도 상태 머신으로 운영한다.
  - snapping은 `bounds`, `handle`, `gap`으로 분리한다.
  - snap threshold는 screen pixel 기준으로 두고 zoom에 따라 canvas unit으로 환산한다.
- Konva
  - resize 시 width/height를 직접 바꾸기보다 `scaleX`, `scaleY` transform으로 처리하는 모델을 제공한다.

### 2.2 SVG 규약 레퍼런스

- MDN `viewBox`
  - `viewBox`는 user space에서 viewport로 매핑되는 작업 좌표계를 정의한다.
- MDN `preserveAspectRatio`
  - `viewBox`와 실제 표시 영역의 비율이 다를 때 정렬과 `meet/slice` 정책을 결정한다.
- MDN `getBBox()`
  - bbox는 현재 SVG space 기준이며 부모/자신의 transform을 반영하지 않는다.
- MDN `getCTM()`
  - local 좌표를 viewport 좌표계로 보내는 transform matrix를 제공한다.
- MDN `transform`
  - SVG 2에서는 root 포함 모든 element에서 transform이 가능하다.

이 조합이 의미하는 바는 명확하다.

- canvas는 `raw bbox` 하나만으로 동작하면 안 된다.
- editor 내부에는 `document viewport`, `world-space visual bounds`, `hit geometry`, `transform matrix`가 분리되어 있어야 한다.
- 구현 우선순위는 `정확한 좌표계 -> selection -> move -> scale -> rotate -> snapping` 순이어야 한다.

## 3. 현재 저장소 상태

현재 코드 기준 canvas는 이미 일부 기능이 있다.

- navigation
  - pan / zoom / reset-to-fit
  - frame move / frame resize
- element interaction
  - single selection
  - move
  - resize
  - selection overlay
- preview pipeline
  - SVG import 후 `PreviewSnapshot`
  - per-element triangle hit geometry 생성

하지만 편집기 관점에서 아직 비어 있는 부분이 많다.

- `CanvasTool`은 사실상 `Move`만 있다.
- rotate 기능이 없다.
- multi-select / marquee selection이 없다.
- rotation pivot이 없다.
- snapping은 현재 selection center guide 수준이며 일반적인 edge/center snapping 체계가 없다.
- bounds/viewport/selection rect 계약이 약하다.
- `preserveAspectRatio`를 포함한 viewport fitting 정책이 문서화되어 있지 않다.
- transform interaction을 단계별로 검증하는 테스트 시나리오가 없다.

즉, 지금 상태는 "canvas가 조금 움직이는 상태"이지 "에디터형 canvas"는 아니다.

## 4. 제품 관점의 필수 기능

### 4.1 V1 필수

이건 없으면 canvas가 제대로 된 편집기로 보이지 않는 기능이다.

- pan / zoom / fit-to-view
- 단일 요소 선택
- hover hit test
- move
- scale
- rotate
- selection overlay와 handle
- transform preview와 commit 일치
- keyboard modifier 기반 constraint
- snap guide
- 구조 패널 선택과 canvas 선택 동기화
- undo/redo와 충돌하지 않는 source patch 적용

### 4.2 V1.5 권장

- marquee selection
- multi-select transform
- duplicate drag
- flip horizontal / vertical
- nudge by arrow key
- ruler / grid / toggleable snap
- fit-to-content

### 4.3 후순위

- skew / free transform
- node edit
- bezier handle edit
- text inline edit
- touch gesture 최적화
- collaboration

## 5. Canvas 상호작용 명세

## 5.1 Navigation

- 기본 툴은 `Move/Select`
- `Space + drag`는 임시 hand pan
- `Wheel`은 zoom
- `Cmd/Ctrl + 0`은 fit-to-view
- `Cmd/Ctrl + 1`은 100% zoom

fit 정책은 두 가지로 분리한다.

- `Fit to Document`
  - `viewBox` 기반
  - 문서 전체 작업 영역을 보는 기본 동작
- `Fit to Content`
  - 실제 visual bounds 기반
  - 후순위 기능

초기 구현은 `Fit to Document`를 canonical 동작으로 둔다.

## 5.2 Selection

- 단일 클릭으로 topmost selectable element 선택
- 빈 공간 클릭으로 selection clear
- 선택 시 overlay 표시
- hover 대상과 selected 대상을 시각적으로 구분

V1 selection overlay 구성:

- bounding box
- 8개 resize handle
- 1개 rotate handle
- size badge
- optional center/pivot marker

V1에서는 single-select만 먼저 안정화한다.
multi-select는 별도 단계로 분리한다.

## 5.3 Move

- drag 시작점 기준으로 selected element를 이동
- transform preview는 실시간 반영
- commit 시 source XML에 `translate(...)`를 prepend

constraint:

- `Shift`를 누르면 dominant axis lock
- snap 대상이 있으면 edge/center 기준으로 보정

## 5.4 Scale

- resize handle drag로 scale
- 기본 anchor는 반대편 edge/corner
- `Shift`는 비율 유지
- `Alt/Option`은 center anchor scale
- `Shift + Alt/Option`은 center 기준 uniform scale

SVG patch 정책:

- geometry attribute를 직접 rewrite하지 않는다.
- 우선은 `scale(sx sy)` + pivot 보정 형태의 transform prepend를 canonical 경로로 둔다.
- 이후 shape-specific numeric resize는 별도 최적화로 본다.

## 5.5 Rotate

- rotate handle drag로 회전
- 기본 pivot은 selection center
- `Shift`는 15도 단위 snap

V1 rotate commit 정책:

- source XML에 `rotate(angle cx cy)`를 prepend
- `cx`, `cy`는 world-space pivot을 element local transform chain에 맞게 변환한 값으로 저장하거나,
  내부적으로는 scene pivot을 유지하고 patch 단계에서 해당 element 기준 좌표계로 환산한다.

권장:

- V1에서는 회전 pivot 이동 없이 center pivot만 지원
- pivot 이동은 rotate가 안정화된 뒤 Phase 5b로 분리

## 5.6 Snapping

V1 snapping 범위:

- canvas/document center snap
- sibling edge snap
- sibling center snap
- selection edge snap during resize
- selection center snap during move

정책:

- snap threshold는 screen pixel 기준으로 유지
- zoom이 커질수록 scene unit threshold는 줄어들어야 한다
- snap line은 실시간으로 시각화한다

V1 제외:

- gap snapping
- distribution snapping
- arbitrary point snapping

## 5.7 Keyboard / precision

V1 최소 범위:

- `Delete/Backspace`: selection clear 또는 이후 삭제 기능과 연결 준비
- arrow key: 1px nudge
- `Shift + arrow`: 10px nudge
- `Esc`: 현재 drag/transform 취소

정밀 numeric input은 초기에 inspector에 둔다.
canvas 위 inline numeric HUD는 후순위다.

## 6. SVG 편집기 특화 규칙

canvas를 일반 2D editor처럼만 만들면 안 되는 이유는 SVG가 별도 좌표계 규약을 가지기 때문이다.

### 6.1 Viewport contract

editor는 아래 rect를 분리해야 한다.

- `DocumentViewportRect`
  - `viewBox` 또는 importer viewport
- `VisualContentBounds`
  - 실제 그려진 전체 도형 bounds
- `ElementVisualBounds`
  - 개별 요소의 world-space bounds
- `HitGeometry`
  - pointer 판정용 geometry

기본 원칙:

- fit-to-view는 `DocumentViewportRect`
- selection은 `ElementVisualBounds`
- hit test는 `HitGeometry`

### 6.2 PreserveAspectRatio

projection layer는 `preserveAspectRatio`를 무시하면 안 된다.

최소 요구사항:

- `none`
- `xMidYMid meet` 기본값
- 주요 align 값 처리

초기 구현이 어려우면 적어도 아래를 명시적으로 제한해야 한다.

- V1 지원: `none`, `xMidYMid meet`
- 그 외 값은 fallback 처리 + debug warning

### 6.3 Bounds policy

`getBBox()` 의미 그대로의 local bbox와 editor에서 필요한 world-space visual bounds는 다르다.
따라서 canonical bounds는 triangle/world transform 기반으로 관리한다.

정책:

- triangle 기반 bounds 성공 시 canonical
- 실패 시 fallback bounds
- 각 element는 bounds quality를 가진다

권장 상태:

- `Exact`
- `Fallback`
- `Unknown`

## 7. 내부 아키텍처 계획

## 7.1 Snapshot layer

책임:

- SVG import
- scene tree와 world transform 계산
- per-element visual bounds / hit geometry 생성
- viewport contract 계산

권장 DTO:

```csharp
internal sealed class PreviewSnapshot
{
    public Rect DocumentViewportRect { get; set; }
    public Rect VisualContentBounds { get; set; }
    public IReadOnlyList<PreviewElementGeometry> Elements { get; set; }
}

internal sealed class PreviewElementGeometry
{
    public string Key { get; set; } = string.Empty;
    public Rect VisualBounds { get; set; }
    public IReadOnlyList<Vector2[]> HitGeometry { get; set; } = Array.Empty<Vector2[]>();
    public BoundsQuality BoundsQuality { get; set; }
    public Matrix2D WorldTransform { get; set; }
}
```

## 7.2 Projection layer

책임:

- scene <-> viewport point 변환
- rect 변환
- frame content rect 계산
- zoom / pan / fit 계산

비책임:

- element hit test
- transform patch 생성

중요:

- `CanvasProjectionMath`는 `DocumentViewportRect`와 `preserveAspectRatio`를 기준으로 다시 정리해야 한다.
- 현재처럼 `width` 기반 단일 scale 가정은 장기적으로 위험하다.

## 7.3 Interaction layer

책임:

- state machine
- pointer gesture routing
- modifier 해석
- snap resolution
- preview transform
- commit/cancel

권장 state:

- `Idle`
- `Hovering`
- `MarqueeSelecting`
- `MovingSelection`
- `ScalingSelection`
- `RotatingSelection`
- `PanningCanvas`

현재 `CanvasGestureRouter` 방향은 맞지만, rotate와 marquee가 추가되면 상태 명시성이 더 강해야 한다.

## 7.4 Patch layer

책임:

- scene delta / pivot / angle를 SVG transform string으로 변환
- `translate`, `scale`, `rotate` prepend
- transient preview source와 final committed source를 동일 경로로 생성

핵심 원칙:

- preview path와 commit path는 같은 계산식을 사용해야 한다.
- preview는 성공하고 commit은 다른 결과가 나오는 구조를 허용하면 안 된다.

## 8. 단계별 구현 계획

모든 단계는 "작게 구현하고 바로 검증"을 기준으로 한다.

### Phase 0. Contract 정리

범위:

- `PreviewSnapshot.SceneViewport` / `SceneBounds` / `EffectiveViewport` 의미 재정의
- `PreviewElementGeometry.SceneBounds`를 `VisualBounds` 개념으로 분리
- bounds quality 추가

완료 조건:

- selection, move, scale가 어떤 rect를 쓰는지 코드상 추적 가능
- debug log로 canonical/fallback bounds 확인 가능

테스트:

- fixture SVG 5개에서 snapshot rect dump 비교
- negative coordinate / root transform / nested group transform 검증

### Phase 1. Navigation 안정화

범위:

- pan / zoom / fit-to-document 정리
- `preserveAspectRatio` 최소 지원
- frame/content rect 계산 정리

완료 조건:

- 같은 SVG가 zoom level과 무관하게 일관된 scene mapping 유지
- fit-to-document이 항상 동일한 결과를 낸다

테스트:

- `viewBox`만 다른 SVG 3종 비교
- `preserveAspectRatio=none`과 `xMidYMid meet` 비교

### Phase 2. Selection / Hit Test 안정화

범위:

- hover target
- single select
- selection overlay
- resize handle hit test 정리

완료 조건:

- 작은 요소와 겹친 요소에서 hit test가 납득 가능
- overlay 위치가 visual bounds와 일치

테스트:

- 겹치는 shape 선택 우선순위
- stroke-only path 선택
- tiny path selection

### Phase 3. Move

범위:

- drag move
- axis lock
- preview/commit 경로 통합

완료 조건:

- drag 중 preview와 commit 결과가 동일
- 구조 패널 selection과 canvas selection sync 유지

테스트:

- 단일 element move
- group transform 하위 element move
- negative coordinate move

### Phase 4. Scale

범위:

- 8 handle resize
- opposite anchor
- uniform scale
- center anchor scale

완료 조건:

- scale preview와 commit 결과 일치
- shift/alt modifier가 정확히 동작

테스트:

- corner scale
- edge scale
- uniform scale
- center scale

### Phase 5. Rotate

범위:

- rotate handle
- center pivot rotate
- 15도 snap

완료 조건:

- rotation preview와 commit 결과 일치
- 회전 후 재선택 overlay가 안정적

테스트:

- 15도 step 회전
- nested transform element 회전
- 회전 후 다시 scale / move 연속 적용

### Phase 5b. Pivot

범위:

- pivot 표시
- pivot drag
- rotate around custom pivot

완료 조건:

- pivot 이동 후 rotate commit이 기대 결과와 일치

### Phase 6. Snapping

범위:

- edge/center snapping
- canvas center snapping
- snap indicators

완료 조건:

- move/scale 시 guideline이 예측 가능하게 나온다
- zoom level이 달라도 체감 snap threshold가 일정하다

테스트:

- center align
- edge align
- resize snap

### Phase 7. Marquee / Multi-select

범위:

- drag selection box
- additive select
- group bounds transform

완료 조건:

- 여러 요소를 묶어서 move/scale/rotate 가능

## 9. 테스트 전략

이번 canvas 작업은 "작동한다"가 아니라 "매 단계 회귀를 잡을 수 있다"가 중요하다.

### 9.1 Fixture 세트

반드시 별도 SVG fixture 세트를 만든다.

- simple rect / circle
- nested group transform
- negative coordinates
- stroke-heavy path
- rotate된 shape
- root transform
- wide viewBox with tiny content
- no viewBox document

### 9.2 검증 방식

각 phase마다 아래 두 가지를 함께 본다.

- geometry assertion
  - snapshot rect
  - transformed rect
  - snap result
- visual assertion
  - overlay screenshot
  - preview screenshot

### 9.3 Debug overlay

개발 중에는 toggle 가능한 debug overlay가 필요하다.

- document viewport outline
- visual content bounds
- selected element visual bounds
- pivot point
- snap lines
- bounds quality badge

이건 개발 속도에 직접 영향을 주는 도구라서 후순위가 아니다.

## 10. 우선순위 결정

지금 당장 필요한 것은 기능을 많이 넣는 것이 아니라 편집기의 축을 고정하는 것이다.

우선순위는 아래로 고정한다.

1. viewport / bounds / projection 계약 정리
2. single selection 안정화
3. move
4. scale
5. rotate
6. snapping
7. marquee / multi-select

즉, `move, rotate, scale`은 모두 필요하지만 실제 구현 순서는 `move -> scale -> rotate`가 가장 안전하다.
rotate를 먼저 넣으면 pivot, bounds 갱신, overlay 재계산까지 동시에 흔들리기 때문이다.

## 11. 이번 계획 기준의 구현 원칙

- 하나의 phase는 가능하면 2~4개 파일 수준의 변경으로 끝낸다.
- phase별로 fixture 테스트가 추가되지 않으면 다음 phase로 넘어가지 않는다.
- preview 계산식과 commit 계산식은 분기시키지 않는다.
- bounds가 불확실한 요소는 숨기지 말고 quality 상태로 드러낸다.
- `preserveAspectRatio`를 무시한 편의 구현은 금지한다.

## 12. 다음 액션

- 이 문서를 기준으로 `Phase 0` 작업부터 시작한다.
- 첫 구현 단위는 `PreviewSnapshot`, `PreviewElementGeometry`, `CanvasProjectionMath` 계약 정리다.
- 그 다음에 `Selection -> Move -> Scale -> Rotate` 순서로 한 단계씩 테스트하며 진행한다.

## 13. 참고 자료

- Figma: Move / Hand / Scale 도구
  - https://help.figma.com/hc/en-us/articles/360041064174-Access-design-tools-from-the-toolbar
- Figma: position / rotation / rotation origin
  - https://help.figma.com/hc/en-us/articles/360039956914-Adjust-alignment-rotation-and-position
- Figma: scale tool
  - https://help.figma.com/hc/en-us/articles/360040451453-Scale-layers-while-maintaining-proportions
- Inkscape selector tool
  - https://inkscape-manuals.readthedocs.io/en/1.3/selector-tool.html
- tldraw selection and transformation
  - https://tldraw.dev/features/composable-primitives/selection-and-transformation
- tldraw snapping
  - https://tldraw.dev/sdk-features/snapping
- Konva transformer
  - https://konvajs.org/docs/select_and_transform/Basic_demo.html
- Konva resize snapping
  - https://konvajs.org/docs/select_and_transform/Resize_Snaps.html
- MDN `viewBox`
  - https://developer.mozilla.org/en-US/docs/Web/SVG/Attribute/viewBox
- MDN `preserveAspectRatio`
  - https://developer.mozilla.org/docs/Web/SVG/Attribute/preserveAspectRatio
- MDN `transform`
  - https://developer.mozilla.org/en-US/docs/Web/SVG/Reference/Attribute/transform
- MDN `getBBox()`
  - https://developer.mozilla.org/en-US/docs/Web/API/SVGGraphicsElement/getBBox
- MDN `getCTM()`
  - https://developer.mozilla.org/en-US/docs/Web/API/SVGGraphicsElement/getCTM
