# 마그넷 스케이터 제출 자료

## 최신 실행본

Unity2022.3.62f1로 실제 생성한 Mac Universal 검수본:

- `Builds/20261115_065006_UTC_4a253086/MagnetEscape.app`
- `Builds/20261115_065006_UTC_4a253086/MagnetEscape-macOS.zip`
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

실제 Unity49건(Edit18/Play31)과 Python21건 통과. 4코스 자동 무사망 경로35.34/41.96/49.44/63.88초, 레일 포함 경로, 모든29개 CP 부활 후 무입력1초 준비 거리, 기록 prefix 분리·랭크를 검증했다.

독립 실행본에서는 한글·큰 화면, 실제 키 입력의 코스 진입·정지·재시작·재도전·정보 화면·화면 전환, Space→Q 첫 레일 구간 무사망 통과를 확인했다. 시간을 맞춘 키보드 전체 완주 자동화는 완주하지 못했으므로 성공으로 세지 않는다. 사람의4코스 완주·세부 조작감·실행본 클리어 기록의 앱 재실행 보존·다른 Mac은 남은 실제 검수다. 전체 완료로 선언하지 않는다.
