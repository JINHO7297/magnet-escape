# Magnet Escape 개발 이력

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
