# Android 출시 후보 체크리스트

## 자동 구성 완료

- [x] 패키지명 `com.carddefense.game` 고정
- [x] `0.3.0`, versionCode `3` 버전 정책 적용
- [x] IL2CPP 및 ARM64 구성
- [x] Android 최소 API 26 구성
- [x] 512×512 스토어 아이콘 준비
- [x] 1024×500 피처 그래픽 준비
- [x] 1080×1920 세로 스플래시 준비
- [x] 한국어 스토어 설명 초안 작성
- [x] 오프라인·로컬 저장 기준 개인정보처리방침 작성
- [x] 비밀번호를 Git에 저장하지 않는 서명 AAB 빌드 경로 구성

## 개발자 확인 필요

- [ ] Google Play App Signing용 업로드 키스토어 생성 및 안전한 별도 백업
- [ ] 아래 네 환경 변수를 빌드 PC에만 설정
  - `CARD_DEFENSE_KEYSTORE_PATH`
  - `CARD_DEFENSE_KEYSTORE_PASSWORD`
  - `CARD_DEFENSE_KEY_ALIAS`
  - `CARD_DEFENSE_KEY_PASSWORD`
- [ ] 실제 Android 기기에서 1080×1920 이상 스크린샷 4~8장 촬영
- [ ] 개인정보처리방침을 공개 HTTPS 주소에 게시
- [ ] 실제 개발자 이메일과 국가·연락처 등록
- [ ] Google Play 콘텐츠 등급 설문 완료
- [ ] 데이터 보안 항목에 현재 데이터 미수집 상태를 정확히 입력
- [ ] 비공개 테스트 트랙에 서명 AAB 업로드
- [ ] 내부 테스터 설치, 저장·복귀·BGM·30분 플레이 확인
- [ ] Pre-launch report의 충돌, ANR, 화면 문제 확인

## 빌드 방법

Unity 메뉴에서 다음 순서로 실행합니다.

1. `Card Defense > Release > Prepare Android Store Settings`
2. 서명 환경 변수가 없는 기술 검증: `Build QA App Bundle`
3. 실제 스토어 제출: `Build Signed Store App Bundle`

명령행에서는 Unity 2022.3.19f1에 `-executeMethod`로 다음 메서드를 전달할 수 있습니다.

- QA AAB: `CardDefense.Editor.AndroidReleaseBuild.BuildQaAppBundle`
- 서명 AAB: `CardDefense.Editor.AndroidReleaseBuild.BuildSignedStoreAppBundle`

QA AAB는 기술 검증용이며 Google Play 제출 파일이 아닙니다. 스토어 제출에는 반드시 별도로
보관한 업로드 키로 생성한 서명 AAB를 사용해야 합니다.
