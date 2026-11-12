# Magnet Escape

폐공장을 탈출하는 자석 로봇의 Unity 2D 정밀 플랫폼 게임입니다. 1인 대학 기말과제 규모로 이동, 점프, 대시, 자력과 장애물 회피에 집중합니다.

**현재 상태: 4개 그레이박스 코스가 코드로 구성된 초기 프로토타입. Unity 컴파일·플레이 검증 전이며 완성본이 아닙니다.**

## macOS에서 열기

1. 저장소를 복제하고 개발 브랜치를 선택합니다.
   ```bash
   git clone https://github.com/JINHO7297/magnet-escape.git
   cd magnet-escape
   git switch develop
   ```
2. Unity Hub에서 **Unity 2022.3.62f1**을 설치합니다. Intel Mac은 Intel 빌드, Apple Silicon Mac은 Apple Silicon 빌드를 선택합니다. 공식 릴리스 API에서 양쪽 배포가 존재함을 확인했습니다. 사용 중인 macOS 버전과의 실제 호환성은 아직 확인하지 못했습니다.
3. Unity Hub에서 `Assets`, `Packages`, `ProjectSettings`가 들어 있는 이 저장소 루트를 프로젝트로 추가합니다. Editor 활성화·라이선스 설정은 Unity Hub에서 진행합니다.
4. 최초 임포트와 패키지 복원을 기다립니다. 다른 Unity 버전으로 자동 업그레이드하지 마세요.
5. **Edit > Project Settings > Player > Other Settings > Active Input Handling**이 `Input Manager (Old)` 또는 `Both`인지 확인합니다. 변경했다면 에디터를 재시작합니다. 이 프로토타입은 `UnityEngine.Input`을 사용합니다.
6. `Assets/_MagnetEscape/Scenes/StageSelect.unity`를 열고 Play합니다. Game 뷰를 클릭해 키보드 포커스를 줍니다.
7. 콘솔 오류가 있으면 먼저 해결하고, 발견한 내용을 `PROGRESS.md`에 기록합니다.

다른 ProjectSettings 파일은 첫 실행 때 Unity가 기본값으로 생성할 수 있습니다. `.meta` 파일은 삭제하거나 재생성하지 마세요.

## 조작

| 키 | 동작 |
| --- | --- |
| A/D 또는 좌우 화살표 | 좌우 이동 |
| Space | 점프. 누르는 시간으로 높이 조절 |
| WASD 또는 방향키 + Shift | 8방향 대시. 방향 미입력 시 바라보는 방향 |
| Q 유지 | 범위 내 자석으로 인력 이동. 입력을 유지하면 대상을 고정 |
| E 유지 | 선택한 자석으로부터 척력 이동. Q와 동시 입력 시 E 우선 |
| Backspace(⌫) | 현재 체크포인트로 빠른 재시도. 시간 유지·사망 +1 |
| R | 스테이지 처음부터 재시작. 세션 시간·사망·체크포인트 초기화 |
| Esc | 일시정지/재개. 일시정지 메뉴에서 스테이지 선택으로 돌아가기 |
| M | 음소거/해제. 메뉴와 일시정지 화면에서 음량 조절 |
| 1~4 | 스테이지 선택 화면에서 바로 시작 |
| Enter | 클리어 후 다음 코스 또는 선택 화면 |

Stage 1에서는 대시와 자력을 잠그고, Stage 2에서 대시, Stage 3부터 자력을 사용합니다. 대시 중에는 자력과 점프를 적용하지 않습니다. 자력 반경은 자석별 설정과 플레이어 최대 도달 거리(기본 7 유닛) 중 작은 값이며 원으로 표시합니다. 현재는 벽에 의한 자력 차단은 없습니다.

## 스테이지와 현재 표현

- Stage 1: 기본 이동·가변 점프, 체크포인트, 단순 위험 구간.
- Stage 2: 이동 발판, 레일을 왕복하는 회전 톱날, 가시, 대시.
- Stage 3: 인력·척력 연습용 간격과 자석.
- Stage 4: 이동 발판·자력·대시·서로 다른 주기의 순찰 톱날 조합.

고정 발판은 경고 줄무늬가 있는 철판이며, 이동 발판은 황토색으로 구분합니다. 로봇, 가시, 톱날, 자석, 체크포인트 깃발, 출구와 공장 벽에 자체 제작 픽셀 스프라이트 16개를 적용했습니다. 로봇은 대기·달리기·점프·낙하·대시 상태별 프레임과 대시 잔상을 표시합니다. 음향은 착지를 포함한 합성 효과음 6종입니다.

아트 원본 PNG는 `Assets/_MagnetEscape/Resources/Art`에 포함되어 있습니다. `Tools/generate_art.py`는 선택적인 재생성 도구이며 Pillow가 필요합니다. 게임 실행에는 Python이 필요하지 않습니다. 외부 유료 에셋을 사용하지 않았습니다.

게임 화면 포커스를 잃으면 자동 일시정지합니다. 일시정지 중 시간·물리·부활 대기는 멈추며, 재개 시 대기 중이던 입력을 제거합니다. 이전 체크포인트로 되돌아가도 부활 위치가 과거로 바뀌지 않습니다.

네 코스는 각각 약 38/60/52/81 유닛 길이이며, 구간별로 조작 안내가 표시됩니다. Stage 3에는 낮은 자석에서 오른쪽으로 척력 도약하는 구간을 추가했습니다. 길이와 배치가 완주 가능성을 의미하지는 않습니다.

각 씬에는 StageBootstrap이 직렬화되어 있고 `Resources/Stages/Stage1~Stage4.asset`의 StageDefinition 데이터를 읽어 플레이 때 코스를 생성합니다. 현재 씬 편집 화면에서 코스 전체가 보이지 않는 것은 이 구조 때문입니다. 최종 난이도 조정 전에는 Unity Play 모드에서 지형을 확인해야 합니다. 코스 완주 가능성은 미검증입니다.

체크포인트는 현재 세션에만 유지합니다. 시간에는 0.2초 부활 대기도 포함됩니다. 최고 시간과 그 플레이의 사망 횟수, 독립적인 최고 랭크와 음량은 PlayerPrefs에 저장합니다. 랭크 임계값은 `GOAL.md`에 명시되어 있으며 플레이 후 조정해야 합니다.

## 구조

```text
Assets/_MagnetEscape/
  Scenes/           # 선택 씬 + Stage1~Stage4
  Scripts/          # 이동, 스테이지 구성, 세션, 장애물, 피드백
  Tests/Editor/     # 랭크 경계값 및 씬 참조 검증
  Resources/Art/    # 자체 제작 픽셀 스프라이트
  Editor/           # 편집용 코스 복사·프리팹 생성 메뉴
Packages/           # 최소 모듈 및 Unity Test Framework
ProjectSettings/    # 버전, 빌드 씬 순서, Ground 레이어
Tools/              # Unity 없이 실행하는 정적 검사
AGENTS.md / GOAL.md / TASKS.md / PROGRESS.md
```

## Unity에서 코스 편집하기

기본 씬은 코드로 코스를 생성합니다. 나중에 직접 지형을 조정하려면 다음 메뉴를 사용합니다. 이 에디터 도구도 현재 실행 검증은 보류되어 있습니다.

1. Play 모드를 종료하고 **Tools > Magnet Escape > Create Editable Stage Copies**를 선택합니다.
2. `Assets/_MagnetEscape/Scenes/Editable/Stage1~Stage4.unity` 복사본이 생성되도록 구현했습니다. 원본 씬·GUID와 이미 존재하는 편집본은 덮어쓰지 않습니다. 열려 있던 씬은 유지합니다.
3. 편집본에는 런타임 생성기 대신 실제 플레이어·발판·장애물·체크포인트·카메라 오브젝트가 저장됩니다. Scene 뷰에서 위치와 크기를 조정합니다. 이동 발판은 Travel/Period, 체크포인트는 Order, 안내 오브젝트는 Message/Range를 조정합니다.
4. **Use Editable Stages in Build**를 선택하면 선택 화면 이후 편집본을 사용합니다. 원본으로 돌아가려면 **Use Generated Source Stages in Build**를 선택합니다.
5. 편집본의 로봇을 선택하고 **Create Player Prefab from Selected Robot**을 선택하면 `Prefabs/MagnetRobot.prefab`을 생성하도록 구현했습니다. 기존 프리팹은 덮어쓰지 않습니다. 이 명령은 프리팹 에셋을 저장하며 현재 로봇을 자동으로 프리팹 인스턴스에 연결하지는 않습니다.

편집본과 프리팹은 클라우드에서 생성한 산출물이 아닙니다. Unity에서 메뉴를 실행해야 만들어집니다. 생성된 씬·재질·프리팹은 각 `.meta`와 함께 커밋하세요. 코드 코스의 이후 변경은 기존 편집본에 자동 반영되지 않으므로 두 버전 중 사용할 것을 명시적으로 선택해야 합니다.

## 검사와 테스트

Unity 없이 수행할 수 있는 검사는 다음과 같습니다. Python 3 표준 라이브러리만 사용합니다.

```bash
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools/tests -v
git diff --check
```

이 검사는 `.meta` 누락·중복 GUID·씬 참조·빌드 순서·어셈블리 참조, PNG 체크섬과 스프라이트 임포트 설정을 확인합니다. 프로젝트 검사기 회귀 테스트 10건은 임시 복사본에 결함을 주입하여 잘못된 설정이 실제로 차단되는지 확인합니다. `.github/workflows/static-checks.yml`은 같은 검사를 GitHub Actions에서 수행하며 Unity 라이선스나 토큰 입력은 필요하지 않습니다. Unity 역직렬화나 C# 컴파일을 대신하지 않습니다. 테스트·빌드 실행 도구의 결과 판정과 공백 포함 경로 처리는 추가 Python 테스트 11건으로 확인합니다(전체 21건).

Mac Unity에서는 **Window > General > Test Runner > EditMode > Run All**로 랭크 경계값 10건, 씬 역직렬화 5건, 빌드 순서 1건을 실행합니다. **PlayMode > Run All**에는 코스 로딩·능력 설정, 중복 사망 방지, 체크포인트 순서, 일시정지, 클리어 후 상태, 재시작, 선택 화면 전환, 속도 초기화 테스트 8건을 추가했습니다. 이 테스트는 실제 키보드 입력을 합성하지 않으므로 이동·점프·대시·자력의 조작감과 코스 완주를 대신하지 않습니다. 변경한 Stage 1 기록은 테스트 후 복원합니다.

EditMode 16건과 PlayMode 8건 모두 현재 환경에서 실행하지 못했습니다. CLI로는 저장소 루트에서 다음 명령을 사용할 수 있습니다.

```bash
/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testResults "$PWD/TestResults.xml" -logFile "$PWD/unity-editmode.log"
```

직접 플레이 검증은 반드시 별도로 수행합니다.

- 짧은/긴 점프, 벽 접촉, 착지와 대각 대시, 쿨다운 중 연타.
- 수평·수직 이동 발판 탑승과 하차, 부활 후 발판 재탑승.
- 자력 범위 안팎, 인력·척력 전환, 대시와 자력 연속 조작.
- 가시·톱날·낙하 사망, 중복 사망 집계 방지, 체크포인트 부활과 속도 초기화.
- 스테이지 4개 완주, 선택/재시작/다음 코스/최종 클리어, 클리어 후 시간 정지.
- 앱 재실행 후 최고 기록, 창 크기 변경 시 UI, 소리와 파티클 표시.

## Mac 테스트·빌드 자동 실행

Unity Hub에서 프로젝트 버전 Editor를 설치·활성화하고, 같은 프로젝트를 열어 둔 Unity 창을 닫은 뒤 저장소 루트에서 실행합니다. Python 3 표준 라이브러리만 사용합니다.

```bash
# EditMode 16건과 PlayMode 8건 순서대로 실행
python3 Tools/unity_tasks.py test

# 한 종류만 실행
python3 Tools/unity_tasks.py test --suite PlayMode

# macOS 앱과 빌드 요약 생성
python3 Tools/unity_tasks.py build-mac
python3 Tools/unity_tasks.py build-mac --development

# 실행하지 않고 명령만 확인
python3 Tools/unity_tasks.py test --dry-run
python3 Tools/unity_tasks.py build-mac --dry-run
```

기본 Editor 경로는 `ProjectSettings/ProjectVersion.txt`의 버전을 읽어 Unity Hub의 macOS 설치 경로를 사용합니다. 다른 위치라면 `--unity "/실제 경로/Unity"` 또는 `UNITY_EDITOR`로 **동일한 Unity 버전**의 실행 파일을 지정하세요. 경로에 공백이 있어도 처리합니다.

테스트 XML·로그는 `artifacts/tests/<실행 ID>/`, 빌드 앱·로그·요약은 `Builds/<실행 ID>/`에 저장합니다. `--output`으로 상위 출력 폴더를 바꿀 수 있습니다. 매 실행은 고유 폴더를 사용하여 이전 성공 결과를 재사용하지 않습니다. 이 경로는 Git에서 제외됩니다.

테스트 프로세스 실패, XML 누락, 테스트 0건, 실패 결과는 명령 실패로 처리합니다. `--dry-run`은 Unity를 실행하지 않으며 성공 검증으로 간주하지 않습니다. Linux에서의 CLI 회귀 테스트는 **가짜 실행 파일**로 인수·결과 처리를 검사할 뿐 Unity 테스트나 Mac 빌드 성공을 의미하지 않습니다.

`Editor/MacBuild.cs`는 빌드 씬 5개와 순서, 씬 존재, Ground 레이어, macOS 지원 모듈을 확인하고 빌드 결과를 `.build.txt`로 저장합니다. 코드 서명·공증·다른 Mac에서의 실행 검증은 포함하지 않습니다. 선택한 macOS 아키텍처는 Unity 프로젝트의 빌드 설정을 따릅니다.

## macOS 빌드

Unity에서 **File > Build Settings > PC, Mac & Linux Standalone > macOS**를 선택합니다. 모듈이 없다면 Unity Hub에서 지원 모듈을 설치합니다. 기본 내장 렌더 파이프라인을 사용합니다. Build Settings에 StageSelect, Stage1, Stage2, Stage3, Stage4가 순서대로 활성화되어 있는지 확인하고 빌드합니다. **Tools > Magnet Escape > Build macOS App** 메뉴도 추가했으며, 메뉴 출력은 `Builds/MagnetEscape.app`입니다. Mac 빌드·실행·배포 서명은 현재 미검증입니다.

현재 클라우드 환경은 Debian Linux이고 Unity Editor가 없어 실제 컴파일, 물리 동작, 음향과 코스 난이도를 검증하지 못했습니다. 구체적인 다음 작업은 `TASKS.md`, 실행한 검사와 알려진 문제는 `PROGRESS.md`를 확인하세요.


톱날의 빨간 레일은 왕복 경로를 표시합니다. 이동은 물리 시간에 맞춰 처리하고 일시정지 때 멈추도록 작성했습니다. 가시의 판정은 투명 영역을 포함하는 사각형에서 톱니 실루엣을 따르는 다각형으로 변경했습니다. 변경된 코스의 실제 판정·통과 타이밍은 미검증입니다. 기존 편집용 씬 복사본은 자동 갱신하지 않으므로 새 코스는 원본 생성 씬에서 사용하거나 편집본에서 직접 조정해야 합니다.


자력 입력을 시작할 때 범위 내 가장 가까운 자석을 선택하고, Q/E를 유지하는 동안 범위 내에 있는 같은 대상을 유지합니다. 입력을 놓거나 범위를 벗어나면 다시 선택합니다. 선택한 자석의 원이 강조되며 인력은 청록색, 척력은 금색으로 표시합니다. 자석의 Range·Force Multiplier는 Inspector에서 조정할 수 있습니다. 자력을 공중에서 해제하면 수평 속도가 즉시 걷기 속도로 바뀌지 않고 Magnetic Air Control 값에 따라 감속하며, 착지·대시·부활 시 별도 관성 상태를 정리합니다. 이 조작 변경의 감각과 코스 난이도는 미검증입니다.


빠른 재시도는 Backspace(⌫) 또는 일시정지 메뉴의 **Retry Checkpoint**로 실행합니다. 체크포인트를 밟기 전이면 시작 위치로 돌아갑니다. 이 조작은 사망 1회로 계산하고 경과 시간은 유지합니다. 재시작(R)은 별도의 새 플레이이므로 시간과 사망을 초기화합니다. 클리어 결과에서는 첫 클리어·최고 시간 경신·이전 최고 시간 대비 차이·최고 랭크 경신을 구분합니다. 선택 화면의 FASTEST 기록에 표시된 사망 횟수는 해당 시간 기록의 것이며, TOP RANK는 별도로 달성한 최고 랭크입니다.


정밀 조작용 점프 유예 시간(Coyote Time), 착지 전 입력 버퍼(Jump Buffer Duration), 점프 버튼 해제 배율, 짧은 점프 배율, 접지 탐색 거리는 로봇의 PlayerMotor Inspector에서 조정할 수 있습니다. 기본 점프 수치는 유지했습니다. 대시 진행 방향을 막는 벽·바닥·천장에 닿으면 대시를 종료하고 중력을 복원하며 쿨다운은 유지하도록 변경했습니다. 착지에는 짧은 효과음과 먼지 파티클을 추가했습니다. 실제 조작·충돌·출력은 미검증입니다.


편집용 씬에서 체크포인트의 `Respawn point` 자식 오브젝트를 옮기면 부활 위치를 정할 수 있습니다. 체크포인트를 선택하면 Gizmo로 연결선과 플레이어 크기의 상자를 표시합니다. 기존 편집본처럼 마커가 없는 체크포인트는 Spawn Offset(기본 위로 1 유닛)을 사용합니다. 플레이어의 최초 배치 위치도 실행 시작 때 기억하여 첫 체크포인트 전 부활 위치로 사용하도록 변경했습니다. GameSession의 Fall Death Height와 Respawn Delay로 낙사 기준과 부활 대기 시간을 조정할 수 있습니다. 실제 재로딩·안전한 부활 위치는 미검증입니다.


이동 발판의 금색 선은 시작점에서 끝점까지의 경로를, 톱날의 붉은 선은 위험물의 순찰 경로를 뜻합니다. 발판을 선택하면 Scene Gizmo에도 양 끝점을 표시합니다. Period는 0으로 나누지 않도록 최소 0.1초로 제한하며, 게임 도중 Travel을 바꾸면 경로 표시를 다시 그립니다. 출구는 초록 외곽선이 천천히 밝아지고 플레이어가 가까워지면 강조되도록 구현했습니다. 이 시각 효과와 실제 이동·충돌의 일치는 아직 실행 검증하지 않았습니다.


## 코스 원본 데이터

`Assets/_MagnetEscape/Resources/Stages/Stage1.asset`부터 `Stage4.asset`까지 네 개의 실제 코스 데이터 에셋을 추가했습니다. 기존 배치와 기본 수치는 옮겨 두었으며, Unity에서 에셋을 선택해 시작점·출구·고정/이동 발판·가시·톱날·자석·체크포인트·안내를 편집할 수 있습니다. ParTime은 랭크 기준, Moving Platforms의 Period/Width와 Saws의 Phase는 이동 설정입니다. Checkpoints 배열 순서가 체크포인트 순번이며 SpawnOffset은 마커 초기 위치입니다.

원본 생성 씬은 다음 실행 때 데이터 변경을 반영합니다. 이미 만든 `Scenes/Editable` 복사본은 별도 편집물이므로 자동 변경하지 않습니다. 코스 데이터를 수정하더라도 기존 씬·프리팹·GUID를 덮어쓰지 않습니다. 네 데이터 파일의 Resources 경로와 Number는 유지해야 합니다. 데이터 임포트와 실제 게임 생성은 아직 Unity에서 검증하지 않았습니다.
