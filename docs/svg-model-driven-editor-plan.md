# SVG Model-Driven Editor Plan

## 현재 상태 메모

- 기준 날짜: 2026-03-10
- 현재 상태: `Batch 1`부터 `Batch 8`까지 핵심 전환 구현 완료
- 현재 남은 일:
  - legacy XML 실시간 편집 fallback 정리
  - dead code 제거
  - 문서 정리
  - renderer draft를 실제 renderer로 끌어올릴지 여부 판단

완료된 배치:

- `Batch 1` `DocumentModel` 타입 / loader / fixture loading tests
- `Batch 2` serializer / roundtrip tests
- `Batch 3` inspector read path model 전환
- `Batch 4` structure read path model 전환
- `Batch 5` drag / resize transient model session 전환
- `Batch 6` canvas renderer draft / preview fallback-safe branch 도입
- `Batch 7` style edit / reorder model mutation 전환
- `Batch 8` save path model-first serialize 전환

남은 단계는 `Phase 8. 레거시 경로 제거` 중심이다.

이 문서는 `unity-svg-editor`를

- `XML 실시간 수정 + preview 전체 rebuild`

구조에서

- `메모리 문서 모델 실시간 편집 + 저장 시 SVG serialize`

구조로 전환하기 위한 상세 실행 계획 문서다.

목적은 두 가지다.

1. 다른 세션의 에이전트가 바로 구현을 시작할 수 있게 한다.
2. 중간 단계에서 구조가 흔들리지 않게 기준 계약을 고정한다.

이 문서는 구현 계획 문서다.
코드 변경 순서, 모듈 경계, 검증 기준, 리스크와 롤백 방식을 포함한다.

## 1. 최종 결정

최종 방향은 아래다.

- SVG XML은 저장 시점의 직렬화 결과물로 취급한다.
- 편집 중 source-of-truth는 메모리 문서 모델이다.
- drag / resize / style / structure 수정은 전부 메모리 모델을 수정한다.
- preview는 메모리 모델 기반 렌더링 결과를 보여준다.
- save 시에만 메모리 모델을 SVG XML로 serialize 하고 validate / import 한다.

중요:

- 목표는 `SVG 요소마다 VisualElement를 하나씩 만드는 것`이 아니다.
- 목표는 `문서 모델 + 렌더러 + interaction state` 구조로 전환하는 것이다.

## 2. 왜 이 전환이 필요한가

현재 구조는 편집 중에도 XML이 source-of-truth다.

대표 경로:

- `Editor/Scripts/Workspace/Canvas/CanvasElementDragController.cs`
- `Editor/Scripts/Document/Structure/StructureDocumentEditService.cs`
- `Editor/Scripts/Workspace/Document/DocumentPreviewService.cs`
- `Editor/Scripts/Preview/PreviewSnapshotBuilder.cs`
- `Editor/Scripts/Preview/PreviewSnapshotGeometryBuilder.cs`

현재 drag 중 비용은 아래 순서로 발생한다.

1. 현재 `WorkingSourceText`에서 XML 문서를 다시 만든다.
2. target element의 transform을 XML에 prepend 한다.
3. `document.OuterXml`로 전체 문자열을 다시 만든다.
4. 그 문자열로 SVG scene import를 다시 수행한다.
5. preview bounds / hit geometry / vector image를 다시 만든다.
6. canvas overlay / selection / hover를 다시 갱신한다.

즉, 현재 병목은 `그리기` 자체보다 `재구성`이다.

## 3. 현재 구조 요약

현재 미리보기 구조는 요소별 UI 트리가 아니다.

- canvas 본문은 사실상 단일 `Image + VectorImage`
- selection / hover / resize handle은 overlay
- element별 정보는 bounds / hit geometry / transform 메타데이터

즉, 현재는 `개별 SVG 요소를 VisualElement로 직접 편집하는 구조`가 아니다.

이 점 때문에, 단순 최적화만으로는 목표 상태에 도달할 수 없다.
편집용 문서 모델 계층이 새로 필요하다.

## 4. 최종 목표 구조

최종 구조는 아래 5계층이다.

### A. Document Model

편집 기준이 되는 메모리 모델.

예상 책임:

- SVG 루트 문서 메타
- node tree
- style / transform / geometry-related raw attributes
- defs / references / dependency edges
- dirty state
- revision

예상 경로:

- `Editor/Scripts/DocumentModel/Model/`

핵심 타입 후보:

- `SvgDocumentModel`
- `SvgNodeModel`
- `SvgRootModel`
- `SvgNodeId`
- `SvgNodeKind`
- `SvgStyleValue`
- `SvgTransformValue`
- `SvgReferenceGraph`

### B. Loader / Serializer

문서 모델과 SVG XML 사이의 변환 계층.

예상 경로:

- `Editor/Scripts/DocumentModel/Loader/`
- `Editor/Scripts/DocumentModel/Serializer/`

핵심 타입 후보:

- `SvgDocumentModelLoader`
- `SvgDocumentModelSerializer`
- `SvgRoundtripValidator`

원칙:

- load는 처음 열 때 수행
- save는 serialize + validate + import 시에만 수행
- edit 중에는 serialize 금지

### C. Render State / Render Cache

렌더링과 hit-test용 계산 결과를 캐시하는 계층.

예상 책임:

- world transform
- draw order
- visual bounds
- hit geometry
- subtree invalidation
- dependency-based refresh

예상 경로:

- `Editor/Scripts/RenderModel/`

핵심 타입 후보:

- `SvgRenderDocument`
- `SvgRenderNode`
- `SvgRenderCache`
- `SvgInvalidationGraph`

### D. Canvas Renderer

문서 모델을 실제로 그리는 계층.

원칙:

- 기본 방향은 `VisualElement per node`가 아니다.
- 기본 방향은 `단일 canvas renderer 또는 소수 layer renderer`다.

이유:

- `path`, `gradient`, `clipPath`, `mask`, `group transform`, `opacity inheritance`를
  일반 `VisualElement` 트리로 관리하는 것은 비용이 크고 fidelity 리스크가 높다.

예상 경로:

- `Editor/Scripts/Renderer/`

핵심 타입 후보:

- `SvgCanvasRenderer`
- `SvgElementVisualLayer`
- `SvgTransientInteractionLayer`

### E. Interaction / UI Binding

Inspector, Structure, Canvas가 모델을 직접 읽고 쓰는 계층.

예상 경로:

- `Editor/Scripts/Workspace/Canvas/`
- `Editor/Scripts/Workspace/InspectorPanel/`
- `Editor/Scripts/Workspace/StructureInspector/`

원칙:

- inspector는 XML 재파싱 금지
- structure panel은 XML snapshot 기반 rebuild 금지
- drag session은 transient state를 별도로 보관

## 5. 주요 설계 원칙

### 5.1 편집 중 XML 금지

편집 중에는 아래를 금지한다.

- `XmlDocument` 재생성
- `document.OuterXml` 생성
- SVG scene import
- 전체 preview rebuild

예외:

- source editor에서 사용자가 XML을 직접 수정하는 모드
- save / validate / reload

### 5.2 변경은 모델에 먼저 반영

모든 UI 수정은 아래 순서를 따른다.

1. 모델 변경
2. invalidate 대상 계산
3. 렌더러 / overlay / inspector / structure 갱신
4. save 시 serialize

### 5.3 drag는 transient session 사용

drag는 모델 commit 이전에 transient session을 사용한다.

예상 타입:

- `SvgInteractionSession`
- `SvgDragSession`
- `SvgResizeSession`

원칙:

- pointer move 때마다 XML commit 금지
- mouse up 때만 모델 commit
- cancel 시 session 폐기

### 5.4 invalidation은 subtree 단위

모든 수정이 전체 문서 rebuild를 유발하면 안 된다.

기본 규칙:

- leaf style / transform 수정: 해당 node 또는 영향 subtree invalidate
- group transform 수정: subtree invalidate
- defs 참조 수정: dependent subtree invalidate
- reorder: 같은 parent 범위 invalidate
- root viewport / preserveAspectRatio 수정: 전역 invalidate

## 6. `VisualElement`를 어떻게 쓸 것인가

### 권장안

- canvas 본문은 전용 renderer가 담당
- `VisualElement`는 stage / overlay / tool chrome / inspector UI에 집중

### 비권장안

- SVG 요소마다 `VisualElement` 하나씩 생성

비권장 이유:

- 노드 수 증가 시 UI Toolkit tree 비용 증가
- path / clip / mask / gradient / reference 표현 부담
- 상속 스타일과 transform 반영 복잡도 증가

### 허용 가능한 hybrid

아래는 hybrid로 허용 가능:

- 선택 요소만 별도 transient visual layer 사용
- 나머지는 cached render layer 유지

## 7. 단계별 구현 계획

## Phase 0. 기초 계측 및 계약 고정

목표:

- 구조 전환 전 기준 동작을 고정한다.

작업:

- fixture SVG 셋 확정
- 현재 drag / resize / save / reload 기준 시나리오 문서화
- golden screenshot 또는 결과 비교 기준 확보

산출물:

- fixture 목록
- roundtrip 기준 문서
- 현재 known limitation 문서

완료 기준:

- 이후 단계에서 회귀 여부를 비교할 수 있다.

## Phase 1. Document Model 도입

목표:

- XML을 메모리 모델로 읽는 로더를 만든다.

작업:

- `SvgDocumentModel` 계층 추가
- 기존 `StructureNode`, `PatchTarget`, preview key 개념을 모델 key 체계로 통합
- loader 작성

세부 작업:

- node identity 규칙 정의
- root / group / leaf / defs reference 모델 정의
- style / transform 값 표현 형식 결정

산출물:

- `DocumentModel/Model/*`
- `DocumentModel/Loader/*`
- model loading tests

완료 기준:

- fixture SVG를 모델로 읽을 수 있다.
- element tree / target key / style / transform이 모델에 담긴다.

## Phase 2. Serializer 도입

목표:

- 모델을 다시 SVG XML로 직렬화할 수 있게 한다.

작업:

- serializer 작성
- validate 연동
- roundtrip diff 테스트 추가

완료 기준:

- load -> model -> serialize -> validate가 가능하다.
- 핵심 fixture에서 허용 범위 내 roundtrip이 보장된다.

## Phase 3. 읽기 경로 전환

목표:

- inspector / structure / selection이 XML 대신 모델을 읽게 한다.

작업:

- inspector target 목록 생성 경로 전환
- selected target attribute read 경로 전환
- structure tree snapshot 생성 경로 전환
- selection lookup 경로 전환

병렬 가능:

- inspector 경로
- structure 경로

직렬 필요:

- shared node key / selection contract

완료 기준:

- drag 전 상태에서 inspector와 structure가 모델만 보고 동작한다.
- XML 재파싱 없이 target lookup이 가능하다.

## Phase 4. Drag / Resize 모델 기반 전환

목표:

- drag / resize가 XML 대신 모델과 interaction session을 사용하게 만든다.

작업:

- `CanvasElementDragController`를 모델 기반으로 재작성
- `StructureDocumentEditService`의 drag 중 호출 제거
- transient drag session 도입
- mouse up 시 모델 commit

핵심 규칙:

- pointer move 중 XML serialize 금지
- transient visual state와 committed model state 분리

완료 기준:

- move / resize 중 XML 문자열을 만들지 않는다.
- drag 종료 시 한 번만 commit 된다.

## Phase 5. Preview Renderer 전환

목표:

- preview를 전체 XML rebuild 없이 모델 기반으로 그린다.

작업:

- model -> render cache 변환
- invalidate graph 작성
- canvas renderer 추가
- hit-test를 render cache 기반으로 전환

완료 기준:

- move / resize 중 preview 전체 rebuild 없이 화면 갱신 가능
- bounds / hit-test / overlay가 기존 수준으로 동작

## Phase 6. Style / Structure 편집 모델 기반 전환

목표:

- fill / stroke / opacity / reorder / group 작업도 모델 기반으로 옮긴다.

작업:

- inspector patch를 모델 mutation으로 전환
- reorder / group / ungroup 도입
- dependent subtree invalidation 처리

완료 기준:

- 일반 편집 작업에서 XML 재생성이 사라진다.

## Phase 7. Save / Import 마감

목표:

- 저장 시점에서만 XML serialize 및 import가 일어나게 한다.

작업:

- save 파이프라인 교체
- validate / import / preview refresh 연동
- dirty tracking / undo checkpoint 연결

완료 기준:

- save에서만 XML 생성
- 저장 결과와 editor state가 일치

## Phase 8. 레거시 경로 제거

목표:

- XML 기반 실시간 편집 경로 제거

작업:

- legacy fallback 삭제
- dead code 제거
- 문서 갱신

완료 기준:

- 실시간 편집 경로에서 XML 기반 코드가 더 이상 호출되지 않는다.

## 8. 다른 세션에서의 실제 작업 배치

현재 기준으로 아래 배치는 모두 구현 완료다.

다음 세션은 아래 단위로 끊어서 진행한다.

### Batch 1

- `DocumentModel` 타입 생성
- loader 초안
- fixture loading tests

### Batch 2

- serializer 초안
- roundtrip tests

### Batch 3

- inspector read 경로 모델 기반 전환

### Batch 4

- structure read 경로 모델 기반 전환

### Batch 5

- drag session / transient model state 도입

### Batch 6

- canvas renderer 초안

### Batch 7

- style edit / reorder 모델 기반 전환

### Batch 8

- save path 최종 연결

### Cleanup Batch

- legacy fallback 삭제
- dead code 제거
- 문서 갱신

## 9. 파일/디렉터리 계획

신규 디렉터리 제안:

- `Editor/Scripts/DocumentModel/Model/`
- `Editor/Scripts/DocumentModel/Loader/`
- `Editor/Scripts/DocumentModel/Serializer/`
- `Editor/Scripts/RenderModel/`
- `Editor/Scripts/Renderer/`
- `Editor/Tests/DocumentModel/`
- `Editor/Tests/Renderer/`

기존 파일 중 가장 먼저 영향받는 축:

- `Editor/Scripts/Workspace/Canvas/CanvasElementDragController.cs`
- `Editor/Scripts/Workspace/Canvas/CanvasInteractionController.cs`
- `Editor/Scripts/Workspace/InspectorPanel/InspectorTargetSyncService.cs`
- `Editor/Scripts/Workspace/StructureInspector/*`
- `Editor/Scripts/Workspace/Document/DocumentPreviewService.cs`
- `Editor/Scripts/Preview/*`
- `Editor/Scripts/Document/*`

## 10. 리스크

### 가장 큰 리스크

- SVG fidelity 저하
- defs / references / inheritance 처리 누락
- save 결과와 preview 결과 불일치

### 구조 리스크

- 모델 키 체계가 불안정하면 inspector / structure / canvas selection이 다 같이 흔들린다.
- renderer가 subtree invalidation을 제대로 못하면 성능 이득이 줄어든다.

### UX 리스크

- drag 중 preview와 최종 저장 결과가 달라지면 신뢰가 무너진다.

## 11. 롤백 전략

- phase별 feature flag 유지
- 초기 단계에서는 `legacy XML path`와 `model path`를 공존
- save는 serializer가 충분히 검증되기 전까지 legacy path 유지 가능

원칙:

- 한 번에 전환하지 않는다.
- 읽기 경로와 쓰기 경로를 분리해서 옮긴다.

## 12. 검증 기준

### 필수 테스트 축

- load fixture -> model tree shape
- model -> serialize -> validate
- roundtrip equality 또는 허용 diff
- drag move / resize visual regression
- inspector value sync
- structure selection sync
- reorder 영향 범위

### 성능 검증 축

- drag 3초 CPU time
- drag GC alloc
- resize CPU time
- inspector refresh 호출 수
- preview rebuild 호출 수

## 13. 구현 시 하지 말 것

- 바로 `VisualElement per SVG node`로 들어가지 말 것
- 첫 단계에서 renderer와 serializer를 동시에 완성하려 하지 말 것
- drag 전환 전에 모델 key 계약을 임시로 만들지 말 것
- XML 기반 편집 경로를 feature flag 없이 바로 삭제하지 말 것

## 14. 가장 먼저 해야 할 일

다음 세션은 아래 순서로 시작한다.

1. `DocumentModel` 타입 이름과 key 계약 확정
2. fixture 3~5개 기준 로더 테스트 작성
3. loader 구현
4. serializer 설계 문서 추가

## 15. 자가 검토

### 가장 어려운 결정

- `VisualElement per node`가 아니라 `문서 모델 + 렌더러` 구조를 택한 점

이유:

- 단기적으로는 per-node `VisualElement`가 쉬워 보여도, SVG fidelity와 유지비 측면에서 더 위험하다.

### 기각한 대안

- 현재 XML 기반 구조를 계속 미세 최적화

기각 이유:

- drag 핵심 병목은 구조 자체라 상한이 낮다.

- 문서 모델 없이 serializer부터 도입

기각 이유:

- 편집 기준 데이터가 없으면 serializer는 저장용 표현 계층에 그친다.

### 가장 불확실한 부분

- 모델 기반 renderer가 현재 preview fidelity를 얼마나 빨리 따라잡을 수 있는가

검증 방식:

- fixture screenshot 비교
- roundtrip diff
- drag / resize 체감 프로파일

## 16. 이 문서 사용 규칙

- 구현 전에는 항상 이 문서를 기준으로 현재 phase를 명시한다.
- 새로운 세션은 먼저 현재 batch 번호와 완료 여부를 문서 상단에 메모한다.
- 계획이 바뀌면 기존 phase를 덮어쓰지 말고 이유를 남긴다.
