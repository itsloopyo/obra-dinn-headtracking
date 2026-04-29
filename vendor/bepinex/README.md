# bepinex (vendored)

This directory contains a bundled copy of the upstream mod loader. It is the install-time
source of truth: install.cmd extracts directly from here and never reaches out to the network.
Refresh manually with `pixi run update-deps`, then commit.

## Snapshot

- Asset: `BepInEx_win_x86_5.4.23.5.zip`
- Tag: `v5.4.23.5`
- Commit: `57f1fb859bd4d0264cd2a59074d0e96c6a492a33`
- Upstream URL: https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x86_5.4.23.5.zip
- SHA-256: `37651c79e40d6f909572a4f461ac25350bb3ef8fe7fbd29f1aa8791a33b84c82`
- Fetched at: 2026-04-28T17:57:09.5777200+01:00
- Source: github

Do not edit this directory by hand. Run ``pixi run package`` (or CI release) to refresh.
