# Changelog

## [1.1.0] - 2026-05-01

### Added

- add Invoke-FetchLatestLoader and Refresh-VendoredLoader helpers

### Fixed

- install.cmd works on Program Files (x86) paths

### Other

- Improve build and release infrastructure
- Add automatic port retry to OpenTrackReceiver
- Vendor BepInEx, add tracking-mode cycle, add chord hotkeys
- Add prediction-error correction to interpolators for smooth high-FPS output
- Port linear interpolation and quaternion SLERP smoothing from C# core
- Add gui_marker_compensation.h for RE Engine GUI world-anchor tracking
- Add REFramework utilities module (cameraunlock_reframework)
- Add velocity extrapolation to interpolators for smooth high-refresh output
- Gate UnityEngine.InputLegacyModule reference on file existence
- Fix batch paren-poisoning in install.cmd template
- Move game detection to data-driven games.json
- Fix install.cmd/uninstall.cmd templates for dev-tree use
- Unify installer CLI across BepInEx/MelonLoader/Cecil/ASI/REFramework/shim
- Make vendored loaders the install-time source of truth
- Add Step-SemanticVersion and Resolve-ReleaseVersion helpers
- Add camera discovery module (RTTI vtable + float classifier)
- Add AGENTS.md with shared code-quality and library API rules
- Sync install/uninstall + packager to cameraunlock-core unified contract
- Expand submodule pointer commits in generated changelogs
- Fix /y flag detection and bundle vendored BepInEx in installers
- Use WriteAllBytes for .cmd output to avoid Defender race

## [1.0.7] - 2026-03-26

### Other

- Remove neck model feature
- Simplify camera rotation to camera-local composition

## [1.0.6] - 2026-03-13

### Fixed

- Update cameraunlock-core submodule and add MultiplyVector to Unity stubs

## [1.0.5] - 2026-03-13

### Other

- Use axis-rotation sequence for camera and exact projection for reticle
- Update README, submodule, and camera controller
- Add PositionLimitZBack config and fix Z clamp direction

## [1.0.4] - 2026-03-10

### Other

- Use spherical coordinate reconstruction for camera and exact projection for reticle

## [1.0.3] - 2026-03-08

### Other

- Auto-recenter on first valid tracking frame

## [1.0.2] - 2026-03-07

### Other

- Auto-detect 6DOF vs 3DOF and add face-to-eye pivot compensation
- Use shared PositionProcessor pivot compensation instead of game-side
- Make TrackerPivotForward configurable (default 0.08m)

## [1.0.1] - 2026-03-05

### Other

- Add 6DOF positional tracking with neck model simulation

## [1.0.0] - 2026-03-04

First release.
