# Magnet Skater 작업 규칙

## 우선순위
- 최신 승인 기획은 Magnet Skater: 오른쪽 자동 전진 2D 러너형 액션, 도심 4코스, 1인 기말과제 규모다.
- 좌우 이동/후진 입력을 사용하지 않는다. Space 가변 점프, Shift 전방 대시, Q/E 공중 수직 자력으로 궤적만 조절한다.
- 정지/사망/결과/전환 중 전진을 멈춘다. 구 기획의 공장/발전소/수동 이동을 다시 구현하지 않는다.
- MagnetEscape 네임스페이스·폴더·씬 이름은 GUID 호환을 위해 유지하며 화면명은 Magnet Skater다.
- 전투, 퍼즐, 복잡한 스토리, 불필요한 프레임워크를 추가하지 않는다.
- 개발 대상은 JINHO7297/magnet-escape이며, 사용자 개발 환경은 macOS다. 클라우드 환경은 Linux다.

## 작업 시작
1. git 상태와 기존 작업 규칙을 확인한다. 사용자 변경을 보존한다.
2. ProjectSettings/ProjectVersion.txt, Packages/manifest.json, Assets 구조를 읽는다.
3. Unity 설치 및 사용 가능한 테스트 환경을 확인한다. 기존 Unity 버전을 임의로 올리지 않는다.
4. 개발 브랜치에서 작은 단위로 구현한다. 기존 씬, 프리팹, .meta의 GUID를 유지한다.

## 코드 및 구조
- 기존 저장소 컨벤션을 우선한다. 새 C# 타입과 파일명은 PascalCase로 맞춘다.
- 플레이어 입력은 Update에서 수집하고 Rigidbody2D 물리 변경은 FixedUpdate에서 처리한다.
- 속도, 점프, 대시, 자력 수치는 Inspector에서 조정할 수 있도록 직렬화한다.
- 기존 입력 시스템을 재사용하고 입력 패키지를 중복 추가하지 않는다.
- 씬 인덱스와 레이어 번호를 설명 없이 코드에 하드코딩하지 않는다.
- 사망, 재시작, 스테이지 전환 때 속도와 일시적인 이동 상태를 명시적으로 초기화한다.
- 새 에셋은 .meta와 함께 추적한다. Library, Temp, Logs, 빌드 산출물을 커밋하지 않는다.
- 현재 구조: Assets/_MagnetEscape/{Scripts,Scenes,Resources/Art,Resources/Stages,Editor,Tests}, Packages, ProjectSettings, Tools.
- 코스의 원본 배치는 Resources/Stages/Stage1~Stage4.asset(StageDefinition)이다. 좌표·장애물·자력·체크포인트·기준 시간은 이 에셋에서 조정하고 코드에 중복 배치를 추가하지 않는다.
- 코스 데이터의 PlayerPrefab이 있으면 해당 프리팹을 사용하고 비어 있으면 기본 로봇을 만든다. 프리팹 연결/오버라이드 처리는 Editor 팩터리를 통해 수행하고 Runtime에 UnityEditor 의존성을 추가하지 않는다.
- 기본 스테이지는 각 씬의 StageBootstrap이 해당 데이터로 런타임에 생성한다. Editor 메뉴로 별도의 편집용 복사본을 만들 수 있으나, 실행 전에는 편집본·프리팹이 생성되었다고 표시하지 않는다.
- 편집본 생성기는 원본 씬·기존 편집본·기존 프리팹을 덮어쓰지 않는다. 생성 후에는 Unity가 만든 .meta를 함께 추적한다.
- 사용자는 별도 지시를 기다리지 않고 남은 게임 구현을 계속 진행하도록 승인했다. 각 단계 후 다음 미완료 작업을 선택하며 계속할지 묻지 않는다.
- 사용자 최신 지시는 실행 검증을 나중으로 남기고 게임 구현을 우선하는 것이다. 검증 항목은 TASKS.md/PROGRESS.md에 기록하되, 검증 도구 작업을 반복하며 기능 개발을 미루지 않는다.
- 게임 동작을 검증하지 않았다면 미검증으로 표시한다. 파일 문법·공백 정리는 실행 검증과 구분한다.
- Python 3 정적 검사는 python3 Tools/validate_project.py로 실행한다.
- 플레이어 프리팹은 root PlayerMotor·Dynamic/Simulated Rigidbody2D·enabled/non-trigger BoxCollider2D를 요구한다. 공통 안내는 StageDefinition.PlayerPrefabError를 사용한다.
- Mac 빌드는 Universal(Intel/Apple Silicon)을 요청하고 제품명·아키텍처 설정은 예외 시에도 복원한다. 실제 바이너리 생성·양쪽 CPU 실행을 확인하기 전에는 호환성 검증 완료로 표시하지 않는다.
- Unity 기준 버전은 2022.3.62f1이다. macOS Intel/Apple Silicon 배포를 공식 API에서 확인했다.
- 현재 프로토타입은 레거시 Input을 사용한다. Active Input Handling은 Input Manager (Old) 또는 Both로 설정한다.
- Ground 레이어는 TagManager.asset의 슬롯 8에 정의되어 있으며 이름으로 조회한다.

## 각 구현 단계 종료
1. 변경 파일과 diff를 검토한다.
2. 가능한 정적 검사와 관련 테스트를 실행한다.
3. TASKS.md에 구현 상태와 검증 상태를 별도로 기록한다.
4. PROGRESS.md에 변경 파일, 실제 실행한 검증, 실패와 미검증 사항, 다음 작업을 기록한다.
5. 커밋 가능한 상태로 정리하고 환경과 권한이 허용하면 개발 브랜치에 커밋한다.
6. 다음 미완료 작업으로 진행한다. 원격 강제 푸시와 기본 브랜치 직접 변경은 피한다.

## 완료 판단
- 코드 작성만으로 플레이 기능을 검증 완료로 표시하지 않는다.
- Unity 실행이 불가능하면 컴파일, 씬 참조, 실제 물리 동작, 스테이지 완주, UI와 오디오를 미검증으로 기록한다.
- 네 스테이지의 플레이 검증과 GOAL.md의 모든 완료 조건이 충족되기 전에는 전체 완료를 선언하지 않는다.

## 2026-11-15 후속 사용자 요구
- 게임 UI/튜토리얼/결과는 한국어, 첫 전체 화면과 큰 창 모드·창 크기 변경을 지원한다.
- 자석 레일 그라인드/도약은 승인된 특색 기능이다. Q 공중 포착·35% 가속, Q 해제, Space/E 도약, Shift 분리 대시. StageDefinition.Rails가 원본. 기존 공중 수직 자력/자동 전진/위험물 규칙을 유지한다.
- 확장 코스 길이248/320/380/480, S 기준42/54/64/82초. 이전 짧은 코스로 되돌리지 않는다. 기록 prefix MagnetSkater.v2.Stage이며 이전 두 prefix를 삭제하지 않는다.
- 실제 Mac에 Unity2022.3.62f1+Rosetta2가 설치되었다. Unity 테스트는 실행 가능하다. 이전 Linux/미설치 검수 보류는 역사이며 최신 실제 로그를 따른다.
- 현재 실제53건(Edit18/Play35)·확장4코스 자동 무사망 경로·29개 CP 준비거리 검수 완료. 자동 입력 경로와 실제 키보드/사람 완주는 구분한다. Python 에셋 복사 회귀는 Unity 테스트 종료 후 실행해 임시 씬 삭제 경합을 피한다.

- 실제 키 입력(CUA)4코스 결승·정상 종료 후 기록4/4 재실행 보존도 확인했다. CP 재도전 포함이며 사람의 직접 플레이/난이도·오디오 검수와 구분한다. 마지막 대시 벽/바닥, 범위 안 자력 해제/연결선, 정지 중 Space 해제, 하강 레일 자연 끝은 작업68 엔진 검수 결과를 따른다.

## 2026-11-18 최신 확장 승인 우선
- 사용자 승인으로 기존4코스 한정/무한맵 제외 범위를 확장했다. 입문5~8/일반1~4/도전9~12 총12코스와 별도 Endless(scene stage13). StageInfo.Count/EndlessStage/NextStage/StageForSlot을 사용하고 코스 개수를4로 가정하지 않는다. 기존GUID 유지.
- RouteShaper는 Q 상층 고속 레일/E 실제 다리, 시작점10unit~1unit 앞에서만 선택. 기존 기본 경로를 막거나 탑승 후 기하를 바꾸지 않는다. 원본은 StageDefinition.Routes/OpeningMessage/EndingMessage.
- 무한 점수 거리10/unit·별50+연속(2.5초·최대10연속)·첫 경로250. 한 번 사망하면 종료. 온라인 순위가 아니라 이 기기 상위5개. EndlessCourse bounded chunk streaming, 별/다리 공용Sprite는 FullRect로재사용.
- 새 고정 기록MagnetSkater.v3.Stage, 기존v2/이전prefix보존. 무한MagnetSkater.Endless.v1.Top5 분리.
- 장시간 Unity검사는 호스트 절전으로 walltime과게임시간이벌어질수있다. 코스driver는Time.time 기준 제한(150초/코스),NUnit최대900초;완료 전 성공으로세지않는다.무제한frame budget은몇ms에소진될수있어부활wait에쓰지않는다.

## 2026-11-19 도시 파쿠르 확장 우선

- StageDefinition.Routes의 Facility에는 임시 연결로, 회전 광고판, 천장 케이블, 축제 차량, 연쇄 전광판을 지정한다. 코스 원본은 Stage1~12 에셋이다.
- 광고판과 전광판은 10m 전부터 1m 전까지 조작한다. 천장 케이블은 Space 점프 후 Q로 아래에서 포착하고 Q 해제/E로 내려온다. 차량에는 지붕 충돌선만 둬 기본 경로를 열어 둔다.
- 무한 모드 8개 템플릿을 섞고 거리·별·실제 시설 사용을 채점한다. 새로운 점수 규칙은 Endless.v2.Top5에 저장하며 이전 키를 보존한다.

## 2026-11-22 통합 축제 코스

- 플레이어가 선택하는 축제 난이도는 쉬움(Stage8)과 어려움(Stage12) 두 개다. 메뉴·기록·실행 안내에는 StageInfo.FestivalCount와 FestivalForSlot을 사용한다. StageInfo.Count는 편집 및 호환을 위한 고정 씬 개수다.
- 두 코스는 각각 단일 결승을 사용한다. Stage8/12의 Routes는 같은 구간에 상하 시설을 겹쳐 놓는다. 레일에서 Q 유지 중 Space로 가까운 천장 케이블에 환승하고 E로 하차한다. 축제 기록은 v4, 무한 순위는 Endless.v2다.

## 2026-11-22 입체 경로와 반전 중력

- 고정 코스의 MidDecks는 단방향 착지 발판, InversionCeilings는 아래쪽 면을 타는 반전 구간이다. 구간 입구에서 Q로 반전하고 Space로 아래로 점프한다. E·구간 끝·부활 때 정상 중력으로 복귀한다. 현 코스 기록 prefix는 MagnetSkater.v5.Stage다.
- 배경은 플레이 화면을 채우는 단일 도시 파노라마다. 기존 이미지 GUID를 유지한다.
