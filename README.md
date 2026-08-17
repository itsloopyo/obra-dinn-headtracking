# Obra Dinn Head Tracking

![Mod GIF](https://raw.githubusercontent.com/itsloopyo/obra-dinn-headtracking/main/assets/readme-clip.gif)

An **unofficial** BepInEx mod that adds head tracking support to Return of the Obra Dinn using OpenTrack. Move your head to look around the ship while your mouse controls where you're aiming.

## Features

- **Decoupled look + aim**: Look around freely with your head while your aim stays independent
- **6DOF head tracking**: Full rotation (yaw, pitch, roll) and positional tracking via OpenTrack UDP protocol
- **Framerate unlock**: Optional removal of the game's 60 FPS cap for smoother tracking

## Requirements

- [Return of the Obra Dinn](https://store.steampowered.com/app/653530/Return_of_the_Obra_Dinn/) (Steam)
- [OpenTrack](https://github.com/opentrack/opentrack) or a compatible head tracking app (smartphone, webcam, or dedicated hardware)
- Windows

## Installation

1. Download the latest release from the [Releases page](https://github.com/itsloopyo/obra-dinn-headtracking/releases)
2. Extract the ZIP anywhere
3. Double-click `install.cmd`
4. If BepInEx was just installed:
   - Run the game once to let BepInEx initialize
   - Run `install.cmd` again to complete mod installation
5. Configure OpenTrack to output UDP to `127.0.0.1:4242`

The installer automatically finds your game via Steam registry lookup. If it can't find the game:
- Set the `OBRA_DINN_PATH` environment variable to your game folder, or
- Run from command prompt: `install.cmd "D:\Games\ObraDinn"`

## Setting Up OpenTrack

1. Download and install [OpenTrack](https://github.com/opentrack/opentrack/releases)
2. Configure your tracker as input
3. Set output to **UDP over network**
4. Host: `127.0.0.1`, Port: `4242`
5. Start tracking before launching the game

### Webcam Setup

No special hardware needed — OpenTrack's built-in **neuralnet tracker** uses any webcam for 6DOF face tracking.

1. In OpenTrack, set the input to **neuralnet tracker**
2. Select your webcam in the tracker settings
3. Set output to **UDP over network** (`127.0.0.1:4242`)
4. Start tracking before launching the game
5. Recenter in OpenTrack via its hotkey, and press **Home** in-game to recenter the mod as needed

### Phone App Setup

This mod includes built-in smoothing for network jitter, so you can send directly from your phone on port 4242 without needing OpenTrack on PC.

1. Install an OpenTrack-compatible head tracking app
2. Configure it to send to your PC's IP on port 4242 (run `ipconfig` to find it)
3. Set the protocol to OpenTrack/UDP

**With OpenTrack (optional):** If you want curve mapping or visual preview, route through OpenTrack. Set OpenTrack's input to "UDP over network" on a different port (e.g. 5252), point your phone app at that port, and set OpenTrack's output to `127.0.0.1:4242`. Make sure your firewall allows incoming UDP on the input port.

## Controls

Two equivalent binding sets - use whichever your keyboard has:

| Action              | Nav-cluster | Chord           |
|---------------------|-------------|-----------------|
| Recenter            | `Home`      | `Ctrl+Shift+T`  |
| Toggle tracking     | `End`       | `Ctrl+Shift+Y`  |
| Cycle tracking mode | `Page Up`   | `Ctrl+Shift+G`  |
| Toggle reticle      | `Page Down` | `Ctrl+Shift+H`  |

`Page Up` / `Ctrl+Shift+G` cycles tracking mode:

1. Normal head-tracked gameplay
2. Positional tracking disabled, rotational tracking enabled
3. Rotational tracking disabled, positional tracking enabled
4. Back to normal

## Configuration

The mod creates a config file at `BepInEx/config/com.headtracking.obradinn.cfg` on first run. Edit it to customize:

A comment has to sit on its own line. BepInEx splits each line at the first `=`
and takes everything after it as the value, so a trailing `# note` becomes part
of the value, the conversion fails, and the entry silently keeps its default -
the only trace is a line in `BepInEx/LogOutput.log`. Put explanations above the
key, never after it.

```ini
[General]
# Start with tracking enabled
EnabledOnStartup = true
# Show controls on startup
ShowStartupNotification = true
# Remove 60 FPS cap
UnlockFramerate = true

[UI]
ShowConnectionNotifications = true
# Aim reticle during gameplay
ShowReticle = true

[Keybindings]
# Also Ctrl+Shift+T
RecenterKey = Home
# Also Ctrl+Shift+Y
ToggleKey = End
# Also Ctrl+Shift+G
CycleTrackingModeKey = PageUp
# Also Ctrl+Shift+H
ToggleReticleKey = PageDown

[Network]
# Must match OpenTrack output port
UDPPort = 4242

[Sensitivity]
# Horizontal rotation (0.1-3.0)
YawSensitivity = 1.0
# Vertical rotation (0.1-3.0)
PitchSensitivity = 1.0
# Head tilt (0.0-3.0)
RollSensitivity = 1.0

[Smoothing]
# Tracker on this machine (loopback), 0 = none, 1 = heavy
LocalSmoothing = 0.0
# Tracker on a remote network device, 0 = none, 1 = heavy
RemoteSmoothing = 0.15

[Position]
# Enable lean/positional tracking
PositionEnabled = true
# Lateral sensitivity (0.0-3.0)
PositionSensitivityX = 2.0
# Vertical sensitivity (0.0-3.0)
PositionSensitivityY = 2.0
# Depth sensitivity (0.0-3.0)
PositionSensitivityZ = 2.0
# Max lateral offset in meters
PositionLimitX = 0.30
# Max vertical offset in meters
PositionLimitY = 0.20
# Max forward lean in meters (0.01-0.5)
PositionLimitZ = 0.40
# Max backward lean in meters (0.01-0.5). Separate from PositionLimitZ and
# much tighter by default, to stop the camera clipping through the player
PositionLimitZBack = 0.10
# Neck-to-face distance, compensates yaw orbit
TrackerPivotForward = 0.08
```

## Troubleshooting

**Mod not loading:**
- Ensure `winhttp.dll` exists in the game folder (installed by BepInEx)
- Try running the game as administrator once
- Check `BepInEx/LogOutput.log` for errors

**No tracking response:**
- Verify OpenTrack is running and outputting data
- Check UDP port matches (default 4242)
- Press **End** to enable tracking, **Home** to recenter
- Check firewall isn't blocking UDP port 4242

**A config edit had no effect:**
- Make sure nothing follows the value on the line. A trailing `# comment` is read as part of the value, the entry falls back to its default, and the game gives no sign of it. `BepInEx/LogOutput.log` records the failed conversion.

**"Player type not found" error:**
- Game version mismatch. The mod may need an update for newer game versions.

## Updating

Download the new release and run `install.cmd` again.

## Uninstalling

Run `uninstall.cmd` from the release folder. This removes the mod DLLs. BepInEx is only removed if it was originally installed by this mod. To force-remove BepInEx:

```
uninstall.cmd /force
```

## Building from Source

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (any recent version)
- [pixi](https://pixi.sh) task runner
- Return of the Obra Dinn installed (for Unity/BepInEx DLL references)

### Build

```bash
git clone --recurse-submodules https://github.com/itsloopyo/obra-dinn-headtracking.git
cd obra-dinn-headtracking

# Build and install to game
pixi run install

# Build only
pixi run build

# Package for release
pixi run package
```

### Available Tasks

| Task | Description |
|------|-------------|
| `pixi run build` | Build the mod (Release configuration) |
| `pixi run install` | Build and install to game directory |
| `pixi run uninstall` | Remove the mod from the game |
| `pixi run uninstall -- --force` | Remove the mod and BepInEx |
| `pixi run package` | Create release ZIP |
| `pixi run clean` | Clean build artifacts |
| `pixi run release` | Version bump, build, tag, and push |

## Community & Support

- Discord: [Loop's Head Tracking Hangout](https://discord.com/invite/dxyZdyFNT9) - setup help, bug reports, and new-release announcements
- [Lopari](https://lopari.app) - free Windows launcher with one-click install and launch for the released head-tracking mods
- [Headcam](https://headcam.app) - free app that turns your iPhone or Android phone into the head tracker

## License

MIT License - see [LICENSE](LICENSE) for details.

## Credits

- [Lucas Pope](https://dukope.com/) - Return of the Obra Dinn
- [BepInEx](https://github.com/BepInEx/BepInEx) - Unity modding framework
- [OpenTrack](https://github.com/opentrack/opentrack) - Head tracking software
- [Harmony](https://github.com/pardeike/Harmony) - Runtime patching library
