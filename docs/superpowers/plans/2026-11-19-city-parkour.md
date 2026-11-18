# 도시 파쿠르 구현 계획

설계는 [도시 파쿠르 확장](../specs/2026-11-19-city-parkour-design.md)을 따른다.

- [ ] PlayMode에 광고판 고정, 천장 포착과 착지, 이동 차량, 연쇄 전광판 및 실제 사용 점수 회귀를 먼저 추가한다.
- [ ] `RouteShaper`에 광고판과 시설 선택/진행을 확장한다. `MagneticRail`과 `PlayerMotor`에 천장 포착과 이탈을 연결하고 `MovingPlatform`에 차량 착지 연결을 둔다. 기존 레일과 기본 경로를 재사용한다.
- [ ] `StageDefinition.RoutePlacement`과 `StageBootstrap`을 확장하고 Stage1~12의 시설을 배치한다. `GameHud`에는 한국어 조작과 성공 안내를 연결한다. 기존 씬과 메타데이터는 유지한다.
- [ ] `EndlessCourse`를 여덟 가지 구간을 섞는 구조로 수정한다. `GameSession`에 실제 사용 및 기술 연속 점수를 연결하고 `EndlessRecords`의 새 키를 사용한다.
- [ ] Unity EditMode/PlayMode, Python 정적 검사와 회귀 검사, macOS Universal 빌드 및 가능한 실행 검수 후 TASKS/PROGRESS/README/SUBMISSION을 갱신한다.
- [ ] 검토한 구현 단위를 JINHO7297 명의로 커밋하고 develop에 푸시한다.
