# 마그넷 스케이터 (Magnet Skater)

자석 보드로 도심을 질주하는 2D 자동 전진 플랫폼 액션입니다. **가변 점프·전방 대시·공중 수직 자력·자석 레일 그라인드/도약**을 조합해 유한한 4코스를 완주합니다. 한국어 메뉴와 첫 실행 전체 화면을 지원합니다.

Unity **2022.3.62f1**, C#, 레거시 Input을 사용합니다. 현재 검수 환경은 macOS Apple Silicon + Rosetta 2입니다. Unity 버전·기존 씬/프리팹/.meta GUID를 유지하며 내부 MagnetEscape 이름과 일부 출력 파일명은 호환용입니다.

2026-11-15 실제 Unity 검사 **EditMode18 + PlayMode31 =49건**, Python21건 통과. 확장 4코스의 실제 엔진 물리·위험물·레일·결승을 통과하는 자동 무사망 경로와 29개 체크포인트 부활 후 준비 거리를 확인했습니다. 자동 경로 검사는 키보드 입력 수집을 대신 공급하므로 사람이 네 코스를 직접 완주한 검수와는 구분합니다. 실제 Intel 하드웨어/다른 Mac과 모든 세부 조작감은 추가 검수 대상입니다.

## 시작

```bash
git clone --branch develop https://github.com/JINHO7297/magnet-escape.git
```

Unity Hub에서 2022.3.62f1을 설치하고 이 폴더를 프로젝트로 추가합니다. Apple Silicon에서도 2022의 Intel Package Manager 때문에 Rosetta 2가 필요했습니다. `Assets/_MagnetEscape/Scenes/StageSelect.unity`를 열어 Play한 뒤 Game 뷰를 클릭합니다. Active Input Handling은 Input Manager (Old) 또는 Both입니다.

Mac 실행본은 처음에 전체 화면으로 열립니다. 선택/정지 화면에서 창 모드로 바꾸면 화면에 맞는 최대1600×900 창을 사용하며 창 크기도 조절할 수 있습니다. 정지 중에도 R 재시작·Backspace 재도전이 가능합니다. 기본60FPS 상한과 SafeArea 비례 UI를 사용합니다. 한글은 Apple SD Gothic Neo 등 OS 시스템 폰트로 표시합니다.

## 조작

| 입력 | 동작 |
| --- | --- |
| 기본 이동 | 오른쪽 자동 전진. A/D·방향키로 정지/후진하지 않음 |
| Space | 길게 누르면 높이, 짧게 누르면 낮게 점프. 레일에서는 도약 |
| Shift | 짧은 전방 대시. 쿨다운 필요, 장애물 무적 없음 |
| Q | 공중 인력. 레일에 접근하면 붙잡아 그라인드 |
| E | 공중 척력. 레일에서는 위로 도약 |
| Q 놓기 | 레일에서 분리 |
| Backspace | 체크포인트 재도전: 충돌 +1, 시간 유지 |
| R / Esc | 전체 재시작 / 일시정지·계속하기 |
| M / 1~4 | 음소거 / 선택 화면에서 코스 선택 |
| H / T / C | 도움말 / 기록 / 제작 정보 |
| F11 | 전체 화면·큰 창 전환 |
| 정지 중 Enter / Tab | 계속하기 / 코스 선택 |

2코스부터 대시와 레일, 3코스부터 공중 자력을 사용합니다. 청록→보라 레일 근처에서 점프하며 Q를 유지하면 보드가 붙어 **35% 빠르게** 전진합니다. Space/E로 도약하거나 Q를 놓아 분리하고, Shift로 바로 대시할 수도 있습니다. 레일 진입 중에도 고체를 통과할 수 없으며 위험물에 닿으면 사망합니다. 정지·사망·클리어·전환 중에는 전진하지 않습니다.

공중 자력은 위쪽 자석에서 Q 상승/E 하강, 아래쪽 자석에서 E 상승/Q 하강입니다. 범위 밖에서는 힘이 없고 놓으면 일반 중력으로 돌아옵니다. 자력은 수평 전진을 멈추거나 뒤집지 않습니다. 레일은 별도의 한정된 포착 범위를 사용합니다.

## 확장 4코스

| 코스 | 핵심 | 길이 / 초기 S 기준 | 체크포인트 |
| --- | --- | --- | --- |
| 도심 골목 | 자동 전진·점프·높이가 다른 착지 | 248 / 42초 | 6 |
| 옥상 질주 | 긴 틈·대시·레일 입문·드론 | 320 / 54초 | 7 |
| 공중 레일 | 상하 자력·경사 레일·도약 연결 | 380 / 64초 | 8 |
| 스카이라인 도전 | 모든 기술과 긴 후반 구간 | 480 / 82초 | 8 |

기존 대비 약2배 길이입니다. 고정 지형과 총17개 레일을 사용하며 무한/무작위 맵·전투·퍼즐은 없습니다. 자동 경로는 약35.34/41.96/49.44/63.88초에 무사망 완주했고 2~4코스에서는 실제 레일도 사용했습니다. 모든 CP는 실제 부활 후 무입력1초(약7unit) 전진해도 추가 사망이 없고 지상에 있음을 확인했습니다. 사람의 체감 난이도는 별도 검수합니다.

충돌/낙사는 사망1회, 체크포인트에서0.2초 후 부활합니다. CP는 세션 한정이며 R/코스 전환 시 초기화합니다. 부활 대기 중 시간은 흐르고 정지/결과 중에는 멈춥니다. S는 기준 이내 무사망, A는1.5배·2회 이하, B는2배·5회 이하, 나머지는 C입니다.

확장판 기록은 `MagnetSkater.v2.Stage`에 저장합니다. 이전 짧은 러너 `MagnetSkater.Stage`와 수동 조작 `MagnetEscape.Stage` 기록은 삭제·혼합하지 않습니다. 편집 리비전 기록은 씬 경로별로 분리합니다. 최고 시간·해당 충돌 수·최고 등급을 독립 저장하며, 기록 화면에서 선택한 코스만 확인 후 초기화할 수 있습니다.

## 코스 편집

원본 배치는 `Resources/Stages/Stage1~4.asset`입니다. Inspector의 Platforms/Spikes/Saws/Magnets/Checkpoints/Rails를 편집합니다. Rails에는 Start/End/CatchRange를 지정하며 오른쪽으로 진행하는 직선/경사를 사용합니다. Scene 미리보기·시작/끝 핸들·Undo를 지원합니다. 기존 `Tools > Magnet Escape` 메뉴에서 새 편집 리비전을 만들고 빌드 코스를 선택할 수 있습니다. 기존 편집본은 덮어쓰지 않습니다.

프리팹은 root PlayerMotor·Dynamic/Simulated Rigidbody2D·enabled/non-trigger BoxCollider2D가 필요합니다. 빈 값이면 기본 스케이터를 만듭니다. 오프셋/배율이 있는 충돌체도 실제 하단으로 레일에 정렬합니다. 내부 `robot_*`, `saw`, `factory_backdrop`은 스케이터/드론/도심 이미지이며 기존 이름과 .meta를 유지합니다.

## 검사·Mac 빌드

```bash
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools/tests -v
python3 Tools/unity_tasks.py test
python3 Tools/unity_tasks.py build-mac --package
```

첫 두 명령은 정적/Python 검사이며 Unity 컴파일·플레이 검사가 아닙니다. Unity 테스트/빌드는 설치와 활성화가 필요하고 `UNITY_EDITOR` 또는 `--unity`로 경로를 지정할 수 있습니다. `--dry-run`은 실행 검증이 아닙니다. 에셋 복사 검사는 Unity 테스트 종료 후 실행합니다.

Unity 결과는 `artifacts/tests/<실행 ID>/`, 빌드는 `Builds/<실행 ID>/`에 저장됩니다. PlayMode 테스트는 빌드 설정에 원본4코스가 있을 때 실행하고 편집 리비전이면 기록/씬 변경 전에 건너뜁니다. 테스트 정상 종료 시 기존 기록을 복원하지만 Unity 강제 종료까지 복원을 보장하지는 않습니다.

출력은 Universal `MagnetEscape.app`, `MagnetEscape-macOS.zip`, `PLAYING.txt`입니다. ZIP에는 앱·한글 안내·CREDITS.md만 넣으며 개발 로그와 개인 경로가 포함된 빌드 요약은 넣지 않습니다. 산출물은 Git 제외입니다. 실제 앱/ZIP 생성·두 아키텍처 포함·Apple Silicon의 한글 전체 화면은 확인했습니다. 최신 검수본 경로와 세부 미검증은 SUBMISSION.md/PROGRESS.md를 확인하세요.

목표·이력: GOAL.md, TASKS.md, PROGRESS.md. 설계·계획: docs/superpowers. 제출 기획서: docs/Magnet_Skater_Game_Design.docx(레일·확장 코스와 검증 현황을 반영한 A4 2페이지).
