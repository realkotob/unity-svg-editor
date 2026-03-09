# SVG Editor 성능 최적화 계획

## 1. 문서 목적

이 문서는 `unity-svg-editor`의 사용 중 체감 성능 저하 원인과 개선 우선순위를 정리한다.
핵심 목표는 큰 구조 개편 전에 가장 비용 대비 효과가 큰 최적화를 먼저 적용하는 것이다.

이 문서는 구현 문서가 아니라 실행 기준 문서다.
즉, 무엇이 느린지, 왜 느린지, 어떤 순서로 줄여야 하는지를 명확히 하는 데 목적이 있다.

## 2. 현재 관찰된 병목

현재 에디터는 입력 또는 드래그 상호작용이 발생할 때 부분 갱신보다 전체 재처리에 가깝게 동작한다.

대표 경로는 아래와 같다.

- 소스 편집
  - `DocumentLifecycleView.OnSourceEditorChanged`
  - `DocumentLifecycleController.OnSourceChanged`
  - `DocumentSourceSyncService.SyncCurrentSource`
- 드래그 프리뷰
  - `CanvasElementGestureHandler.ApplyElementDelta`
  - `CanvasElementDragController.TryRefreshMovePreview`
  - `CanvasElementDragController.TryRefreshResizePreview`
  - `DocumentPreviewService.TryRefreshTransientPreview`

이 경로에서 반복적으로 발생하는 작업은 다음과 같다.

- SVG XML 재파싱
- preview scene 재import
- geometry bounds 재계산
- `VectorImage` 재생성
- structure tree 재구성
- patch target 목록 재수집
- selected target attribute 재읽기

즉, 문제의 본질은 특정 UI 이벤트가 무거운 전체 파이프라인을 너무 자주 호출한다는 점이다.

## 3. 핵심 원인 정리

### 3.1 소스 입력 1회당 전체 동기화

현재 source field 변경은 즉시 `SyncCurrentSource`로 연결된다.
이 메서드는 한 번의 호출 안에서 아래 작업을 모두 처리한다.

- patch inspector target refresh
- live preview rebuild
- structure view rebuild
- interactivity update

이 구조는 작은 텍스트 수정에도 비용이 너무 크다.

### 3.2 드래그 중 transient preview 재빌드

이동/리사이즈 드래그 중에는 pointer move 이벤트마다 임시 source text를 만들고, 그 source로 다시 preview snapshot을 빌드한다.
이 과정은 사실상 실시간 전체 SVG 재해석에 가깝다.

큰 SVG일수록 아래 비용이 누적된다.

- `XmlDocument.LoadXml`
- `SVGParser.ImportSVG`
- `PreviewSnapshotGeometryBuilder.BuildElementBounds`
- `VectorUtils.TessellateScene`
- `VectorImage` 생성 및 교체

### 3.3 동일 source에 대한 중복 파싱

같은 `WorkingSourceText`를 대상으로 preview, structure, patch inspector가 각각 독립적으로 XML을 다시 읽는다.
현재는 결과 재사용보다 각 서비스의 독립성이 우선되어 있어 CPU 비용이 중첩된다.

### 3.4 상호작용 종류에 비해 갱신 범위가 과함

모든 변경이 동일한 강도의 갱신을 유발하는 것도 문제다.

예를 들어 아래는 비용 수준이 달라야 한다.

- source text typing
- canvas hover
- move/resize drag preview
- drag commit
- explicit save/validate

하지만 현재는 여러 경우가 비슷한 수준의 갱신 흐름으로 수렴한다.

## 4. 최적화 목표

우선 단계의 목표는 다음 네 가지다.

- typing 시 프리뷰와 패널 갱신 빈도를 줄인다.
- drag 중에는 무거운 preview rebuild를 제한한다.
- 동일 source에 대한 XML 파싱 중복을 줄인다.
- 구조 패널, patch inspector, preview를 서로 다른 갱신 정책으로 분리한다.

## 5. 우선순위별 개선안

## 5.1 P0: 계측 추가

최적화 전에 실제 비용 분포를 먼저 기록해야 한다.

추가 대상:

- `DocumentSourceSyncService.SyncCurrentSource`
- `DocumentPreviewService.RefreshLivePreview`
- `DocumentPreviewService.TryRefreshTransientPreview`
- `EditorWorkspaceCoordinator.RefreshStructureViews`
- `PatchInspectorTargetSyncService.RefreshTargets`

권장 측정값:

- 호출 횟수
- 평균 ms
- 최대 ms
- source length
- element count 추정치
- interaction type

권장 방식:

- `Stopwatch` 기반 lightweight instrumentation
- `#if UNITY_EDITOR` 내 간단 로그 또는 샘플러 사용
- 과도한 로그 스팸 방지를 위한 threshold 로그

이 단계 없이 바로 캐시를 넣으면 효과가 적은 지점에 복잡도만 올릴 수 있다.

## 5.2 P1: source typing debounce

가장 먼저 적용할 가치가 큰 개선이다.

정책:

- source editor 변경 시 즉시 전체 sync를 실행하지 않는다.
- 마지막 입력 이후 `150ms ~ 300ms` 뒤에 한 번만 동기화한다.
- debounce pending 중 추가 입력이 오면 기존 예약은 취소하고 다시 예약한다.

권장 초기값:

- 기본 `200ms`

기대 효과:

- 일반 타이핑 중 preview rebuild 횟수 급감
- structure/patch panel 불필요 갱신 감소
- 체감 입력 지연 완화

주의:

- `Validate`, `Save`, `Reload`는 debounce를 우회하고 즉시 반영해야 한다.

## 5.3 P1: drag preview throttling 또는 commit-only preview

두 번째로 체감 효과가 큰 개선이다.

옵션 A: throttling

- drag 중 transient preview refresh를 최대 `30fps` 또는 `15fps`로 제한
- pointer move는 계속 받되 preview rebuild는 일정 주기마다만 수행

옵션 B: commit-only preview

- drag 중에는 overlay rect와 selection visual만 갱신
- 실제 SVG preview rebuild는 pointer up 시점에만 수행

권장 판단:

- 먼저 `throttling`으로 들어가고, 여전히 무거우면 특정 asset size 이상에서 `commit-only`로 degrade

기대 효과:

- move/resize 중 프레임 드랍 감소
- large SVG에서 마우스 추적성 향상

## 5.4 P1: structure / patch / preview 갱신 분리

현재는 source sync 한 번에 세 영역이 같이 갱신된다.
이 결합을 풀어야 한다.

권장 정책:

- preview
  - typing debounce 후 갱신
  - drag 중 throttled 갱신
- structure tree
  - 더 긴 debounce 또는 explicit refresh
  - drag preview 중 갱신 금지
- patch inspector
  - selected target 유지 시 attribute refresh 최소화
  - target 목록 재수집은 source 구조가 바뀌었을 때만 수행

첫 단계에서는 완벽한 change detection보다 단순한 정책 분리가 더 중요하다.

## 5.5 P2: XML parse result 재사용

현재 여러 서비스가 같은 source text를 각각 `LoadXml`한다.
이 비용을 줄이기 위해 parse result를 재사용한다.

가능한 접근:

- `WorkingSourceText` 해시 기준으로 `XmlDocument` 캐시
- parse result + root + key mapping을 묶은 immutable snapshot 캐시
- preview/structure/patch가 공유하는 `ParsedSvgDocumentContext` 도입

주의:

- `XmlDocument` 자체를 여러 서비스가 mutate하면 위험하다.
- 공유 object를 쓰려면 read-only contract 또는 clone 전략이 필요하다.

권장:

- 처음부터 범용 캐시를 크게 만들기보다, parse result를 묶은 read-mostly context부터 도입

## 5.6 P2: patch inspector 비용 축소

현재 patch inspector는 source 변화마다 target 목록 추출과 attribute read를 같이 수행한다.
여기서 불필요한 재읽기를 줄일 수 있다.

개선안:

- selected target이 유지되면 attributes만 갱신
- 구조 변화가 없으면 target 목록 재수집 생략
- typing 중에는 inspector refresh를 지연
- drag preview 중 inspector refresh 금지

이 영역은 preview보다는 우선순위가 낮지만, typing 체감에는 꽤 영향을 준다.

## 5.7 P3: preview quality 단계화

모든 상황에서 동일한 preview 품질을 유지할 필요는 없다.

단계화 예시:

- idle / commit 후
  - full quality tessellation
- typing 중
  - medium quality 또는 debounce 후 full quality
- drag 중
  - low quality 또는 preview rebuild 생략

가능하면 `PreviewBuildOptions`를 상호작용 상태에 따라 다르게 구성한다.

## 6. 구현 순서 제안

권장 순서는 아래와 같다.

1. 계측 추가
2. source typing debounce
3. drag preview throttling
4. structure / patch / preview 갱신 분리
5. patch inspector refresh 최소화
6. XML parse 재사용
7. preview quality 단계화

이 순서를 권장하는 이유는 다음과 같다.

- 1~4단계만으로도 체감 개선 가능성이 높다.
- 5 이후 단계는 구조적 복잡도가 올라간다.
- 캐시와 공유 컨텍스트는 측정 없이 먼저 넣으면 유지보수 비용이 커질 수 있다.

## 7. 검증 기준

최적화 적용 후 아래를 최소 확인 기준으로 둔다.

- source editor에서 연속 입력 시 UI 멈춤이 줄어들었는가
- large SVG에서 move/resize drag 중 pointer tracking이 자연스러운가
- preview, structure, patch inspector 간 state 불일치가 없는가
- `Save`, `Reload`, `Validate`, drag commit 후 최신 state가 확실히 반영되는가
- invalid XML 입력 중에도 editor가 과도한 재시도로 흔들리지 않는가

권장 비교 항목:

- 입력 10초 동안 preview rebuild 횟수
- drag 3초 동안 transient preview rebuild 횟수
- 평균 rebuild ms
- 최대 frame hitch

## 8. 비목표

이번 문서의 범위에 포함하지 않는 항목은 아래와 같다.

- multi-thread 기반 SVG parse
- runtime 최적화
- importer 교체
- SVG feature fidelity 개선
- 새로운 editing feature 추가

이번 최적화의 목적은 기능 확장이 아니라 현재 편집 경험의 반응성을 높이는 것이다.

## 9. 첫 실행안

바로 시작한다면 아래 조합을 1차 배치로 묶는 것이 가장 안전하다.

- `SyncCurrentSource` 계측 추가
- source editor debounce
- drag transient preview throttling
- drag 중 structure refresh 차단

이 조합은 구현 난도가 비교적 낮고, 기존 아키텍처를 크게 깨지 않으면서 가장 큰 체감 개선을 노릴 수 있다.
