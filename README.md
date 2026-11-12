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
| Q 유지 | 범위 내 가장 가까운 자석으로 인력 이동 |
| E 유지 | 범위 내 가장 가까운 자석으로부터 척력 이동. Q와 동시 입력 시 E 우선 |
| R | 스테이지 처음부터 재시작. 세션 시간·사망·체크포인트 초기화 |
| Esc | 일시정지/재개. 일시정지 메뉴에서 스테이지 선택으로 돌아가기 |
| M | 음소거/해제. 메뉴와 일시정지 화면에서 음량 조절 |
| 1~4 | 스테이지 선택 화면에서 바로 시작 |
| Enter | 클리어 후 다음 코스 또는 선택 화면 |

Stage 1에서는 대시와 자력을 잠그고, Stage 2에서 대시, Stage 3부터 자력을 사용합니다. 대시 중에는 자력과 점프를 적용하지 않습니다. 자력 반경은 7 유닛이며 자석 주변 원으로 표시합니다. 현재는 벽에 의한 자력 차단은 없습니다.

## 스테이지와 현재 표현

- Stage 1: 기본 이동·가변 점프, 체크포인트, 단순 위험 구간.
- Stage 2: 이동 발판, 회전 장애물, 가시, 대시.
- Stage 3: 인력·척력 연습용 간격과 자석.
- Stage 4: 이동 발판·자력·대시·장애물 조합.

고정 발판은 경고 줄무늬가 있는 철판이며, 이동 발판은 황토색으로 구분합니다. 로봇, 가시, 톱날, 자석, 체크포인트 깃발, 출구와 공장 벽에 자체 제작 픽셀 스프라이트 16개를 적용했습니다. 로봇은 대기·달리기·점프·낙하·대시 상태별 프레임과 대시 잔상을 표시합니다. 음향은 현재 합성 효과음입니다.

아트 원본 PNG는 `Assets/_MagnetEscape/Resources/Art`에 포함되어 있습니다. `Tools/generate_art.py`는 선택적인 재생성 도구이며 Pillow가 필요합니다. 게임 실행에는 Python이 필요하지 않습니다. 외부 유료 에셋을 사용하지 않았습니다.

게임 화면 포커스를 잃으면 자동 일시정지합니다. 일시정지 중 시간·물리·부활 대기는 멈추며, 재개 시 대기 중이던 입력을 제거합니다. 이전 체크포인트로 되돌아가도 부활 위치가 과거로 바뀌지 않습니다.

네 코스는 각각 약 38/60/52/81 유닛 길이이며, 구간별로 조작 안내가 표시됩니다. Stage 3에는 낮은 자석에서 오른쪽으로 척력 도약하는 구간을 추가했습니다. 길이와 배치가 완주 가능성을 의미하지는 않습니다.

각 씬에는 StageBootstrap만 직렬화되어 있고 플레이 때 코스를 생성합니다. 현재 씬 편집 화면에서 코스 전체가 보이지 않는 것은 이 구조 때문입니다. 최종 난이도 조정 전에는 Unity Play 모드에서 지형을 확인해야 합니다. 코스 완주 가능성은 미검증입니다.

체크포인트는 현재 세션에만 유지합니다. 시간에는 0.2초 부활 대기도 포함됩니다. 최고 시간과 그 플레이의 사망 횟수, 독립적인 최고 랭크와 음량은 PlayerPrefs에 저장합니다. 랭크 임계값은 `GOAL.md`에 명시되어 있으며 플레이 후 조정해야 합니다.

## 구조

```text
Assets/_MagnetEscape/
  Scenes/           # 선택 씬 + Stage1~Stage4
  Scripts/          # 이동, 스테이지 구성, 세션, 장애물, 피드백
  Tests/Editor/     # 랭크 경계값 및 씬 참조 검증
Packages/           # 최소 모듈 및 Unity Test Framework
ProjectSettings/    # 버전, 빌드 씬 순서, Ground 레이어
Tools/              # Unity 없이 실행하는 정적 검사
AGENTS.md / GOAL.md / TASKS.md / PROGRESS.md
```

## 검사와 테스트

Unity 없이 수행할 수 있는 검사는 다음과 같습니다. Python 3 표준 라이브러리만 사용합니다.

```bash
python3 Tools/validate_project.py
git diff --check
```

이 검사는 `.meta` 누락·중복 GUID·씬 참조·빌드 순서·어셈블리 참조를 확인합니다. Unity 역직렬화나 C# 컴파일을 대신하지 않습니다.

Mac Unity에서는 **Window > General > Test Runner > EditMode > Run All**로 랭크 경계값 10건, 씬 역직렬화 5건, 빌드 순서 1건을 실행합니다. 테스트 코드도 아직 Unity에서 실행하지 못했습니다. CLI로는 저장소 루트에서 다음 명령을 사용할 수 있습니다.

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

## macOS 빌드

Unity에서 **File > Build Settings > PC, Mac & Linux Standalone > macOS**를 선택합니다. 모듈이 없다면 Unity Hub에서 지원 모듈을 설치합니다. 기본 내장 렌더 파이프라인을 사용합니다. Build Settings에 StageSelect, Stage1, Stage2, Stage3, Stage4가 순서대로 활성화되어 있는지 확인하고 빌드합니다. Mac 빌드·실행·배포 서명은 현재 미검증입니다.

현재 클라우드 환경은 Debian Linux이고 Unity Editor가 없어 실제 컴파일, 물리 동작, 음향과 코스 난이도를 검증하지 못했습니다. 구체적인 다음 작업은 `TASKS.md`, 실행한 검사와 알려진 문제는 `PROGRESS.md`를 확인하세요.
