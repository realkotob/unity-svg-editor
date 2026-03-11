# Inspector Layout Plan

## 문서 상태

이 문서는 현재 구현의 상위 계획 문서가 아니라, inspector layout 후속 정리용 메모다.

이미 끝난 내용:

- patch tool 느낌의 inspector UI 제거
- `source` / `patch` 중심 상단 액션 제거
- model-driven inspector 흐름 전환

## 현재 유지할 레이아웃 원칙

- 선택 정보보다 편집 가능한 값을 우선한다
- 상단부터 자주 만지는 값이 먼저 나온다
- `opacity`는 `Appearance`에 둔다
- `Fill` / `Stroke` alpha는 color 기반으로 유지한다
- `Layout`은 geometry 성격으로 취급한다

## 현재 권장 섹션 순서

1. `Position`
2. `Layout`
3. `Appearance`
4. `Fill`
5. `Stroke`
6. 타입별 섹션
7. `Advanced`
8. `Document`

## 앞으로 정리할 것

- `rect` / `circle` / `ellipse` / `line` 타입별 geometry field 노출 정책
- `text` 노드 전용 섹션 설계
- gradient / reference 노드용 inspector 섹션 설계
- unsupported feature를 inspector에서 어떻게 표시할지 결정

## 비목표

- `Read From Target`
- `Apply Patch`
- XML source 편집 UI
- code inspector 부활

위 항목은 현재 계획에 없다.
