# 마그넷 스케이터 제출 자료

## 최신 실행본

- `Builds/20261117_235454_UTC_969c7978/MagnetEscape.app`
- `Builds/20261117_235454_UTC_969c7978/MagnetEscape-macOS.zip`
- 같은 폴더 PLAYING.txt 한글 안내, ZIP에는 CREDITS.md도 포함.

Unity2022.3.62f1 Mac Universal입니다. Apple Silicon에서 실제 실행했고 Intel/arm64 두 슬라이스를 포함합니다. 실제 Intel/다른 Mac·서명/공증 배포는 별도 검수입니다. 산출물/로그는 Git 제외, 소스는 develop입니다.

## 구현 범위

축제 이야기·입문/일반/도전 각각4개로 총12코스, Q상층 레일/E실제 다리99지점, 기존 자동 전진/점프/대시/수직 자력/CP/기록을 포함합니다. 새 축제 기록v3는 이전v2를 보존합니다.

별도 무한 점수 모드: N 시작, B 이 기기 상위5개 순위. 무작위32unit 템플릿·구간 생성/정리·거리10점/m·별50+연속보너스·첫경로250점·사망1회 종료. 온라인 랭킹은 아닙니다.

## 검수

Unity Edit20+Play38=58건 PASS. 고정12코스 기본 경로의 실제 물리 무사망 완주, 모든93CP 준비거리, 제작 다리/레일·상태 차단, 무한 청크 정리/별 점수/중복 방지/사망 종료/로컬순위 저장 확인. 최종 무한 배경 회귀는 추가 필터1건 RED→GREEN. Python21·87GUID/14씬·diff 검사 통과.

실행본에서는 한글 난이도 메뉴/스토리·Q로 만든 레일 탑승·E다리로 콘 우회, 무한 별2개/570점/46m/충돌1회 종료와 정상 종료·재실행 후 순위 보존을 확인했습니다. 사람의12코스 직접 완주/모든경로 조합·오디오체감·장시간무한은 별도입니다.

## 제출 소스와 문서

Assets/Packages/ProjectSettings, README·GOAL·TASKS·PROGRESS·CREDITS와 docs/Magnet_Skater_Game_Design.docx를 사용합니다. 기획서는 최신스토리/12코스/경로/무한점수를 반영한2페이지이며 렌더검수했습니다. 원본씬/GUID는 유지합니다. Library/Temp/Logs/UserSettings/Builds/artifacts는 제출소스에서 제외합니다.

```bash
python3 Tools/unity_tasks.py test
python3 Tools/unity_tasks.py build-mac --package
```

최종 빌드 요약: 오류0/경고0. 앱 실행 파일과 UnityPlayer 모두 x86_64+arm64. ZIP의 안내/크레딧 포함·개발로그 제외 확인.
