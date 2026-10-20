# FINAL DEVELOPMENT PLAN

작성일: 2026-10-03  
대상 프로젝트: `LoadGame` / Unity `6000.3.18f1`  
목표 플랫폼: Android 모바일 / Google Play Store / Portrait  
문서 성격: 게임 디자인·구현·시뮬레이션·QA를 연결하고 완료 결과와 남은 출시 게이트를 추적하는 살아 있는 개발 기준서.

## DOCUMENT ROLE AND OWNERSHIP

이 문서는 사용자가 모든 수치를 미리 지정하는 요구사항 목록이 아니다. 앞으로 작업자는 다음 역할을 함께 맡는다.

- Lead Game Designer: 한 판의 목표, 진행 곡선, 시스템 간 관계와 승리 조건을 설계한다.
- Economy / Balance Designer: 현재 데이터를 수치로 분석하고 초기 가설을 세운 뒤 반복 검증한다.
- Unity Developer: 설계를 실제 런타임 구조에 안전하게 구현하고 자동화 가능한 검증 도구를 만든다.
- QA / Balance Tester: 자동 시뮬레이션과 실제 Play Mode 테스트를 병행해 재미와 안정성을 검증한다.
- Mobile Game UI/UX Designer: Portrait 한 손 플레이를 기준으로 실제 화면 배치, 정보 구조, 터치 동선, 상태 피드백을 설계하고 기존 uGUI Scene/Prefab/Script 구조 안에서 구현·검증한다.

작업자는 건물 가격·생산·유지비·인구·고용·여가·업그레이드·프로젝트·금리·파산 기준·최종 목표 등 가역적인 게임 수치를 직접 제안하고 조정할 수 있다. 숫자는 감으로 정하지 않고 파생 지표, 시뮬레이션, 실제 플레이 결과를 근거로 결정한다. 사용자에게는 방향을 바꾸거나 외부 책임이 필요한 결정만 요청한다.

핵심 반복 흐름:

```text
DESIGN
  → IMPLEMENT
  → SIMULATE
  → ANALYZE
  → ADJUST VALUES
  → SIMULATE AGAIN
  → PLAYTEST
  → ADJUST
  → REGRESSION TEST
  → POLISH
  → RELEASE
```

## PRODUCT VISION

완성할 게임은 다음 경험을 제공하는 모바일 도시 경제 타이쿤이다.

> 대출을 성장 도구로 적극 활용해 도시를 빠르게 키우되, 무리한 확장으로 발생하는 이자·유지비·인구·고용·행복 문제를 관리하며 최종 도시 프로젝트를 완성한다.

대출은 단순 페널티가 아니다. 생산성이 좋은 투자에 적정 수준으로 사용하면 안전 전략보다 빠르게 성장해야 한다. 반대로 낮은 수익 자산, 부족한 노동력, 과도한 유지비에 빚을 쓰면 부채가 커지고 실제 파산할 수 있어야 한다. 안전한 현금 중심 전략과 공격적 대출 전략 모두 유효하지만 어느 하나도 항상 정답이어서는 안 된다.

최종 목표는 “코드가 작동함”이 아니라 다음 한 판이다.

- 처음 실행한 플레이어가 WORK로 시동 자금을 마련한다.
- 토지·주거·일자리·여가를 연결해 도시 경제를 만든다.
- 대출로 성장을 앞당기고, 최소 한 번은 상환 압박이나 경제적 긴장을 경험한다.
- 프로젝트를 통해 다음 목표와 새 선택지를 얻는다.
- 기다리기만 하는 구간 없이 도시 상태에 맞는 결정을 반복한다.
- 균형 잡힌 최종 프로젝트를 완성하고 명확한 Victory 화면을 본다.
- 잘못 운영하면 회복 기회 후 실제 파산한다.

## V1 CONFIRMED DECISIONS

| 결정 | v1 기준 |
|---|---|
| 출시 | Google Play Store 단독 출시 |
| 화면 | Portrait 고정 |
| 저장 | 로컬 진행 저장 필수 |
| 광고 | Google Mobile Ads 포함 |
| 자동 상환 | 기존 구현 유지. 활성화 시 모든 `AddCash` 유입을 Debt 상환에 우선 사용 |
| 파산 Capacity | 이자는 이미 Debt에 누적되므로 분모에서 재차감하지 않음. `Gross Production - Maintenance` 유지 |
| 주식 구매 | 자동 대출 허용 |
| 철거 환급 | 건설 비용의 50% |

## ANALYSIS BASELINE (STAGE 0 SNAPSHOT)

- 활성 씬은 `Assets/Scenes/SampleScene.unity` 하나이며 확인 당시 GameObject 251개, 루트 7개였다.
- Unity CLI 1.0.0-beta.12와 Unity Editor 6000.3.18f1 연결 상태는 ready다.
- 이전 Play Mode 확인에서 컴파일 Error 0, Console Error 0, Warning 2였다. `AdManager`와 `SoundManager`가 `/Manager` 자식에서 `DontDestroyOnLoad`를 호출한다.
- 시작 상태는 Cash 0, Debt 0, Population 0, Capacity 0, Jobs 0, Happiness 100이었다.
- `DevelopmentProjectManager`가 2개이고 하나는 ProjectDatabase가 없다.
- 등록 프로젝트는 1개뿐이며 존재하지 않는 `small_office`를 요구해 완료할 수 없다.
- 최종 프로젝트, Victory 판정, 게임 진행 저장, 자동화된 EditMode/PlayMode 테스트는 없다.
- Android Build Profile과 Google Mobile Ads 11.5.0은 있으나 현재 광고는 테스트 배너이고 운영 ID가 비어 있다.
- `test_map`, 테스트 캔들, 테스트 광고, placeholder 건물 이름과 과도한 디버그 로그가 남아 있다.
- 최신 작업 트리는 대량의 미커밋 변경을 포함하므로 구현 전에 기준 커밋 또는 별도 백업이 필요하다.

## GAME DESIGN ASSUMPTIONS

다음은 구현 전 1차 설계 가설이며, 시뮬레이션과 플레이테스트로 변경한다.

1. 첫 엔딩 기준 목표 플레이타임은 **25~35분**, 숙련 재도전은 **15~25분**이다.
2. 플레이어는 10~20초마다 작은 행동, 45~90초마다 의미 있는 구매 또는 정책 결정을 할 수 있어야 한다.
3. 의도적인 저축을 제외하고 60초 이상 아무 판단 없이 돈만 기다리는 구간은 실패로 본다.
4. 정상적인 첫 플레이에서도 대출을 한 번 이상 쓰는 것이 자연스럽고, 좋은 투자라면 대출 사용이 실제 완주 시간을 줄여야 한다.
5. 안전 전략은 느리지만 안정적이고, 공격 전략은 빠르지만 회복 가능한 위험을 가진다.
6. Population은 일자리 생산을 제한하고, Jobs는 주거 투자의 가치를 만들며, Leisure는 인구 성장과 최종 목표에 실질적으로 필요해야 한다.
7. Happiness는 장식용 숫자가 아니라 성장 속도와 프로젝트 달성 시간에 체감 가능한 영향을 줘야 한다.
8. Quality는 기대감과 빌드 변형을 만들되 Rainbow 하나가 중후반 경제 전체를 무력화해서는 안 된다.
9. 주식은 보조적 고위험 선택지다. 도시 건설보다 안정적으로 높은 수익을 내거나 부채를 우회하는 주력 엔진이 되어서는 안 된다.
10. 최종 프로젝트는 단순 Cash 체크가 아니라 Population, Employment, Happiness, 선행 Projects, 경제 지속 가능성을 함께 검증한다. Debt 보유 자체는 승리를 막지 않는다.
11. 광고는 핵심 경제 보상을 강제하지 않고 플레이 흐름을 훼손하지 않는 위치와 빈도로 시작한다. 정확한 배치는 실제 세션 테스트로 조정한다.
12. v1은 기존 시스템을 서로 의미 있게 연결하는 데 집중하며 대형 신규 시스템을 추가하지 않는다.

## TARGET SESSION CURVE

### 제안 플레이타임

Google Play Portrait 타이쿤의 첫 완주 목표를 **30분 중앙값**으로 둔다. 현재는 엔딩이 없고 수치가 임시 상태이므로 아래 값은 목표 역산을 위한 H0 가설이다. 실제 데이터 10회 이상을 얻기 전에는 확정값으로 취급하지 않는다.

| 구간 | 시간 | 플레이어 목표 | 목표 상태 범위(H0) | 핵심 결정 | 위험 수준 |
|---|---:|---|---|---|---|
| EARLY GAME | 0~6분 | WORK로 시동, 첫 주거+일자리, 첫 대출 이해 | Cash 0~2k, Debt 0~2k, Production 20~80/s, Maintenance 5~20/s, Pop 5~25, Jobs 10~30, Happiness 55~100 | 현금 대기 vs 소액 대출, 첫 건물 조합 | 낮음. 실수 후 1~2분 내 회복 가능 |
| MID GAME | 6~16분 | 도시 기능 분화, 여가 확보, 첫 프로젝트 1~2개 | Cash 1k~8k, Debt 1k~15k, Production 100~450/s, Maintenance 30~140/s, Pop 30~100, Jobs 30~120, Happiness 60~90 | 확장·상환·업그레이드·프로젝트 우선순위 | 중간. 잘못된 대출은 경고/회복 행동 요구 |
| LATE GAME | 16~28분 | 대형 건물과 프로젝트 체인, 경제 안정화 | Cash 5k~30k, Debt 5k~60k, Production 500~1,800/s, Maintenance 150~650/s, Pop 100~300, Jobs 100~340, Happiness 70~95 | 고효율 대출, 노동력·여가 확충, 주식/업그레이드 선택 | 중~높음. 공격 전략은 Insolvent 접근 가능 |
| END GAME | 28~35분 | 최종 프로젝트 조건 정리와 완공 | Cash 15k~80k, Debt 0~100k 허용, Production 1,500~4,000/s, Maintenance 400~1,400/s, Pop 250~500, Employment ≥85%, Happiness ≥75 | 최종 투자 시점, 현금/부채/운영 여유 판단 | 관리된 위험. 부채가 있어도 양의 운영 여력으로 승리 가능 |

구간별 콘텐츠 가설:

- Early: Small House, 소형 Workplace, 도로, WORK, 자동 대출·상환 소개.
- Mid: 중형 Workplace, 첫 Leisure, City Efficiency/Work Power, Industrial 계열 프로젝트.
- Late: 대형 2x2 건물, 고비용 Leisure, 복합 프로젝트, 주식의 선택적 활용.
- End: 선행 프로젝트 완료 후 최종 프로젝트 해금. 일정 Population, Employment, Happiness, City Value, 양의 운영 Capacity를 동시에 요구.

세션 곡선은 “평균 Cash가 계속 커지는가”보다 **다음 선택까지 걸리는 시간**, **부채 부담 대비 신규 생산**, **회복 가능성**, **시스템 사용 다양성**으로 평가한다.

## CURRENT ECONOMY MODEL

현재 Economy Tick은 0.5초다.

```text
OperatingCapacity = GrossProduction - Maintenance
InterestPerSecond = Debt × CurrentInterestRate
DisplayedNetIncome = GrossProduction - Maintenance - InterestPerSecond

Tick order:
1. Maintenance를 Cash에서 지불
2. 부족하면 같은 Tick Production으로 지불
3. 그래도 부족하면 Debt 증가
4. 남은 Production을 AddCash로 전달
5. Debt 이자를 Debt에 직접 누적
6. 금리·위험도·파산 카운트다운 갱신
```

자동 상환 ON이면 운영 생산뿐 아니라 WORK와 주식 매도 등 모든 `AddCash`가 Debt를 먼저 줄인다. v1은 이 동작을 유지하고 UI에 명시한다.

파산 위험은 `OperatingCapacity`와 Debt payback seconds를 사용한다. v1은 Interest를 Capacity 분모에서 재차감하지 않는다. 다만 이자로 Debt가 증가하는 상황을 플레이어가 오해하지 않도록 현재 이자, Debt 변화율, 예상 상환 시간을 보여 줘야 한다.

Scene의 시작/최소 금리는 1%/초, 최대 25%/초이며 신규 Debt에 따라 빠르게 증가한다. 이 단위는 연이율이 아니라 초당 비율이므로 현재 핵심 정체성인 “생산적 대출”을 성립시키기 어렵다.

## CURRENT BUILDING DATA AND DERIVED METRICS

BuildingRole은 `1=Residential`, `2=Workplace`, `3=Leisure`로 해석했다. Unlock 필드는 현재 BuildingDefinition에 없으며 모두 사실상 즉시 구매 가능하다. Quality는 생산량과 City Value에 배율을 적용하지만 비용·Maintenance·Population Capacity·Required Workers·Leisure Capacity에는 적용하지 않는다. Residential/Leisure의 `productionPerSecond`는 현재 Population 생산 집계에서 사용되지 않는다.

### 원본 데이터

| ID / 표시명 | Role | Cost | Prod/s | Maint/s | Pop Cap | Workers | Leisure | Size | City Value | 현재 Unlock |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `road_normal` / ROAD | Road 데이터상 Leisure | 10 | 0 | 0 | 0 | 0 | 0 | 1x1 | 0 | 즉시 |
| `small_house` / Small House | Residential | 500 | 0 | 10 | 10 | 0 | 0 | 1x1 | 100 | 즉시 |
| `factory` / Factory | Workplace | 2,000 | 60 | 0 | 0 | 10 | 0 | 1x1 | 200 | 즉시 |
| `bank` / Bank | Workplace | 20,000 | 400 | 0 | 0 | 10 | 0 | 1x1 | 200 | 즉시 |
| `tower` / Tower | Workplace | 20,000 | 400 | 0 | 0 | 10 | 0 | 2x2 | 200 | 즉시 |
| `hospital` / Hospital | Workplace | 20,000 | 400 | 0 | 0 | 10 | 0 | 1x1 | 200 | 즉시 |
| `school` / School | Workplace | 20,000 | 400 | 0 | 0 | 10 | 0 | 1x1 | 200 | 즉시 |
| `finance` / Finance | Workplace | 20,000 | 400 | 0 | 0 | 10 | 0 | 2x2 | 200 | 즉시 |
| `888` / 888 | Workplace | 20,000 | 400 | 0 | 0 | 10 | 0 | 1x1 | 200 | 즉시 |
| `999` / 999 | Workplace | 20,000 | 400 | 0 | 0 | 10 | 0 | 1x1 | 200 | 즉시 |
| `1010110` / 1010110 | Leisure | 20,000 | 400* | 0 | 0 | 0 | 10 | 1x1 | 200 | 즉시 |
| `11111111` / 11111111 | Leisure | 20,000 | 400* | 0 | 0 | 0 | 10 | 1x1 | 200 | 즉시 |
| `12121212` / 12121212 | Leisure | 20,000 | 400* | 0 | 0 | 0 | 10 | 1x1 | 200 | 즉시 |
| `131313131313` / so high building | Workplace | 20,000 | 400 | 0 | 0 | 10 | 0 | 2x2 | 200 | 즉시 |

`*` Leisure 생산 400/s는 에셋에 기록되어 있지만 현재 경제 집계에서는 무시된다.

### 현재 파생 지표

| 건물/조합 | 기본 회수시간 | Maint 포함 회수시간 | Worker당 생산 | 타일당 생산 | 가격 대비 Capacity | 대출 위험 |
|---|---:|---:|---:|---:|---:|---|
| Small House | 해당 없음 | 순비용 10/s | 해당 없음 | 0 | 10 Pop / 500 = 0.02 | 수입 없이 Debt와 Maint만 증가하므로 단독 대출은 위험 |
| Factory, 완전고용 | 33.3초 | 33.3초 | 6/s | 60/s | 해당 없음 | 현재 100% 대출 금리에서는 생산보다 이자가 커질 수 있음 |
| House + Factory | 50초 | `2,500 / (60-10) = 50초` | 6/s | 30/s | Pop 10, Jobs 10 | 현 구조의 최소 자립 단위 |
| 20k 1x1 Workplace, 완전고용 | 50초 | 50초 | 40/s | 400/s | 해당 없음 | 50~100% 대출 시 현재 금리 상한 때문에 사실상 회복 불가 가능성 큼 |
| House + 20k Workplace | 52.6초 | `20,500 / (400-10) ≈ 52.6초` | 40/s | 200/s | Pop 10, Jobs 10 | Factory 조합과 회수시간이 거의 같아 티어 차별이 약함 |
| 20k 2x2 Workplace | 50초 | 50초 | 40/s | 100/s | 해당 없음 | 같은 1x1 건물보다 공간 효율만 75% 낮음 |
| 20k Leisure | 유효 생산 없음 | 회수 불가 | 해당 없음 | 0 | Leisure 10 / 20k = 0.0005 | 직접 수익이 없고 비싸 구매 이유가 매우 약함 |

현재 City Value/Cost는 House 0.20, Factory 0.10, 대부분의 20k 건물 0.01이다. 대형 건물이 더 비싸고 공간도 더 쓰지만 City Value는 Factory와 동일하다.

### 가장 큰 현재 밸런스 이상점

1. **대출 성장 판타지가 현재 금리에서 성립하기 어렵다.** Factory 100% 대출 2,000은 부채 가산 금리만 약 10%p/s이고 시작 금리를 더하면 약 11%/s 수준이 될 수 있다. 초기 이자 약 220/s는 완전고용 생산 60/s보다 크다. 50% 대출도 약 60/s 이자로 생산과 맞먹는다.
2. **Maintenance가 사실상 비어 있다.** Small House만 10/s이고 다른 모든 건물이 0이라 도시 운영비가 주거에만 역진적으로 걸린다. 고급·대형 건물의 위험과 운영 선택이 사라진다.
3. **중후반 Workplace가 복제 데이터다.** 다수 건물이 Cost 20,000 / Prod 400 / Workers 10 / Maint 0 / City Value 200으로 동일해 이름과 그림 외 전략 차이가 없다.
4. **2x2 건물은 명백히 열등하다.** 동일 비용·생산·노동·가치의 1x1 대안이 있는데 4타일을 차지한다.
5. **Leisure는 너무 비싸고 보상이 약하다.** 20,000으로 Leisure 10만 제공하며 기록된 생산 400은 무시된다. Leisure 없이도 Happiness 하한 40으로 진행 가능하면 완전히 버려질 수 있다.
6. **Quality 기대값과 분산이 지나치게 크다.** 가중치 80/20/10/5는 실제 확률 약 69.57%/17.39%/8.70%/4.35%다. 생산 기대 배율은 약 1.739배다. Rainbow 20k Workplace는 3,200/s로 회수시간 6.25초가 되어 한 번의 운이 전체 곡선을 건너뛸 수 있다.
7. **Quality가 House/Leisure의 핵심 기능에는 영향을 주지 않는다.** Capacity는 그대로이고 City Value만 배율을 받아 품질 피드백의 의미가 역할별로 불균형하다.
8. **City Value가 비용·크기와 연결되지 않는다.** 2k Factory와 20k 대형 건물이 모두 200이라 후반 프로젝트 조건이 값싼 건물 반복을 유도한다.
9. **프로젝트가 현재 완료 불가능하다.** `industrial_district`가 DB에 없는 `small_office` 2개를 요구한다. 비용 2,000, City Value 요구 5,000, 보상 1,000도 위험과 진척 보상의 관계가 약하다.
10. **업그레이드는 장기적으로 항상 정답이 될 위험이 크다.** Work Power와 City Efficiency 모두 비용 `100 × 1.18^level`, 레벨당 +10%, 최대 999다. City Efficiency는 전체 생산에 곱해져 건물 투자보다 강한 복리 엔진이 될 수 있다.
11. **모든 건물이 즉시 노출된다.** Unlock 구조가 없어 30분 세션의 Early/Mid/Late 구분과 학습 순서가 없다.
12. **주식 대출과 자동 상환 조합은 우회 수익 가능성이 있다.** 허용 정책은 유지하되 기대수익이 도시의 생산적 투자보다 안정적으로 높아지지 않게 해야 한다.

## ECONOMY BALANCE METHODOLOGY

### 설계 단위

모든 수치는 다음 관계를 함께 본다.

```text
EffectiveProduction = BaseProduction × EmploymentEfficiency × Quality × CityEfficiency
OperatingNet = EffectiveProduction - Maintenance
DebtGrowth = Interest - DebtRepayment
CashPaybackSeconds = Cost / max(OperatingNet, epsilon)
DebtPaybackSeconds = Debt / max(OperatingCapacity, epsilon)
TileEfficiency = EffectiveProduction / FootprintArea
LaborEfficiency = EffectiveProduction / RequiredWorkers
```

각 구매는 현금, 50% 대출, 100% 대출 세 경우를 최소 60/180/600초 구간으로 비교한다. 좋은 투자의 적정 대출은 무대출보다 다음 의미 있는 구매를 앞당겨야 한다. 낮은 생산성 자산의 과도한 대출은 Debt 증가 또는 Danger 진입을 유발해야 한다.

### 초기 목표 가드레일(H0)

- Early 핵심 조합의 무대출 회수시간: 60~120초.
- Mid 수익 건물 회수시간: 90~180초. 노동·여가 조건을 맞추면 하단, 무시하면 상단 이상.
- Late 대형 건물 회수시간: 120~240초. 높은 절대수익 대신 유지비와 노동 위험을 가진다.
- 좋은 투자 50% 대출: 정상 운영 시 이자 포함 2~4분 내 안정화 가능.
- 좋은 투자 100% 대출: 공격적이지만 회복 가능. 즉시 금리 상한이나 확정 파산이 되어서는 안 됨.
- 나쁜 투자 100% 대출: 30~90초 안에 명확한 경고가 나타나되 대응 행동으로 회복 여지를 제공.
- Maintenance는 Gross의 정상 15~35%, 잘못된 조합에서 40~70%를 목표로 시작.
- 자동 상환 ON/OFF는 속도와 유동성의 trade-off여야 하며 한쪽이 항상 우월하지 않아야 함.
- 경제 수치 변경은 한 번에 한 축을 우선 변경하고 같은 seed/전략으로 재실행한다.

금리 곡선은 절대 Debt보다 “해당 단계 생산력 대비 Debt”를 반영하는 방향을 우선 검토한다. 현재 초당 1~25%는 H0에서 크게 낮추되, 정확한 값은 시뮬레이터로 공격적 대출의 완주율과 회복시간을 맞춰 결정한다.

## BUILDING BALANCE METHODOLOGY

### 역할 설계

| 계층 | 의도 | 경제 특성 |
|---|---|---|
| 소형 | 초기 안전성과 조합 학습 | 저렴함, 낮은 절대수익, 낮은 유지비, 빠른 회복 |
| 중형 | 주력 효율과 시스템 상호작용 | 높은 효율, 인구·고용·여가 조건 필요, 중간 위험 |
| 대형 | 후반의 큰 선택과 대출 긴장 | 높은 가격·유지비·Workers, 높은 절대수익, 공간 효율 또는 특수 보상 |
| Residential | 노동 공급과 도시 성장 | 직접 생산보다 Capacity 제공, 거주 유지비와 Happiness 영향 |
| Workplace | 주 수입원 | 고용률에 따라 생산 변동, 티어별 노동·공간·유지비 차별 |
| Leisure | Happiness와 성장 유지 | 직접 수익은 낮거나 없지만 인구 유입·프로젝트·고급 운영에 필요 |

### 건물별 분석 표준

각 BuildingDefinition 변경 전후에 다음을 계산한다.

- Cost, Base Production, Maintenance, Population Capacity, Required Workers, Leisure Capacity.
- Footprint, Role, Unlock 조건, Quality 적용 대상.
- 기본/운영비 포함 투자 회수 시간.
- Worker 1명당 생산, 타일당 생산과 City Value.
- 가격당 Population/Leisure Capacity.
- 현금·50% 대출·100% 대출 구매 시 이자 부담, 안정화 시간, 최대 Debt.
- 같은 티어 경쟁 건물 대비 장점과 단점.

모든 효율을 같게 만들지 않는다. 1x1은 공간 효율, 2x2는 높은 절대 성능이나 유지비 절감·특수 가치 등 명확한 이유를 가져야 한다. 각 건물은 최소 한 상황에서 최선이고 최소 한 상황에서 열등해야 한다.

### Quality 검증

현재 x1/x2/x4/x8의 재미는 유지 대상으로 두되 다음을 검증한다.

- 기대 생산 배율과 P50/P90/P95 세션 결과.
- Rainbow 1개가 최종 프로젝트 시간을 몇 분 단축하는지.
- Rainbow 없이도 정상 목표 시간 안에 완주 가능한지.
- 품질 운이 파산을 자동 회피하거나 승리를 확정하는지.
- Residential/Leisure 품질이 플레이어에게 의미 있게 설명되는지.

필요하면 배율, 확률, 적용 대상, 품질별 추가 유지비 또는 품질 보정 비용을 조정한다. 첫 후보는 극단값 완화와 실제 확률 표기이며, Quality 자체를 제거하지 않는다.

## POPULATION / JOBS / LEISURE DESIGN

필수 전략 비교:

| 전략 | 기대 결과 | 실패 판정 |
|---|---|---|
| Residential Heavy | 높은 잠재 인구지만 유지비와 낮은 생산으로 압박 | Jobs 부족이 무의미하거나 주거만으로 엔딩 가능 |
| Workplace Heavy | 높은 명목 생산, 낮은 고용률로 실제 생산 저하 | 노동 부족인데도 수익이 거의 줄지 않음 |
| No Leisure | 초반 가능, 중반부터 인구 유입과 프로젝트가 느려짐 | Leisure를 끝까지 무시해도 불이익 없이 승리 |
| Balanced | 안정 성장과 예측 가능한 완주 | 항상 압도적이라 공격적 변형이 의미 없음 |
| Debt Expansion | 빠른 확장, 관리 가능한 금리·현금 압박 | 무조건 최적이거나 반대로 항상 파산 |

Happiness는 현재 `40 + 60 × LeisureCoverage`이며 인구 유입에만 영향을 준다. 시뮬레이션 후 영향이 약하면 생산에 단순 배율을 더하기보다 프로젝트 조건, 고급 건물 효율, 인구 이탈/유입 속도 중 가장 이해하기 쉬운 한 축을 우선 연결한다.

## FINAL PROJECT DESIGN

최종 프로젝트는 시뮬레이션 결과를 바탕으로 작업자가 직접 설계한다. H0 구조는 다음과 같다.

- 선행 프로젝트 3~5개 완료.
- Population 250~500 범위에서 목표 세션에 맞춘 값.
- Employment Efficiency 85% 이상.
- Happiness 75 이상.
- 일정 City Value와 역할별 핵심 건물 보유.
- 최근 30~60초 OperatingCapacity 양수.
- 큰 건설 비용을 현금 또는 대출로 지불 가능.
- Debt는 허용. 다만 완공 직후 확정 파산이 되지 않을 운영 여력이 필요.

최종 프로젝트 비용은 “마지막 5~7분 동안 여러 시스템을 정리한 뒤 감당 가능한 큰 선택”이 되도록 역산한다. 단순히 돈을 모아 버튼을 누르는 대신 조건 충족 상태를 유지하며 건설을 완료하게 한다.

## BALANCE SIMULATION PLAN

### 도구 구조

구현 단계에서 Editor/Development 전용 `BalanceSimulator` 또는 `EconomySimulationTest`를 만든다. 시뮬레이터용 공식을 별도로 복제하지 않는다.

권장 구조:

```text
Runtime rules/services (순수 계산 및 상태 전이)
  ├─ 실제 EconomyManager / PopulationManager가 호출
  └─ BalanceSimulator가 같은 규칙을 호출

Strategy input
  → buy/build/borrow/repay/upgrade/remove/project actions
  → accelerated tick loop
  → snapshot/event log
  → CSV summary
```

현재 MonoBehaviour와 씬 참조에 묶인 계산은 최소한의 규칙 계층으로 추출하되, 실제 게임과 시뮬레이터가 같은 코드를 사용하도록 한다. 시뮬레이션은 Unity EditMode 또는 전용 테스트 씬에서 결정론적 seed를 지원한다.

### 기록 데이터

```text
runId, seed, strategy, elapsedSeconds, phase,
cash, debt, debtDelta, grossProduction, maintenance, interest, operatingNet,
interestRate, riskState, bankruptcyCountdown, debtPaybackSeconds,
population, capacity, jobs, employed, employmentEfficiency,
leisureCapacity, leisureCoverage, happiness,
ownedLand, buildingCount, countByBuildingId, countByQuality,
upgradeLevels, stockValue, activeProject, completedProjects,
action, actionCost, result, bankruptcyOrVictory
```

1초 간격과 주요 이벤트 시점에 기록한다. 각 실행은 Victory/Bankruptcy/Timeout, 완주시간, 최대 Debt, 최저 Net, Insolvent 진입 횟수, 무행동 대기시간, 건물별 구매 수를 요약한다.

### Bot Strategies

| Bot | 규칙 | 기대 |
|---|---|---|
| SAFE_PLAYER | 구매 후 현금 buffer 유지, Debt 최소화 | 느리지만 높은 완주율 |
| AGGRESSIVE_DEBT_PLAYER | 양의 대출 후 ROI만 충족하면 적극 구매 | 더 빠른 평균, 높은 변동성과 일부 파산 |
| RESIDENTIAL_HEAVY_PLAYER | Capacity 우선 | 고용 부족/유지비 문제 노출 |
| WORKPLACE_HEAVY_PLAYER | 생산 건물 우선 | Employment 저하와 Debt 위험 노출 |
| NO_LEISURE_PLAYER | Leisure 회피 | Happiness/성장 손실의 실효성 검증 |
| BALANCED_PLAYER | 역할 비율과 위험도 기준 운영 | 기준 완주 곡선 |
| UPGRADE_HEAVY_PLAYER | 건물보다 효율 업그레이드 우선 | 업그레이드 항상 정답 문제 검증 |
| STOCK_LEVERAGE_PLAYER | 대출 매수와 반복 매매 | 무위험 차익·부채 우회 검증 |
| BAD_PLAYER | ROI 무시, 과도한 대출 | 경고 후 파산하되 너무 즉사하지 않는지 검증 |

### 실행 매트릭스

- 10분, 20분, 30분, 40분 timeout.
- 고정 seed 최소 10개 × 각 Bot.
- 랜덤 seed 최소 100회 × 핵심 Bot.
- Quality 고정 Normal, 확률 적용, 강제 Rainbow 각각 비교.
- 현금, 50% 대출, 100% 대출 구매 비교.
- 자동 상환 ON/OFF 비교.
- 기준 기기 성능과 별개로 시뮬레이션 로직은 가능한 빠르게 실행.

자동화 가능한 범위:

- Economy Tick 순서와 Cash/Debt 보존 법칙.
- 이자 누적, 금리 변화, 위험 상태와 파산 countdown.
- 건물별 ROI, 노동/타일 효율, 유지비 부담.
- Population/Jobs/Happiness/Leisure 집계와 성장.
- Quality 확률 분포와 세션 결과 민감도.
- 업그레이드 비용·효과와 지배 전략 여부.
- Project unlock/requirements/reward/final victory 도달 가능성.
- 주식 대출 악용, 철거 50% 환급 중복 악용.
- 저장 round-trip과 결정론적 replay.

## PLAYTEST PLAN

자동 시뮬레이션으로 명백한 수치 오류와 지배 전략을 제거한 뒤 실제 Play Mode 테스트를 수행한다.

### 사람이 반드시 판단할 항목

- 첫 3분 안에 무엇을 해야 하는지 이해되는가.
- 건물 구매가 기대와 보상감을 주는가.
- 돈이 쌓이는 속도가 답답하거나 무의미하게 빠르지 않은가.
- 대출 버튼이 유혹적이고, 결과는 긴장되지만 이해 가능한가.
- Danger/Insolvent 경고 후 무엇을 해야 회복되는지 알 수 있는가.
- 기다리는 시간이 실제로 지루한가, 계획을 세우는 시간인가.
- Population/Jobs/Happiness 숫자와 건설 선택의 관계가 직관적인가.
- Project 목표와 보상이 다음 행동을 자연스럽게 안내하는가.
- Quality 결과가 즐겁지만 불공정하게 느껴지지 않는가.
- 주식과 업그레이드가 도시 건설을 대체하지 않는가.
- 후반 10분이 반복 구매와 대기만으로 흐르지 않는가.
- Portrait UI에서 한 손 조작, 숫자 가독성, Safe Area, 뒤로가기가 편한가.
- Victory와 Bankruptcy가 한 판의 결과로 충분한 감정적 마침표를 주는가.

### 실제 테스트 순서

1. Designer smoke test: 새 게임→첫 생산→첫 대출→첫 프로젝트.
2. 목표 전략 테스트: Safe, Aggressive, Balanced 각 3회.
3. 실패 전략 테스트: Residential Heavy, Workplace Heavy, Bad Player.
4. Full run: 신규 설치에서 Victory 3회, Bankruptcy 2회, recovery 2회.
5. UX 관찰 테스트: 시스템을 모르는 플레이어에게 설명 없이 제공.
6. Android 실기기: 저사양/중간급, 서로 다른 Portrait 해상도와 노치.
7. 저장/복귀: 홈 이동, 강제 종료, 재실행, 앱 업데이트 상황.
8. 광고 포함 세션: 노출 전후 이탈감, 오입력, 오디오/일시정지, 실패 fallback.

각 플레이테스트는 행동 타임라인, 첫 혼란 지점, 30초 이상 무행동 구간, 구매 후 감정, 실패 원인 이해 여부를 기록한다.

## BALANCE ITERATION AND CHANGELOG

한 번 정한 수치를 완료로 보지 않는다.

```text
IMPLEMENT → SIMULATE → ANALYZE → ADJUST VALUES
→ SIMULATE AGAIN → PLAYTEST → ADJUST → REGRESSION TEST
```

구현 단계에서 프로젝트 루트에 `BALANCE_CHANGELOG.md`를 추가한다. 각 변경은 가능한 한 아래 형식을 따른다.

```text
Date / Test batch / Seed set
Target: Factory

Before:
Cost = 2,000
Production = 60/s
Maintenance = 0/s

After:
Cost = [new]
Production = [new]/s
Maintenance = [new]/s

Reason:
[어떤 파생 지표 또는 플레이 문제가 있었는가]

Observed Result:
[시뮬레이션과 실제 플레이에서 무엇이 변했는가]

Decision:
Keep / Revert / Iterate
```

모든 변경에 Before, After, Reason, Observed Result를 남긴다. 단순 미세조정은 한 묶음으로 기록할 수 있지만 서로 다른 경제 축을 동시에 바꾼 경우 영향을 분리해 재검증한다.

## BALANCE SUCCESS CRITERIA

### 세션

- 첫 정상 완주의 중앙값이 25~35분 범위다.
- 숙련된 합리적 전략은 15~25분 완주가 가능하다.
- 의도적 저축 외 60초 이상 무판단 대기가 반복되지 않는다.
- Early/Mid/Late/End 전환을 구매 가능 항목과 목표 변화로 체감할 수 있다.

### 전략

- SAFE_PLAYER는 높은 완주율을 가지되 평균 시간이 가장 빠르지 않다.
- AGGRESSIVE_DEBT_PLAYER는 평균 완주가 더 빠를 수 있지만 Debt peak와 파산률이 높다.
- BAD_PLAYER는 실제 파산할 수 있고 원인을 UI로 이해할 수 있다.
- 하나의 건물, 업그레이드, 주식 전략이 모든 seed에서 지배하지 않는다.
- Residential, Workplace, Leisure 중 하나를 장기간 무시하면 명확한 trade-off가 생긴다.

### 경제

- 좋은 투자에 쓴 50% 대출은 합리적인 조건에서 성장을 앞당긴다.
- 좋은 투자 100% 대출은 위험하지만 무조건적인 즉사 선택은 아니다.
- 낮은 ROI 자산의 과도한 대출은 Debt 증가와 파산 위험을 만든다.
- Net Income, 실제 Cash 변화, Debt 변화와 HUD 설명이 일치한다.
- Maintenance와 Interest가 모두 의미 있지만 같은 비용을 이중 차감하지 않는다.

### Quality와 진행

- Rainbow 하나만으로 중후반 전체를 건너뛰거나 파산 가능성이 사라지지 않는다.
- Rainbow가 없어도 정상 목표 시간 안에 승리할 수 있다.
- Project 요구사항은 모두 실제 등록 건물로 달성 가능하다.
- 최종 프로젝트는 기존 시스템을 두루 사용하고 Debt 보유 플레이도 배제하지 않는다.

### 안정성과 출시

- 저장/불러오기, 앱 pause/resume, 강제 종료 후 상태가 손실·중복되지 않는다.
- Portrait UI와 Safe Area가 목표 Android 기기에서 정상이다.
- 치명적 Console Error 0, Missing Script 0, 필수 Inspector null 0.
- 서명된 ARM64 AAB가 재현 가능하고 Play Console 내부 테스트를 통과한다.
- Google Mobile Ads 운영 ID, 동의, 개인정보 처리, 실패 fallback이 검증된다.

## UI/UX DESIGN

### 1. 현재 UI 구조 분석

- 런타임 UI는 단일 Screen Space Overlay uGUI `Canvas` 아래 `SafeArea`로 구성된다. CanvasScaler는 1080×1920, Scale With Screen Size 기준이고 EventSystem은 1개다.
- 상단 왼쪽 `경제 정보 텍스트`는 `CashDisplay`와 VerticalLayoutGroup으로 Cash, Production, Debt, Interest, Maintenance, Net, Rate, Risk, City Value, Net Worth를 접기/펼치기한다. Population/Jobs/Happiness는 별도 텍스트 3개로 같은 상단 영역에 배치된다.
- 우측 상단에는 Zoom In/Out과 Camera Reset이 세로 배치된다.
- 하단에는 큰 WORK 버튼, 자동 상환 토글, 스마트폰 열기 버튼이 각각 독립 배치된다. 주요 기능은 스마트폰 홈의 PROJECT/STOCK/UPGRADES/SETTINGS 아이콘과 Repay 10%/All 버튼으로 진입한다.
- 스마트폰 프레임은 1024×1364로 화면 너비의 약 95%, 높이의 약 71%를 차지한다. 앱은 동일 프레임 안의 `PhoneAppPanel`을 교체하는 구조다.
- BUILD는 스마트폰 앱이 아니라 셀 선택 시 열리는 1080×1000 하단 시트다. Residential/Workplace/Other 필터와 ScrollRect 기반 건물 카드가 있다.
- PROJECT는 All/Available/Active/Complete 필터, 프로젝트 ScrollRect, 상세 Bottom Sheet 구조다. 조건별 행 UI와 Accept/Complete 액션은 이미 구현되어 있다.
- STOCK은 가격/보유/PnL, 캔들 차트, 주문 수량, Sell/Buy를 한 화면에 배치한다. UPGRADES는 세로 카드 목록이다.
- 파산 위험은 상단 텍스트와 별도 Bankruptcy/Recovered 텍스트로 표시한다. Victory/Bankruptcy는 공용 `GameOverPanel`이 제목, 최종 Cash/Debt, 최고 생산, 생존 시간, Restart를 표시한다.

분석 캡처는 `Assets/BalanceReports/UIUX/`의 `phone_home_clean.png`, `build.png`, `project_clean.png`, `stock_clean.png`, `upgrades.png`에 저장했다.

### 2. 현재 UI 문제점

#### P0 — 정보 판독과 진행을 방해

- 상단 HUD의 경제 정보와 Population/Jobs/Happiness가 같은 가로 공간을 사용해 Cash/Debt 숫자와 `POP`이 실제 9:16 화면에서 겹친다.
- 밝고 복잡한 월드 위에 흰 텍스트를 직접 표시해 안정적인 대비가 없다. 값의 자릿수가 늘면 충돌이 더 심해진다.
- BUILD 카드 높이와 내부 여백이 과도해 한 화면에 1개 남짓만 읽히며, 이름/아이콘 대비가 낮고 구매 판단 정보가 멀리 흩어진다.
- PROJECT 카드의 회색 배경·회색 텍스트 대비가 부족하고 설명과 `LOCKED` 배지가 겹친다. 현재 무엇이 부족한지 목록 화면에서 알 수 없다.
- 저장된 파산 상태에서 HUD와 종료 패널이 앱 패널보다 위에 남을 수 있다. 종료 상태에서는 다른 조작을 차단하고 결과 화면만 명확히 보여야 한다.

#### P1 — 구조와 우선순위 불일치

- 자주 사용하는 WORK, BUILD, PROJECT는 서로 다른 진입 방식이고 엄지 동선이 분산된다. 스마트폰 홈 아이콘에는 텍스트 라벨이 없어 이미지 학습이 필요하다.
- Repay 10%/All만 일반 흰색 버튼이라 기존 픽셀/다크 네이비 스타일과 일관되지 않는다.
- HUD 확장 상태는 경제 수치를 모두 동일한 위계로 나열한다. Cash·Net·Debt Risk보다 City Value/Net Worth/Rate가 같은 무게로 보인다.
- STOCK은 Fair/City/Fair Bias/Price Impact/Final Change 같은 내부 계산값을 플레이어 화면에 노출한다. 의사결정에 필요한 가격, 보유, 평균가, PnL, 주문 합계보다 시선이 분산된다.
- UPGRADES 카드의 기본 구조는 양호하지만 `LV. 1 / 999`는 실제 진행 감각을 주지 못하고, 설명·효과·가격의 정렬 기준이 카드마다 흔들릴 수 있다.
- Victory와 Bankruptcy가 같은 색 전환/통계 구조를 공유해 감정적 구분이 약하다. Bankruptcy에는 직접적인 원인 설명이 없다.

### 3. 최종 HUD 정보 hierarchy

HUD는 `항상 표시`, `상태에 따라 강조`, `상세 펼침`의 세 층으로 나눈다.

#### 항상 표시

- 1순위 경제: `Cash`와 `Net / sec`를 좌측 상단 단일 다크 네이비 카드에 표시한다. Cash가 가장 크고 Net은 바로 아래 작은 녹색/적색 값으로 둔다.
- 1순위 위험: `Debt`와 `SAFE/RISKY/DANGER/INSOLVENT` 칩을 우측 상단 카드에 둔다. Debt 0에서도 작은 값으로 유지해 위치가 흔들리지 않게 한다.
- 2순위 도시: `Population`, `Jobs/Employment`, `Happiness`를 아이콘+짧은 값의 한 줄 요약으로 두 카드 아래에 배치한다. `POP 84/100 · JOB 91% · HAPPY 72`처럼 축약한다.

#### 상태에 따라 강조

- Net이 음수면 Net만 적색으로 바꾸고 카드 전체를 깜빡이지 않는다.
- RISKY부터 위험 칩의 채도와 테두리를 강화한다. DANGER는 주황, INSOLVENT는 적색을 사용한다.
- Bankruptcy countdown은 상단 카드 아래 전폭 경고 바로 표시한다. `INSOLVENT · RECOVER IN 12s`와 회복 조건을 한 줄로 보여주며 초당 숫자만 갱신한다.
- 새 Debt 발생, 건물 설치, 프로젝트 조건 충족은 1.0~1.5초의 비차단 toast/popup으로 알린다.

#### 상세 펼침에서만 표시

- Production, Maintenance, Interest, Rate, City Value, Net Worth, 자동 상환 상태는 경제 카드 펼침 또는 경제 상세 팝오버에서 제공한다.
- 상세식은 `Production 120 - Maintenance 25 - Interest 10 = Net +85/s`처럼 한 줄 산식으로 묶어 이중 차감 오해를 막는다.

### 4. Portrait 화면 layout

1080×1920 기준 레이아웃을 다음 세 구역으로 고정한다. 모든 수치는 Safe Area 내부 기준이며 실제 기기 inset에 따라 바깥 여백이 늘어난다.

```text
┌──────────────────────────────┐
│ Cash / Net       Debt / Risk │  Top HUD 180~220
│ POP · JOB · HAPPY             │
├──────────────────────────────┤
│                              │
│          GAME WORLD          │  최대 가시 영역
│                              │
│ Context toast / risk bar     │
├──────────────────────────────┤
│ BUILD PROJECT [WORK] UP STOCK│  Bottom Dock 128~152
└──────────────────────────────┘
              Ad safe region
```

- HUD 기본 점유는 전체 높이의 12% 이하, 하단 Dock은 광고 제외 가용 높이의 9% 이하로 유지한다.
- 기본 플레이에서는 월드 세로 가시 영역 70% 이상을 목표로 한다.
- BUILD는 42~48% 높이의 하단 시트로 시작하고 드래그/확장 시 최대 72%까지 사용한다.
- PROJECT/UPGRADES/STOCK은 읽기 작업이므로 현재 스마트폰 프레임을 재사용하되 화면 너비 92%, 광고 위 가용 높이 68% 이내로 정리한다. 패널이 열리면 배경 월드를 어둡게 15~25% 처리해 정보 대비를 확보한다.
- 종료 화면은 Safe Area 전체 modal로 처리하고 다른 HUD·Dock·스마트폰 입력을 비활성화한다.

### 5. 주요 버튼 navigation

- 하단 고정 Dock 순서는 `BUILD · PROJECT · WORK · UPGRADES · STOCK`으로 한다.
- WORK는 반복 빈도가 가장 높으므로 중앙에 112~128px 원형/팔각 픽셀 버튼으로 두고 나머지 4개보다 15~20% 크게 한다.
- BUILD/PROJECT는 진행 핵심이므로 WORK 양옆에 둔다. UPGRADES/STOCK은 바깥쪽에 둔다.
- 각 버튼은 아이콘과 1단어 라벨을 함께 표시한다. 현재 스마트폰 홈은 보조 홈/설정 화면으로 축소하고 핵심 액션의 유일한 진입점으로 사용하지 않는다.
- 자동 상환과 Repay 10%/All은 Debt 카드 또는 경제 상세 안으로 이동한다. 설정은 우측 상단 또는 스마트폰 보조 메뉴에 유지한다.
- 한 번에 하나의 패널만 열고, Android Back은 상세→앱/시트→기본 월드 순으로 닫는다.

### 6. BUILD UI 구조

- 필터는 `Residential · Workplace · Leisure · Road` 4개로 확정한다. 현재 `Other`는 Leisure와 Road를 분리한다.
- 기본 시트 헤더: 선택 셀 좌표/상태 대신 `BUILD` 제목, 보유 Cash, 닫기 버튼을 표시한다.
- 건물은 세로 ScrollRect 안의 1열 compact card로 배치해 1080px 화면에서 최소 3개 카드의 제목과 가격이 동시에 보이게 한다.
- 카드 구조: 좌측 96~112px 아이콘, 중앙 이름+역할 핵심 수치 1~2줄, 우측 가격/BUILD 버튼. 잠김/구매 불가는 카드 전체를 흐리기보다 버튼에 부족 금액 또는 조건을 표시한다.
- Residential 우선 정보: 가격, Population Capacity, Maintenance, 크기.
- Workplace 우선 정보: 가격, Production, Required Workers, Maintenance, 크기.
- Leisure 우선 정보: 가격, Leisure Capacity, Maintenance, 크기.
- Road 우선 정보: 가격/자동 연결 여부, 크기. 생산 0 같은 무의미한 값은 숨긴다.
- 구매 직전 요약은 `Cash -500`, 대출 발생 시 `Debt +300 · Interest +0.05/s`, 설치 결과는 `POP +10 · Maintenance +2/s`처럼 역할별 최대 3개 변화만 보여준다.

### 7. PROJECT UI 구조

- 앱 첫 화면 상단에 필터보다 `CURRENT PROJECT` 요약 카드를 먼저 둔다. Active가 없으면 다음 Available 프로젝트를 추천한다.
- 요약 카드에는 제목, 단계 `2/5`, 가장 부족한 조건 1개, 비용, 주 액션을 표시한다.
- 전체 목록은 Active→Available→Locked→Completed 우선순위로 정렬하고, 필터는 필요한 경우 두 번째 줄의 작은 segmented control로 유지한다.
- 상세 화면 조건은 `항목 / 현재 / 목표 / 상태` 행으로 고정한다. 예: `Population 84 / 100`, `Employment 91% / 85% ✓`.
- 충족 조건은 녹색 체크와 낮은 강조, 부족 조건은 흰색 숫자+주황 부족량으로 표시한다. 색상만으로 상태를 구분하지 않는다.
- 비용과 보상은 조건 목록 아래 별도 두 칸으로 분리한다. `COST $20K`, `REWARD City Value +5K`를 같은 줄에서 비교한다.
- Locked 프로젝트는 긴 설명 대신 선행 프로젝트 이름과 단계만 보여준다. 다음 단계는 완료 후 toast와 목록 자동 포커스로 안내한다.

### 8. 경제/대출/파산 feedback

- 건물 설치: 월드 셀 위 `+$30/s`, HUD 근처 `Maintenance +$5/s`, 필요 시 `Jobs +10`을 1~2개 popup으로 묶는다. 같은 순간 3개를 넘기지 않는다.
- 대출 구매: 화면 전체 flash 대신 Debt 카드 pulse 1회와 `Borrowed $500 · Debt $2.1K` toast를 사용한다. 예상 Interest 증가는 실제 의미가 있을 때만 두 번째 줄에 표시한다.
- 상환: `Debt -$500`, `Interest -$0.08/s`를 짧게 표시하고 Debt 카드가 원래 색으로 부드럽게 복귀한다.
- SAFE: 청록/녹색 작은 칩. RISKY: 황색. DANGER: 주황 테두리와 짧은 진동 1회. INSOLVENT: 적색 경고 바와 countdown. 지속 깜빡임은 사용하지 않는다.
- 회복 시 `RECOVERED · Net +$12/s`를 녹색 toast로 표시하고 countdown을 즉시 제거한다.

### 9. Victory/Bankruptcy 구조

- 공통: 전체 modal, 명확한 제목, 3~4개 핵심 통계, 큰 Restart 버튼, 배경 입력 차단. 기존 GameOverPanel과 픽셀 프레임을 재사용한다.
- Victory: 청록/금색 제목, `CITY COMPLETE`, Completion Time, Final Net Worth, Peak Population, Max Debt 또는 Completed Projects를 표시한다. Debt가 있어도 승리라는 설계와 모순되지 않게 평가 문구로 비난하지 않는다.
- Bankruptcy: 적색/주황 제목, `BANKRUPT`, 직접 원인 한 줄(`Debt exceeded capacity while Net Income was negative`), Final Cash, Debt, Net/sec, Interest/sec, Survival Time을 표시한다.
- 현재 공용 4개 통계 바인딩을 결과 타입별 통계 모델로 확장한다. Victory와 Bankruptcy가 같은 색 애니메이션을 공유하지 않게 한다.
- 저장된 종료 상태로 재진입할 때 modal이 최상위이며 HUD 위험 텍스트나 앱 패널이 그 위에 중복 표시되지 않게 한다.

### 10. 모바일 터치 및 Safe Area 기준

- 모든 주요 터치 영역은 현재 기준인 최소 88×88 reference px를 유지한다. Dock의 WORK는 112px 이상, 닫기/뒤로가기는 최소 88px로 한다.
- 인접 버튼 사이 시각 간격은 최소 16px, 오동작 위험이 큰 Buy/Sell·Restart는 24px 이상 둔다.
- 비상호작용 Image/TMP의 Raycast Target은 끄고, ScrollRect의 Viewport/Content/Layout 조합은 기존 prefab 구조에서 정리한다.
- 패널은 anchor/stretch와 LayoutGroup을 우선하며 1080×1920 절대 좌표 의존을 줄인다. 같은 축을 LayoutGroup과 수동 RectTransform이 동시에 제어하지 않게 한다.
- 노치, 둥근 모서리, Android navigation inset, adaptive banner 영역을 각각 Safe Area와 광고 예약 영역으로 분리한다.
- 검증 해상도는 최소 720×1280, 1080×1920, 1440×3200 및 좌우/상하 inset 시뮬레이션을 포함한다.
- Play Mode 합격 기준: 텍스트 겹침 0, 잘린 숫자 0, Missing reference 0, 주요 버튼 88px 이상, 기본 월드 가시 영역 70% 이상, BUILD 초기 시트에서 카드 3개 이상 제목/가격 식별 가능.

### UI 구현 순서

1. **Stage 6A-1 — HUD/Dock 기반:** 상단 카드 2개+도시 요약 행, 위험 바, 하단 5버튼 Dock, 기존 액션 연결. 가장 큰 겹침과 엄지 동선을 먼저 해결한다.
2. **Stage 6A-2 — BUILD/PROJECT:** 기존 CellInfoPopup·ProjectPanelUI·ScrollRect prefab을 재배치하고 역할별 카드/진행 행을 정리한다.
3. **Stage 6A-3 — UPGRADES/STOCK/경제 피드백:** 카드 정렬, 플레이어용 지표만 노출, 대출·설치·상환 toast를 통합한다.
4. **Stage 6A-4 — 종료/위험 상태:** Victory/Bankruptcy별 통계와 원인, modal 입력 차단, 복귀 상태를 수정한다.
5. **Stage 6A-5 — Portrait 회귀:** 3개 해상도와 Safe Area, 광고 포함/미포함, 월드 점유율, Android Back, 터치 크기, 실제 기기 한 손 테스트를 수행한다.

기존 Canvas, SafeArea, PhoneAppPanel, CellInfoPopup, ScrollRect, Project/Upgrade item prefab, GameOverPanel 스크립트를 우선 재사용한다. 전면 스타일 교체나 런타임 임시 UI 생성을 기본 해법으로 사용하지 않는다.

## STAGE EXECUTION STATUS

| Stage | 상태 | 완료일 | 결과 |
|---|---|---|---|
| Stage 0 — Design Baseline | **COMPLETE** | 2026-10-03 | 현재 데이터 추출, H0 건물/경제/세션/프로젝트 설계, 계산 검증 완료 |
| Stage 1 — Implement Core Completeness | **COMPLETE** | 2026-10-03 | 5단계 프로젝트·Victory·로컬 저장·50% 철거 환급·런타임 안정화 완료 |
| Stage 2 — Implement Shared Balance Rules | **COMPLETE** | 2026-10-03 | 공유 순수 규칙, 결정론 시뮬레이터, 100회 회귀·CSV 기반 완료 |
| Stage 3 — Simulate and First Balance Pass | **COMPLETE** | 2026-10-03 | H0 에셋 반영, 현금/50%/100% 전략 300회 완주 및 1차 곡선 확정 |
| Stage 4 — Playtest and Second Balance Pass | **COMPLETE** | 2026-10-03 | 관찰 지표·압박 회귀 추가, 프로젝트 곡선과 대출 전략 2차 조정 완료 |
| Stage 5 — Mobile UX, Ads, Performance | **IN PROGRESS** | - | 구현·에디터 검증 완료, 운영 배너 ID와 Android 실기기 검증 대기 |
| Stage 6 — Polish and Regression | **COMPLETE (AUTOMATED SCOPE)** | 2026-10-04 | 저장 확장·placeholder 정리·승패/회복 회귀·300회 시뮬레이션·씬 무결성 완료 |
| Stage 6A — Mobile UI/UX Pass | **DESIGN COMPLETE** | 2026-10-04 | 9:16 실화면·Canvas·HUD·BUILD/PROJECT/UPGRADES/STOCK·종료 화면 분석 및 구현 규격 확정 |
| Stage 7 — Release | NOT STARTED | - | Stage 5 외부 게이트와 신규 사용자 관찰 완료 후 진행 |

Stage 0 산출물은 프로젝트 루트의 `BALANCE_BASELINE.xlsx`다. 이 워크북은 다음 시트를 포함한다.

- `Current Buildings`: 14개 BuildingDefinition 원본값과 수식 기반 파생 지표.
- `H0 Buildings`: 모든 현재 건물에 대응하는 14개 초기 재설계안. Unity 에셋에는 아직 적용하지 않음.
- `Session Curve`: Early/Mid/Late/End의 25~35분 목표 범위.
- `Project Chain`: First District부터 Metropolitan Landmark까지 5단계 H0.
- `Economy H0`: 금리·Quality·세션·철거 환급 가설.
- `Checks`: 원본/H0 개수, 확률 합, 기대 배율, 초기 조합 회수시간, 프로젝트 순서, 이름 중복 검증.

Stage 0 H0 핵심값:

- 초기 자립 조합: Small House 500 + Workshop 1,500, 완전고용 운영 순수익 23/s, 회수시간 약 86.96초.
- Workplace H0는 Early 30/s, Mid 75~180/s, Late 420~750/s로 역할을 분리했다.
- Residential은 Small House 10명과 Apartment Tower 80명으로 분리했다.
- Leisure는 Pocket Park 35, School 70, Culture Center 100, Hospital 120, Stadium 320으로 단계화했다.
- 모든 비도로 건물에 Maintenance를 배정했다.
- Quality H0는 Normal/Silver/Gold/Rainbow 확률 70/20/8/2%, 생산 배율 1/1.35/1.8/2.5로 두고 기대 배율을 1.164로 낮췄다.
- 금리 H0는 시작 0.015%/s, 최소 0.005%/s, 최대 0.3%/s다. 실제 에셋 반영 전 대출 시뮬레이션이 필요하다.
- 최종 프로젝트 H0는 선행 프로젝트 4개, Population 350, Employment 85% 이상, Happiness 80 이상, 60초간 양의 OperatingCapacity, 비용 80,000이다. Debt 보유는 허용한다.

Stage 0 검증 결과:

- 현재/H0 Building 행 수: 14/14 일치.
- Quality 확률 합: 부동소수점 허용오차 내 1.0.
- Quality 기대 생산 배율: 1.164.
- 초기 Small House + Workshop 회수시간: 86.9565초로 H0 가드레일 60~120초 충족.
- Project 순서: 1~5 연속, 최종 프로젝트 포함.
- H0 표시명 중복: 0건.
- 워크북 수식 오류 검색: 0건.
- Unity Editor 연결: ready. 현재 compilation failure 0, Console Error 0. 기존 Console Warning 2건은 Stage 1 수정 대상이다.
- 게임 코드·씬·ScriptableObject 변경: 없음.

현재 작업 트리의 사용자 변경을 임의로 커밋하거나 묶지 않았다. `BALANCE_BASELINE.xlsx`가 수치 기준선을 고정하며, Stage 1 시작 시 실제 파일 수정 전에 별도 안전 체크포인트를 만든다.

## DEVELOPMENT ROADMAP

### Stage 0 — DESIGN BASELINE — COMPLETE

목표: 구현 전에 현재 상태와 목표 곡선을 수치화한다.

- 완료: 현재 Building/Upgrade/Project/Economy 기준과 테스트 위험을 문서화.
- 완료: BuildingDefinition 14개를 기계 판독 가능한 워크북으로 추출.
- 완료: H0 세션 곡선, 역할별 건물 설계, 5단계 프로젝트 체인 작성.
- 완료: 수식 기반 파생 지표와 일관성 검사 시트 확정.
- 완료 판정: 모든 현재 값과 계산식의 출처를 추적하고 첫 밸런스 가설과 검증 결과를 기록함.

### Stage 1 — IMPLEMENT CORE COMPLETENESS — COMPLETE

목표: 처음부터 Victory/Bankruptcy까지 기능적으로 연결한다.

- 완료: 유효한 `DevelopmentProjectManager`를 런타임 단일 인스턴스로 선택하고 DB 없는 중복 인스턴스를 비활성화.
- 완료: 달성 불가능한 단일 프로젝트 대신 First District→Metropolitan Landmark 5단계 fallback 체인 구현.
- 완료: 최종 프로젝트에 선행 4개, Population 350, Employment 85%, Happiness 80, 양의 Net 60초, 비용 80,000 조건 연결.
- 완료: 최종 프로젝트 완료 시 경제 틱 종료, Victory 이벤트, 결과 통계, `CITY COMPLETE` 화면과 재시작 연결.
- 완료: SaveData v1에 Economy, Population, Upgrade, Project, 소유 토지, 건물 품질, Reveal 상태 저장.
- 완료: 15초 autosave, pause/quit 저장, 임시 파일 교체, backup, 손상 파일 격리, 자동 이어하기, 재시작 시 새 게임 처리.
- 완료: 플레이어 건물과 도로 철거 성공 후 구매가의 50%를 Cash로 환급. 자동 생성 도로는 환급하지 않음.
- 완료: 기존 CashDisplay의 Maintenance/Net/Debt/Interest/Risk 실시간 HUD 연결을 검증.
- 완료: Sound/Ad 영속 오브젝트를 root로 분리해 `DontDestroyOnLoad` 경고 제거.
- DoD 결과: 플레이 모드에서 5개 프로젝트와 최종 1개 생성, Victory 이벤트/종료 상태, 저장 왕복, 재컴파일과 Console 무오류를 확인함.

Stage 1 검증 결과:

- Unity Editor `recompile`: compilation error 0, warning 0.
- 별도 C# 어셈블리 빌드: error 0. 남은 obsolete API warning 3건은 기존 `FindObjectOfType` 호출이며 Stage 1 기능과 무관.
- PlayMode 프레임 진행: auto-tick 환경에서 102ms 내 frameCount 변화 확인.
- Runtime 검사: Project 5개, Final Project 1개, SaveGameManager 1개 연결.
- Save/Load 왕복: 성공, Economy/Project 상태 유지, Console error/warning 0.
- Victory smoke test: Final accept=true, complete=true, Victory event=true, game-ended=true.
- 테스트 종료 후 PlayMode를 정상 종료했으며 씬 에셋에는 테스트용 조건 완화가 저장되지 않음.

### Stage 2 — IMPLEMENT SHARED BALANCE RULES — COMPLETE

목표: Runtime과 테스트가 같은 계산식을 사용하게 한다.

- 완료: `EconomyRules`, `PopulationRules`, `ProjectRules` 순수 계산 계층을 추가하고 Runtime Manager가 동일 계산을 사용하도록 연결.
- 완료: 정수 seed 기반 xorshift RNG와 1초 가속 틱을 사용하는 `BalanceSimulator` 구축.
- 완료: Safe/Balanced/Aggressive Bot strategy와 Victory/Bankruptcy/Timeout 결과 분류 구현.
- 완료: 10초 간격 snapshot CSV와 session summary CSV 출력 구현.
- 완료: deterministic seed, 이자·순수익·철거 환급·고용·행복, 결과 회계, 음수 상태, 시간 범위를 검사하는 자동 회귀 suite 구현.
- 완료: `BALANCE_CHANGELOG.md` 생성 및 Stage 2 기준 기록.
- DoD 결과: 실제 Runtime과 공유하는 규칙으로 100회 세션을 실행하고 209개 assertion 및 CSV 비교 기반을 통과함.

Stage 2 기준 실행 결과:

- 총 100회: Victory 33, Bankruptcy 0, Timeout 67.
- 전략은 seed별로 Safe/Balanced/Aggressive를 순환 배정.
- 동일 seed/strategy 재실행 결과 일치.
- `balance_summary.csv`: header 포함 101행.
- `balance_snapshots.csv`: header 포함 16,645행.
- 결과 해석: Stage 2는 측정 기반 구축 단계다. Safe/Balanced의 높은 Timeout과 Bankruptcy 0건은 Stage 3에서 수치·전략 매트릭스로 교정할 첫 기준선이다.

### Stage 3 — SIMULATE AND FIRST BALANCE PASS — COMPLETE

목표: 현재의 극단적 수치 문제를 제거하고 30분 곡선의 첫 버전을 만든다.

- 완료: 시작 Cash, 금리 범위와 압력 계수, Population 정지 임계값을 H0 곡선에 맞게 조정.
- 완료: 저장 호환 ID를 유지하며 13개 비도로 건물의 이름·역할·비용·생산·Maintenance·Capacity/Workers/Leisure·City Value를 차등화.
- 완료: Quality를 70/20/8/2%와 1/1.35/1.8/2.5배로 조정해 기대 배율 1.164 적용.
- 완료: 현금 중심 Safe, 50% 조달 Balanced, 100% 조달 Aggressive 전략 매트릭스 실행.
- 완료: 초기 주거→일자리→여가 순서와 행동 간격을 교정해 모든 전략의 확정 Timeout 제거.
- DoD 결과: 각 전략 100회, 총 300회 모두 Victory로 완주하며 서로 다른 시간·최대 Debt 프로필을 재현함.

Stage 3 최종 300회 결과:

- Safe: Victory 100/100, 평균 24.65분, 평균 최대 Debt 66.83.
- Balanced: Victory 100/100, 평균 18.72분, 평균 최대 Debt 82,984.75.
- Aggressive: Victory 100/100, 평균 15.47분, 평균 최대 Debt 85,605.01.
- 회귀 assertion: 609개 통과, Unity compile error/warning 0.
- 남은 관찰점: 자동 Bot에서는 Bankruptcy가 발생하지 않았다. Stage 4에서 실제 플레이의 실수·과잉 확장·회복 경로를 관찰하고 파산 압력과 목표 시간을 2차 조정한다.

### Stage 4 — PLAYTEST AND SECOND BALANCE PASS — COMPLETE

목표: 수치상 정상인 게임이 실제로 재미있고 이해 가능한지 확인한다.

- 완료: Designer proxy full run용 결정 횟수, 최대 무행동 시간, 프로젝트별 완료 시점을 시뮬레이터 결과에 추가.
- 완료: 행동 간격을 Safe 28초, Balanced 26초, Aggressive 24초로 조정해 전략별 판단 빈도를 분리.
- 완료: 최종 프로젝트 비용을 80,000에서 100,000으로 올려 후반 목표의 무게와 Balanced/Aggressive 간 선택 비용을 강화.
- 완료: 고금리·낮은 파산 한도의 과잉 확장 회귀 케이스를 추가하고 실제 Bankruptcy 도달을 검증.
- 완료: Play Mode 화면에서 초기 HUD와 월드 진행을 확인하고 Console compile error/warning 0을 유지.
- DoD 결과: 300회 모두 18.88~24.65분에 Victory, 평균 결정 39~45회, 최대 무행동 75~87초, 전략별 Debt 격차와 압박 케이스 Bankruptcy를 재현함.

Stage 4 최종 300회 결과:

- Safe: Victory 100/100, 평균 24.65분, 평균 최대 Debt 71, 평균 결정 45회, 최대 무행동 87초, 첫 프로젝트 평균 261초.
- Balanced: Victory 100/100, 평균 18.88분, 평균 최대 Debt 77,170.78, 평균 결정 39회, 최대 무행동 81초, 첫 프로젝트 평균 28초.
- Aggressive: Victory 100/100, 평균 19.05분, 평균 최대 Debt 102,776.57, 평균 결정 42회, 최대 무행동 75초, 첫 프로젝트 평균 26초.
- 회귀 assertion: 910개 통과. 금리 1%/s·파산 한도 50,000의 Aggressive shock case는 Bankruptcy 도달.
- 해석: Aggressive가 Balanced보다 빠르면서 위험 차이가 작았던 Stage 3 우세를 제거했다. 이제 Aggressive는 더 많은 Debt를 감수하지만 평균 완료는 Balanced보다 소폭 느리다.
- Play Mode 관찰: Cash/Debt/Population/Jobs/Happiness 표시는 정상이나 가로 화면과 테스트 광고 배너가 남아 있다. Portrait/Safe Area와 광고 운영 UI는 Stage 5의 명시적 차단 항목이다.
- 실제 신규 사용자 관찰은 자동화로 대체할 수 없으므로 출시 전 Stage 6 회귀에서 최소 1회 별도 수행한다.
- 산출물: `BalanceReports/Stage4/balance_summary.csv`, `BalanceReports/Stage4/balance_snapshots.csv`, `Assets/BalanceReports/Stage4/playtest.png`.

### Stage 5 — MOBILE UX, ADS, PERFORMANCE

목표: Portrait Android 환경에서 출시 가능한 체감 품질을 만든다.

- 완료: Android 방향을 Portrait로 고정하고 Landscape/거꾸로 Portrait 회전 및 Safe Area 밖 렌더링을 비활성화.
- 완료: 메인 uGUI Canvas를 Scale With Screen Size, 1080×1920, Match 0.5로 변경하고 전체 HUD를 `SafeArea` 아래로 이동.
- 완료: Safe Area 변화 추적, 88px 미만 Selectable의 raycast 영역 확장, 60 FPS/vSync 0 모바일 런타임 정책 추가.
- 완료: 앱 패널→휴대폰→앱 종료 순서의 Android 뒤로가기 처리와 테스트 가능한 공개 경로 추가.
- 완료: 100만 이상 Cash/Debt/City Value/Net Worth를 M/B/T 축약 표기로 전환.
- 완료: Google UMP 동의 결과가 광고 요청을 허용한 뒤에만 Mobile Ads를 초기화하도록 변경.
- 완료: 광고 로드 실패 시 최대 2회 지연 재시도, Release 운영 ID 누락 시 광고 비활성 fallback, 설정 앱의 Privacy Options 진입 추가.
- 완료: Map/입력/선택 debug 로그를 씬에서 비활성화하고 60 FPS Play Mode, 45개 버튼 및 17개 확장 터치 영역을 확인.
- 완료: Android AAB 대상, 빌드 씬, 출력 경로 dry-run 검증 통과.
- 대기: 운영 Android Banner Ad Unit ID가 비어 있으므로 사용자 제공 전에는 Release 광고를 요청하지 않음.
- 대기: 연결된 ADB 기기가 없어 저사양/중간급 Android 실기기 30분 세션과 발열·메모리 검증을 수행하지 못함.
- 현재 판정: 구현 및 Editor DoD는 통과했으나 운영 광고 ID와 실기기 DoD가 남아 Stage 5를 `IN PROGRESS`로 유지.
- 산출물: `BalanceReports/Stage5/STAGE5_REPORT.md`, `Assets/BalanceReports/Stage5/mobile_portrait_runtime.png`.

### Stage 6 — POLISH AND REGRESSION — COMPLETE (AUTOMATED SCOPE)

목표: 콘텐츠를 잠그고 전체 회귀를 통과한다.

- 완료: Economy와 주식 가격·보유량·평균 매수가를 포함한 저장 직렬화 왕복 3회 통과.
- 완료: 실제 경제 틱으로 파산, 파산 카운트다운 회복, 최종 프로젝트 Victory 이벤트와 `CITY COMPLETE` 결과 화면 검증.
- 완료: GameOverPanel의 초기화 순서 의존 Victory 구독 결함 수정.
- 완료: 300회 Bot/seed 회귀에서 Safe/Balanced/Aggressive 각 100회 Victory와 910 assertion 재현.
- 완료: 테스트 캔들 기본 비활성화, `test_map`→`city_map`, 숫자 placeholder 설명과 과도한 런타임 로그 정리.
- 완료: DB 없는 중복 DevelopmentProjectManager 제거, Missing Script 0, EventSystem 1, 필수 MapManager 참조 null 0 확인.
- 완료: Unity compile 및 최종 Console error/warning 0.
- 제한: Project Auditor 패키지가 설치되어 있지 않아 해당 스캔은 unavailable이며 패키지를 임의 설치하지 않음.
- 외부 게이트: 실제 신규 사용자 관찰, 운영 광고 ID, Android 실기기 30분 세션은 자동화로 대체하지 않고 출시 전 필수로 유지.
- 산출물: `BalanceReports/Stage6/STAGE6_REPORT.md`, `BalanceReports/Stage6/balance_summary.csv`, `BalanceReports/Stage6/balance_snapshots.csv`.

### Stage 7 — RELEASE

목표: Google Play 내부 테스트에서 검증된 Release Candidate를 출시한다.

- Package name, version code, keystore, ARM64, target API, AAB 확정.
- 개인정보처리방침, 데이터 보안, 콘텐츠 등급, 스토어 등록정보 정리.
- 내부 테스트 설치본으로 전체 한 판과 저장·광고·복귀 검증.
- DoD: 서명된 AAB 재현, Play Console 사전 검사 통과, 최종 체크리스트 완료.

## CURRENT PRIORITIES

### P0 — SHIP BLOCKER

1. 기준선 백업/커밋과 현재 미커밋 파일 의도 확인.
2. 중복 `DevelopmentProjectManager`, 필수 null, Sound/Ad `DontDestroyOnLoad` 경고 해결.
3. 불가능한 프로젝트 수정, 프로젝트 체인·최종 프로젝트·Victory 구현.
4. 전체 진행의 로컬 저장/불러오기와 Android 생명주기 복구.
5. Runtime과 Simulator가 공유할 경제 규칙 구조 확보.
6. HUD Maintenance/Net/Debt 변화와 핵심 온보딩.
7. 현재 초당 금리와 Maintenance 공백을 첫 시뮬레이션 가능한 값으로 재설계.
8. Portrait UI, Safe Area, 터치, 뒤로가기.
9. Google Mobile Ads 운영 구성과 개인정보·동의 처리.
10. Android release AAB와 실제 기기 전체 세션 검증.

### P1 — BALANCE AND QUALITY

1. placeholder 건물 ID/이름/설명/역할을 의미 있는 콘텐츠로 정리.
2. 소형/중형/대형 역할별 비용·생산·유지비·Workers·Capacity·Value 재설계.
3. Unlock 순서와 프로젝트 보상 설계.
4. Quality 확률/배율/적용 대상 검증.
5. City Efficiency/Work Power 상한과 비용 곡선 조정.
6. 주식 대출 악용과 자동 상환 상호작용 조정.
7. 철거 50% 환급 악용 방지.
8. 도로 16 mask, Chunk 경계, 제거 도로 저장 검증.
9. Balance Simulator, Bot, CSV, 회귀 테스트 확장.

### P2 — OPTIONAL POLISH

- 플레이를 방해하지 않는 그래픽 미세 수정과 추가 피드백.
- 사운드 다양화와 세부 믹싱.
- 추가 카메라/엔딩 연출.
- 필수 역할이 아닌 신규 건물.
- 추가 통계·업적·리더보드.

## OUT OF SCOPE

- 섬/매립 중심으로 월드 전면 변경.
- 새로운 월드 생성 구조.
- 4차선 도로와 복잡한 교통/차량/보행자 시뮬레이션.
- 시민 개별 AI.
- 턴제 전환.
- 대규모 세금 시스템.
- 복잡한 랜덤 이벤트 시스템.
- 그래픽 스타일 전면 교체.
- 출시 필수와 무관한 로그인/IAP/대형 온라인 기능.

기존 잔디·나무·도로·건물 아트는 유지한다. 작은 디자인 변경은 기존 시스템 연결과 한 판의 재미를 명백히 개선할 때 작업자가 제안하고 계획에 포함할 수 있다.

## AUTONOMOUS DECISION POLICY

### 작업자가 스스로 결정하고 검증할 사항

- 한 판 목표시간의 세부 조정과 Early/Mid/Late/End 구간.
- 모든 BuildingDefinition의 비용, 생산, 유지비, Capacity, Workers, Leisure, Value, Unlock.
- Quality 확률·배율·적용 대상.
- 금리 곡선, Debt 압력, 위험 threshold, 카운트다운 시간.
- Population 성장, Happiness 영향과 Leisure 효율.
- Upgrade 비용, 효과, 상한.
- Project 체인, 비용, 요구조건, 보상.
- 최종 프로젝트의 테마·조건·비용과 Victory 수치.
- 저장 UX의 안전한 기본안, 온보딩 순서, HUD 정보 우선순위.
- 광고의 보수적인 초기 위치·빈도와 테스트 가설.
- 자동 시뮬레이션 전략, seed, 성공 기준과 밸런스 수정.
- 작은 범위의 디자인 보완과 필요한 코드 구조 개선.

각 결정은 문서/Changelog에 근거와 관찰 결과를 남기며 회귀 가능해야 한다.

### 정말로 사용자 결정이 필요한 사항

현재 개발을 시작하기 위해 추가로 필요한 게임 밸런스 결정은 없다. 다음처럼 프로젝트 방향이나 외부 책임이 바뀌는 경우에만 사용자 판단을 요청한다.

- 핵심 타이쿤 정체성, 목표 플랫폼, Portrait, 광고 포함 등 이미 확정된 제품 방향을 뒤집는 변경.
- 유료 결제, 계정, 온라인 서비스처럼 비용·법무·운영 책임이 생기는 신규 범위.
- 기존 아트를 폐기하는 전면적 비주얼 방향 변경 또는 유료 에셋 구매.
- 앱 이름·브랜드·스토어 설명의 최종 승인, 개인정보처리방침 주체, keystore/Play Console 자격 증명.
- 출시 일정, 마케팅, 광고 수익성과 사용자 경험 사이의 최종 사업 우선순위.

## DEFINITION OF DONE

- 신규 플레이어가 설명 없이 핵심 루프를 시작하고 목표를 이해한다.
- 도시 건설, 대출 활용, 위기, 회복 또는 파산, 성장, 최종 프로젝트, Victory가 한 판으로 연결된다.
- 목표 플레이시간과 판단 빈도가 `BALANCE SUCCESS CRITERIA`를 충족한다.
- Cash/Debt/Interest/Maintenance/자동 상환과 HUD가 같은 규칙을 설명한다.
- Population/Jobs/Employment/Happiness/Leisure가 실제 구매 선택에 영향을 준다.
- 건물별 역할이 구분되고 하나의 고정 빌드가 항상 정답이 아니다.
- Quality는 기대감을 주지만 한 번의 Rainbow로 게임 전체가 무력화되지 않는다.
- 건물 철거 시 비용의 50%가 정확히 한 번 환급된다.
- Project와 최종 프로젝트의 모든 요구사항이 달성 가능하다.
- 좋은 대출은 성장을 앞당기고 나쁜 대출은 회복 가능한 위기 또는 파산을 만든다.
- 자동 시뮬레이션 결과와 Runtime의 같은 입력이 같은 경제 결과를 낸다.
- 모든 주요 밸런스 변경에 Before/After/Reason/Observed Result가 기록된다.
- 로컬 저장, pause/resume, 강제 종료 후 상태가 손실되거나 중복되지 않는다.
- Victory/Bankruptcy 화면, 통계, 입력 종료와 다시 플레이가 정상이다.
- Portrait UI, Safe Area, 터치와 Android 성능이 목표 기기에서 정상이다.
- 컴파일 Error 0, 치명적 Console Error 0, Missing Script 0, 필수 Inspector null 0.
- Google Mobile Ads 운영 설정과 실패 fallback이 검증된다.
- 서명된 ARM64 AAB가 재현 가능하고 Play Console 내부 테스트를 통과한다.
- P0 이슈가 0건이며 남은 P1/P2는 출시 기록에 명시된다.
