# 마그넷 스케이터 제출 자료

## 최신 실행본

- `Builds/20261118_154111_UTC_b24b7c4a/MagnetEscape.app`
- `Builds/20261118_154111_UTC_b24b7c4a/MagnetEscape-macOS.zip`
- 같은 폴더의 PLAYING.txt는 한국어 조작 안내다. ZIP에는 안내와 크레딧이 들어 있다.

Unity 2022.3.62f1로 빌드한 macOS Universal 앱이다. 앱과 UnityPlayer의 x86_64·arm64 슬라이스, ZIP 무결성과 안내 포함을 확인했다. 빌드 오류는 0개다. 경고 14개는 헤드리스 빌드 중 Unity가 씬별 Ambient/Reflection Probe를 갱신할 수 없다는 메시지였다. 실제 실행은 Apple Silicon에서 확인했다.

## 구현 범위

난이도별 네 구간씩 총 12코스에서 민의 연락을 따라 도심을 잇고 옥상 축제를 연다. 99곳의 선택 지점 중 79곳에 회전 광고판, 천장 케이블, 이동 차량, 연쇄 전광판을 배치했다. Space 가변 점프, Shift 전방 대시, Q/E 수직 자력으로 시설을 이용한다. 코스 기록은 축제 v3에 저장한다.

별도 무한 질주는 선택 화면 N으로 시작한다. 32m 구간 8종을 섞고 거리·별·실제 시설 이용으로 점수를 얻는다. B는 이 기기의 상위 5개 순위다. 새 점수 규칙은 v2에 저장하며 이전 v1 데이터도 보존한다.

## 검수

Unity EditMode 20건과 PlayMode 42건으로 12코스 자동 무사망 완주, 93개 체크포인트, 네 시설의 물리 동작, 무한 청크·점수·저장을 확인했다. Python 21건과 GUID 87개·14씬 정적 검사를 통과했다.

macOS에서 실제 키 입력으로 입문 5~8코스의 시설을 각각 성공했다. 무한 질주에서 광고판 +180점과 최종 1,275점·81m·별 5개의 로컬 순위가 표시됐으며, 정상 종료와 최종 앱 재실행 후에도 해당 기록을 확인했다. 최종 한국어 메뉴와 큰 전체 화면을 확인했다. 사람의 전체 코스 밸런스, 장시간 무한, 오디오 체감, 실제 Intel Mac 검수는 남아 있다.

## 제출 소스와 문서

[기말과제 게임 기획서](docs/Magnet_Skater_Game_Design.docx)는 최신 실행본 기준의 A4 2장이다. 게임 개요부터 유사 게임 및 차별성까지 일곱 항목과 실제 광고판 주행 화면을 담았다. 세부 구현은 [도시 파쿠르 상세 설계](docs/superpowers/specs/2026-11-19-city-parkour-design.md)를 참고한다. 소스는 Assets, Packages, ProjectSettings와 저장소 문서를 사용한다. Library, Temp, Logs, UserSettings, Builds, artifacts는 소스 제출에서 제외한다.

```bash
python3 Tools/unity_tasks.py test
python3 Tools/unity_tasks.py build-mac --package
```
