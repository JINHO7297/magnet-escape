# Magnet Escape

폐공장을 탈출하는 자석 로봇의 Unity 2D 정밀 플랫폼 게임입니다. 이동·가변 점프·방향 대시·인력/척력과 장애물 회피로 네 코스를 통과합니다. 전투·퍼즐·복잡한 스토리는 포함하지 않습니다.

**현재 상태: 게임 기능과 편집·빌드 도구를 구현한 프로토타입입니다. Unity 컴파일·플레이·실제 완주·macOS 실행본은 미검증이며 완성본으로 표시하지 않습니다.**

## Mac에서 시작하기

```bash
git clone https://github.com/JINHO7297/magnet-escape.git
cd magnet-escape
git switch develop
```

1. Unity Hub에서 **Unity 2022.3.62f1**을 설치·활성화합니다. Intel/Apple Silicon Mac용 배포 존재는 공식 릴리스 정보에서 확인했으며, 실제 사용자 OS·설치·실행 호환성은 미확인입니다.
2. `Assets`, `Packages`, `ProjectSettings`가 있는 저장소 루트를 프로젝트로 추가하고 최초 임포트를 기다립니다. 다른 Editor 버전으로 자동 업그레이드하지 않습니다.
3. Player Settings의 **Active Input Handling**을 `Input Manager (Old)` 또는 `Both`로 설정합니다. 변경했다면 Editor를 재시작합니다.
4. `Assets/_MagnetEscape/Scenes/StageSelect.unity`를 열고 Play한 다음 Game 뷰를 클릭합니다.

프로젝트가 처음 열릴 때 Unity가 기본 ProjectSettings를 추가할 수 있습니다. 기존 `.meta`는 삭제·재생성하지 않습니다.

## 조작과 메뉴

| 입력 | 동작 |
| --- | --- |
| A/D 또는 화살표 | 좌우 이동. 위/아래 입력은 대시 방향에도 사용 |
| Space | 점프. 길게 누르면 높이, 일찍 놓으면 낮게 점프 |
| WASD/방향키 + Shift | 8방향 대시. 방향이 없으면 바라보는 방향 |
| Q / E 유지 | 인력 / 척력. 동시에 누르면 E 우선 |
| Backspace(⌫) | 체크포인트 빠른 재시도. 사망 +1, 시간 유지 |
| R | 스테이지 전체 재시작. 시간·사망·체크포인트 초기화 |
| Esc | 일시정지/재개. 도움말·기록 패널이 열렸으면 해당 패널부터 닫기 |
| M | 음소거/해제 |
| 1~4 | 선택 화면에서 스테이지 시작 |
| Enter | 클리어 후 다음 코스 또는 최종 선택 화면 |

선택/일시정지 화면에는 HELP·RECORDS·CREDITS·음량 조절이 있습니다. HUD의 HELP는 먼저 일시정지하고 안내를 엽니다. 안내를 닫아도 일시정지는 유지하며 Esc/Resume으로 재개합니다.

독립 실행판에는 QUIT과 창/전체 화면 전환 버튼이 있습니다. 표시 모드는 저장해 앱 시작 시 한 번 적용하며 Unity 편집기에서는 변경하지 않습니다. HUD는 안전 영역과 종횡비를 고려해 배치하도록 작성했습니다. 포커스를 잃으면 플레이를 일시정지합니다.

## 네 스테이지

| 코스 | 주요 요소 | 기본 S 기준 |
| --- | --- | --- |
| Stage 1 / Assembly Line | 이동·짧고 긴 점프·가시·체크포인트 | 35초·무사망 |
| Stage 2 / Machine Hall | 이동 발판·대시·순찰 톱날 | 45초·무사망 |
| Stage 3 / Magnetic Shaft | 인력·척력·자력 해제 후 관성 | 55초·무사망 |
| Stage 4 / Last Exit | 모든 기술의 조합 | 65초·무사망 |

모든 코스는 처음부터 선택할 수 있습니다. 대시는 Stage 2, 자력은 Stage 3부터 허용합니다. 현재 기본 코스 길이는 약 38/60/52/81 유닛이며 실제 완주 가능성과 난이도는 미검증입니다.

금색 레일은 발판 경로, 붉은 레일은 톱날 경로, 원은 자력 범위, 깃발은 체크포인트, 초록 문은 출구입니다. HUD는 시간·사망·대시 충전·자력 상태·저장 CP·진행률과 화면 밖 출구 방향을 표시합니다.

자력은 입력 시작 시 범위 내 가까운 자석을 선택하고, 입력 유지 중에는 그 대상이 범위 내에 있으면 유지합니다. 놓거나 범위를 벗어나면 다시 선택합니다. 실제 범위는 자석 반경과 플레이어 도달 거리 중 작은 값입니다. 인력은 청록색, 척력은 금색으로 강조합니다. 자력 해제 후 공중 수평 속도는 점진적으로 감속하며 착지·대시·부활 시 관성을 정리합니다. 대시 중에는 자력과 점프를 적용하지 않고, 진행을 막는 고체와 충돌하면 대시를 종료하도록 작성했습니다.

가시는 실루엣에 맞춘 다각형 트리거, 톱날은 원형 트리거를 사용합니다. 고속 이동에는 다음 물리 스텝의 Collider Cast를 보조 경로로 사용합니다. 이 물리 동작과 실제 시각·접촉 시점은 검증하지 않았습니다.

## 체크포인트와 기록

체크포인트는 현재 스테이지 세션에만 유지하며 앱 종료·전체 재시작·스테이지 전환 시 초기화합니다. 이전 체크포인트로 돌아가도 부활 위치를 과거로 바꾸지 않습니다. 기본 부활 대기는 0.2초이고, 시간에는 이 대기도 포함합니다. 클리어 이후 시간은 정지합니다.

최고 시간과 그 플레이의 사망 횟수, 독립적인 최고 랭크를 PlayerPrefs에 저장합니다. 첫 클리어·최고 시간·랭크 경신과 이전 최고 시간 대비 차이를 결과 화면에 표시합니다. 정확히 같은 시간이면 더 적은 사망 횟수를 해당 기록에 반영합니다.

기준 시간은 StageDefinition의 ParTime입니다. S는 기준 이내·무사망, A는 기준 1.5배 이내·2회 이하 사망, B는 기준 2배 이내·5회 이하 사망, 나머지는 C입니다.

플레이 중 `RANK PACE`는 현재 시간·사망 횟수로 아직 가능한 최고 랭크와 그 제한까지 남은 시간을 표시합니다. 추가 사망이나 시간 초과로 S→A→B→C로 바뀔 수 있으며 클리어를 보장하는 예측은 아닙니다. C에서는 `FINISH TO CLEAR`를 표시합니다. 일시정지 중 시간은 멈추고, 편집 코스는 그 씬에 저장된 기준 시간을 사용합니다.

- 원본 `Scenes/Stage1~4.unity`는 기존 기록 키를 유지합니다.
- 서로 다른 Editable/리비전 씬 경로는 별도 기록을 사용합니다. 선택 화면은 현재 빌드에서 실행할 코스의 기록을 읽습니다.
- 같은 파일의 코스 내용을 수정해도 기록 묶음은 같습니다. 새 기록을 원하면 RECORDS에서 그 코스만 초기화하거나 새 리비전을 만듭니다.
- 이전에 혼합 저장한 기록은 출처를 구분할 수 없어 원본 키에 그대로 두며 자동 이동하지 않습니다.
- 빌드에 없는 코스의 시작·기록 초기화를 비활성화합니다. 미저장 편집 씬의 연습 실행은 원본 기록에 쓰지 않습니다.
- CLEAR는 확인/취소를 거칩니다. 확인을 열었을 때의 대상만 초기화하며 다른 코스·음량·현재 플레이는 유지합니다.
- 저장 예외가 발생해도 클리어 흐름을 유지하고 저장 실패를 표시하도록 작성했습니다. 실제 실패·복구·재실행은 미검증입니다.

## 코스 데이터와 Scene 편집

원본은 `Assets/_MagnetEscape/Resources/Stages/Stage1.asset`부터 `Stage4.asset`까지입니다. 네 에셋의 경로와 Number를 유지합니다. 시작/출구·능력 허용·낙사·부활 대기·ParTime·발판·가시·톱날·자석·체크포인트·안내를 Inspector에서 조정합니다. 체크포인트 배열 순서가 순번이며 SpawnOffset은 부활 마커의 초기 위치입니다.

원본 씬은 StageBootstrap이 데이터를 읽어 Play 때 지형을 생성합니다. 기본 씬 편집 화면에 코스 오브젝트가 모두 없는 것은 이 구조 때문입니다.

코스 에셋을 선택하면 다음 Scene 편집 도구를 사용할 수 있도록 구현했습니다.

- **Scene preview / Frame Entire Course**: 실제 배치·경로·반경·부활 위치를 포함해 전체 보기.
- **Move in Scene / Element**: 종류와 번호를 선택해 중심 위치 이동.
- **TRAVEL END / RESPAWN**: 이동 경로 끝점과 상대 부활 위치 편집.
- **RANGE**: 자석·힌트 반경 편집. 실제 자력에는 플레이어 도달 거리 상한도 적용.
- **Edit end / radius only**: 중심과 끝점이 겹칠 때 보조 핸들만 조작.

변경은 SerializedProperty/Undo 흐름을 사용합니다. 이것은 편집 미리보기이며 물리 시뮬레이션이나 실제 플레이 화면이 아닙니다.

### 편집용 복사본과 리비전

**Tools > Magnet Escape** 메뉴를 사용합니다.

1. **Create Editable Stage Copies**는 `Scenes/Editable/Stage1~4.unity`를 생성하도록 작성했습니다. 기존 파일은 덮어쓰지 않습니다.
2. 원본 데이터 수정 후 비교본이 필요하면 **Create Fresh Course Revision** 또는 에셋의 생성 버튼을 사용합니다. `Scenes/Revisions/Revision_<UTC시각>_<고유값>/`에 새 네 코스를 만들고 이전 버전을 유지합니다.
3. 생성만으로 Build Settings를 바꾸지 않습니다. **Use Editable Stages in Build**, **Use Selected Revision in Build**, **Use Generated Source Stages in Build**로 사용할 코스 집합을 선택합니다. 4개 씬이 없으면 전환하지 않습니다.
4. 새 편집본은 생성 당시 ParTime을 저장합니다. 원본 ParTime을 나중에 바꿔도 해당 편집본의 기준은 유지하며, 예전 편집본은 원본 기준을 사용하는 호환 경로를 둡니다.

복사본에는 실제 오브젝트가 저장되며 원본의 이후 변경을 자동으로 적용하지 않습니다. 체크포인트의 Respawn point 자식을 옮겨 부활 위치를 정할 수 있습니다. 마커가 없는 이전 편집본은 SpawnOffset을 사용합니다. GameSession은 실제 초기 플레이어 위치와 출구를 읽고, 현재 지형·경로·자석·부활 위치에 배경과 카메라 범위를 맞추도록 작성했습니다.

생성된 씬·프리팹·재질과 `.meta`는 함께 커밋합니다. **이 환경에서는 Unity 메뉴를 실행하지 않았으므로 편집본·리비전·프리팹이 실제 생성되었다고 표시하지 않습니다.**

### 플레이어 프리팹

편집본의 로봇을 선택하고 **Create Player Prefab from Selected Robot**으로 `Prefabs/MagnetRobot.prefab`을 저장하도록 구현했습니다. 기존 프리팹은 덮어쓰지 않으며 선택한 로봇을 자동 연결하지는 않습니다.

저장한 프리팹을 StageDefinition의 Player Prefab에 지정하면 원본 생성과 새 편집본에서도 사용합니다. 비워 두면 기존 기본 로봇입니다. 루트에 PlayerMotor·BoxCollider2D·Dynamic Rigidbody2D가 필요합니다. 시작 위치와 대시/자력 허용은 코스 데이터가 적용하며 나머지 외형·물리 재질은 유지합니다. 편집본 생성은 프리팹 연결과 오버라이드를 기록하도록 작성했습니다.

중력·점프·대시·자력 수치는 PlayerMotor에서 조정합니다. 특히 일반 중력은 Motor의 Gravity Scale을 사용합니다. Scene 미리보기의 로봇 상자는 기본 크기 참고이며 사용자 프리팹의 실제 충돌 크기는 별도 확인해야 합니다.

## macOS 빌드와 배포

Mac Unity를 설치·활성화하고 같은 프로젝트를 연 Editor를 닫은 뒤 저장소 루트에서 실행합니다. Python 3 표준 라이브러리만 필요합니다.

```bash
python3 Tools/unity_tasks.py build-mac
python3 Tools/unity_tasks.py build-mac --development
python3 Tools/unity_tasks.py build-mac --package
python3 Tools/unity_tasks.py build-mac --package --dry-run
```

기본 Editor 경로는 프로젝트 버전의 Unity Hub macOS 설치 경로입니다. 다른 위치면 `--unity "/실제 경로/Unity"` 또는 `UNITY_EDITOR`로 같은 버전 실행 파일을 지정합니다. `--output`으로 상위 출력 폴더를 바꿀 수 있습니다.

실제 빌드 성공 시 `Builds/<고유 실행 ID>/`에 `.app`, `PLAYING.txt`, `.build.txt`, `build.log`를 남기도록 작성했습니다. 메뉴 **Build macOS App**은 `Builds/MagnetEscape.app`을 사용합니다. macOS 지원 모듈·빌드 대상·5개 씬 순서·기본 코스 데이터·Ground 레이어를 확인하며, 빌드 중 제품 이름은 Magnet Escape로 적용한 뒤 기존 값을 복원합니다.

`--package`는 Mac의 ditto로 앱·PLAYING.txt·CREDITS.md를 `MagnetEscape-macOS.zip`에 묶습니다. 앱 번들의 링크·권한·메타데이터를 보존하고 원본 앱은 남기며, 개발 로그·개인 경로를 담은 빌드 요약은 배포 ZIP에 넣지 않도록 작성했습니다. `--dry-run`은 실행 계획만 출력합니다.

**현재 검증된 macOS 앱·ZIP을 제공하는 것은 아닙니다.** 실제 빌드·압축·해제·다른 Mac 실행·서명·공증은 미검증입니다. 편집본의 실제 S 기준은 게임 UI가 우선하며 PLAYING.txt에는 원본 데이터의 기준 시간을 기록합니다. 제출 파일 구성은 [SUBMISSION.md](SUBMISSION.md), 출처는 [CREDITS.md](CREDITS.md)를 참고하세요.

## 나중에 실행할 검증

현재 환경은 Debian Linux이고 Unity Editor가 없습니다. Unity와 무관하게 수행하는 저장소 검사는 다음과 같습니다.

```bash
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools/tests -v
git diff --check
```

이 검사는 에셋 `.meta`·GUID·씬/어셈블리 참조·레이어·PNG/임포트, 실행 도구의 인수·결과 처리를 다룹니다. CLI 회귀 검사는 가짜 실행 파일을 사용하며 실제 Unity나 Mac 앱 성공을 뜻하지 않습니다.

Unity에서는 Test Runner의 EditMode 16건과 PlayMode 13건을 실행해야 합니다. CLI는 다음과 같습니다.

```bash
python3 Tools/unity_tasks.py test
python3 Tools/unity_tasks.py test --suite EditMode
python3 Tools/unity_tasks.py test --suite PlayMode
```

XML과 로그는 `artifacts/tests/<고유 실행 ID>/`에 저장하며 이전 결과를 재사용하지 않습니다. 프로세스 실패·보고서 누락·실패·0건·모두 건너뜀은 통과로 처리하지 않습니다. **Unity 테스트 29건은 아직 이 환경에서 실행하지 못했습니다.** 실제 키 입력·정밀 물리·코스 완주는 수동 플레이 확인도 필요합니다.

구현과 미검증 항목은 [TASKS.md](TASKS.md), 변경·검사 이력은 [PROGRESS.md](PROGRESS.md)에 구분합니다. 파일 문법 검사와 게임 동작 검수는 별개입니다.

## 프로젝트 구조

```text
Assets/_MagnetEscape/
  Scripts/            # 게임 동작·코스 생성·기록·표현
  Scenes/             # 선택 + 원본 Stage1~4
  Resources/Art/      # 자체 제작 픽셀 스프라이트 16개
  Resources/Stages/   # 코스 원본 데이터 4개
  Editor/             # 코스 편집·복사·리비전·프리팹·빌드
  Tests/              # Editor·PlayMode 테스트
Tools/                # 로컬 검사·Unity 실행·선택적 아트 재생성
Packages/
ProjectSettings/
```

기본 PlayerVisual은 프리팹 루트에 기존 TrailRenderer/LineRenderer가 있으면 표준 잔상·자력선으로 재사용합니다. 표현 컴포넌트를 비활성화하면 이 두 효과도 중지합니다. 별도 장식 선은 자식 오브젝트에 둡니다.

로봇의 시선과 방향키 없이 누르는 대시는 마지막 좌우 입력 방향을 사용합니다. 이동 발판의 운반 속도를 뺀 상대 속도로 대기·달리기 애니메이션을 구분하도록 구현했으며 실제 탑승 표현은 Unity 확인이 필요합니다.

로봇 애니메이션과 대시 잔상·자력선·착지 파티클·출구 표현·합성 효과음 8종(인력·척력 시작음 포함)을 사용합니다. 원본 아트 재생성은 선택적으로 Pillow가 필요한 `Tools/generate_art.py`로 수행하며 게임 실행에는 Python/Pillow가 필요하지 않습니다.

Mac 빌드 메뉴와 CLI는 Intel 64-bit + Apple silicon(Universal)을 요청하고 기존 아키텍처 설정을 복원합니다. 실제 Universal 바이너리 생성·실행은 아직 확인하지 못했습니다.

선택적 플레이어 프리팹은 루트 Motor·Dynamic/Simulated Rigidbody2D·활성 비 Trigger BoxCollider2D가 필요합니다. Inspector와 원본 코스 빌드가 이 설정을 확인합니다. 독립 편집본은 그 씬의 로봇 설정을 사용합니다.

잘못된 코스 데이터나 필수 플레이어·카메라·Ground·출구가 없으면 복귀 화면을 제공하도록 작성했고, 씬 전환 중에는 중복 요청·진행 입력을 막습니다. 오류 복구를 포함한 실제 게임 동작은 여전히 미검증입니다.

자석의 힘 범위는 월드 거리 기준입니다. 범위 원은 비균등 배율·회전에 맞춰 보정하고 자석 컴포넌트를 끄면 숨기도록 구현했습니다. 실제 변환·활성 전환은 Unity 확인이 필요합니다.
