# LoanGame 프로젝트 분석 보고서

분석일: 2026-10-20  
프로젝트 경로: `C:\Users\wkdrj\LoadGame`  
분석 범위: Unity Editor 상태, 씬 구성, C# 코드, 패키지, 빌드 설정, 에셋 구성, 테스트 및 Git 작업 트리

## 1. 요약

이 프로젝트는 모바일 세로 화면을 목표로 하는 2D 도시 건설·경제 시뮬레이션 게임이다. 플레이어가 토지를 확보하고 건물을 배치해 생산과 도시 가치를 높이며, 부채·변동 금리·유지비·주식·업그레이드·개발 프로젝트를 관리하는 구조다. 최종 개발 프로젝트를 완료하면 승리하고, 재무 위험 상태가 악화된 뒤 파산 카운트다운이 끝나면 게임 오버가 된다.

현재 상태는 프로토타입을 넘어 여러 시스템이 통합된 플레이 가능한 개발 단계로 보인다. Unity Editor와 MCP 연결은 정상이며 컴파일 오류도 없다. 반면 자동화 테스트가 없고, 단일 씬과 다수의 전역 매니저에 시스템이 집중되어 있으며, 대용량 오디오와 대규모 미커밋 변경이 유지보수 및 배포 위험을 높이고 있다.

## 2. 기술 환경

| 항목 | 현재 상태 |
|---|---|
| Unity | `6000.3.18f1` |
| 렌더링 | URP `17.3.0`, 2D Renderer |
| 타깃 플랫폼 | Android |
| 스크립팅 백엔드 | IL2CPP |
| API 호환성 | .NET Standard 2.0 |
| 제품명 | `LoanGame` |
| 회사명 | `HygeonStudio` |
| 번들 버전 | `4.0` |
| 활성 씬 | `Assets/Scenes/SampleScene.unity` |
| 빌드 씬 | `SampleScene` 1개 |
| Editor 상태 | Ready, Play Mode 정지, 컴파일 오류 없음 |

주요 패키지는 Input System, uGUI, TextMesh Pro, URP, 2D Tilemap, DOTween, Google Mobile Ads, Unity Test Framework, Unity AI Assistant 및 Unity Pipeline이다. `com.unity.ai.assistant`와 `com.unity.pipeline`은 프리릴리스/실험 버전이므로 업데이트 시 호환성 검증이 필요하다.

## 3. 프로젝트 규모

- 자체 게임 코드: C# 91개, 약 13,813줄
- 전체 `Assets` 크기: 약 915.9MB
- 주요 에셋 수: WAV 841개, PNG 87개, ScriptableObject/설정 `.asset` 34개, 프리팹 7개, 씬 2개
- 코드 비중:
  - Map: 22개 파일 / 약 4,919줄
  - UI: 33개 파일 / 약 4,465줄
  - Economy: 22개 파일 / 약 3,100줄
  - Balance: 5개 파일 / 약 416줄
  - Audio: 3개 파일 / 약 397줄
  - Ads: 1개 파일 / 약 219줄
  - Upgrades: 4개 파일 / 약 203줄

오디오가 전체 용량의 대부분을 차지한다. 가장 큰 단일 WAV 파일은 약 77.9MB이며 30~50MB급 파일도 다수 존재한다. 실제 사용 파일만 선별하고 모바일용 압축·스트리밍 설정을 점검하는 것이 빌드 크기와 메모리 사용량 개선에 가장 큰 효과를 낼 가능성이 높다.

## 4. 게임 시스템 구조

### 맵과 건설

`MapManager`가 `Resources/city_map.json`을 읽어 `GridMapData`를 구성한다. `ChunkedGridMapRenderer`는 타일맵 기반으로 셀, 건물, 장식, 나무 등을 렌더링하며 `GridCellSelectionController`와 `CellInfoPopup`이 사용자 입력 및 구매 UI를 담당한다.

건물 정보는 `BuildingDefinition` ScriptableObject와 `BuildingDatabase`로 데이터화되어 있다. 건물은 생산, 유지비, 도시 가치, 역할, 배치 방식, 도로 연계 등의 속성을 가지며 품질 시스템과 배치 피드백도 분리되어 있다.

### 경제 루프

`EconomyManager`가 현금, 부채, 생산량, 유지비, 금리, 순수익, 파산 상태를 관리한다. 기본 틱 간격은 0.5초이며 기본 시작 현금은 1,000, 시작 부채는 0, 기본 금리는 3%다.

핵심 흐름은 다음과 같다.

1. 건물과 업그레이드가 생산량 및 유지비에 영향을 준다.
2. 틱마다 생산 수익과 유지비를 정산한다.
3. 현금으로 충당하지 못한 유지비는 부채로 전환된다.
4. 부채에는 변동 금리가 적용되고 신규 차입은 금리 압력을 높인다.
5. 순자산과 재무 상태가 악화되면 파산 카운트다운이 진행된다.
6. 회복하지 못하면 게임 오버, 최종 프로젝트를 완수하면 승리한다.

금액은 대부분 `long`, 비율과 계산 중간값은 `double`을 사용하며 소수 누적 버퍼를 두어 틱 기반 반올림 손실을 줄였다. 핵심 계산을 `EconomyRules`, `PopulationRules`, `ProjectRules` 같은 순수 규칙 클래스로 분리한 점은 테스트 가능성과 밸런스 조정 측면에서 좋은 구조다.

### 도시 성장과 승리 조건

`CityValueManager`, `PopulationManager`, `DevelopmentProjectManager`가 도시 성장 축을 구성한다. 개발 프로젝트는 도시 가치, 인구, 고용 효율, 행복도, 양의 순수익 유지 시간, 선행 프로젝트 및 특정 건물 수를 요구할 수 있다.

프로젝트 데이터가 부족하면 코드에서 5단계 기본 체인을 생성한다. 마지막 `Metropolitan Landmark` 프로젝트에는 인구 350, 고용 효율 85%, 행복도 80, 양의 순수익 60초 등의 조건이 설정되며 완료 시 게임 승리로 연결된다.

### 부가 시스템

- 주식: `StockMarketManager`, 캔들 데이터와 거래 UI
- 업그레이드: ScriptableObject 데이터와 레벨별 효과
- 저장: 경제, 지도, 건물, 프로젝트, 업그레이드, 주식 상태 통합 저장
- 오디오: BGM/SFX/물소리, PlayerPrefs 기반 볼륨 설정
- 광고: Google Mobile Ads 및 UMP 동의 흐름
- UI: 휴대폰 앱 형태의 패널, 건물 구매, 프로젝트, 업그레이드, 설정, 게임 오버 화면
- 렌더링: 레트로 포스터 디더링 Renderer Feature와 커스텀 2D 셰이더

## 5. 씬 구성

활성 씬은 `SampleScene` 하나이며 주요 루트는 카메라, Grid, Canvas, EventSystem, Manager 계층 등으로 구성된다.

`Manager` 아래에는 다음 런타임 시스템이 집중되어 있다.

- MapManager / ChunkedGridMapRenderer / GridCellSelectionController
- EconomyManager / StockMarketManager / CityValueManager / NetWorthManager
- DevelopmentProjectManager / PopulationManager / UpgradeManager
- SoundManager / WaterAmbienceController
- AdManager / BuildingPlacementFeedback

`Grid` 아래에는 가시 셀, 건물, 선택 하이라이트, 장식, 나무용 Tilemap이 분리되어 있다. `Canvas`는 휴대폰 UI, 셀 상세 정보, 구매 피드백, 프로젝트·주식·설정·게임 오버 UI를 포함한다.

장점은 씬에서 시스템 관계를 빠르게 파악할 수 있다는 점이다. 단점은 하나의 대형 씬에 런타임 시스템과 UI가 결합되어 초기화 순서, 참조 누락, 씬 병합 충돌의 영향을 크게 받을 수 있다는 점이다.

## 6. 저장과 데이터 안정성

`SaveGameManager`는 `Application.persistentDataPath` 아래 JSON 저장 파일을 사용한다. 임시 파일 작성 후 교체하며 `.bak` 백업을 만들고, 손상된 저장은 별도 `.corrupt-타임스탬프` 파일로 이동한다. 저장 범위도 경제, 지도 소유 셀, 건물, 공개 셀, 개발 프로젝트, 업그레이드, 주식까지 포함되어 비교적 완성도가 높다.

보완이 필요한 부분은 다음과 같다.

- 저장 버전 필드는 있으나 명시적인 버전별 마이그레이션 계층이 보이지 않는다.
- 모바일에서는 앱 강제 종료 상황을 고려해 중요한 상태 변화 직후 저장 정책을 검토할 필요가 있다.
- 실제 기기에서 저장 파일 교체, 백업 복원, 저장 공간 부족, 손상 파일 복구를 테스트해야 한다.

## 7. 품질 상태와 주요 위험

### 높은 우선순위

1. **자동화 테스트 0개**  
   Unity Test Framework는 설치되어 있으나 Editor/PlayMode 테스트가 발견되지 않았다. 경제·파산·저장·건물 배치·프로젝트 조건은 회귀 가능성이 높은 핵심 로직이다.

2. **Git 작업 트리의 대규모 변경**  
   수정·삭제·신규 파일이 매우 많다. 특히 기존 `Assets/Sprites`가 삭제되고 `Assets/Resources/Sprites`로 이동된 형태이며, 씬·렌더러·경제·맵 코드·패키지 설정도 동시에 변경되어 있다. 기능 단위로 검증하고 작은 커밋으로 분리하지 않으면 회귀 원인 추적이 어렵다.

3. **모바일 빌드 크기와 메모리 위험**  
   `Assets` 약 916MB와 WAV 841개는 Android 배포에 부담이 크다. 미사용 오디오 제거, Vorbis/ADPCM 선택, Load Type 및 Sample Rate 최적화, 긴 BGM/환경음 스트리밍 적용이 필요하다.

4. **단일 씬·전역 매니저 결합**  
   다수 시스템이 `Instance` 싱글턴과 `FindFirstObjectByType`로 서로를 찾는다. 빠른 개발에는 유리하지만 초기화 순서와 씬 참조에 민감하고 단위 테스트 및 씬 분리에 불리하다.

### 중간 우선순위

5. **대형 클래스 집중**  
   Map, UI, Economy 계층에 책임이 큰 클래스가 있다. 렌더링, 입력, 규칙, 상태 변환, 에디터 전용 임포트 처리를 더 분리하면 변경 영향 범위를 줄일 수 있다.

6. **프리릴리스 패키지 의존성**  
   AI Assistant와 Pipeline이 프리릴리스/실험 버전이다. 게임 런타임 필수 기능과 개발 도구 의존성을 분리하고 버전을 고정한 상태에서 업그레이드해야 한다.

7. **명칭 불일치**  
   저장소/네임스페이스는 `LoadGame`, 제품명은 `LoanGame`이다. 의도된 브랜딩인지 오타인지 확정하고 패키지명, 저장 경로, 문서, 스토어 메타데이터를 일관되게 맞추는 것이 좋다.

8. **씬 이름이 SampleScene**  
   현재 유일한 빌드 씬이 `SampleScene`이다. 출시 전 목적에 맞는 이름으로 변경하고 부트스트랩/게임플레이 씬 분리 여부를 결정하는 것이 좋다.

### 현재 확인된 Editor 상태

- 컴파일 실패: 없음
- Console Error: 0
- Console Warning: 1
- 경고 내용: Unity AI Assistant가 Codex 실행 파일의 Windows 서명 정보를 인증서로 해석하지 못한 개발 도구 경고

이 경고는 현재 게임 코드 오류는 아니지만 AI/MCP 패키지 업데이트 후 재확인할 가치가 있다.

## 8. 테스트 권장안

우선 순수 규칙 계층부터 EditMode 테스트를 추가하는 것이 비용 대비 효과가 높다.

1. `EconomyRules`
   - 수익·유지비 정산
   - 현금 부족분의 부채 전환
   - 이자 및 순수익 계산
   - 극단값과 오버플로 경계
2. `PopulationRules`
   - 주거·일자리·행복도 조합별 인구 계산
3. `ProjectRules`
   - 잠금 해제 및 완료 조건 경계값
4. `BalanceRegressionSuite`
   - 고정 시드 시뮬레이션 결과의 허용 범위
5. 저장 시스템
   - 왕복 직렬화
   - 이전 버전 및 손상 파일 복구
6. PlayMode
   - 건물 구매 → 생산 증가 → 저장 → 재실행 → 복원
   - 부채 증가 → 위험 상태 → 회복/파산
   - 최종 프로젝트 완료 → 승리 화면

## 9. 권장 작업 순서

### 1단계: 현재 상태 고정

- Android 개발 빌드 및 실제 기기 스모크 테스트
- 현재 대규모 변경을 기능 단위로 분리
- 씬과 ScriptableObject 참조 누락 검사
- 저장 파일 백업 후 저장/복원 검증

### 2단계: 회귀 방지

- Balance 규칙 EditMode 테스트 추가
- 핵심 게임 루프 PlayMode 테스트 추가
- CI에서 컴파일 및 테스트 실행
- 데이터베이스 ID 중복과 참조 무결성 검증 도구 추가

### 3단계: 모바일 최적화

- 오디오 사용 여부 조사 및 미사용 파일 제거
- 압축 포맷, Load Type, Sample Rate 재설정
- 텍스처 아틀라스와 Android 압축 포맷 점검
- Unity Memory Profiler와 실제 기기 프로파일링

### 4단계: 구조 개선

- 게임 상태와 MonoBehaviour 표현 계층 분리
- 초기화 순서를 명시하는 Bootstrap 계층 도입
- 대형 UI 및 Map 클래스를 역할별 서비스로 분리
- 필요하면 Bootstrap/Game/UI 씬 또는 Prefab 단위로 분리

## 10. 결론

LoanGame은 경제·도시 건설·진행·저장·광고·오디오·모바일 UI가 이미 연결된 중규모 Unity 프로젝트다. 규칙 클래스와 ScriptableObject 데이터 분리는 좋은 기반이며 현재 컴파일 상태도 정상이다. 다음 품질 도약의 핵심은 새 기능 추가보다 **테스트 기반 확보, 대규모 작업 트리 정리, 모바일 에셋 최적화, 전역 매니저 결합 완화**다.

이 보고서는 2026-10-20의 로컬 작업 트리와 열린 Unity Editor 상태를 기준으로 작성되었다. 미커밋 변경이 많으므로 이후 분석 결과는 커밋 또는 씬 상태에 따라 달라질 수 있다.
