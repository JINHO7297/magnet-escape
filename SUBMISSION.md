# 마그넷 스케이터 제출 자료

## 최신 실행본

- `Builds/20261121_160053_UTC_f4e895c6/MagnetEscape.app`
- `Builds/20261121_160053_UTC_f4e895c6/MagnetEscape-macOS.zip`
- 같은 폴더의 PLAYING.txt는 한국어 조작 안내다. ZIP에는 안내와 크레딧이 들어 있다.

Unity 2022.3.62f1, macOS Universal. EditMode 20건·PlayMode 46건, Python 회귀 검사 21건이 통과했다. 두 난이도에서 생성된 구간 4곳씩 점프와 대시로 다음 옥상에 착지하는 것을 확인했다. 구간 스트리밍·일회 종료와 점수 저장도 검사했다. 사람의 장시간 주행 난이도 평가는 남아 있다.

## 구현 범위

시작 화면에서 1(쉬움)이나 2(어려움)를 선택하면 결승 없이 달린다. 거리와 난이도에 따라 틈의 폭이 달라지며, 구간 경계 위치와 옥상 높이는 새 질주마다 달라진다. 별이 이륙 지점을 안내한다. 움직이는 중층 발판, 지붕, 반전 천장으로 경로를 선택한다. 충돌이나 낙사로 종료하고 거리·별·시설 점수를 합산해 해당 난이도의 기기 상위 5개 기록을 저장한다.

## 이전 버전 검수

Unity EditMode 20건과 PlayMode 42건으로 12코스 자동 무사망 완주, 93개 체크포인트, 네 시설의 물리 동작, 무한 청크·점수·저장을 확인했다. Python 21건과 GUID 87개·14씬 정적 검사를 통과했다.

macOS에서 실제 키 입력으로 입문 5~8코스의 시설을 각각 성공했다. 무한 질주에서 광고판 +180점과 최종 1,275점·81m·별 5개의 로컬 순위가 표시됐으며, 정상 종료와 최종 앱 재실행 후에도 해당 기록을 확인했다. 최종 한국어 메뉴와 큰 전체 화면을 확인했다. 사람의 전체 코스 밸런스, 장시간 무한, 오디오 체감, 실제 Intel Mac 검수는 남아 있다.

## 제출 소스와 문서

[기말과제 게임 기획서](docs/Magnet_Skater_Game_Design.docx)는 12코스 버전을 기준으로 작성한 A4 2장이다. 게임 개요부터 유사 게임 및 차별성까지 일곱 항목과 실제 광고판 주행 화면을 담았다. 세부 구현은 [도시 파쿠르 상세 설계](docs/superpowers/specs/2026-11-19-city-parkour-design.md)를 참고한다. 소스는 Assets, Packages, ProjectSettings와 저장소 문서를 사용한다. Library, Temp, Logs, UserSettings, Builds, artifacts는 소스 제출에서 제외한다.

```bash
python3 Tools/unity_tasks.py test
python3 Tools/unity_tasks.py build-mac --package
```
