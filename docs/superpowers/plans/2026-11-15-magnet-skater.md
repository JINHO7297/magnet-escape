# Magnet Skater Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 게임을 자동 전진 스케이터의 4코스 액션으로 전환한다.
**Architecture:** PlayerMotor 물리·기존 GameSession 생명주기를 활용한다. StageDefinition 4개를 러너 코스로 교체하고 같은 GUID의 원본 스프라이트를 스케이터/도심으로 재생성한다. Runtime에서 UnityEditor를 참조하지 않는다.
**Tech Stack:** Unity 2022.3.62f1, C#, 레거시 Input, Python 3/Pillow.
**Spec:** docs/superpowers/specs/2026-11-15-magnet-skater-design.md

## Global Constraints
- Unity 2022.3.62f1 및 기존 씬/프리팹/.meta GUID 유지.
- 자동 전진, Space 점프, Shift 전방 대시, Q/E 수직 자력.
- 전투·퍼즐·발전소·복잡한 스토리 제외. 원본 이력과 기존 편집본 보존.
- 사용자 승인에 따라 현재 develop에서 직접 작업·단계별 커밋·푸시한다.
- Unity 검수 보류. 불가능한 테스트는 미실행으로 기록하며 후속 구현 계속.

## Review Focus
- 정지/부활/결과/전환 중 자동 전진이 남지 않아야 한다: Task 1 Unity 연동 사례.
- 자력이 후진시키거나 발판 운반 속도가 고정 전진을 깨지 않아야 한다: Task 1 물리 코드 검토·미검증 기록.
- 재출발 직후 장애물/벽으로 강제 사망하지 않아야 한다: Task 2 코스 데이터 배치 검토·완주 보류.
- 이전 수동 조작 기록이 새 기록에 혼합되지 않아야 한다: Task 2 별도 기록 prefix.
- UI 도움말/빌드 안내/기존 편집본이 새 조작과 충돌하지 않아야 한다: Task 3 전체 문구 검색과 호환 설명.

### Task 1: 러너 조작
**Files:** PlayerMotor.cs, Tests/PlayMode/SessionLifecycleTests.cs, TASKS.md, PROGRESS.md
**Interfaces:** PlayerMotor.RunSpeed(float 읽기 속성), 기존 ResetAt/SetFrozen/Session.
- [ ] 자동 전진·일시정지/재개 연동 UnityTest를 먼저 작성한다. Unity 부재로 미실행 기록.
- [ ] 수평 입력 제거, 전방 대시, 수직 자력, 기본 전진 속도 유지 구현.
- [ ] python3 Tools/validate_project.py, Python 회귀, C# 구문·diff 검사.
- [ ] 기록 갱신·커밋·푸시.

### Task 2: 4코스와 기록 분리
**Files:** Resources/Stages/Stage1~4.asset, StageInfo.cs, StageRecords.cs, 기존 PlayMode 테스트 키.
**Interfaces:** 기존 StageDefinition 스키마·Scene 이름 유지, MagnetSkater 기록 prefix.
- [ ] 골목/옥상/공중 레일/스카이라인 배치. 각 체크포인트 뒤 준비거리 확보, 정지 대기 배제.
- [ ] 안내를 자동 전진/전방 대시/수직 자력으로 교체, 새 기록 키와 테스트 백업 키 일치.
- [ ] 기존 Python/에셋 검사·배치 읽기 검토, 실제 완주 미검증 기록.
- [ ] 기록 갱신·커밋·푸시.

### Task 3: 도심 표현·UI·문서
**Files:** Tools/generate_art.py, Resources/Art/*.png (기존 GUID), GameHud.cs, Editor/MacBuild.cs, README/GOAL/AGENTS/TASKS/PROGRESS/CREDITS/SUBMISSION.
**Interfaces:** 기존 스프라이트 파일명·크기·import 설정 유지.
- [ ] 스케이터/보드·도심 배경·공사 장애물·드론·결승 표현 생성.
- [ ] UI 제목·도움말·결과·Mac 안내를 실제 새 조작으로 통일.
- [ ] 목표/규칙/실행법 갱신, 새 기획서 복사.
- [ ] 전체 Python/에셋/C# 구문·문구·diff 검사 후 자체 리뷰, Unity 미검증 명시.
- [ ] 기록 갱신·커밋·푸시 및 원격 SHA 확인.

## Execution ledger
- Ruling: 사용자가 이미 검수 보류/자율 연속 개발을 요청했으므로 새 승인 대기나 TDD 성공 주장을 하지 않는다.
- Ruling: 기존 승인된 develop에서 직접 작업한다. 원본 상태는 clean이고 부모 커밋은 61fe3cd다.
