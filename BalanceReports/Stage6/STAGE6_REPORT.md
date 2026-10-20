# Stage 6 — Polish and Regression Report

Date: 2026-10-04

## Result

Stage 6's editor and automated regression scope is complete. The runtime build compiles without errors or warnings, the active scene has no missing scripts or required `MapManager` references, and the 300-run balance baseline is unchanged.

This is not a release approval. Stage 5 still requires a production Android banner Ad Unit ID and physical Android-device testing. A first-time-player observed session and Play Console internal-track validation also remain release gates.

## Changes

- Added stock price, owned shares, and average buy price to the local save payload and restore path.
- Replaced the remaining numeric placeholder building descriptions with role-specific copy.
- Renamed the runtime map resource from `test_map` to `city_map` while preserving its Unity metadata/GUID.
- Disabled test candle generation by default.
- Restricted map initialization diagnostics to Editor/development builds.
- Removed the database-less duplicate `DevelopmentProjectManager` scene object.
- Fixed `GameOverPanel` so it retries the Victory subscription in `Start` when script execution order made the manager unavailable during `OnEnable`.
- Changed the expected bankruptcy terminal-state message from a warning to a normal diagnostic log.

## Runtime regression

- Economy + stock save serialization round-trip: 3/3 passed (`cash=123456`, `debt=7890`, price `321.5`, shares `7`, average buy `300.25`).
- Insolvency recovery: countdown started under debt pressure and cancelled after production recovered; no bankruptcy occurred.
- Bankruptcy: shortened test countdown reached the bankrupt state and displayed `BANKRUPT`.
- Victory: final project accepted and completed, Victory event fired, economy entered completed state, and the result title displayed `CITY COMPLETE`.
- Test-only requirement/countdown changes were made in Play Mode and were not saved to assets.

## Balance regression

- Runs: 300 (100 Safe, 100 Balanced, 100 Aggressive).
- Result: 300 Victory, 0 Timeout in the baseline strategies.
- Assertions: 910 passed, including the separate forced-bankruptcy pressure case.
- Outputs: `balance_summary.csv`, `balance_snapshots.csv`.

## Scene and compile checks

- Active scene: `SampleScene`, 253 GameObjects after duplicate cleanup.
- Missing scripts: 0.
- EventSystem: 1; Canvas: 1; valid DevelopmentProjectManager: 1.
- Required `MapManager` references: camera, renderer, pan controller, selection controller, popup, and economy manager all assigned.
- Unity recompile: 0 errors, 0 warnings.
- Final Unity Console: 0 errors, 0 warnings.
- Project Auditor: unavailable because the Project Auditor package is not installed; no package was added during this stage. The scene/reference/manual static checks above were used instead.

## Remaining release gates

- Provide and verify the production Android banner Ad Unit ID.
- Run a 30-minute session on low/mid-tier physical Android devices, including pause/resume, force-close restore, heat, memory, touch, safe area, consent, and ad fallback.
- Observe at least one first-time player from new game to a terminal state and log comprehension/friction issues.
- Complete signed ARM64 AAB and Google Play internal-track validation in Stage 7.
