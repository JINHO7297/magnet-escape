# 마그넷 스케이터 제출 자료

## 최신 실행본

Unity2022.3.62f1로 실제 생성한 Mac Universal 검수본:

- `Builds/20261115_072336_UTC_52ab5b3a/MagnetEscape.app`
- `Builds/20261115_072336_UTC_52ab5b3a/MagnetEscape-macOS.zip`
- 같은 폴더의 `PLAYING.txt`: 한글 조작 안내

ZIP에는 앱·PLAYING.txt·CREDITS.md가 들어 있으며 개발 로그와 개인 경로를 포함한 빌드 요약은 제외했다. 산출물은 Git 제외 정책에 따라 로컬에만 있다. 소스는 develop에서 받는다. 최신 커밋은 PROGRESS.md 및 git log로 확인한다.

이 실행본은 한국어·전체 화면/큰 창·레일 그라인드·확장4코스·v2 기록·정지 메뉴 단축키를 포함한다. 실행 파일과 UnityPlayer 양쪽에 x86_64/arm64 슬라이스가 있으며 Apple Silicon에서 실행했다. 실제 Intel 하드웨어·다른 Mac·서명/공증 배포는 검증하지 않았다.

## 기말과제 소스와 기획서

- Unity 버전: **2022.3.62f1**. Assets/Packages/ProjectSettings와 README를 제출 소스로 사용한다.
- `docs/Magnet_Skater_Game_Design.docx`: 레일·확장 길이·실제 검증 현황을 반영한 A4 2페이지. 원본 구조를 유지해 수정했고 한글 폰트를 지정해 두 페이지 렌더를 확인했다.
- README.md: 실제 조작·시작·편집·검사·빌드.
- GOAL.md: 확정 및 후속 기획·완료 조건.
- TASKS.md / PROGRESS.md: 실행 결과·이력·미검증 항목.
- CREDITS.md: 그래픽·소리·도구 출처.

Library/Temp/Logs/UserSettings/개인 설정과 테스트 임시 씬은 제출 소스에 넣지 않는다. 기존 씬/.meta GUID를 유지한다. 편집본을 새로 만들 때는 기존 Tools > Magnet Escape 메뉴를 사용하며 원본은 Resources/Stages/Stage1~4.asset이다.

## 재빌드

```bash
python3 Tools/unity_tasks.py test
python3 Tools/unity_tasks.py build-mac --package
```

생성 폴더의 앱·한글 안내·크레딧 ZIP을 사용한다. 출력 파일명 MagnetEscape는 기존 도구 호환용이며 게임 표시 이름은 Magnet Skater/마그넷 스케이터다.

## 검수 범위

실제 Unity53건(Edit18/Play35)과 Python21건 통과. 4코스 자동 무사망 경로35.34/41.96/49.44/63.88초, 레일 포함 경로, 모든29개 CP 부활 후 무입력1초 준비 거리, 기록 prefix 분리·랭크를 검증했다.

독립 실행본에서 CUA 실제 키 입력으로4코스 결승에 도달했다. 결과35.35초/0회/S, 73.15초/14회/C, 65.04초/10회/C, 72.32초/7회/C. 후반 코스는 체크포인트 재도전을 포함하며 사람의 직접 조작 검수와 구분한다. 정상 종료 후 재실행해 네 기록과 완주4/4·전체 화면 유지도 확인했다. 한글·큰 화면·정지/재시작·결과 Enter 전환·Q/E 자력도 실제 확인했다. 대시 마지막 벽/바닥 접촉·자력 해제/연결선·정지 중 Space 해제·하강 레일 자연 끝은 엔진 검수했다. CP 정지 알림의 HUD 가림을 하단 배치로 고치고 새 실행본에서4.11초/충돌0/CP1과 알림이 함께 보이는 것을 확인했다. 사람의 난이도/오디오 체감, 다른 Mac/실제 Intel은 추가 검수다.
