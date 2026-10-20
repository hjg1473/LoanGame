# Stage 5 Mobile UX / Ads / Performance Report

Date: 2026-10-03

## Passed

- Unity compile: 0 errors, 0 warnings.
- Android orientation: Portrait only.
- Android safe-area rendering: enabled (`renderOutsideSafeArea = false`).
- Canvas: Scale With Screen Size, 1080x1920, Match 0.5.
- Runtime: target 60 FPS; observed 59.4 FPS in Editor Portrait Play Mode.
- Input: back request closes an open phone (`handled=True`, `isOpen=False`).
- Touch: 45 Buttons found; 17 undersized targets received expanded raycast padding.
- Console after Play Mode smoke test: 0 errors, 0 warnings.
- Android AAB dry-run: valid target, scene, and output path; 0 validation errors.
- Portrait evidence: `Assets/BalanceReports/Stage5/mobile_portrait_runtime.png`.

## Ads and privacy

- Google Mobile Ads SDK: 11.5.0.
- UMP consent update/form completes before SDK initialization.
- Ads are requested only when `ConsentInformation.CanRequestAds()` is true.
- Settings contains a Privacy Options button.
- Banner failures retry at 15 and 30 seconds, then stop.
- Google test ads are limited to Editor/development builds or an explicit force-test flag.
- Production AdMob app ID exists, but the Android Banner Ad Unit ID is empty. Release gracefully disables the banner until a production unit ID is supplied.

## Remaining device gates

- No ADB device was connected during Stage 5.
- Run a 30-minute full session on at least one low-tier and one mid-tier Android device.
- Record average/minimum FPS, peak memory, device temperature/thermal throttling, pause/resume save integrity, safe-area appearance, consent form, and production/test-device ad behavior.
