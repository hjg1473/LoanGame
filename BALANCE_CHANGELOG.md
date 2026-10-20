# Balance Changelog

This file records balance-rule changes separately from content tuning. Runtime and simulator must use the same rules before a value change is accepted.

## 2026-10-03 — Stage 2 shared-rule foundation

- Change: extracted pure `EconomyRules`, `PopulationRules`, and `ProjectRules` and connected Runtime callers.
- Reason: remove formula drift between gameplay and automated simulation.
- Scope: no intentional balance-value change.
- Simulator: added deterministic seeds, Safe/Balanced/Aggressive bots, accelerated one-second ticks, snapshots, summaries, and CSV export.
- Regression: 100 sessions, 209 assertions, deterministic-seed and accounting checks passed.
- Baseline result: 33 Victory, 0 Bankruptcy, 67 Timeout.
- Interpretation: infrastructure is working; the high timeout rate and absent bankruptcy cases are inputs for Stage 3 tuning, not corrected in Stage 2.
- Artifacts: `BalanceReports/Stage2/balance_summary.csv`, `BalanceReports/Stage2/balance_snapshots.csv`.

## 2026-10-03 — Stage 3 first balance pass

- Starting cash: 0 → 2,000 so the Small House + Workshop opening is immediately actionable.
- Interest: base/start 1%/s → 0.015%/s, minimum 0.005%/s, maximum 0.3%/s. Debt-pressure and repayment coefficients were rescaled to the new rate range.
- Population: stopped-happiness threshold 40 → 35 so an unserved starter district grows slowly instead of deadlocking at 0.9 population.
- Buildings: all 13 non-road definitions received distinct roles, names, costs, production, maintenance, capacity/workers/leisure, and cost-scaled City Value. Asset IDs were preserved for save compatibility.
- Quality: Normal/Silver/Gold/Rainbow set to 70/20/8/2% and 1.0/1.35/1.8/2.5 multipliers; expected multiplier 1.164.
- Strategy matrix: Cash-focused Safe, 50%-financed Balanced, and 100%-financed Aggressive; a 100-cash operating tolerance prevents the cash strategy from deadlocking between its first house and workshop.
- Final 300-run result: all strategies completed 100/100 without Timeout.
- Average completion: Safe 24.65 min, Balanced 18.72 min, Aggressive 15.47 min.
- Average maximum debt: Safe 66.83, Balanced 82,984.75, Aggressive 85,605.01.
- Interpretation: borrowing now produces a clear speed/debt tradeoff. Automated bots still recorded no bankruptcy, so human playtest and more adversarial strategies remain Stage 4 work.
- Artifacts: `BalanceReports/Stage3/balance_summary.csv`, `BalanceReports/Stage3/balance_snapshots.csv`.

## 2026-10-03 — Stage 4 playtest metrics and second balance pass

- Observation metrics: added successful decision count, maximum idle interval, and every project completion timestamp to each simulation summary.
- Decision cadence: Safe/Balanced/Aggressive action intervals set to 28/26/24 seconds.
- Project curve: final project cost increased from 80,000 to 100,000 in both the simulator and runtime fallback chain.
- Pressure regression: added an Aggressive high-interest scenario with a 50,000 bankruptcy threshold; it must end in Bankruptcy.
- Final 300-run result: all strategies completed 100/100 without Timeout.
- Average completion: Safe 24.65 min, Balanced 18.88 min, Aggressive 19.05 min.
- Average maximum debt: Safe 71.00, Balanced 77,170.78, Aggressive 102,776.57.
- Decision/idle profile: Safe 45 decisions/87 sec max idle, Balanced 39/81 sec, Aggressive 42/75 sec.
- First project average: Safe 261 sec, Balanced 28 sec, Aggressive 26 sec.
- Regression: 910 assertions passed, including the forced-bankruptcy pressure case.
- Interpretation: the Stage 3 Aggressive dominant strategy is removed; extra leverage now carries substantially higher debt without beating Balanced completion time.
- Runtime observation: HUD state and world progression render correctly. Landscape layout and the test ad banner remain explicit Stage 5 mobile-release work.
- Artifacts: `BalanceReports/Stage4/balance_summary.csv`, `BalanceReports/Stage4/balance_snapshots.csv`, `Assets/BalanceReports/Stage4/playtest.png`.

## 2026-10-03 — Stage 5 mobile runtime pass

- Display: Android is Portrait-only, renders inside the safe area, targets 60 FPS, and disables vSync.
- Canvas: main uGUI Canvas now scales from 1080×1920; top-level HUD content is contained by a runtime Safe Area fitter.
- Touch: selectable hit regions smaller than 88 reference pixels are expanded without enlarging their visuals.
- Navigation: Android back closes the active phone app, then the phone, then exits the app.
- Readability: million-and-above economy values use compact M/B/T formatting.
- Ads: UMP consent now gates SDK initialization and ad requests; failed banners use bounded delayed retries.
- Privacy: a Privacy Options entry was added to the settings app.
- Release safety: Editor/development builds use Google's test banner; a release with no production banner ID disables ads instead of shipping test traffic.
- Performance: verbose map/input/selection diagnostics were disabled in the scene; Portrait Play Mode held approximately 59.4 FPS in the Editor.
- Validation: 1080×1920 runtime capture, 45 buttons/17 expanded targets, back handling, Console 0 errors/0 warnings, and Android build dry-run passed.
- Remaining: production Android Banner Ad Unit ID and physical low/mid-tier Android device testing.

## 2026-10-04 — Stage 6 polish and regression lock

- Balance values: no intentional tuning change; the Stage 4 curve remains locked.
- Regression: 300 baseline sessions produced 300 Victory and 0 Timeout; 910 assertions passed, including the forced-bankruptcy pressure case.
- Persistence: stock price, owned shares, and average buy price now participate in local save/restore alongside the existing economy and progression state.
- Runtime terminal paths: recovery, Bankruptcy, Victory event, completed economy state, `BANKRUPT`, and `CITY COMPLETE` were exercised in Play Mode.
- Content cleanup: numeric placeholder descriptions, default test candles, and the `test_map` resource name were removed from production defaults.
- Stability: removed the database-less duplicate project manager and fixed the result panel's execution-order-sensitive Victory subscription.
- Validation: Unity compile and final Console both reported 0 errors/0 warnings; active scene missing scripts and required MapManager null references were 0.
- Remaining release gates are external to the locked balance curve: production banner ID, physical Android testing, first-time-player observation, and Play Console internal testing.
- Artifacts: `BalanceReports/Stage6/STAGE6_REPORT.md`, `BalanceReports/Stage6/balance_summary.csv`, `BalanceReports/Stage6/balance_snapshots.csv`.
