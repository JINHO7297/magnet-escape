# Magnet Skater

자석 보드를 타고 도심을 자동 전진하는 **2D 러너형 플랫폼 액션**. Space 점프·Shift 전방 대시·Q/E 공중 자력으로 틈과 장애물을 피하며 유한한 4코스를 완주합니다. 퍼즐·전투·발전소·공장 탈출 스토리는 사용하지 않습니다.

이 저장소는 기존 Magnet Escape에서 새 기획으로 전환 중입니다. **Unity 컴파일·자동 물리 경로의 4코스 무사망 완주는 확인했습니다. 사람의 키보드 완주·Mac 배포 실행은 추가 검수 중입니다.** 기존 네임스페이스/폴더/씬/일부 파일명은 GUID 호환을 위해 유지합니다.

- Unity: **2022.3.62f1** (버전 변경 없음), C#, 레거시 Input
- 개발/현재 검수: macOS Apple Silicon, Unity 2022.3.62f1 + Rosetta 2
- 최신 기획: [설계](docs/superpowers/specs/2026-11-15-magnet-skater-design.md), [전환 계획](docs/superpowers/plans/2026-11-15-magnet-skater.md), [A4 2장 기획서](docs/Magnet_Skater_Game_Design.docx)

## Mac에서 열기

```bash
git clone --branch develop https://github.com/JINHO7297/magnet-escape.git
```

Unity Hub에서 2022.3.62f1을 설치하고 이 저장소 폴더를 프로젝트로 추가합니다. `Assets/_MagnetEscape/Scenes/StageSelect.unity`를 열어 Play한 뒤 Game 뷰를 클릭합니다. Active Input Handling은 Input Manager (Old) 또는 Both를 사용합니다. 메뉴에서 코스를 선택하면 자동 전진을 시작합니다.

## 조작

| 입력 | 동작 |
| --- | --- |
| 기본 이동 | 오른쪽 자동 전진. A/D·방향키로 정지/후진하지 않음 |
| Space | 가변 높이 점프. 짧게 누르면 낮게 점프 |
| Shift | 오른쪽 전방 대시. 쿨다운 필요, 장애물 무적 없음 |
| Q / E | 공중에서 자석 쪽/반대쪽 수직 궤적 조절. 수평 전진 유지 |
| Backspace | 체크포인트 재시도: 사망 +1, 시간 유지 |
| R / Esc | 전체 재시작 / 일시정지·재개 |
| M / 1~4 | 음소거 / 선택 화면에서 코스 선택 |

상단 자석에는 Q가 상승·E가 하강 방향, 하단 자석에는 E가 상승·Q가 하강 방향입니다. 범위 밖에서는 힘이 없고 놓으면 일반 중력으로 돌아옵니다. 기본 추진이 수평 속도를 유지하며 자력은 공중 수직 속도만 바꿉니다. 정지·사망·결과·전환 중에는 자동 전진을 멈춥니다.

## 4코스와 기록

| 코스 | 핵심 | 길이 / 초기 S 기준 |
| --- | --- | --- |
| ALLEY START | 점프 타이밍 | 124 / 22초 |
| ROOFTOP RUSH | 점프＋전방 대시 | 160 / 28초 |
| SKY RAIL | 상하 자석과 공중 궤적 | 180 / 32초 |
| SKYLINE CHALLENGE | 모든 기술 연속 사용 | 228 / 40초 |

기본 코스는 정지 대기형 이동 발판 대신 고정 옥상·틈·상하 자석·순찰 드론을 사용합니다. 실제 난이도·점프 여유·출발/부활 준비 거리·랭크 기준 적합성은 플레이 확인이 필요합니다.

장애물 접촉/낙사는 사망 1회, 최근 체크포인트에서 0.2초 뒤 부활합니다. 체크포인트는 세션 한정이며 전체 재시작/코스 전환 시 초기화합니다. 부활 대기 중에는 시간이 흐르고, 정지/클리어 후에는 멈춥니다. S는 기준 이내 무사망, A는 기준 1.5배 이내·2회 이하, B는 2배 이내·5회 이하, 나머지는 C입니다. RANK PACE는 현재 가능한 랭크와 남은 시간을 표시합니다.

러너 기록은 `MagnetSkater.Stage` 키로 저장합니다. 구 `MagnetEscape.Stage` 기록은 삭제하지 않고 별도로 남깁니다. 편집본/리비전은 씬 경로별로 기록을 분리하고 최고 시간·해당 사망 횟수·독립 최고 랭크를 저장합니다. RECORDS에서 선택한 코스만 확인 후 초기화하며, 미저장 씬은 기록에 쓰지 않습니다.

## 코스 편집과 호환

코스 배치는 `Resources/Stages/Stage1~4.asset`의 StageDefinition에 있습니다. 파일 경로/Number/GUID를 유지하고 위치·장애물·자석·체크포인트·ParTime을 Inspector에서 조절합니다. 원본 씬은 StageBootstrap으로 Play 때 생성합니다.

기존 **Tools > Magnet Escape** 메뉴명은 호환을 위해 유지합니다.

- Create Fresh Course Revision: 기존 편집본을 덮어쓰지 않고 새 4코스 생성
- Use Selected Revision in Build / Use Generated Source Stages in Build: 사용할 코스 세트 전환
- Create Editable Stage Copies: 기존 파일 보존
- Create Player Prefab from Selected Robot: 선택한 캐릭터 프리팹 저장(메뉴/파일명은 이전 이름)

Scene 뷰 미리보기·이동/끝점/반경 핸들·Undo를 사용할 수 있도록 구현했습니다. 실제 도구 실행은 Unity 검수 전입니다. 기존 편집본은 자동 갱신되지 않으며 새 자동 전진에서 완주를 보장하지 않습니다. 원본 러너 코스나 새 리비전을 사용하세요.

PlayerPrefab은 root PlayerMotor·Dynamic/Simulated Rigidbody2D·enabled/non-trigger BoxCollider2D가 필요합니다. 빈 값이면 기본 스케이터를 만듭니다. 루트 Trail/LineRenderer는 표준 효과로 재사용하므로 별도 장식은 자식에 둡니다. 내부 `robot_*`, `saw`, `factory_backdrop` 파일은 이제 스케이터/드론/도심 이미지이며 이름과 .meta는 참조 보존을 위해 유지합니다.

## 검사와 Mac 빌드

```bash
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools/tests -v
python3 Tools/unity_tasks.py test
python3 Tools/unity_tasks.py build-mac --package
```

첫 두 명령은 로컬 Python/에셋 검사를 실행합니다. 뒤 두 명령은 실제 Unity 설치·활성화가 필요합니다. `UNITY_EDITOR` 또는 `--unity`로 Editor 경로를 지정할 수 있습니다. `--dry-run`은 명령만 보여주며 성공 검증이 아닙니다.

Unity 2022.3.62f1 실제 검사에서 **EditMode 17 + PlayMode 21 = 38건**이 통과했습니다(2026-11-15). 자동 입력 명령으로 실제 물리·위험물·결승을 통과하는 4코스 경로와 가변 점프·대시·수직 자력·체크포인트 부활을 포함합니다. 사람이 키보드로 완주했다는 뜻은 아닙니다. Apple Silicon에서도 2022의 Intel Package Manager 실행을 위해 Rosetta 2가 필요했습니다. XML/로그는 `artifacts/tests/<실행 ID>/`에 저장됩니다. Python CLI 테스트는 가짜 Unity 실행 파일을 사용해 결과 처리만 검사합니다.

Mac 빌드는 Intel/Apple Silicon Universal을 요청하고 기존 제품명/아키텍처를 복원합니다. 표시 제품명은 Magnet Skater지만 호환되는 CLI/메뉴 출력 파일명은 `MagnetEscape.app`, 패키지명은 `MagnetEscape-macOS.zip`입니다. 빌드 폴더에 PLAYING.txt·요약·로그를 남기고 배포 ZIP에는 앱·PLAYING.txt·CREDITS.md만 담습니다. 실제 빌드·ZIP·두 CPU 실행은 미검증입니다.

## 남은 실제 검수

키보드 입력을 통한 4코스 완주와 난이도, 모든 체크포인트 준비 거리, 실제 도심/스케이터 표시와 UI/오디오, 기록 재실행, 편집본/프리팹, Mac Universal 빌드·실행을 확인해야 합니다. 전체 완료는 이 검수 전 선언하지 않습니다.

작업 목록은 [TASKS.md](TASKS.md), 실제 검사와 이력은 [PROGRESS.md](PROGRESS.md)를 확인하세요. 코드는 `Assets/_MagnetEscape`, 선택적 픽셀 아트 생성기는 `Tools/generate_art.py`에 있습니다. 재생성에만 Pillow가 필요하며 게임 런타임에는 Python이 필요하지 않습니다.
