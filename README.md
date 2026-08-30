# Obra Dinn Head Tracking

![Return of the Obra Dinn running with this mod](https://raw.githubusercontent.com/itsloopyo/obra-dinn-headtracking/main/assets/readme-clip.gif)

An unofficial head tracking mod for Return of the Obra Dinn that moves the view with your head while your mouse or controller keeps aiming, driven by OpenTrack over UDP, with no VR headset required.

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

The mod listens for OpenTrack pose data on UDP port `4242`, on every network
interface. One datagram is six little-endian 64-bit floats in the order
`x, y, z, yaw, pitch, roll`: position in centimetres, rotation in degrees, 48
bytes in total. Anything that sends that to that port drives the view.
OpenTrack's **UDP over network** output sends exactly this, and the steps below
set it up.

1. Install [OpenTrack](https://github.com/opentrack/opentrack/releases).
2. Pick a tracker under **Input**, using the notes below.
3. Set **Output** to **UDP over network**, host `127.0.0.1`, port `4242`.
4. Press **Start**. Tracking and the game can start in either order.

### Webcam

OpenTrack ships a `neuralnet tracker` input that reads a plain webcam. Select it
under **Input**, pick your camera in its settings, and use the output settings
above. How well it tracks depends on your camera and your lighting, so try it
before buying anything.

### Phone

A phone app can reach the mod directly, with no OpenTrack on the PC, if it sends
the datagram described above. Point it at this PC's IP address (run `ipconfig`
to find it) on port `4242`. Not every phone tracker speaks this protocol, so
check yours for an OpenTrack or UDP output option first. [Headcam](https://headcam.app)
sends it, and I wrote it so decent tracking is free for anyone who already owns
a phone.

Sending direct works when the app filters its own signal on the device. The
mod's smoothing is sized to take the edge off a clean signal rather than to
rescue a noisy one, so a raw feed sent direct will jitter. If it does, point the
app at OpenTrack's **UDP over network** *input* on some other port, say 5252,
and let OpenTrack's filters and curves clean it up before its output forwards to
`127.0.0.1:4242`.

Anything arriving from outside `127.0.0.0/8` counts as a remote connection and
is smoothed with `RemoteSmoothing` rather than `LocalSmoothing`. That includes a
tracker on this very PC that sends to the machine's own LAN address, because the
mod reads the source address and not the machine.

### Headset or other hardware

If your device has an OpenTrack input driver, select it under **Input** and use
the same output settings. OpenTrack's own **Input** list is the authority on
what it can read; the mod only ever sees what OpenTrack sends.

### Centring

Centring belongs to your tracker. The mod subtracts no centre of its own: it
applies the pose it receives exactly as it arrives, so a stream of zeros holds
the view where the game itself puts it. Press the centre control in your tracker
(OpenTrack's **Center** bind, or the CENTER button in Headcam) and the tracker
zeroes its own output, which leaves the view centred with the mod doing nothing.

That is why there is no centre hotkey here and nothing to re-centre in game. Two
centres in series would drift apart, because each side re-centres at moments the
other cannot see, and you would end up pressing twice to centre once. If the
view sits off to one side, centre it in the tracker.

## Controls

Two equivalent binding sets - use whichever your keyboard has:

| Action              | Nav-cluster | Chord           |
|---------------------|-------------|-----------------|
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
- Press **End** to enable tracking
- Check firewall isn't blocking UDP port 4242

**View is off-centre:**
- Centre it in your tracker app. The mod keeps no centre of its own and applies the tracker pose as sent, so use OpenTrack's Center hotkey (or your phone app's CENTER button) while looking straight ahead.

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
