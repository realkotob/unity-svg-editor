# SVG Editor General Backlog

- 기준 날짜: 2026-03-13
- 범위: canvas selection 외의 에디터 전반 후속 작업
- 목적: 저장 무결성, unsupported feature 처리, 편집 UX 관련 후속 이슈를 고정한다.

## P0

### BL-GEN-01. Namespace / Prefix Round-Trip 보존

- 배경:
  - 현재 `text/tspan/textPath`가 포함된 문서는 모델 기반 편집을 차단했다.
  - 하지만 serializer 자체는 여전히 element prefix / non-root namespace declaration / foreign namespace round-trip을 보존하지 못한다.
- 문제:
  - unsupported text를 경고로 막아도, namespace/prefix가 있는 문서는 다른 편집 경로에서 여전히 의미가 바뀌거나 serialize 실패할 수 있다.
- 목표:
  - load/save 후에도 element prefix, attribute prefix, namespace declaration scope가 보존된다.
- 완료 기준:
  - root 외부에서 선언된 namespace가 있는 문서가 round-trip 후에도 의미를 유지한다.
  - prefixed element/attribute가 serialize 실패 없이 유지된다.
  - foreign namespace가 포함된 fixture 또는 테스트가 추가된다.
- 후보 작업:
  - `SvgDocumentModelLoader`가 `LocalName`만 보관하는 구조를 재검토
  - `SvgDocumentModelSerializer`에서 `prefix: null` 고정 write 제거
  - namespace declaration 수집 범위를 root-only에서 확장하거나 원문 보존 전략 도입

### BL-GEN-02. Unsupported Text Feature 정책 정리

- 배경:
  - 현재는 `text/tspan/textPath`가 포함되면 preview는 허용하고 모델 기반 편집만 차단한다.
- 목표:
  - 사용자에게 왜 편집이 막혔는지 더 명확히 보여주고, 허용/차단 경계를 문서화한다.
- 완료 기준:
  - 상태 메시지 외에 UI 레벨 경고 표면이 있다.
  - README와 지원 범위 문서가 현재 정책과 일치한다.
  - 어떤 액션이 차단되는지 테스트로 고정된다.

## P1

### BL-GEN-03. Save Button Wiring 복구

- 배경:
  - 현재 저장은 단축키 경로는 살아 있지만, UI save button은 별도 연결 확인이 필요하다.
- 목표:
  - toolbar/button과 save command가 동일 경로로 동작한다.
- 완료 기준:
  - 버튼 클릭과 `Cmd/Ctrl+S`가 같은 저장 경로를 호출한다.
  - 관련 EditMode 테스트가 있다.

### BL-GEN-04. XML Declaration / Encoding Round-Trip 정책 결정

- 배경:
  - 현재 load/save는 사실상 UTF-8 무BOM + XML declaration 제거 쪽으로 수렴한다.
- 목표:
  - 원문 보존을 할지, normalize를 공식 정책으로 할지 결정하고 코드와 문서를 맞춘다.
- 완료 기준:
  - 정책이 문서화되어 있다.
  - save 후 declaration/encoding 동작이 테스트로 고정된다.

## 메모

- `BL-GEN-01`이 해결되기 전까지는 unsupported feature를 warning만으로 풀지 않는다.
- `text/tspan/textPath` 차단은 임시 안전장치로 취급한다.
