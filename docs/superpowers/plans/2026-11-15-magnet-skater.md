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
- [x] 자동 전진·일시정지/재개 연동 UnityTest를 먼저 작성한다. Unity 부재로 미실행 기록.
- [x] 수평 입력 제거, 전방 대시, 수직 자력, 기본 전진 속도 유지 구현.
- [x] python3 Tools/validate_project.py, Python 회귀, C# 구문·diff 검사.
- [x] 기록 갱신·커밋·푸시.

### Task 2: 4코스와 기록 분리
**Files:** Resources/Stages/Stage1~4.asset, StageInfo.cs, StageRecords.cs, 기존 PlayMode 테스트 키.
**Interfaces:** 기존 StageDefinition 스키마·Scene 이름 유지, MagnetSkater 기록 prefix.
- [x] 골목/옥상/공중 레일/스카이라인 배치. 각 체크포인트 뒤 준비거리 확보, 정지 대기 배제.
- [x] 안내를 자동 전진/전방 대시/수직 자력으로 교체, 새 기록 키와 테스트 백업 키 일치.
- [x] 기존 Python/에셋 검사·배치 읽기 검토, 실제 완주 미검증 기록.
- [x] 기록 갱신·커밋·푸시.

### Task 3: 도심 표현·UI·문서
**Files:** Tools/generate_art.py, Resources/Art/*.png (기존 GUID), GameHud.cs, Editor/MacBuild.cs, README/GOAL/AGENTS/TASKS/PROGRESS/CREDITS/SUBMISSION.
**Interfaces:** 기존 스프라이트 파일명·크기·import 설정 유지.
- [x] 스케이터/보드·도심 배경·공사 장애물·드론·결승 표현 생성.
- [x] UI 제목·도움말·결과·Mac 안내를 실제 새 조작으로 통일.
- [x] 목표/규칙/실행법 갱신, 새 기획서 복사.
- [x] 전체 Python/에셋/C# 구문·문구·diff 검사 후 자체 리뷰, Unity 미검증 명시.
- [x] 기록 갱신·커밋·푸시 및 원격 SHA 확인.

## Execution ledger
- Ruling: 사용자가 이미 검수 보류/자율 연속 개발을 요청했으므로 새 승인 대기나 TDD 성공 주장을 하지 않는다.
- Ruling: 기존 승인된 develop에서 직접 작업한다. 원본 상태는 clean이고 부모 커밋은 61fe3cd다.

- Task 1: implementation written; Python/asset/syntax checks pass; Unity RED/GREEN and play unverified per user directive.

- Task 2: data/record migration written; Python 21/21 and asset checks pass; course completion and rank tuning unverified.

- Task 3: code/art/UI/docs conversion written; Python21, assets, syntax, metadata/placement/DOCX checks pass. Independent review: no Critical/Important; two copy fixes included. Unity32 and all actual play/build checks unexecuted.

## 실행 검수·마감 후속 작업 (2026-11-15, 사용자 연속 실행 승인)

기존 Task 1~3은 재구현하지 않는다. 최신 요청에 따라 Unity 검수 보류를 해제하고 지정 버전에서 실행한다. 현재 clean develop/a2d8de7, 원격 동일. 현재 체크아웃을 유지한다.

### Task 4: 지정 Unity에서 첫 실행과 컴파일/기존 회귀 수정
- [ ] Unity 2022.3.62f1 설치·활성화 확인 후 `python3 Tools/unity_tasks.py test` 실행. 컴파일 또는 기존 테스트 실패를 원인별로 수정한다.
- [ ] Python 전체 21건·에셋 무결성·diff 확인, TASKS/PROGRESS/README의 환경·실행 결과 갱신, 커밋·푸시.

### Task 5: 러너 물리 조작과 상태 복귀
- [ ] 기존 PlayMode 파일에 가변 점프·전방 대시/쿨다운·상하 인력/척력·부활 후 전진 회귀를 먼저 추가해 실행. 테스트에서만 입력 필드를 설정하며 런타임 입력 프레임워크는 추가하지 않는다.
- [ ] 재현된 실제 결함만 기존 PlayerMotor/GameSession에서 수정, 전체 Unity/Python 검사·diff·기록 후 커밋·푸시.

### Task 6: 원본 4코스의 완주·체크포인트 준비 거리
- [ ] 실제 Unity 물리에서 코스별 점프/대시/자력 경로를 실행해 완주와 체크포인트 재출발을 확인한다. 자동 경로 검증과 사람 키보드 플레이는 구별해 기록한다.
- [ ] 완주를 막는 배치/조작 결함을 Stage1~4.asset 또는 Motor에서 수정. 기존 GUID 유지, 검사·기록·커밋·푸시.

### Task 7: macOS 제출 실행본
- [ ] 지정 버전에서 `python3 Tools/unity_tasks.py build-mac --package`, 앱 아키텍처·실행 로그 확인. 실제 UI/키보드 플레이 가능 범위 확인, 제출 문서 갱신.
- [ ] 미검증 항목과 바로 다음 작업 기록. 전체 완료는 필수 기능과 네 코스 실제 검수 전 선언하지 않는다.

Review Focus: 바닥/벽 접촉과 대시 마지막 스텝, 자력 해제/범위 이탈, 정지 중 버튼 해제, 체크포인트 후 회피 거리, 기록 키 분리와 사용자 기록 보존. 각 단계는 위 공유 PlayerMotor/GameSession 인터페이스와 원본 StageDefinition을 그대로 사용한다.

Follow-up execution: Task4/5 implemented and verified (c17fff8), Task6 automatic four-course routes and all11 checkpoint run-ups verified (Unity18+23); human keyboard route QA remains. Task7 in progress. No course data changes were needed for the automated routes.


### Task 8: 한국어와 큰 화면 (추가 사용자 요청)
- [ ] GameHud/GameSession/StageInfo/Stage1~4 안내 번역, Mac 한글 폰트 명시, GameOptions 첫 전체 화면·큰 창 모드, 창 크기 변경 허용. 실제 빌드에서 글자/레이아웃/화면 크기 확인.
- [ ] 리뷰 Important: 원본 플레이 테스트는 Build Settings의 모든 원본 경로를 검사해 편집 리비전이면 키/씬 변경 전에 건너뛴다. 실제 대체 GUID/경로 fixture로 skip 확인.
- [ ] 전체 Unity/Python 검사·기록·커밋·푸시.

### Task 9: 자석 레일 그라인드
Files: MagneticRail.cs(.meta), PlayerMotor/PlayerVisual/StageDefinition/StageBootstrap/GameSession/GameHud, 기존 PlayMode 테스트.
Interface: MagneticRail.Configure(start,end,catchRange), CanCatch(position,boardOffset), HeightAt(x), All 목록. PlayerMotor.RailEnabled/IsGrinding, 기존 Q/E/Space/Shift 입력·SetFrozen/ResetAt 사용. StageDefinition.Rails(Start,End,CatchRange) 배열.
- [ ] 먼저 실제 레일 포착·속도·Space/E 도약·Q 해제·끝점 이탈·동결/부활 초기화 회귀를 작성하고 실패 확인.
- [ ] 직선/경사 레일 표현과 Motor의 제한된 레일 상태를 구현한다. 기본 전진7, 그라인드 배율1.35, 포착 범위1.1, 재포착 잠금0.2초. 자력은 수평을 뒤집지 않는다.
- [ ] 전체 테스트·diff·기록·커밋·푸시.

### Task 10: 길어진 4코스와 기록 분리
Files: Stage1~4.asset, StageRecords/랭크·생명주기 테스트, README/GOAL/TASKS/PROGRESS.
- [ ]248/320/380/480 길이·42/54/64/82초 기준으로 확장하고 2코스부터 레일을 배치, CP1초 준비 거리·난이도 점증 유지. v2 기록 prefix로 이전 기록 보존.
- [ ] 실제 Unity 네 코스 무사망 경로·모든 CP·필수 조작 회귀 후 Mac 앱/ZIP 재빌드·한국어/큰 화면 확인. 완주 미확인 상태를 완료로 표시하지 않는다.
