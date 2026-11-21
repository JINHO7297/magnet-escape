# 마그넷 스케이터

자석 보드로 매번 달라지는 옥상을 건너는 2D 무한 러너입니다. 1은 쉬움, 2는 어려움입니다. 같은 난이도로 다시 시작해도 옥상 높이와 간격, 시설 조합이 바뀝니다. 충돌하거나 추락하면 거리·별·시설 점수로 난이도별 기기 상위 5개 기록에 도전합니다.

| 조작 | 동작 |
| --- | --- |
| 기본 이동 | 항상 오른쪽 자동 전진. 좌우 입력으로 멈추거나 후진하지 않음 |
| Space | 길게 누르면 높은 점프. 레일에서는 도약 |
| Shift | 전방 대시·쿨다운. 장애물 무적 없음 |
| Q/E | 공중 수직 인력/척력. 레일은 Q로 붙잡고 E/Space로 도약 |
| 보라색 천장 | 입구에서 점프 + Q로 중력을 뒤집습니다. Q를 놓아도 천장을 달립니다. Space로 아래로 점프하고 E 또는 천장 끝에서 바닥 쪽으로 돌아옵니다. |
| 회전 광고판 | 접근 중 Q를 유지해 기울기를 올립니다. E로 낮은 다리를 펼칩니다. |
| 천장 케이블 | Space로 뛰고 Q로 케이블에 붙습니다. E나 Q 해제로 아래에 착지합니다. |
| 축제 차량 | Space로 첫 지붕에 오른 뒤 앞 차량으로 건너갑니다. |
| 연쇄 전광판 | 접근 중 E로 열고 Space와 Q로 이어진 조각을 탑니다. |
| Backspace | 현재 질주 종료 및 점수 확정 |
| R / Esc | 현재 모드 새로 시작 / 정지·재개 |
| 선택 화면 1 / 2 | 쉬움 / 어려움 시작 |
| 선택 화면 N / B | 쉬움 바로 시작 / 난이도별 순위표 |
| H/T/C/M/F11 | 도움말/무한 기록/제작 정보/음소거/전체 화면 전환 |
| 정지 중 Enter/Tab | 재개/선택 화면 |

광고판과 전광판 조작 범위는 시작점 10m 전부터 1m 전까지입니다. 기본 자력은 수직에 작용하고, 레일 탑승 중에는 전진 속도가 35% 빨라집니다. 시설 이용 중에도 장애물과 충돌하면 사망합니다.

## 코스와 점수

| 난이도 | 구간 사이 틈 | 거리 변화 |
| --- | --- | --- |
| 쉬움 | 첫 구간 이후 약 2.3~4.8m | 약 640m에 걸쳐 간격이 늘어납니다. |
| 어려움 | 첫 구간 이후 약 4.1~6.45m | 약 384m에 걸쳐 간격이 늘고 착지 높이 변화가 커집니다. |

가장자리의 별이 점프 타이밍을 알려 줍니다. 청록색 발판은 달리는 동안 움직이며, 일부는 틈 위까지 나옵니다. 첫 반전 천장 이후 3~5구간마다 보라색 천장을 다시 만납니다. 최대 난이도에 도달한 뒤에도 다음 구간을 계속 생성합니다.

무한 점수는 거리 1m당 10점, 별 50점부터 최대 140점, 시설 기술 성공당 180~320점입니다. 서로 다른 시설을 8초 안에 연속 성공하면 100점을 추가로 받습니다. 쉬움·어려움 순위는 Endless.v3의 별도 키에 저장합니다. 세부 동작과 점수 조건은 [도시 파쿠르 상세 설계](docs/superpowers/specs/2026-11-19-city-parkour-design.md)를 참고하세요.

## 실행과 편집

```bash
git clone --branch develop https://github.com/JINHO7297/magnet-escape.git
```

Unity Hub2022.3.62f1로 프로젝트를 엽니다. Apple Silicon의2022 Package Manager에는 Rosetta2가 필요했습니다. StageSelect 씬에서 시작하고 Game뷰를 클릭합니다. 레거시 Input Manager(Old 또는Both)를 사용합니다.

Resources/Stages/Stage1~12.asset이 원본 배치입니다. Routes에는 Start/UpperEnd/LowerEnd/Label/Facility, 이야기에는 OpeningMessage/EndingMessage를 지정합니다. 기존 씬/프리팹/.meta GUID를 유지합니다. 새 편집 리비전 생성 도구는 원본을 덮어쓰지 않습니다. 무한 모드는 Endless 씬/데이터와 EndlessCourse의 안전 템플릿으로 분리됩니다.

```bash
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools/tests -v
python3 Tools/unity_tasks.py test
python3 Tools/unity_tasks.py build-mac --package
```

Python 검사는 Unity 동작 검증과 다릅니다. Unity 테스트 종료 후 Python 에셋 복사 검사를 실행합니다. 빌드는 앱·한글 안내·크레딧 ZIP을 생성하고 Library/Temp/Logs/Builds/artifacts는 Git에서 제외합니다. 실제 실행본 경로·검사 결과·미검증 범위는 SUBMISSION.md/PROGRESS.md를 확인하세요.
