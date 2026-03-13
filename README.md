# unity-svg-editor

`unity-svg-editor`는 Unity 안에서 SVG를 읽고, 구조를 이해하고, 자주 수정하는 속성을 시각적으로 편집하고, 다시 저장하기 위한 editor module입니다.

- Namespace: `UnitySvgEditor.Editor`
- Menu: `Window/Unity SVG Editor/SVG Editor`

## 현재 방향

이 프로젝트는 더 이상 edit-time XML patch editor가 아닙니다.

현재 고정 원칙:

- edit-time source-of-truth는 `SvgDocumentModel`
- save-time interchange format은 SVG XML
- inspector / structure / canvas interaction은 model-driven
- save 시에만 serialize + validate + import

즉, XML source editor / code inspector를 중심으로 한 툴이 아니라, model-driven SVG editor입니다.

## 현재 포함 기능

- 프로젝트 내 `.svg`(VectorImage) 에셋 목록 / 검색
- 구조 트리 / selection sync
- canvas selection / move / resize
- inspector 기반 속성 편집
- save + reimport
- SVG feature scan / renderer fallback 진단

## 지원 상태

아래 구분은 “현재 editor preview / interaction 기준”입니다.

### Editor Preview / Interaction Support

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

### Preview Works, Editing Is Limited

- `tspan`
  - 표시 / 선택은 가능
  - 개별 `tspan` 편집은 제한적
- `textPath`
  - direct editing target이 아님
- 복합 조합
  - `use + gradient + clipPath`
  - `mask + shape`

### Fallback / Not A Direct Editing Target

아래는 상태줄에서 `Renderer fallback likely: ...`로 드러날 수 있습니다.

- `filter`
- `image`
- `style`
- `text`
  - Unity `VectorImage` native 렌더링 대상이라고 보지 않는다
  - 현재는 model 기반 text overlay preview로 다룬다
- `textPath`
- `tspan` 개별 편집

원칙:

- Unity `VectorImage`와 일관되게 갈 수 있는 SVG feature까지만 direct 지원
- 그 외는 fallback 또는 편집 제한으로 명시

주의:

- 현재 `text`는 editor preview에서 다룰 수 있지만, 이를 Unity `VectorImage` 자체 text 지원으로 간주하지 않는다.
- 즉 `text`는 “VectorImage native feature”가 아니라 “editor overlay-backed feature”로 취급한다.

## Fixture-First Rule

새 renderer 작업은 fixture 없이 시작하지 않습니다.

fixture 위치:

- `Assets/Resources/TestSvg/`

현재 renderer 검증 fixture 예시:

- `defs-use-basic.svg`
- `path-relative-commands.svg`
- `path-curves-basic.svg`
- `text-tspan-basic.svg`
- `clippath-basic.svg`
- `mask-basic.svg`
- `radial-gradient-basic.svg`
- `polyline-polygon-basic.svg`
- `use-gradient-clip-combo.svg`

## 테스트

기본 검증 기준:

- `UnitySvgEditor.Editor.Tests` EditMode green 유지

프로젝트 상황에 따라 test runner가 초기화 타임아웃을 낼 수 있으므로, 필요하면 해당 fixture를 editor에서 직접 열어 아래를 확인합니다.

- preview가 보이는지
- selection / hover가 자연스러운지
- drag / resize / save 후 결과가 유지되는지

## 패키지 / UI 의존

- `unity-svg-editor`는 private `CoreUI` 전체에 의존하지 않습니다.
- 공용 UI는 `com.newmassmedia.unity-uitoolkit-foundation` 패키지 표면을 사용합니다.
- 테마 토큰은 `com.maemi.unity-uitoolkit-shadcn-theme` 패키지를 사용합니다.
- editor 리소스는 패키지 내부 `Editor/Resources/` 아래에 두고 `Resources.Load`로 읽습니다.

## 현재 비목표

- Illustrator / Figma 급 범용 벡터 툴
- 모든 SVG feature 100% authoring
- edit-time XML patch UI 복귀
- XML source editor / code inspector 복귀

## 다음 단계

- unsupported feature 진단 / fallback 메시지 정리
- `tspan` / `text-anchor` 세부 정밀도 개선 여부 판단
- `text`를 overlay 유지 대상으로 둘지, 더 축소할지 판단
- 추가 fixture 기반 renderer coverage 확장

## 리팩터 상태

최근 정리 방향:

- folder 단위 `unity-guide` cleanup
- shared utility 추출
- 과도한 helper/type 이름 축소
- `4개 이상 파라미터` 메서드 축소

이미 반영된 shared 예시:

- `DeferredActionGate`
- `CallbackBindingUtility`
- `SvgFragmentReferenceUtility`
- `PreviewCollectionRenderer`
- `SvgMutationWriter`

현재 로컬 작업 방향:

- canvas / mutation / selection request 객체 도입
- long-name cluster 추가 축소
- parameter count 4+ 메서드 지속 축소

핸드오프:

- 최신 상태와 남은 작업은 `HANDOFF.md` 참고

## 추가 문서

- Unity `VectorImage` 지원 범위 정리: `docs/unity-vectorimage-support.md`
