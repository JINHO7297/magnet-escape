# Magnet Escape 제출 자료 안내

## 소스 프로젝트

저장소의 develop 브랜치에 Assets, Packages, ProjectSettings와 실행 설명이 포함되어 있다. Unity 2022.3.62f1로 연다. Library, Temp, Logs, 개인 설정, 로컬 테스트 로그는 제출 소스에 포함하지 않는다.

## 게임 구성

- Stage 1 / Assembly Line: 기본 이동·짧은/긴 점프·체크포인트.
- Stage 2 / Machine Hall: 이동 발판·방향 대시·순찰 톱날·가시.
- Stage 3 / Magnetic Shaft: 인력·척력·자력 해제 후 이동.
- Stage 4 / Last Exit: 모든 기술을 조합한 코스.

선택 화면에서 모든 코스에 접근할 수 있다. 안내·크레딧·음량은 선택/일시정지 화면에 있다. 독립 실행판에는 종료 버튼이 있다. 자세한 키 조작과 기록 정책은 README.md에 있다.

## 코스 수정

Resources/Stages/Stage1~Stage4.asset이 원본이다. 에셋 선택 후 Scene preview, Move in Scene, Frame Entire Course 도구로 좌표를 편집하도록 구현했다. 필요하면 편집용 씬 복사본을 생성할 수 있으며 기존 복사본은 자동 변경하지 않는다.

## macOS 실행본 만들기

Mac Unity 설치·활성화 후 다음 명령을 사용한다.

```bash
python3 Tools/unity_tasks.py build-mac
# 앱·안내·크레딧을 ZIP으로도 묶기
python3 Tools/unity_tasks.py build-mac --package
```

실제로 빌드가 성공하면 Builds/<실행 ID>/에 MagnetEscape.app, PLAYING.txt, MagnetEscape.build.txt와 build.log가 남도록 작성했다. 제출 대상에 맞게 앱과 PLAYING.txt를 함께 묶는다. 출력 폴더는 Git에 포함되지 않는다. `--package`를 지정하면 macOS의 ditto로 앱 번들 권한·심볼릭 링크·메타데이터를 보존해 MagnetEscape-macOS.zip에 앱·PLAYING.txt·CREDITS.md를 묶도록 구현했다. 빌드 로그와 개인 경로를 담은 개발 요약은 배포 ZIP에 넣지 않는다. 원본 앱은 보존하고 임시 복사본만 정리한다. 실제 패키지 생성과 다른 Mac에서의 실행은 미검증이다.

현재 저장소에는 검증된 macOS 실행본을 포함하지 않는다. 빌드 코드 작성과 실제 빌드 성공은 별개이며, 현재 환경에서는 Unity 실행·앱 생성·서명·공증·다른 Mac 실행을 확인하지 않았다. 미검증 항목은 TASKS.md와 PROGRESS.md에 남겨 두었다.

## 포함된 문서

- README.md: 시작 방법·조작·코스 편집·빌드.
- GOAL.md: 기획 범위와 완료 조건.
- TASKS.md / PROGRESS.md: 구현 상태·보류 검증·변경 이력.
- CREDITS.md: 이미지·사운드·도구 출처.
