# Magnet Escape 개발 이력

## 현재 상태 요약

- 개발 브랜치: develop. 이번 연속 작업의 마지막 게임 코드 커밋은 159bdbf이며 34~42번 작업을 단계별 커밋·푸시했다.
- 추가 구현: 씬 전환/오류 복귀, 기록 대상/연습 실행 보호, Mac 창 모드·SafeArea, 경로/부활/반경 핸들, Mac ZIP 패키징, 저장 예외 흐름, 핸들/캐시 보완, 실제 지형 배경/카메라, 플레이어 프리팹 연결.
- 이번에 실제 통과한 범위: Python 회귀 테스트 21건, 에셋 무결성 검사(식별자 63개·원본 씬 5개·스프라이트 16개), C# 24개 파일 구문 파싱과 공백 검사. C# 의미 분석·Unity 실행을 뜻하지 않는다.
- 이전에 확인한 원격 CI는 b886825의 run 43146400001 등이다. 이번 최신 커밋의 원격 CI 결과는 대기하며 조회하지 않았다.
- 미실행: Unity EditMode 16건, PlayMode 9건, 실제 코스 완주, 편집 도구/프리팹 실행, macOS 앱 빌드·ZIP·실행·서명. 미검증으로 유지한다.
- Mac 명령: `python3 Tools/unity_tasks.py test`, `python3 Tools/unity_tasks.py build-mac --package`.
- 기본 필수 기능은 코드로 존재한다. 기획 범위를 늘리기보다 발견한 실제 빈틈·마감 작업을 진행하며 전체 완료는 선언하지 않는다. 아래는 누적 이력이고 최신 항목이 이전 상태를 대체한다.

## 2026-11-12 — 환경 조사, 새 프로젝트와 첫 그레이박스 구현

### 저장소와 환경

- 대상: https://github.com/JINHO7297/magnet-escape, 개발 브랜치 develop.
- 사용자가 지정한 개발 환경: macOS. Mac의 칩·OS 버전과 Editor 설치 상태는 아직 확인하지 않았다.
- 현재 실행 환경: Debian GNU/Linux 13. Unity, unity-editor, unityhub, dotnet, mono, csc 명령 없음. /opt·/usr/local 검색에서도 Unity Editor를 찾지 못함.
- 초기 /workspace는 Git 저장소가 아니었으며 게임 파일도 없었다. 초안은 work/magnet-escape-plan에 준비한 후 새 로컬 저장소 work/magnet-escape의 루트 문서로 반영했다.
- 처음 GitHub 저장소 생성은 CreateRepository 권한 부족으로 거부됨. 사용자가 직접 새 저장소를 생성했으며 이후 gh repo view에서 isEmpty=true, viewerPermission=ADMIN과 git ls-remote의 빈 결과를 확인함. 기존 Unity 버전·씬·프리팹·GUID·사용자 코드 없음.
- Unity 공식 릴리스 API에서 2022.3.62f1, 리비전 4af31df58517 및 MAC_OS X86_64/ARM64 다운로드 존재를 확인하고 고정함. macOS 실제 설치·활성화·실행은 미확인.

### 단계별 변경

1. 루트 AGENTS.md, GOAL.md, TASKS.md, PROGRESS.md, README.md를 작성했다. 개발 브랜치와 Unity 최소 프로젝트 구조를 만들었다.
2. PlayerMotor에 좌우 이동, 가변 점프, 코요테 시간·입력 버퍼, 방향 대시·쿨다운, 범위 내 가장 가까운 자석에 대한 인력·척력을 작성했다. 입력은 Update, 물리는 FixedUpdate에서 처리한다.
3. GameSession에 중복 사망 방지, 0.2초 부활, 세션 체크포인트, 재시작, 클리어 시간·사망·랭크와 PlayerPrefs 최고 기록을 작성했다.
4. MovingPlatform, RotatingSaw, Hazard, Checkpoint, ExitGate, MagnetAnchor를 작성했다. 발판의 물리 속도를 플레이어에 반영하며 실제 탑승 안정성은 미검증이다.
5. StageSelect 및 Stage1~Stage4 씬과 EditorBuildSettings를 구성했다. 각 씬은 StageBootstrap을 통해 런타임에 기본 도형 코스를 생성한다. 완성형 아트나 수동 배치 프리팹은 없다.
6. PlayerVisual의 임시 변형·색상 애니메이션, 합성 효과음, 파티클, 자력 범위 표시, IMGUI 선택·HUD·결과 창을 작성했다.
7. Unity EditMode 랭크 테스트 10건, 씬 역직렬화 테스트 5건, 빌드 순서 테스트 1건과 Python 정적 검사 도구를 추가했다.

### 변경 파일

- 루트: .gitignore, AGENTS.md, GOAL.md, TASKS.md, PROGRESS.md, README.md.
- ProjectSettings: ProjectVersion.txt, EditorBuildSettings.asset, TagManager.asset.
- Packages/manifest.json: 필요한 내장 모듈과 Unity Test Framework 1.1.33.
- Assets/_MagnetEscape/Scripts: PlayerMotor.cs, GameSession.cs, StageBootstrap.cs, PlayerVisual.cs, MovingPlatform.cs, RotatingSaw.cs, Hazard.cs, Checkpoint.cs, ExitGate.cs, MagnetAnchor.cs, MagnetEscape.Runtime.asmdef.
- Assets/_MagnetEscape/Scenes: StageSelect.unity, Stage1.unity, Stage2.unity, Stage3.unity, Stage4.unity.
- Assets/_MagnetEscape/Tests/Editor: RankTests.cs, SceneReferenceTests.cs, MagnetEscape.Editor.Tests.asmdef.
- Assets 하위 파일과 폴더의 .meta를 함께 추가했다. 신규 GUID를 고정했으며 기존 참조를 교체한 것은 없다.
- Tools/validate_project.py: 메타·GUID·씬·어셈블리·레이어 정적 검사.

### 실제 검증

- Python 정적 검사: 에셋 .meta 존재, GUID 유일성, 씬의 스크립트/로컬 참조, 5개 빌드 씬 순서, asmdef 참조, 패키지 선언과 Ground 레이어를 검사함.
- C# 구문 검사: 별도 임시 경로에 설치한 tree-sitter 0.25.2, tree-sitter-c-sharp 0.23.1로 구문 오류 여부를 검사함. 파서는 저장소 의존성이 아니다. Unity API의 타입·메서드 적합성을 검증하는 컴파일이 아니다.
- YAML 구조 검사와 최종 staged diff 검사의 결과는 아래 최종 검증 항목에 기록한다.
- Unity 컴파일, Editor 임포트, EditMode 16건 실행, Play 모드, macOS 빌드·실행: 모두 미수행. 여기서는 Editor가 없다.

### 알려진 문제와 제약

- 목표는 미완료다. 네 스테이지는 코드상 배치가 있으며 실제 완주 가능성과 난이도는 미검증이다.
- 씬 참조의 정적 일관성을 확인해도 Unity가 정상 역직렬화한다고 보장하지 않는다. Unity 테스트를 실제 실행해야 한다.
- 이동 발판 탑승·하차, 자력 중 방향 제어, 대시 후 충돌·중력 복원, 연속 사망과 부활을 우선 플레이 검증해야 한다.
- 자력과 대시를 이용해 학습 구간을 건너뛸 수 있는지는 미검증이다. 특히 Stage 3의 척력 사용을 유도하는 코스 조정이 필요하다.
- 가시와 톱날은 위험 도형이고, 로봇은 사각형이다. 최종 아트·Animator·오디오 에셋과 표현 보완이 남았다.
- UI는 IMGUI 기반 최소 구성이다. 다양한 Mac 해상도와 실제 키보드 포커스를 확인해야 한다.
- 체크포인트는 앱 종료 후 저장하지 않는다. 최고 시간과 최고 랭크는 별도로 저장한다.
- macOS 코드 서명·배포·라이선스 설정은 하지 않았다.

### 다음 작업

1. Mac Unity Hub로 2022.3.62f1을 설치·활성화하고 프로젝트를 임포트한다.
2. 콘솔 오류 및 씬 Missing Script를 확인하고 EditMode 테스트 16건을 실행한다.
3. Stage 1부터 차례대로 완주하고 이동·발판·자력·부활 수치를 조정한다.
4. 임시 표현을 보완하고 macOS 빌드를 테스트한다.
5. 실제 결과를 TASKS.md와 이 파일에 반영한다. 검증되지 않은 항목은 완료 처리하지 않는다.

### 최종 정적 검증 결과

- `python Tools/validate_project.py`: PASS, 에셋 식별자 24개와 5개 씬의 정적 참조·빌드 순서·레이어·어셈블리 확인.
- tree-sitter C# 구문 파싱: PASS, C# 파일 12개. 의미 분석과 Unity API 컴파일은 아님.
- PyYAML 구문 파싱: PASS, 씬·설정·메타 파일 31개. Unity 역직렬화는 아님.
- `git diff --cached --check`: PASS, 최초 구현 54개 파일의 staged 변경 검사.
- Unity 테스트 16건은 작성만 했으며 통과했다고 표시하지 않는다.


### 로컬 커밋 및 원격 반영 차단

- 로컬 develop 최초 구현 커밋: `82fa02c` (`feat: bootstrap Magnet Escape four-stage graybox prototype`).
- `git push -u origin develop`: GitHub 403, `Permission to JINHO7297/magnet-escape.git denied to JINHO7297`로 실패했다.
- 연결 도구로 저장소 메타데이터를 다시 조회하니 계정의 push 권한은 true였다. 그러나 `.gitignore` 초기화를 위한 Contents API 쓰기도 403 `Resource not accessible by integration`으로 실패했다. 계정 권한과 연결 인증의 쓰기 권한은 별개다.
- 위 쓰기는 둘 다 실패했으며 원격 브랜치나 커밋이 생성되었다고 주장하지 않는다. GitHub 연결의 대상 저장소 접근 및 Contents 쓰기 권한 갱신이 필요하다. 사용자에게 연결 갱신을 요청했다.
- 환경의 자동 승인 심사에 의한 거절은 아니다. GitHub 서버가 접근을 거부했다.
- 원격 권한이 복구되면 이 로컬 저장소에서 `git push -u origin develop`을 실행하고 원격 SHA가 로컬 HEAD와 일치하는지 확인한다. 코드 재작성은 필요 없다.
- 로컬 작업과 원격 차단 상태를 문서에 기록하는 후속 커밋도 준비한다.

## 2026-11-12 — 연결 갱신 후 재시도

- 사용자가 GitHub 연결 권한 갱신을 완료했다고 알려 재시도했다.
- 새 `git push -u origin develop` 호출도 GitHub 403으로 실패했다.
- 연결 도구의 `.gitignore` 생성도 403 `Resource not accessible by integration`으로 실패했다. 원격 변경은 발생하지 않았다.
- 저장소 조회는 가능하며 사용자 계정의 push=true가 반환된다. 이는 연결 앱에 Contents 쓰기 권한이 있음을 뜻하지 않는다.
- 현재 연결의 manageable installations, installed accounts, `gh api user/installations` 조회 결과는 모두 비어 있다. 앱 설치가 실제로 없는지, 현재 인증에서 설치를 조회할 수 없는지는 단정하지 않는다.
- 다음 조치: GitHub Settings > Applications > Installed GitHub Apps에서 ChatGPT/Codex 연결 앱 설치와 magnet-escape 저장소 접근을 확인하고 연결을 재인증한다.
- 기존 구현 커밋은 로컬 develop에 보존되어 있다. Unity 실행 검증 상태에는 변화가 없다.


## 2026-11-12 — 원격 푸시 성공

- 사용자 권한 변경 후 `git push -u origin develop`이 성공했다. 이전 GitHub 403 차단은 해소되었다.
- 원격 develop 생성 및 추적 브랜치 설정을 확인했다.
- 최초 성공 푸시 후 `git rev-parse HEAD`와 `git ls-remote origin refs/heads/develop` 모두 `879f3b132cd52e543dbd78abe7a156de750287a1`로 일치했다.
- 변경 파일: TASKS.md의 원격 반영 상태를 검증됨으로 변경하고 이 문서에 성공 결과를 추가했다.
- 이 문서 변경도 별도 커밋으로 develop에 반영한다. 게임 코드는 수정하지 않았다.
- Unity 컴파일·EditMode 테스트·플레이·macOS 빌드는 여전히 미검증이다. 다음 작업은 README에 따라 Mac Unity에서 프로젝트를 열고 테스트 16건과 Stage 1 조작을 확인하는 것이다.


## 2026-11-12 — 표현 구현 1: 픽셀 아트와 애니메이션

- 사용자 요청에 따라 Unity 실행 검증은 나중으로 미루고 구현을 이어간다. 완료 기준은 유지한다.
- 자체 제작 PNG 16개를 Resources/Art에 추가했다. 로봇 8프레임, 철판, 가시, 톱날, 자석, 체크포인트 2상태, 출구, 공장 배경이다.
- Tools/generate_art.py에 재생성 소스를 포함했다. 선택적 authoring 의존성은 Pillow이며 게임 런타임 의존성은 없다.
- GameArt.cs와 StageBootstrap.cs에 스프라이트 로딩·배치·배경을 연결했다. 새 파일에만 .meta를 추가하고 기존 GUID는 유지했다.
- PlayerVisual.cs에 상태별 프레임, 좌우 방향 전환, 대시 잔상을 추가했다. PlayerMotor.cs는 시각 효과용 상태를 노출한다.
- Checkpoint.cs는 활성화된 깃발로 교체한다. 톱날은 원형 트리거로 변경했다.
- README.md, TASKS.md를 갱신했다. PNG 컨택트 시트를 로컬에서 열어 에셋 외형을 확인했다. 이는 게임 렌더링 검증이 아니다.
- 다음 작업: 일시정지·UI·학습 안내·체크포인트 흐름과 효과음 보완.
- Unity 컴파일·플레이·애니메이션 전환·충돌 판정·Mac 빌드는 미검증이다.
- 가능한 검사: Python 에셋·GUID·씬 참조 검사 PASS(43개 식별자), C# 구문 파싱 PASS(13개 파일), staged diff 공백 검사 PASS. Unity 테스트는 실행하지 않았다.


## 2026-11-12 — 플레이 흐름과 4코스 확장

- 사용자 지시: 실행 검증은 기록하여 보류하고 구현을 계속한다. Unity 테스트와 플레이는 수행하지 않았다.
- GameHud.cs: 공장 테마 선택 카드, 최고 기록, 대시 충전 막대, 진행 막대, 자력 상태, 체크포인트 알림, 안내 메시지, 결과·일시정지 화면.
- GameSession.cs: Esc 일시정지·재개, 포커스 상실 자동 정지, 시간 복원, 키보드 스테이지 선택/다음 코스, 부활 카메라 스냅, 사망·클리어 시 물리 정지, 최고 시간 당시 사망 횟수 저장.
- GameOptions.cs와 GameAudio.cs: 음량 저장·음소거, 사망/점프/대시/체크포인트/클리어를 구별하는 합성 효과음 5종.
- PlayerMotor.cs와 PlayerVisual.cs: 일시정지 중 입력 수집 방지, 입력 초기화, 부활 잔상 제거, 자력 연결 대상선, 짧은 점프 입력이 두 번 잘리는 처리 수정.
- Checkpoint.cs: 순번을 부여해 이전 체크포인트로 되돌아가도 부활 위치가 과거로 변경되지 않도록 했다.
- StageInfo.cs와 TutorialSign.cs: 코스 제목과 구간별 조작 안내.
- StageBootstrap.cs: 4코스를 약 38/60/52/81 유닛으로 확장하고 발판·자석·체크포인트·위험물 및 안내를 추가했다. Stage 3에 낮은 자석을 두어 척력 도약을 안내한다. 공용 물리 재질을 재사용한다.
- README.md, GOAL.md, TASKS.md를 현재 조작·저장 정책에 맞춰 갱신했다. 기존 .meta GUID는 보존했다.
- 보류 검증: 화면 레이아웃·실제 음향·포커스 자동 정지·재개 입력·부활 물리·자력선·네 코스 완주·랭크 기준·Mac 빌드. 코드 작성으로 통과 처리하지 않았다.
- 다음 구현: 에디터에서 코스를 확인하고 편집할 수 있는 생성 도구.
- 이번 단계의 파일 정리: C# 구문 파싱과 staged diff 공백 확인만 수행했다. Unity 의미 분석·실행 테스트는 보류했다.


## 2026-11-12 — 편집용 씬 및 프리팹 생성 도구 구현

- Editor/StageAuthoring.cs와 전용 Editor asmdef를 추가했다. 런타임 빌드에 UnityEditor 의존성이 포함되지 않도록 분리했다.
- Tools > Magnet Escape에 편집용 Stage1~4 씬 생성, 편집본/원본 빌드 씬 선택, 선택된 로봇의 프리팹 저장 메뉴를 구현했다.
- 원본 씬과 기존 편집본·프리팹은 덮어쓰지 않는다. 새 씬은 additive로 준비하고 저장 후 닫으며 기존 활성 씬을 유지하도록 했다.
- 편집본에서 동적 생성기가 다시 실행되지 않도록 제거하고, 참조 재질은 에셋으로 저장하도록 구현했다. 기존 GUID는 유지했다.
- PlayerMotor의 대시·자력 활성화와 GameSession의 코스 끝 위치를 직렬화하여 편집본에서도 스테이지별 설정을 유지하도록 했다. StageBootstrap의 생성 코드를 에디터에서도 호출할 수 있게 분리했다.
- README.md, AGENTS.md, TASKS.md에 선택적 편집 워크플로와 사용자 검증 보류 지시를 반영했다.
- 도구 코드는 작성했지만 Unity에서 메뉴를 실행하지 않았다. 편집본 씬·프리팹·Generated 재질이 실제 생성되었다고 표시하지 않는다.
- 보류 검증: 도구 컴파일, 편집본 생성·저장·재로딩, 원본/기존 씬 보호, 프리팹 참조, 빌드 설정 전환과 각 코스 실행.
- 이번 단계도 C# 문법과 staged diff 공백만 확인했다. Unity 컴파일·테스트·플레이는 보류했다.


## 2026-11-12 — 저장소 재분석 및 자동 검사 보강

- AGENTS.md, GOAL.md, TASKS.md, PROGRESS.md와 실제 변경 이력을 대조했다. 시작 HEAD는 f384d7d이며 작업 트리는 깨끗했고 git fetch 후 원격 추가 커밋이 없었다.
- 직전 구현 3커밋에는 아트·애니메이션, 4코스·HUD·일시정지·음향, 편집 도구가 이미 포함되어 있었다. 기능을 중복 구현하지 않고 미완료 14번(자동 검증과 재현 가능한 실행 절차)을 우선했다.
- Tools/validate_project.py에 PNG CRC·RGBA 형식·스프라이트 임포트, 필수 16종 스프라이트, 런타임의 Editor 전용 어셈블리 참조 검사를 추가했다.
- Tools/tests/test_validate_project.py에 정상 저장소와 메타 누락·중복 GUID·잘못된 씬/빌드 참조·레이어·스프라이트 손상·잘못된 임포트·Editor 의존성 등 회귀 테스트 10건을 추가했다. 테스트는 임시 복사본에서 수행한다.
- .github/workflows/static-checks.yml에 develop/main 푸시·PR의 읽기 전용 CI를 작성했다. .gitignore에 __pycache__를 추가하고 README.md, TASKS.md를 갱신했다.
- 실행 결과: 정적 무결성 검사 PASS(식별자 51개, 씬 5개, 스프라이트 16개), Python unittest 10/10 PASS, git diff --check PASS.
- 보류: Unity 컴파일·EditMode·PlayMode·실제 완주와 macOS 빌드. 현재 환경에서 Unity 실행 명령을 다시 확인했으나 찾지 못했다.
- 다음 작업은 기존 부활·체크포인트·일시정지의 PlayMode 회귀 테스트와 Mac 빌드 자동화다. Unity 부재를 이유로 다음 구현을 중단하지 않는다.


## 2026-11-12 — 세션 PlayMode 회귀 테스트 작성

- 테스트 8건을 Tests/PlayMode/SessionLifecycleTests.cs에 추가했다. 기존 게임 기능 구현은 변경하지 않았다.
- 대상: 4코스 로딩과 능력 플래그, 중복 사망 집계·빠른 부활, 체크포인트 역행 방지, 일시정지 중 시간·물리 정지, 클리어 후 시간·사망 정지, 재시작, 선택 화면 전환 후 시간 배율, ResetAt의 운동량·중력 초기화.
- 사용자의 실제 키 입력에 의존하지 않도록 모터 입력 처리를 테스트 중 비활성화한다. 이 검사는 입력 조작감·정밀 물리·완주 검증을 대신하지 않는다. 테스트가 변경한 Stage 1 최고 기록은 teardown에서 복원한다.
- PlayMode 테스트 어셈블리는 UNITY_INCLUDE_TESTS 조건과 TestAssemblies 참조를 사용하며 런타임 게임 코드에 테스트 의존성을 추가하지 않았다.
- 실제 실행: 정적 무결성 PASS(식별자 54개), Python unittest 10/10 PASS, C# 구문 파싱 PASS(20개 파일). 의미 분석 컴파일은 아니다.
- 원격 GitHub Actions run 43145623736이 7bcbb12에서 success임을 확인했다: https://github.com/JINHO7297/magnet-escape/actions/runs/43145623736
- 별도 보류: Unity EditMode 16건과 이번 PlayMode 9건은 Unity가 없어 모두 미실행. 통과했다고 표시하지 않았다.
- 다음 작업: macOS 빌드 진입점과 테스트 실행 스크립트를 구현한다.


## 2026-11-12 — Mac 테스트·빌드 자동화

- Editor/MacBuild.cs: macOS 지원 모듈·빌드 대상·5개 씬 순서·씬 파일·Ground 레이어를 확인하고 .app을 빌드하는 메뉴/CLI 진입점을 추가했다. Unity BuildReport 실패는 예외로 반환하고 결과 요약을 .build.txt로 저장한다.
- Tools/unity_tasks.py: 프로젝트 버전에 맞는 Mac Editor 경로, 공백 포함 인수, EditMode/PlayMode 순차 실행, 고유 실행별 산출물, 테스트 XML 판정, macOS 빌드 호출과 dry-run을 구현했다.
- Unity 종료 코드가 0이어도 결과 누락·0건·실패·모두 건너뜀은 통과로 처리하지 않는다. dry-run도 실행 성공으로 표시하지 않는다.
- Tools/tests/test_unity_tasks.py: 가짜 Unity 실행 파일로 프로세스 실패·누락/빈 XML·스킵·공백 경로·빌드 출력 누락·dry-run을 검사하는 11건을 추가했다. 실제 Unity나 macOS 앱을 검증한 테스트가 아니다.
- 실행 결과: Python 전체 21/21 PASS, 에셋 정적 검사 PASS(55개 식별자), C# 구문 파싱 PASS(21개 파일), 테스트·빌드 dry-run 명령 생성 PASS, git diff 공백 검사 PASS.
- 실제 Unity 실행도 `python Tools/unity_tasks.py test --suite EditMode`로 시도했으나 기본 경로의 Editor가 없어 종료 코드 1과 Unity executable unavailable을 반환했다. Unity 테스트가 실행된 것으로 표시하지 않았다.
- README.md, GOAL.md, TASKS.md와 현재 상태 요약을 갱신하고 artifacts/를 Git에서 제외했다. README·기록 존재 조건만 확인 완료로 변경했으며 게임 실행 조건은 모두 미완료다.
- 별도 미검증: MacBuild C# 의미 분석·모듈 감지·실제 .app 생성, 테스트 24건, 에디터 씬 생성, 플랫폼 아키텍처·서명·공증·코스 완주. 현재 환경에서는 Unity가 없어 해당 실행이 불가능하다.
- 다음 우선순위: Unity 실행 환경에서 컴파일/24건 실행, 발견된 실제 실패 수정, Stage 1부터 코스 난이도 조정. 그동안 코드로 확인 가능한 결함은 계속 수정할 수 있다.


### 최종 원격 반영 확인

- 구현·테스트 3개 커밋: 7bcbb12(무결성·CI), 3f0b23e(PlayMode 9건), b886825(Mac 테스트·빌드 도구).
- b886825에서 로컬 HEAD와 origin/develop의 SHA가 일치했고 작업 트리가 깨끗했다.
- 최종 코드 CI run 43146400001은 completed/success다: https://github.com/JINHO7297/magnet-escape/actions/runs/43146400001
- 이 항목은 성공 결과를 기록하는 문서 변경이며 실제 Unity 테스트 24건·Mac 빌드 미실행 상태는 유지한다.


## 2026-11-12 — 구현 우선: 장애물 판정과 타이밍 코스

- 사용자 지시에 따라 검증 도구를 더 추가하지 않고 게임 구현을 진행한다. 실행 검증은 미검증으로 남긴다.
- StageBootstrap.cs: 가시의 사각 트리거를 비활성화하고 반복 스프라이트의 톱니·마지막 잘린 타일을 따라가는 PolygonCollider2D를 추가했다. 투명 모서리에서 사망하는 판정을 줄이기 위한 변경이다.
- RotatingSaw.cs: Rigidbody2D 기반 회전과 왕복 이동, 주기·위상·이동량 설정, 월드 좌표의 붉은 경로 레일을 구현했다. 기존 편집 씬의 Rigidbody 없는 톱날도 초기화 때 보완하도록 했다.
- Stage 2와 Stage 4에 서로 다른 이동 방향·주기의 톱날 6개를 배치하고 안내를 변경했다. 추가 RNG·전투·퍼즐 시스템은 만들지 않았다.
- TASKS.md와 README.md에 변경을 반영했다. 기존 씬과 .meta GUID는 변경하지 않았다.
- 검증 보류: 가시 모서리·마지막 타일 충돌, 순찰 톱날의 트리거·일시정지, 코스 완주와 난이도. 이번 변경으로 정상 작동을 확인했다고 표시하지 않는다.
- 다음 구현: 자력 선택 대상 안정화, 자석별 반경·세기와 해제 후 운동량.


## 2026-11-12 — 구현 우선: 자력 조작 보완

- PlayerMotor.cs: 자력 버튼을 누르는 동안 대상이 유효하면 고정하고, 놓거나 범위를 벗어날 때 가장 가까운 대상을 재선택한다. 인접한 자석 사이에서 유지 중 대상이 바뀌는 현상을 줄이기 위한 변경이다.
- MagnetAnchor.cs: 자석별 반경·세기, 플레이어 도달 거리를 반영하는 범위 표시, 선택/인력/척력 색 구분을 추가했다. 기존 편집 씬의 범위 LineRenderer를 재사용한다.
- 자력 해제 후 공중 수평 속도를 점진적으로 되돌리는 관성 상태와 조정 수치 Magnetic Air Control을 추가했다. 일반 이동은 기존 방식이며 착지·대시·부활 시 자력 관성을 정리한다.
- StageBootstrap.cs: Stage 3의 척력 자석을 짧은 반경·강한 세기로, 높은 이동용 자석을 별도 세기로 구성했다. GameHud.cs는 범위 밖/대시 중 상태를 구분한다.
- README.md와 TASKS.md에 선택 유지·반경·관성 정책을 기록했다. 기존 GUID·씬 참조는 유지했다.
- 미검증: 입력 유지·해제·범위 이탈·Q/E 전환, 범위 원 일치, 자력 관성과 발판·대시 상호작용, 코스 완주. 사용자의 구현 우선 지시에 따라 실행 검증을 기다리지 않고 진행한다.


## 2026-11-12 — 구현 우선: 빠른 재시도와 기록 피드백

- GameSession.cs: Backspace(⌫)와 일시정지 메뉴에서 체크포인트로 빠르게 돌아오는 RetryCheckpoint를 추가했다. 기존 사망/부활 흐름을 재사용하므로 1회 사망 페널티와 경과 시간을 유지한다. 부활 중·클리어 후 중복 요청은 무시한다.
- 결과 상태에 이전 최고 시간·시간 신기록·랭크 신기록을 보관하고 GameHud.cs에 첫 클리어, 신기록, 이전 기록 대비 시간 차를 표시했다. 최고 시간과 독립적인 최고 랭크를 선택 화면에서도 구별하고 완료 스테이지 수를 표시한다.
- Stage 1 안내에 체크포인트 재시도와 전체 재시작의 차이를 명시했다.
- AGENTS.md/TASKS.md를 최신 구현 우선 지시에 맞췄으며, GOAL.md/README.md에 재시도 페널티를 기록했다. 이전 자동화 결과는 b886825 기준임을 표시하여 이번 미검증 게임 변경과 구분했다.
- 실행 검증 미수행: 재시도 중복 입력·일시정지 해제·시간 유지·사망 집계, 첫 클리어/신기록/기록 미경신, UI 배치와 Mac 키보드 입력. 추가 검증 도구를 만들며 개발을 지연하지 않았다.


## 2026-11-12 — 자율 개발: 조작과 착지 피드백

- 별도 지시를 기다리지 않고 구현을 이어가라는 사용자 선호를 AGENTS.md에 기록했다. 대기·확인 질문 없이 다음 구현으로 진행한다.
- PlayerMotor.cs에 벽·바닥·천장으로 막힌 대시의 종료를 추가했다. 새 충돌과 이미 접촉 중인 충돌을 처리하고 중력을 복원하며 쿨다운은 유지한다.
- 기존 코요테·점프 버퍼·점프 높이 조절·접지 탐색 수치를 Inspector에 노출했다. 0초 설정에서도 해당 물리 프레임의 직접 입력은 처리하도록 했다.
- 착지 전 수직 속도와 발판 상대 속도를 사용해 착지 효과를 요청한다. GameAudio는 숫자 주파수에 의존한 종류 선택에서 명시적인 Cue enum으로 바꾸고 착지음을 포함한 6종을 제공한다.
- 부활 시 이전 지지 속도·착지 속도·방향·잔상·자력선을 정리하도록 보완했다.
- 변경 파일: AGENTS.md, TASKS.md, README.md, PlayerMotor.cs, PlayerVisual.cs, GameAudio.cs, GameSession.cs. 기존 .meta와 GUID는 유지했다.
- 미검증: 모서리/경사면/천장·바닥·벽 대시, 점프 버퍼와 유예 시간 경계, 착지 판별·효과음·먼지, 부활 직후 방향. 실행 검증을 기다리지 않고 다음 작업을 진행한다.


## 2026-11-12 — 자율 개발: 부활 위치 편집

- Checkpoint.cs에 지정 가능한 Respawn Marker와 이전 씬 호환용 Spawn Offset을 추가했다. 마커가 없는 기존 체크포인트는 이전처럼 기본 위로 1 유닛 위치를 사용한다.
- StageBootstrap.cs가 새 코스를 만들 때 각 체크포인트 아래 Respawn point를 생성한다. 편집용 복사본에서는 해당 자식을 직접 옮길 수 있다. 원본 씬이나 기존 편집본은 덮어쓰지 않는다.
- 체크포인트 선택 시 부활 위치와 플레이어 크기를 Gizmo로 표시한다. 활성화된 깃발에는 느린 밝기 변화를 추가했다.
- GameSession.cs는 최초 플레이어의 실제 위치를 기억한다. 편집 씬에서 플레이어를 옮겨도 첫 부활이 고정 (0, 1)로 돌아가던 문제를 보완했다. 낙사 높이와 부활 대기 시간도 직렬화했다.
- 변경 파일: Checkpoint.cs, StageBootstrap.cs, GameSession.cs, TASKS.md, README.md. 기존 GUID 유지.
- 미검증: 편집본 저장·재로딩 후 마커 참조, 기존 씬 호환, 이동한 초기 위치의 부활, 낙사 기준·대기 시간, 깃발 표시. 구현을 계속 진행한다.


## 2026-11-12 — 자율 개발: 발판 경로와 출구 표현

- MovingPlatform.cs에 월드 좌표의 금색 경로, Scene 선택 Gizmo, Configure 진입점과 0초 주기 보호를 추가했다. 기존 Travel/Period 필드는 유지하고 이전 편집 씬도 Awake에서 경로를 구성한다.
- 실행 중 Travel이 바뀌면 표시 경로도 갱신한다. 실제 운동은 기존처럼 FixedUpdate/MovePosition이며 표시 오브젝트에는 충돌 판정이 없다.
- ExitGate.cs에 출구 외곽선·느린 밝기 변화·접근 강조·클리어 색상을 추가했다. 기존 LineRenderer가 있으면 재사용하고 새 렌더러는 공용 재질을 사용한다.
- StageBootstrap.cs는 초기화 함수를 통해 발판과 출구를 만들고, Stage 2의 금색/붉은 경로 안내를 갱신한다. 편집 도구에서도 표시가 구성되도록 했다.
- TASKS.md/README.md와 현재 상태 요약을 갱신했다. 기존 에셋 GUID는 유지했다.
- 미검증: 경로의 시작·끝과 실제 운동 일치, 주기 변경, 기존 씬/새 편집본 재로딩, 출구 강조·UI 가독성·일시정지, macOS 출력. 실행 검증을 수행했다고 표시하지 않는다.


## 2026-11-12 — 제출 준비 개발: 코스 데이터 에셋

- StageDefinition.cs와 Resources/Stages/Stage1~4.asset을 추가하고 기존 하드코딩 배치를 에셋으로 이전했다. 기존 배치 호출 14/19/15/30개와 각 코스 공통 시작 발판을 옮겼다. 시작점·출구·대시/자력 허용·낙사·부활·기준 시간·안내도 함께 정의한다.
- StageBootstrap.cs에서 스테이지별 중복 분기를 제거하고 에셋 배열을 읽어 기존 생성 함수를 호출하도록 변경했다. 신규 이동 발판 폭·주기와 톱날 위상도 데이터로 조정한다.
- StageInfo.cs의 제목·설명·ParTime과 GameSession.cs의 랭크 계산, GameHud.cs의 목표 시간 표시가 같은 데이터를 사용한다. 기본 기준 시간과 기존 코스 수치는 유지했다.
- Checkpoint.cs에는 데이터 적용용 SpawnOffset 속성을 추가했다. 기존 편집본은 바꾸지 않는다.
- 신규 에셋에만 GUID/.meta를 추가했다. 기존 씬·스크립트 GUID와 편집 도구는 유지한다. AGENTS.md, GOAL.md, TASKS.md, README.md를 원본 데이터 정책에 맞췄다.
- 미검증: ScriptableObject 임포트·역직렬화, 런타임/편집본 생성, 에셋 수정 후 반영, 기존 랭크 테스트. 실제 검수 없이 다음 편집 기능 구현으로 진행한다.


## 2026-11-12 — 제출 준비 개발: 코스 데이터 시각 편집

- Editor/StageDefinitionEditor.cs를 추가했다. 선택한 원본 코스 에셋의 시작·출구·고정/이동 발판·가시·톱날 경로·자석·체크포인트 생성 위치를 Scene 뷰에 그리도록 했다.
- 코스 전체 프레이밍, 종류/요소 번호 선택, 2D 위치 핸들, 선택적 안내 범위 표시를 구현했다. SerializedProperty로 위치를 수정하여 Unity의 Undo/에셋 저장 흐름을 사용한다.
- 선택 해제 시 SceneView 콜백을 해제하며, Play 중에는 편집 미리보기를 그리지 않는다. 기존 열린 씬·편집본을 변경하지 않는다.
- TASKS.md와 README.md에 편집 사용법과 미리보기의 한계를 기록했다. 새 스크립트 .meta만 추가했다.
- 미검증: CustomEditor 컴파일, 핸들/Undo, 에셋 저장, Scene 뷰 표시와 프레이밍. 검수를 기다리지 않고 제출용 마감 작업으로 진행한다.


## 2026-11-12 — 제출 준비 개발: 앱 메뉴와 동봉 문서

- GameHud.cs에 조작 안내·크레딧 패널을 추가했다. 선택/일시정지 화면과 HUD 도움말에서 접근하며, 패널이 열린 동안 뒤쪽 UI를 비활성화한다. GUI.enabled와 GUI.matrix를 복원하도록 했다.
- GameSession.cs가 Esc로 안내를 먼저 닫고, 안내 중 다른 진행 키는 처리하지 않도록 했다. HUD 도움말은 먼저 일시정지한다. 독립 실행판의 종료 버튼은 설정을 저장한 후 Application.Quit을 호출하며 편집기에서는 숨긴다.
- MacBuild.cs는 4개 코스 에셋을 확인하고 빌드 때 제품 이름을 적용한 후 finally에서 기존 값을 복원한다. 빌드 성공 후 앱 옆에 조작·코스·실제 ParTime을 담은 PLAYING.txt를 생성하도록 구현했다.
- CREDITS.md에 리소스 출처, SUBMISSION.md에 소스·앱·동봉 문서 구성을 작성하고 README.md, TASKS.md를 갱신했다. 새로운 배포 라이선스는 임의로 지정하지 않았다.
- 미검증: 메뉴/패널·입력 차단·Esc/일시정지 흐름·앱 종료, Mac 빌드/제품명 복원/동봉 파일. Unity 실행과 실제 앱 출력을 완료 처리하지 않는다.


## 2026-11-12 — 이어서 개발: 코스 리비전 생성

- StageAuthoring.cs의 공통 생성 루틴을 분리하고 고유 UTC 시각/식별자 폴더에 4개 코스를 새로 생성하는 메뉴를 추가했다. 원본·기존 편집본·이전 리비전을 덮어쓰지 않는다.
- 리비전 생성만으로 Build Settings를 바꾸지 않는다. 사용자가 리비전 폴더 또는 안의 씬을 선택한 후 별도 메뉴로 전환하도록 했다. 4코스가 모두 없거나 Play 중이면 전환하지 않는다.
- StageDefinitionEditor.cs에서 새 리비전 생성 버튼에 접근할 수 있게 했다. 열린 씬은 기존 additive 생성/복원 흐름을 그대로 사용한다.
- README.md와 TASKS.md에 데이터 변경→새 편집본→비교/전환 절차를 기록했다. 실제 리비전 파일은 Unity 메뉴 실행 후 생성된다.
- 미검증: 새 폴더/씬 생성·GUID·기존 파일 보존·중간 실패·선택과 빌드 전환. 검수는 보류하고 HUD 작업을 진행한다.


## 2026-11-12 — 이어서 개발: 체크포인트·실제 출구 안내

- GameSession.cs가 시작 시 현재 씬의 실제 ExitGate 위치를 읽도록 했다. 편집본에서 출구만 옮겨도 코스 끝·진행률·카메라에 반영한다.
- 진행률과 부활 위치 비율은 실제 시작 X와 출구 X를 기준으로 계산한다. 카메라의 수평 범위·시선 방향과 수직 범위도 시작/출구 좌표에 맞추어 고정 원점 의존을 줄였다.
- GameHud.cs에 저장된 CP 번호, 진행 막대의 금색 부활 위치 틱, 화면 밖 출구 방향을 추가했다. 화면 좌표를 HUD 스케일/여백으로 변환하고 기존 알림·하단 안내 영역을 피한다.
- 코스 리비전 메뉴의 선택 없음 처리를 보완하고, 생성 버튼에서 선택이 바뀐 뒤 GUI 처리를 종료하도록 했다. 원본으로 전환하는 메뉴도 같은 Play 모드 보호 흐름을 사용한다.
- README.md, TASKS.md, 현재 상태 요약을 갱신했다. 실행 검수는 미루며 기존 GUID를 유지했다.
- 미검증: 화면 밖/보이는 출구, 종횡비·여백, 음수·역방향 시작/출구, 진행률·카메라 범위, 체크포인트 틱, 리비전/GUI 전환. 이번 변경을 플레이 검증 완료로 표시하지 않는다.


## 2026-11-12 — 이어서 개발: 리비전 랭크 기준 보존

- GameSession.cs에 생성 당시 ParTime을 직렬화하고 실제 랭크 계산과 결과 UI가 동일한 RankTarget을 사용하도록 했다. StageBootstrap.cs가 데이터 에셋의 기준 시간을 전달한다.
- 원본 ParTime을 변경해도 새 방식으로 만든 기존 리비전의 기준은 유지한다. 필드가 없는 이전 편집본은 원본 데이터 기준을 사용하는 호환 경로를 남겼다. 기존 EvaluateRank API는 유지한다.
- GameHud.cs는 화면 크기가 0일 때 그리기를 건너뛰어 HUD 좌표 변환의 0 나눗셈을 피한다. MacBuild.cs의 동봉 안내는 원본 기준과 편집본의 실제 UI 기준을 구분한다.
- TASKS.md와 README.md에 보존 정책을 반영했다. 코스/에셋 GUID는 변경하지 않았다.
- 미검증: 리비전 저장/재로딩 후 ParTime 보존, 원본 변경 전후 랭크와 UI, 이전 편집본 호환, 최소화/화면 크기 변화. 실제 검수는 보류 상태다.


## 2026-11-12 — 이어서 개발: 실제 코스별 기록 분리

- 코스 리비전이 생겼지만 기록 키는 스테이지 번호만 사용하여 다른 코스의 최고 기록이 섞일 수 있음을 코드에서 확인했다.
- StageRecords.cs로 읽기·저장을 모으고 실제 씬 경로의 결정적 식별자로 편집본/리비전 기록을 분리했다. 원본 씬은 기존 키를 유지하여 기존 기록을 삭제하지 않는다.
- GameSession.cs는 실제 플레이 씬에 저장하고 GameHud.cs는 현재 빌드에서 선택될 씬을 찾아 읽도록 연결했다. 기존 최고 시간·독립 최고 랭크 정책은 유지했다.
- 정확히 같은 시간이라면 더 적은 사망 횟수를 기록에 반영한다. 유효하지 않은 시간 입력은 저장하지 않고, 읽은 랭크/사망은 표시 범위로 제한한다.
- 이전에 혼합 저장한 기록은 출처를 알 수 없어 자동 이동하지 않는다. 동일 파일 직접 수정은 기존 기록 묶음을 유지하며 수동 초기화가 필요한 정책을 README.md에 명시했다.
- 변경 파일: StageRecords.cs와 신규 .meta, GameSession.cs, GameHud.cs, TASKS.md, README.md. 기존 GUID 유지.
- 미검증: 원본 기록 호환·다른 리비전 기록 분리·빌드 폴더 전환 후 표시·저장/재실행·동시간 낮은 사망. 이어서 선택 초기화 UI를 구현한다.


## 2026-11-12 — 이어서 개발: 기록 패널과 선택 초기화

- GameHud.cs의 선택/일시정지 메뉴에 RECORDS를 추가했다. 현재 Build Settings가 가리키는 네 코스의 최고 시간·사망·랭크를 같은 StageRecords 경로로 표시한다.
- 코스별 CLEAR는 별도 확인 화면을 거친다. Esc/Cancel은 삭제하지 않고 목록으로 돌아간다. 확인 시 선택한 실제 코스의 세 기록 키만 제거하며 PlayerPrefs.DeleteAll은 사용하지 않는다.
- 음량·다른 코스/리비전·현재 세션 진행은 그대로 둔다. 실제 저장 데이터는 이 개발 환경에서 삭제하지 않았으며 UI 코드만 구현했다.
- README.md/TASKS.md에 사용법과 미검증 상태를 반영했다.
- 미검증: 목록/빌드 코스 매칭·확인/취소·Esc 흐름·삭제 범위·동일 플레이 중 삭제 후 재기록·패널 UI. 실행 검수는 계속 보류한다.


## 2026-11-12 — 이어서 개발: 고속 위험물 접촉 경로

- PlayerMotor.cs에 다음 물리 스텝의 속도·중력을 반영한 Collider Cast를 추가했다. 대시 분기와 일반/자력 이동 분기에서 위험물 접촉을 보완한다.
- 캐스트 결과는 미리 할당한 배열을 재사용한다. Hazard 컴포넌트로 분류하여 기존 편집 씬의 레이어를 바꾸지 않는다. 체크포인트·출구는 선행 활성화하지 않는다.
- 진행 방향을 정면으로 막는 고체를 먼저 만나면 그 뒤의 위험물은 제외한다. 바닥을 스치는 접촉은 전방 검사에서 벽으로 취급하지 않는다. 기존 OnTriggerEnter2D 접촉과 중복 사망 방지는 유지한다.
- 변경 파일: PlayerMotor.cs, TASKS.md, README.md, PROGRESS.md. 기존 GUID 유지.
- 미검증: 고속 가시/톱날, 시작 겹침·얇은 끝·벽/바닥 모서리, 움직이는 발판/톱날, 중력과 실제 접촉 시점, 중복 사망/재시도. 캐스트 추가가 문제를 완전히 해결했다고 주장하지 않는다.
- 이번 작업은 게임 구현과 파일 문법·공백 정리만 수행한다. Unity 실행·물리 검수는 계속 별도로 남긴다.


## 2026-11-12 — 연속 작업 1: 씬 전환과 오류 복구

- GameSession.cs에 단일 씬 전환 잠금, 빌드 포함 여부 확인, 동기 호출 실패 복구와 안내를 추가했다. 전환 중 중복 로드·사망·체크포인트·클리어 집계를 막고 플레이어 입력/속도 처리를 정지한다.
- GameHud.cs에 전환 표시와 COURSE UNAVAILABLE 패널을 추가했다. 안내문/선택 복귀/재시도는 플레이어 오브젝트가 없어도 동작하도록 작성했다.
- StageBootstrap.cs는 런타임의 누락/번호 불일치 데이터에 사용자 복귀 화면을 제공한다. 편집용 생성은 예외로 중단하여 잘못된 복사본 저장을 피한다.
- 변경 파일: GameSession.cs, GameHud.cs, StageBootstrap.cs, PlayerMotor.cs, TASKS.md, README.md. 기존 GUID와 정상 전환 경로는 유지한다.
- 미검증: 이중 클릭·반복 키·전환 중 물리, 누락 씬·누락 데이터·번호 불일치·R/Esc 복구, 전환 실패 시 시간 배율. 실제 Unity 검수는 보류한다.
- 단계 종료 후 커밋하고 기록 초기화/가용성 보완으로 계속 진행한다.


## 2026-11-12 — 연속 작업 2: 기록 대상 고정과 미포함 코스 보호

- StageRecords.cs는 빌드에서 코스를 찾지 못하면 원본 키로 대체하지 않는다. 없는 코스의 읽기/초기화가 원본 기록에 닿지 않도록 했다.
- GameHud.cs의 선택/기록 UI가 가용성을 표시하고 해당 시작·초기화를 비활성화한다. 초기화 확인 화면은 선택 당시 씬 경로와 제목을 고정하고 ClearCourse로 그 대상만 삭제한다.
- 저장되지 않은 씬에서의 실행은 저장 성공으로 취급하지 않으며 원본 기록을 갱신하지 않는다. GameSession.cs의 RecordSaved와 결과 화면에 이를 반영했다.
- 변경 파일: StageRecords.cs, GameHud.cs, GameSession.cs, TASKS.md, README.md. 실제 사용자 기록을 삭제한 작업은 없고 게임 UI/저장 코드를 수정했다.
- 미검증: 빠진 씬, 확인 중 빌드 대상 변경, 미저장 씬 연습·기록 보존, 정상 코스 저장/읽기·초기화. 실제 검수는 보류하고 표시 모드 작업을 이어간다.


## 2026-11-12 — 연속 작업 3: 실행판 표시 모드와 안전 영역

- GameOptions.cs에 실행판 전용 창/전체 화면 전환과 저장을 추가했다. 앱 시작의 첫 GameSession에서만 적용하며 Editor에서는 표시 모드를 변경하지 않는다.
- GameHud.cs에 선택/일시정지 메뉴 버튼을 배치하고 일시정지 패널 공간을 조정했다. GUI는 Screen.safeArea 기준으로 스케일·여백을 계산하며 유효한 영역이 없으면 화면 전체를 사용한다.
- 출구 방향 표시도 기존 HUD 좌표 변환을 통해 새 안전 영역과 같은 기준으로 그린다.
- 변경 파일: GameOptions.cs, GameSession.cs, GameHud.cs, TASKS.md, README.md. 기존 GUID 유지. 실제 Mac 화면 설정을 이 환경에서 변경한 것은 아니다.
- 미검증: Mac 전체 화면/창 전환·설정 복원, 노치·Retina·다양한 비율, 메뉴/출구 표시·볼륨과의 배치. 커밋 후 코스 편집 핸들 작업을 계속한다.


## 2026-11-12 — 연속 작업 4: 배치 보조 핸들과 전체 프레이밍

- StageDefinitionEditor.cs에 이동 발판/톱날 Travel 끝점, 체크포인트 SpawnOffset, 자석/힌트 Range의 보조 핸들을 추가했다. 중심 이동과 별도로 상대 위치·반경을 수정한다.
- 반경은 최소 0.5로 제한하고 SerializedProperty를 통해 Undo/에셋 변경 흐름을 유지한다. 부분적인 배열의 null 항목은 미리보기에서 건너뛴다.
- Frame Entire Course가 고정 높이 대신 실제 배치/경로/반경/부활 위치를 포함한 Bounds를 사용하도록 했다. 먼 곳이나 높은 곳의 요소가 프레임 밖으로 빠지는 기존 제한을 보완했다.
- README.md/TASKS.md에 편집 방법과 상태를 갱신했다. 새 에셋/GUID는 추가하지 않았다.
- 미검증: 중심/끝점 선택과 드래그·Undo·반경 제한·전체 Bounds·편집 후 생성. 커밋 후 Mac 배포 패키징 구현을 이어간다.


## 2026-11-12 — 연속 작업 5: Mac 배포 ZIP 구성

- Tools/unity_tasks.py의 build-mac에 --package를 추가했다. 실제 Mac 빌드가 성공한 뒤 앱·PLAYING.txt·CREDITS.md를 임시 폴더에 모아 ditto로 ZIP을 만드는 경로다.
- 일반 파일 ZIP 복사 대신 macOS 네이티브 복사/압축을 사용하여 앱 번들의 권한·심볼릭 링크·메타데이터를 보존하도록 했다. 원본 앱은 남기며 임시 staging만 정리하고 개발 로그는 포함하지 않는다.
- 패키지 옵션은 Mac/ditto가 없으면 실제 실행을 거부한다. dry-run은 명령 계획만 출력한다. ZIP에 앱 Info.plist와 동봉 문서가 없으면 성공으로 표시하지 않는다.
- invoke의 오류 라벨을 Unity/ditto로 구분하며 기존 Unity 실행 명령은 유지한다. README.md, SUBMISSION.md, TASKS.md를 갱신했다.
- 미검증: 실제 Mac 앱 빌드·ditto 복사/압축·번들 링크/권한·압축 해제·배포 실행·코드 서명/공증. 이 환경에서 앱 또는 배포 ZIP을 생성했다고 표시하지 않는다.


## 2026-11-12 — 연속 작업 6: 저장 실패와 연습 실행 구분

- GameSession.cs는 기록 저장의 PlayerPrefsException을 처리하여 저장 실패가 클리어 효과·결과 UI의 진행까지 끊지 않도록 했다. 예상하지 않은 일반 코드 예외는 숨기지 않는다.
- GameHud.cs가 미저장 씬의 연습 실행과 저장 장치의 기록 실패를 다르게 표시한다. 초기화 저장 예외도 목록으로 돌아가 안내하고 확인 요청이 처리 완료되었다고 단정하지 않는다.
- UI 예외 안내는 현재 세션의 일시적 상태이며 새로운 저장 데이터 형식이나 자동 삭제/재설정 정책을 추가하지 않았다.
- TASKS.md/README.md를 갱신했다. 미검증: 실제 PlayerPrefs 예외, 저장 일부 실패 후 상태, 클리어 효과·패널 유지와 목록 표시. 실제 저장 실패를 재현했다고 표시하지 않는다.


## 2026-11-12 — 연속 작업 7: 겹친 핸들과 새 플레이 캐시

- StageDefinitionEditor.cs에 보조 핸들만 편집하는 모드를 추가했다. Travel/SpawnOffset이 0일 때 중심 핸들과 끝점이 겹쳐도 끝점을 선택할 수 있게 하기 위한 변경이다.
- StageInfo.cs/GameArt.cs의 정적 캐시는 SubsystemRegistration에서 참조를 비운다. 새 Play 시 코스·아트 리소스를 다시 찾으며 리소스 에셋을 삭제하거나 파괴하지 않는다.
- README.md/TASKS.md를 갱신했다. 미검증: 0 오프셋 조작·Undo·그룹 전환, Domain Reload 비활성화·리소스 이름/임포트 변경 후 Play. 다음은 실제 배경/카메라 범위 보완이다.


## 2026-11-12 — 연속 작업 8: 실제 지형의 배경·카메라 범위

- GameArt.cs의 배경 크기를 부호 있는 출구 X값이 아니라 Bounds 기준으로 만들고, StageBootstrap.cs가 시작/출구 위치와 배경 참조를 연결한다. 음수·역방향 좌표에서 음수 SpriteRenderer.size가 생기는 경로를 제거했다.
- GameSession.cs는 시작 시 물리 Transform을 동기화하고 현재 씬의 Ground 콜라이더·이동 발판 끝·톱날 경로·자석·체크포인트 생성점을 포함해 배경과 수직 카메라 범위를 구성한다. 매 프레임 전체 씬을 탐색하지 않는다.
- RotatingSaw.cs는 경로 계산용 Travel 읽기 속성을 제공한다. 새 편집본은 배경 참조를 저장하며 이전 편집본은 동일한 장식 이름을 찾아 맞춘다.
- 필수 플레이어·Main Camera·Ground 레이어·ExitGate가 누락된 편집 코스도 오류 복귀 화면으로 안내한다. 원본 생성 단계의 Ground 누락도 감지한다.
- README.md/TASKS.md를 갱신했다. 기존 GUID를 유지하고 사용자 이름으로 바꾼 장식을 임의로 대체하지 않는다.
- 미검증: 지형 Bounds·Physics2D 동기화, 높거나 낮은 경로·음수/역방향 좌표·부활점, 배경 타일·카메라 범위, 이전 편집본·필수 요소 누락. 실행 검수는 계속 보류한다.


## 2026-11-12 — 연속 작업 9: 플레이어 프리팹 실제 연결

- 생성 도구로 저장한 로봇 프리팹이 원본 코스 생성에는 연결되지 않았던 빈틈을 보완했다. StageDefinition.cs에 선택적 PlayerPrefab을 추가하고 네 기본 에셋은 null로 두어 현재 기본 로봇을 유지한다.
- StageBootstrap.cs는 지정된 프리팹의 루트 Motor/BoxCollider/Dynamic Rigidbody를 확인하고 인스턴스를 만든다. 시작 위치와 대시/자력 허용은 코스 데이터로 적용하며 프리팹의 물리 재질·표현 구성은 덮어쓰지 않는다.
- 편집기는 생성 함수를 주입해 PrefabUtility로 인스턴스를 만들고 GameObject/루트 컴포넌트 오버라이드를 기록한다. 런타임 어셈블리에 UnityEditor 의존성을 추가하지 않았다.
- PlayerMotor.cs의 일반 중력을 직렬화하여 프리팹에서 조절할 수 있게 했다. 기존 부활 테스트는 고정 3 대신 원래 설정된 중력 복원을 기대하도록 맞췄으며 실행하지 않았다.
- 변경 파일: StageDefinition.cs, StageBootstrap.cs, StageAuthoring.cs, PlayerMotor.cs, 기본 Stage1~4.asset, 기존 PlayMode 테스트, TASKS.md/README.md. 기존 GUID와 기본 배치·수치는 유지했다.
- 미검증: 프리팹 생성/할당/실행, 비활성 프리팹, 루트 컴포넌트 누락, 편집본 프리팹 링크·오버라이드·재로딩, 중력/부활. 실제 프리팹 산출물을 생성했다고 표시하지 않는다.


## 2026-11-12 — 연속 작업 10: 사용 설명과 인수 상태 정리

- README.md에 누적된 중복 설명을 현재 코드 기준으로 통합했다. 실행·조작·기록·코스 데이터·편집본/리비전·프리팹·Mac 빌드/배포·보류 검수·구조를 한 번씩 설명한다.
- AGENTS.md의 실제 구조와 PlayerPrefab/Editor 팩터리 정책을 갱신하고 TASKS.md에 최신 연속 작업·남은 검수를 반영했다.
- 기존의 짧은 자동 검사만 마지막에 한 번 수행했다: Tools/validate_project.py PASS(63개 에셋 식별자·5개 원본 씬·16개 스프라이트), Python unittest 21/21 PASS, C# 구문 파싱 24개 PASS, git diff 공백 검사 PASS.
- Python CLI 테스트는 가짜 실행 파일을 사용한다. 새 native ditto 패키징 경로와 Unity C#의 의미 분석/게임 동작을 검증한 결과가 아니다. C# 구문 파싱도 컴파일이 아니다.
- 이번 실행에서 Unity 테스트·플레이·Mac 앱/ZIP 생성은 수행하지 않았다. 필수 동작 완료 조건은 미완료로 유지한다.
- 앞선 9개 코드 단계는 모두 origin/develop에 푸시했고 로컬과 원격 159bdbf 일치를 확인했다. 이 문서 정리도 별도 커밋한다.


## 2026-11-13 — 편집 톱날 경로와 안내 레일 일치

- RotatingSaw.cs의 레일은 생성 당시 월드 좌표를 계속 표시하던 반면 실제 이동은 Awake에서 읽은 편집 위치를 사용했다. 실행 시 현재 원점으로 레일을 갱신하고 Configure 반복 호출에도 기존 LineRenderer를 재사용한다.
- Travel 변경을 반영하고 이동량이 없는 톱날은 레일을 숨긴다. 선택한 톱날의 양 끝점·위험 반경을 Scene Gizmo로 표시한다. 기존 GUID·코스 좌표·물리 주기는 유지했다.
- 기존 PlayMode 파일에 편집 원점/기존 레일 재사용/정지 경로 회귀 사례 1건을 추가했다. 총 9건이며 Unity 부재로 모두 미실행이다.
- 실제 검사: 에셋 무결성 PASS(63 식별자·5 씬·16 스프라이트), C# 24개 구문 파싱 PASS(컴파일 아님), git diff --check PASS.
- 미검증: Unity 컴파일, 재로딩한 편집본의 레일 정합·Gizmo·실제 왕복 이동. 다음은 입력·부활 및 프리팹 설정의 실행 연결을 보완한다.


## 2026-11-13 — 프리팹의 실제 충돌 가능 설정 안내

- StageDefinition.cs에 선택적 플레이어 프리팹의 공통 오류 안내를 두었다. 루트 컴포넌트 존재뿐 아니라 Dynamic/Simulated Rigidbody2D와 enabled/non-trigger BoxCollider2D도 필요하다. 비활성 프리팹 자체는 기존처럼 생성 시 활성화한다.
- StageBootstrap.cs·StageDefinitionEditor.cs·MacBuild.cs가 같은 안내를 사용한다. 원본 코스의 잘못된 프리팹은 Inspector에서 보이고 원본 빌드도 중단한다. 이미 베이크한 독립 리비전은 원본 PlayerPrefab의 후속 수정 때문에 중단하지 않는다.
- 변경: 위 C# 4개, README.md/TASKS.md/PROGRESS.md. 기존 에셋·GUID 유지. README의 테스트 수를 현재 16 EditMode + 9 PlayMode = 25로 갱신했다.
- 실제 검사: 에셋 무결성 PASS, C# 24파일 구문 파싱 PASS(컴파일 아님), 공백 검사 PASS. Unity 컴파일·잘못된 프리팹 UI·빌드 거부·유효 프리팹 플레이는 미검증이다.
- 다음 작업: 실제 플레이 중 남는 입력과 부활 상태를 검토하고 발견한 결함을 보완한다.


## 2026-11-13 — Intel/Apple Silicon 동시 배포 구성

- MacBuild.cs가 Standalone/OSXUniversal의 Architecture=x64ARM64를 명시하여 두 Mac CPU용 빌드를 요청한다. 기존 설정은 제품명과 함께 finally에서 복원하고 보고서에 요청 아키텍처를 남긴다. PLAYING 안내에도 대상을 추가했다.
- Unity 2022.3 공식 Manual/macos-building.html에서 Intel 64-bit + Apple silicon 지원을 확인했다. UnityCsReference 2022.3의 EditorUserBuildSettings.bindings.cs에서 4인자 플랫폼 설정 API를 확인했다. iOS용 PlayerSettings.SetArchitecture를 잘못 사용하지 않는다.
- 변경: MacBuild.cs, README.md, TASKS.md, PROGRESS.md. 에셋 무결성·C# 구문 파싱·공백 검사 PASS.
- 미검증: Unity 컴파일/실제 빌드, 요청 문자열의 설치된 Editor 동작, 결과 실행 파일의 x86_64/arm64 포함 여부, 두 Mac CPU 실행과 예외 시 설정 복원. Universal 실행을 검증 완료로 표시하지 않는다.
- 다음 작업은 런타임 입력·부활 경계와 남은 사용자 흐름의 실제 결함 보완이다.


## 2026-11-13 — 일시정지·클리어 화면의 실패 안내

- GameHud.cs에서 일시정지/결과 패널 뒤 HUD에만 있던 씬 전환 실패 Notice를 전경 상단에도 표시한다. 코스 오류 복귀 화면·기록 모달은 기존 안내와 겹치지 않게 유지한다.
- 다음 코스가 현재 빌드에 없으면 클리어 화면 버튼이 사유를 표시하며 비활성화된다. 다시하기·선택 복귀는 계속 사용할 수 있고 Enter 경로도 기존 OpenScene의 가용성 검사를 거친다.
- 변경: GameHud.cs, TASKS.md, PROGRESS.md. 에셋 무결성·C# 24파일 구문·공백 검사 PASS. Unity의 실제 GUI 배치·클릭·Enter 오류 안내·전환 실패 복귀는 미검증이다.
- 다음 작업: 편집 코스와 플레이어 입력의 생명주기 연결을 계속 보완한다.
