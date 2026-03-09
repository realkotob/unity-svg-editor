# unity-svg-editor (Phase 0)

`unity-svg-editor`는 커서 빌더 이전 단계에서 SVG 원본 충실도(fidelity)를 우선으로 다루기 위한 에디터 모듈입니다.

- Namespace: `UnitySvgEditor.Editor`
- Menu: `Window/Unity SVG Editor/SVG Editor`

## 목표

- SVG XML을 source-of-truth로 유지
- Unity 내에서 벡터 프리뷰를 즉시 확인
- 저장 전 XML 유효성 검증
- 저장 후 자동 reimport로 결과 확인

## 현재 포함 기능

- 프로젝트 내 `.svg`(VectorImage) 에셋 목록/검색
- 선택 SVG 프리뷰 표시
- 원본 XML 편집, Validate, Save & Reimport
- Quick Patch UI (Target: Root `<svg>` 또는 `id` 노드)
  - `fill`, `stroke`, `stroke-width`, `opacity`, `fill-opacity`, `stroke-opacity`
  - `stroke-linecap`, `stroke-linejoin`, `stroke-dasharray`
  - `transform` + translate/rotate/scale helper
  - `Read From Target`로 현재 속성 동기화
- SVG 고난도 피처 스캔 (gradient/clipPath/mask/filter/text 등)

## UI 의존 구조

- `unity-svg-editor`는 private `CoreUI` 전체에 의존하지 않습니다.
- 공용 UI는 `com.newmassmedia.unity-uitoolkit-foundation` 패키지의 에디터/런타임 표면을 직접 참조합니다.
- shadcn 스타일 토큰은 `com.maemi.unity-uitoolkit-shadcn-theme` 패키지에서 공급합니다.
- 에디터 로컬 UXML/TSS/USS/아이콘은 패키지 내부 `Editor/Resources/` 아래에 두고 `Resources.Load`로 읽습니다.
- 에디터 로컬 USS는 테마 엔트리 기준 상대 경로로 import 되므로 `Assets/`와 `Packages/` 어느 루트에서도 동일하게 동작해야 합니다.

이 구조를 기준으로 `Svg Editor`는 공개 가능 패키지로 분리할 수 있고, private `CoreUI`는 그 위에 추가 컴포넌트를 얹는 방식으로 유지합니다.

## 패키지화 메모

- self USS/TSS import는 절대 `Assets/unity-svg-editor/...` 경로를 사용하지 않습니다.
- foundation 및 shadcn theme는 패키지 dependency로 소비하는 구조를 전제로 합니다.
- 에디터 쉘 리소스는 `Resources.Load` 기반이므로 `Assets/`와 `Packages/` 사이 이동 시 self path 재계산이 필요하지 않습니다.

## 참고 레퍼런스

- Unity Vector Graphics 패키지/Importer 문서  
  https://docs.unity3d.com/Packages/com.unity.vectorgraphics@2.0/manual/index.html
- Unity UI Toolkit `Painter2D`  
  https://docs.unity3d.com/ScriptReference/UIElements.Painter2D.html
- Unity UI Toolkit `generateVisualContent`  
  https://docs.unity3d.com/ScriptReference/UIElements.VisualElement-generateVisualContent.html
- SkiaForUnity  
  https://github.com/ammariqais/SkiaForUnity
- UIToolkit SVG 아이콘/그라디언트 레퍼런스  
  https://github.com/YIJUAN7/Unity-UIToolkit-SVG-Icon-And-Gradinet-Color
- Cursor Theme Builder 레퍼런스  
  https://github.com/phisch/cursor-theme-builder
- Phinger cursors 레퍼런스  
  https://github.com/phisch/phinger-cursors

## 다음 단계

1. 노드 단위 속성 패치 레이어(fill/stroke/transform) 추가
2. role별 hotspot/guide overlay 추가
3. Cursor Builder와 preset 교환 포맷 연결
