# SVG Model-Driven Editor Plan

## 현재 상태 메모

- 기준 날짜: 2026-03-11
- 현재 상태:
  - document model / loader / serializer 전환 완료
  - inspector / structure / selection read path model 전환 완료
  - drag / resize / style / reorder / save path model-first 전환 완료
  - legacy edit-time XML patch 경로 제거 완료
  - `AttributePatcher*` 제거 완료
  - preview live/transient refresh의 외부 계약 model 전환 완료
  - renderer direct scene path 도입 완료
- 현재 남은 일:
  - direct renderer 지원 범위 확대
  - unsupported SVG feature에 대한 fixture 추가와 회귀 테스트 강화
  - renderer invalidation / rebuild 비용 추가 최적화 여부 판단

## 1. 현재 고정 원칙

- 편집 중 source-of-truth는 `SvgDocumentModel`이다.
- save 시에만 SVG XML을 serialize 한다.
- inspector는 XML을 읽지 않는다.
- structure panel은 XML snapshot fallback을 쓰지 않는다.
- drag 중 inspector 값은 transient document model 기준으로 실시간 갱신한다.
- XML source editor / code inspector는 다시 도입하지 않는다.
- edit-time XML patch 경로는 늘리지 않는다.

## 2. 현재 완료 범위

### 2.1 Document Model

- `SvgDocumentModel`
- `SvgNodeModel`
- `SvgNodeId`
- raw attribute / reference 보존
- loader / serializer / roundtrip 테스트

### 2.2 Editing Flow

- inspector field apply는 model mutation 기준
- drag / resize는 transient model session 기준
- commit은 interaction 종료 시 한 번만 수행
- save는 model serialize + validate + import 기준

### 2.3 Runtime Cleanup

- `AttributePatcher.cs`
- `AttributePatcherXmlPath.cs`
- `AttributePatcherRegexPath.cs`
- `AttributePatchAttributeEditHelper.cs`

위 legacy patch 경로는 제거 완료다.

### 2.4 Preview / Renderer

- live preview는 current document model 기준
- transient preview는 transient document model 기준
- `PreviewSnapshotBuilder`의 추가 model-to-import fallback 제거 완료
- direct scene path는 현재 아래를 지원한다
  - `rect`
  - `circle`
  - `ellipse`
  - `line`
  - 단순 `path`
  - `g` transform
  - `use`
  - basic `linearGradient`
  - relative path fixture

## 3. 현재 남아 있는 renderer 현실

아직 renderer 전체가 “완전한 자체 SVG 구현”은 아니다.

- direct scene builder가 common shape / `use` / basic gradient를 처리한다.
- unsupported feature가 나오면 `SvgCanvasRenderer` 내부에서만 import fallback을 사용한다.
- 즉, 외부 편집 계약은 model-driven으로 정리됐고, 내부 renderer coverage만 단계적으로 넓히는 상태다.

## 4. 다음 단계

남은 계획은 cleanup이 아니라 renderer coverage 확장이다.

### Phase R1. Fixture-First Renderer Expansion

원칙:

- 새 모양이나 새 SVG 기능을 지원할 때는 먼저 fixture SVG를 추가한다.
- fixture는 `Assets/Resources/TestSvg/` 아래에 둔다.
- fixture 추가 후 EditMode 테스트를 먼저 쓴다.
- 그 다음 renderer 구현을 확장한다.

우선순위:

1. complex path command 지원 확대
2. gradient variant 확대
3. `text`
4. `clipPath`
5. `mask`

### Phase R2. Renderer Stability

- preview rebuild 빈도 계측
- transient preview 교체 비용 계측
- texture / vector image lifecycle 안정화
- invalidation 범위 축소 여부 판단

## 5. 새 작업 규칙

### 5.1 Fixture Rule

- 새 renderer 작업은 fixture 없이 시작하지 않는다.
- fixture 파일명은 지원하려는 feature가 드러나게 짓는다.
- 예:
  - `use-gradient-transform.svg`
  - `path-relative-commands.svg`

### 5.2 Test Rule

- fixture를 추가하면 대응하는 EditMode 테스트를 같이 추가한다.
- 최소 검증:
  - snapshot build 성공
  - target key 존재
  - bounds 또는 projection rect 유효

### 5.3 Fallback Rule

- 외부 API에서 string/XML fallback을 다시 만들지 않는다.
- fallback이 필요하면 renderer 내부 한정으로만 유지한다.
- 새 fallback을 넣을 때는 “왜 direct path로 못 가는지”를 테스트와 함께 남긴다.

## 6. 현재 기준 테스트/fixture 메모

fixture 위치:

- `Assets/Resources/TestSvg/`

현재 renderer 검증에 중요한 fixture:

- `no-viewbox-basic.svg`
- `transformed-parent.svg`
- `defs-use-basic.svg`
- `use-gradient-transform.svg`
- `path-relative-commands.svg`

현재 기준 EditMode 테스트는 `UnitySvgEditor.Editor.Tests`에서 green 유지가 기준이다.

## 7. 지금 지우지 말 것

- save-time serializer 경로
- `SvgCanvasRenderer` 내부 unsupported feature fallback
- direct renderer coverage를 아직 갖추지 못한 feature의 fixture

## 8. 다음 세션 시작 규칙

다음 세션은 아래 순서로 시작한다.

1. 지원하려는 SVG feature 하나를 고른다.
2. 해당 feature fixture를 `Assets/Resources/TestSvg/`에 추가한다.
3. 대응하는 EditMode 테스트를 먼저 추가한다.
4. renderer를 그 fixture 기준으로만 확장한다.
5. `UnitySvgEditor.Editor.Tests` green을 확인한다.
