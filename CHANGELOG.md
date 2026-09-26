# Changelog

## [Unreleased]

### Changed

- Settings move to `BepInEx\config\CameraUnlock.ini`. Earlier versions of the mod kept these settings in `com.headtracking.obradinn.cfg`, in the same folder. The first time this version starts and finds no `CameraUnlock.ini`, it reads your settings from `com.headtracking.obradinn.cfg` and writes them into `CameraUnlock.ini`. It never changes `com.headtracking.obradinn.cfg`, and does not read it again while `CameraUnlock.ini` exists.
- A setting that the defaults the README shows set to `default` is written as `default` when the value imported for it equals its default at that start, which is the value `Defaults.ini` gives it, or the built-in value where `Defaults.ini` gives none. It then follows `Defaults.ini`. Every other setting is written with the value imported for it.
- `RotationEnabled` and `PositionEnabled` are one setting here, the tracking mode, so both are written as `default` or neither is.
- Comments, and keys the mod never read, are not carried over. Nor are these, where your old file had them:
  - A sensitivity, scale, deadzone, response curve or axis inversion you changed from its default. Set these in your tracker instead.
  - Reticle settings, and a key that toggled the reticle.
- An older version of the mod reads `com.headtracking.obradinn.cfg` and never reads `CameraUnlock.ini`, so a setting you change after updating is not in `com.headtracking.obradinn.cfg`.
- Deleting only `CameraUnlock.ini` makes the next start read `com.headtracking.obradinn.cfg` again. To go back to the defaults, replace everything in `CameraUnlock.ini` with the defaults the README shows. Every setting they set to `default` then follows `Defaults.ini`.
- BepInEx's ConfigurationManager no longer lists these settings. Edit `BepInEx\config\CameraUnlock.ini` with any text editor.
- Hotkeys are written as key names, and each hotkey lists every key that triggers it, the Ctrl+Shift chord included: `ToggleKey=End, Ctrl+Shift+Y`.
- A hotkey bound to a plain key no longer fires while Ctrl and Shift are both held, so Ctrl+Shift with that key reaches only a binding that names the chord.
- On Linux and macOS without Wine or Proton, this version reads its settings and saves none: it creates no `CameraUnlock.ini`, reads your settings from `com.headtracking.obradinn.cfg` again at every start while there is no `CameraUnlock.ini`, and a change made in game lasts until the game closes.
- The tracking mode `Page Up` picks is saved to `CameraUnlock.ini`, and the next start begins in it. With positional tracking off at start, the first press now goes to position only; earlier versions spent that press on rotation only, which was already the state.
- Several settings have new names and sections in `CameraUnlock.ini`: `EnabledOnStartup` is `EnableOnStartup`, `UDPPort` is `UdpPort`, `[Keybindings]` is `[Hotkeys]`, `UnlockFramerate` is under `[Display]`, and the two notification switches are under `[Notifications]`. The import carries each value over.
- `PositionLimitY` no longer sets the downward limit as well: `PositionLimitYDown` is its own setting. The import writes your old `PositionLimitY` into both.
- A new `CameraUnlock.ini` sets `TrackerPivotForward` to `default`, whose built-in value is 0.0, which turns the compensation off. Earlier versions started at 0.08. An imported `com.headtracking.obradinn.cfg` keeps the value it held, 0.08 unless you changed it.
- Turning your head left and right now turns the view around the world's up axis, so it stays level while you look up or down with the mouse. Earlier versions turned it around the camera's own up axis, which tilts with the mouse's up and down. `Page Down` or `WorldSpaceYaw=false` brings that back.
- An old file that holds a number where a key name belongs for `ToggleKey` or `CycleTrackingModeKey` (for example `ToggleKey = 2`) is not imported. The mod runs that session on the settings it read, saves nothing, says so on screen and in `BepInEx/LogOutput.log`, and tries again at the next start.

### Added

- `Page Down` / `Ctrl+Shift+H` switches the yaw mode between world-locked and camera-local. The choice is saved to `CameraUnlock.ini` as `WorldSpaceYaw`, and the keys are the `YawModeKey` list.
- A setting set to `default` in `CameraUnlock.ini` takes its value from `Defaults.ini`, which every head tracking mod that keeps its settings in `CameraUnlock.ini` reads. Head tracking mods that keep their settings in another file do not read it, and neither do earlier versions of this mod. Writing a value in place of `default` changes that setting for this game only. When the mod saves a setting that a hotkey changed in game, it writes the new value in place of `default`, so that setting no longer follows `Defaults.ini` in this game until you set it to `default` again.
- `Defaults.ini` is `%AppData%\CameraUnlock\Defaults.ini` on Windows; `$XDG_CONFIG_HOME/CameraUnlock/Defaults.ini` on Linux, or `~/.config/CameraUnlock/Defaults.ini` where `XDG_CONFIG_HOME` is not set, under Wine and Proton too; and `~/Library/Application Support/CameraUnlock/Defaults.ini` on macOS. The mod's log, where it writes one, names the file it read.
- When the mod starts and finds no `Defaults.ini`, it creates one holding the built-in values, unless Windows runs the game as a packaged app, or the game runs on Linux or macOS without Wine or Proton. The mod never changes `Defaults.ini` after that.

### Removed

- The key that toggled the reticle, and the reticle settings. The aim dot is drawn whenever head tracking is turning the view during gameplay, as it was by default. `Page Down` and `Ctrl+Shift+H` switch the yaw mode now.
- The sensitivity, scale, deadzone, response curve and axis inversion settings. Set these in your tracker app instead.
- With these settings at their shipped defaults the camera moves as it did before.

## [1.3.0] - 2026-08-20

### Fixed

- give the forward lean its own travel budget again
- remove mod-side centring, log connection changes unconditionally
- harden the release pin sync and announce steps
- Log `OpenTrack connection established` / `lost` regardless of the on-screen
  notification setting. It is the only evidence in `BepInEx/LogOutput.log` that
  tracker packets ever arrived, and a user who had turned notifications off sent
  a log that could not answer "did the tracker reach the game".

### Changed

- Removed all mod-side centring. The `Home` / `Ctrl+Shift+T` hotkey, the
  `RecenterKey` config entry and the handling of the tracker's CENTER request are
  gone, and the mod now applies the tracker pose as absolute. Every tracker
  centres itself, so a centre in the mod was a second centre in series with the
  tracker's own and the two drifted apart. Centre your view in your tracker app.

## [1.2.1] - 2026-08-18

### Fixed

- migrate to the per-connection smoothing pair
- match stub member kinds to the shipped Unity assemblies

### Changed

- Replace the single `Smoothing` config key with `LocalSmoothing` (default 0.0) and `RemoteSmoothing` (default 0.15), selected per connection from the packet source address
- Remove the `PositionSmoothing` key: position now uses the same connection-selected value as rotation
- Remove the hidden 0.15 baseline smoothing floor, so local trackers get zero-latency tracking by default

## [1.2.0] - 2026-08-03

### Fixed

- show full control set in pixi install via shared -Controls
- recenter only when armed, honor tracker recenter requests

### Other

- Link Discord, Lopari and Headcam from the README

## [1.1.3] - 2026-06-07

### Added

- guard the .original backup against patched assemblies

### Fixed

- subscribe Camera.onPreCull via reflection for SRP-only Unity 6
- expose Camera render callbacks as fields not events in UnityStubs

## [1.1.2] - 2026-06-07

### Added

- add HeadTrackingSession and expand C++ core with RE Engine, Unreal, and tracking-session modules
- aim projection, reframework/unreal hooks, input/logging hardening, games
- add Mass Effect Legendary Edition to games catalog
- expand games catalog, fix unicode games.json read, stage launcher manifest
- add Pacific Drive to games catalog
- add Homeworld: Remastered Collection to games catalog
- add manifest-mode installer validator and ASI loader subdir support
- authenticate GitHub API requests via env token when present
- add R.E.P.O. detection data

### Fixed

- fail fast in ASI dev-deploy when the game is running
- restore il2cpp camera position by undoing applied local delta
- set SO_REUSEADDR so the receiver reclaims its port on relaunch
- harden release.ps1 - changelog gate before version bump, add -Force
- skip empty-env pixi list in setup-pixi

### Other

- Add Ubisoft Connect detection and VendorZip BepInEx install
- Add PluginSubfolder param to Invoke-DevDeployBepInEx
- Add Xbox install path for Easy Delivery Co
- Add GOG IDs for Cyberpunk 2077
- Add PLUGIN_SUBFOLDER support to BepInEx install/uninstall bodies
- scripts: drop the two-phase loader-init prompt from install bodies
- data: add Black & White (Lionhead) to games registry
- scripts: detect BepInEx 6 IL2CPP via BepInEx.Core.dll marker
- powershell: skip cameraunlock-core remote refresh in CI
- scripts: add UE4SS install template, fix delayed expansion in ASI body, expand games registry
- protocol: reject finite-but-out-of-float-range packet values
- data: add Subnautica 2 to games registry
- detection: add installer-registry game path lookup (Black & White GameDir)
- protocol: reorder tracking data member in udp_receiver
- data: fix Subnautica 2 Steam app id (3367150 -> 1962700)
- data: add Ni no Kuni Remastered and Yakuza 0; switch find-game output to UTF-8
- detection: add Xbox/GDK build support for Subnautica 2 (and any future GDK title)
- find-game: escape `&` in GAME_DISPLAY_NAME so echo doesn't split
- templates: add uninstall.ps1; data: add Deus Ex Mankind Divided
- powershell: add NightlyRelease module for Patreon-gated nightly builds
- protocol: disable SIO_UDP_CONNRESET and add one-shot receiver diagnostics; powershell: write nightly manifest.json without UTF-8 BOM; data: add Mixtape
- powershell: stop redirecting git stderr in Update-CameraUnlockCoreToRemoteTip
- powershell: publish dev builds as GitHub pre-releases
- protocol: disable SIO_UDP_CONNRESET and add one-shot receiver diagnostics
- data: add Mixtape
- powershell: stop redirecting git stderr in Update-CameraUnlockCoreToRemoteTip
- powershell: run gh under Continue so its stderr doesn't abort the dev-release publish
- reframework: strip VR runtime DLLs on install for flatscreen mode
- reframework: cache GetValue method and avoid per-call heap in ArrayGetValue; data: add BioShock Infinite
- uninstall: remove reframework_revision.txt marker dropped at game root
- install: render MOD_CONTROLS multi-line via percent expansion
- Add YAPYAP to games.json
- powershell: write state file BOM-less so Lopari JSON parser accepts it
- Reuse cameraunlock-core helpers for caching, aim offset, and chord hotkeys
- Add launcher manifest and route CI through pixi run package
- powershell: stop redirecting git stderr in Invoke-VersionCommit

## [1.1.1] - 2026-05-03

### Other

- Add DX11 overlay header for crosshair rendering
- Update PositionInterpolator tests for bounded extrapolation
- Skip vendor refresh when SHA-256 matches existing copy
- Fix degenerate-input bugs in scanners, projection, and color parser
- Add yaw-mode key and WorldSpaceYaw config options
- Quote /y flag detection and add shared install/uninstall bodies
- Add DevDeploy module with Cecil dev-install orchestrator
- Auto-refresh cameraunlock-core submodule in Copy-SharedBundle
- Add install bodies and dev-deploy orchestrators for non-Cecil frameworks
- Convert deploy/install/uninstall scripts to thin wrappers
- Resolve exe relpath from games.json in ASI/shim dev-deploy
- Add automatic port retry to C++ UdpReceiver
- Take BuildOutputPath in dev-deploy and add loader/config auto-install
- Adapt deploy.ps1 to BuildOutputPath signature
- Verify existing BepInEx loader arch and replace on mismatch
- Fall back to dev-tree vendor path in BepInEx install body
- Pass -Architecture x86 to dev-deploy and sync MOD_VERSION on release
- Use the same Unity stubs locally and in CI
- Revert Unity stubs to monolithic UnityEngine.dll layout
- Match Unity 2017's UnityAction<Scene, LoadSceneMode> on sceneLoaded

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
