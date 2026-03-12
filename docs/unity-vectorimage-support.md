# Unity VectorImage Support Notes

- 기준 날짜: 2026-03-12
- 목적:
  - `unity-svg-editor`가 어떤 SVG 기능을 Unity `VectorImage` 범위 안으로 보는지 정리한다.
  - 공식 Unity 문서 기준과 이 저장소의 현재 구현 해석을 분리한다.
  - 앞으로 기능을 추가할 때 과장된 지원 표현을 막는다.

## 1. 공식 Unity 기준

이 문서에서 말하는 공식 기준은 Unity 문서에 명시된 내용만 적는다.

### 1.1 `VectorImage` 자체 성격

- `VectorImage`는 authoring 포맷이 아니라 opaque asset이다.
- 즉, `VectorImage`를 직접 편집 대상으로 삼는 것이 아니라 SVG importer나 Vector Graphics API 결과물로 다룬다.

### 1.2 UI Toolkit에서의 위치

- UI Toolkit / UI Builder에서 `VectorImage`를 요소 background로 지정할 수 있다.
- 하지만 Unity 6.0 매뉴얼도 vector image 사용을 “limited capacity”로 설명한다.
- 현재 렌더링은 실제 벡터 유지가 아니라 tessellation을 거친 polygon 기반이다.
- UI Toolkit에서는 raster 이미지가 여전히 권장 포맷이다.

### 1.3 SVG importer 공식 제한

Unity Vector Graphics 문서가 명시하는 핵심 제한:

- SVG importer는 SVG 1.1의 전체가 아니라 subset만 구현한다.
- `text` 요소는 지원하지 않는다.
- per-pixel `mask`는 지원하지 않는다.
- `filter`는 지원하지 않는다.
- interactivity는 지원하지 않는다.
- animation은 지원하지 않는다.

즉, `text`, full `mask`, `filter`, animation/interactivity는 “VectorImage native 지원”이라고 적으면 안 된다.

## 2. 공식 기준이 허용하는 API 표면

Unity Vector Graphics 문서 기준으로, API 표면에서 직접 표현 가능한 핵심 축은 아래다.

- `SceneNode`
  - child node
  - transform
  - clipper
- `Path`
  - `BezierContour`
  - `PathProperties`
- `Shape`
  - contour 기반 fill/stroke
- `GradientFill`
  - linear / radial
- `SolidFill`

중요:

- clipper는 지원되지만 “shape만 clipper가 될 수 있고, clipper의 stroke는 무시된다.”
- 따라서 `clipPath`류는 shape clipping으로 환원 가능한 경우만 안전한 범위다.
- 공식 문서가 말하는 `mask` 제한은 여전히 유효하므로, 일반 SVG `mask`를 `clipPath`처럼 생각하면 안 된다.

## 3. 이 저장소의 현재 해석

이 저장소는 Unity 공식 제한을 넘는 기능을 “VectorImage native 지원”으로 간주하지 않는다.

판정 기준:

- Unity 공식 문서에 직접 제한이 걸린 기능은 native 지원으로 승격하지 않는다.
- direct scene builder가 `SceneNode` / `Path` / `Shape` / `GradientFill` 축으로 안정적으로 표현 가능한 경우만 native 쪽으로 본다.
- 그 외는 아래 중 하나로 분류한다.
  - constrained support
  - overlay-backed support
  - fallback only

## 4. 현재 지원 범위 표

### 4.1 VectorImage-native로 간주하는 범위

아래는 현재 저장소 기준으로 “Unity `VectorImage` 축 안에서 다룬다”고 봐도 되는 범위다.

- 기본 shape
  - `rect`
  - `circle`
  - `ellipse`
  - `line`
  - `polyline`
  - `polygon`
- path geometry
  - `path`
  - relative command
  - curved path (`C`, `S`, `Q`, `T`)
- transform / viewport
  - node transform
  - `viewBox`
  - width / height 기반 viewport 해석
- fill / stroke
  - solid fill
  - solid stroke
  - dasharray
  - linecap / linejoin
- gradient
  - `linearGradient`
  - `radialGradient`
- clipping
  - shape 기반 `clipPath`

### 4.2 Constrained Support

아래는 현재 저장소에서 일부 direct 지원이 있지만, Unity 공식 문서가 넓게 보증한다고 쓰기 어려운 범위다.

- `use`
  - 현재 direct scene builder에서 처리한다.
  - 다만 Unity 공식 문서가 별도 보증 범위를 자세히 적고 있지는 않으므로 fixture 검증 전제다.
- basic `mask`
  - 현재 저장소에서는 제한적인 형태만 scene clipper 축으로 근사한다.
  - 하지만 Unity 공식 제한은 “per-pixel masking unsupported”다.
  - 따라서 일반 SVG `mask` 전체를 지원한다고 적지 않는다.

### 4.3 Overlay-Backed Support

아래는 editor에서 일부 보이거나 상호작용돼도, Unity `VectorImage` native 지원으로 보지 않는다.

- `text`
  - 현재 editor는 model 기반 text overlay로 표시 / hit-test / selection 일부를 보강한다.
  - 이는 `VectorImage` text 지원이 아니다.
- `tspan`
  - 표시 / 선택 일부는 가능해도 개별 편집 지원으로 보지 않는다.
- `textPath`
  - direct editing target이 아니다.

## 5. Fallback Only / Unsupported

아래는 현재 기준으로 `VectorImage` 안전 범위 밖으로 본다.

- `text`
  - native `VectorImage` 지원 아님
- `tspan` 개별 편집
- `textPath`
- per-pixel `mask`
- `filter`
- animation
- interactivity
- `<style>` tag 기반 스타일 해석
- `<image>`
  - Unity API에는 texture fill 개념이 있어도, 이 저장소는 SVG `<image>`를 direct authoring target으로 보지 않는다.

## 6. 현재 코드 기준 체크포인트

현재 구현에서 위 판정의 근거가 되는 핵심 파일:

- `Editor/Scripts/Renderer/SvgModelSceneBuilder.cs`
  - shape / path / gradient / clipPath / basic mask / `use` direct scene 처리
  - `text`, `tspan`, `textPath`는 drawable 생성 대신 별도 경로로 남긴다
- `Editor/Scripts/Preview/PreviewSnapshotBuilder.cs`
  - direct scene build 실패 시 import fallback 사용
- `Editor/Scripts/Preview/PreviewSnapshotBuilder.cs`
  - `PreviewVectorImage`와 `TextOverlays`를 분리해 다룬다
- `Editor/Scripts/Preview/PreviewSnapshotTextBuilder.cs`
  - `text`를 native vector draw가 아니라 overlay로 만든다
- `Editor/Scripts/Document/Analysis/RendererSupportDiagnostics.cs`
  - `tspan-edit`, `textPath`, `filter`, `image`, `style`를 fallback likely로 경고한다

## 7. 문서/제품 표현 규칙

앞으로 문서에서 아래 표현을 지킨다.

- `text 지원`이라고만 쓰지 않는다.
  - `editor overlay support`인지 `VectorImage native support`인지 분리해서 쓴다.
- `mask 지원`이라고만 쓰지 않는다.
  - basic / constrained support인지, full mask 지원인지 분리해서 쓴다.
- Unity 공식 제한이 있는 기능은 “지원 완료”로 승격하지 않는다.
- README와 plan 문서의 지원 표는 이 문서를 기준으로 유지한다.

## 8. 실무 가이드

Unity `VectorImage` 안전 범위를 유지하려면 아래를 권장한다.

1. 텍스트는 가능하면 outline path로 변환한다.
2. `mask`보다 `clipPath`를 우선한다.
3. `<style>` 태그보다 presentation attribute를 우선한다.
4. `filter`, animation, interactivity는 authoring target으로 잡지 않는다.
5. `use`와 constrained feature는 fixture와 EditMode 검증을 먼저 붙인다.

## 9. 공식 문서

- Unity Vector Graphics package manual:
  - https://docs.unity.cn/Packages/com.unity.vectorgraphics@2.0/manual/index.html
- Unity `VectorImage` scripting API:
  - https://docs.unity3d.com/ScriptReference/UIElements.VectorImage.html
- Unity 6 UI Toolkit best practice, vector images section:
  - https://docs.unity3d.com/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/graphic-and-font-assets-preparation.html
- UI Toolkit image import settings for SVG Vector image:
  - https://docs.unity3d.com/Manual/UIE-image-import-settings.html
