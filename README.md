# Obra Dinn Head Tracking

![Mod GIF](assets/readme-clip.gif)

An **unofficial** BepInEx mod that adds head tracking support to Return of the Obra Dinn using OpenTrack. Move your head naturally to look around the ship while your mouse controls where you're aiming.

## Features

- **6DOF head tracking**: Yaw, pitch, roll rotation plus positional tracking (lean in/out/side-to-side) via OpenTrack UDP protocol
- **Neck model**: Simulates realistic head rotation around the neck pivot, so tilting your head moves your eyes along a natural arc
- **Framerate unlock**: Optional removal of the game's 60 FPS cap for smoother tracking
- **Aim reticle**: Shows where your mouse is aiming when head tracking moves the camera

## Requirements

- [Return of the Obra Dinn](https://store.steampowered.com/app/653530/Return_of_the_Obra_Dinn/) (Steam)
- [OpenTrack](https://github.com/opentrack/opentrack) or an OpenTrack-compatible head tracking app (smartphone, webcam, or dedicated hardware)
- Windows (x86/x64)

## Installation

1. Download the latest release from the [Releases page](https://github.com/itsloopyo/obra-dinn-headtracking/releases)
2. Extract the ZIP anywhere
3. Double-click `install.cmd`
4. If this is the first time installing BepInEx:
   - The installer will download and install BepInEx
   - Run the game once to let BepInEx initialize
   - Run `install.cmd` again to complete mod installation
5. Configure OpenTrack to output UDP to `127.0.0.1:4242`

The installer automatically finds your game by checking the Windows registry for your Steam installation and scanning all Steam library folders. If it can't find the game, either:
- Set the `OBRA_DINN_PATH` environment variable to your game folder
- Run from command prompt: `install.cmd "D:\Games\ObraDinn"`

## Controls

| Key | Action |
|-----|--------|
| **Home** | Recenter (set current head position as neutral) |
| **End** | Toggle head tracking on/off |
| **Insert** | Toggle aim reticle on/off |
| **Page Up** | Toggle positional tracking on/off |

## Configuration

The mod creates a configuration file at `BepInEx/config/com.headtracking.obradinn.cfg` with default settings on first run. Edit it to customize:

### General
| Setting | Default | Description |
|---------|---------|-------------|
| EnabledOnStartup | true | Start with head tracking enabled |
| ShowStartupNotification | true | Show controls notification on startup |
| UnlockFramerate | true | Remove 60 FPS cap |

### UI
| Setting | Default | Description |
|---------|---------|-------------|
| ShowConnectionNotifications | true | Show notifications when OpenTrack connection is lost or restored |
| ShowReticle | true | Show aim reticle when head tracking is active |

### Keybindings
| Setting | Default | Description |
|---------|---------|-------------|
| ToggleKey | End | Key to toggle tracking |
| RecenterKey | Home | Key to recenter |
| ToggleReticleKey | Insert | Key to toggle aim reticle |
| TogglePositionKey | PageUp | Key to toggle positional tracking |

### Network
| Setting | Default | Description |
|---------|---------|-------------|
| UDPPort | 4242 | UDP port for OpenTrack data |

### Sensitivity
| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| YawSensitivity | 1.0 | 0.1-3.0 | Horizontal rotation multiplier |
| PitchSensitivity | 1.0 | 0.1-3.0 | Vertical rotation multiplier |
| RollSensitivity | 1.0 | 0.0-3.0 | Head tilt multiplier |

### Smoothing
| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| Smoothing | 0.0 | 0.0-1.0 | Smoothing factor (higher = smoother but adds latency) |

### Position
| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| PositionEnabled | true | | Enable positional tracking (lean in/out/side-to-side) |
| PositionSensitivityX | 2.0 | 0.0-3.0 | Lateral (left/right) position multiplier |
| PositionSensitivityY | 2.0 | 0.0-3.0 | Vertical (up/down) position multiplier |
| PositionSensitivityZ | 2.0 | 0.0-3.0 | Depth (forward/back) position multiplier |
| PositionLimitX | 0.30 | 0.01-0.5 | Max lateral displacement in meters |
| PositionLimitY | 0.20 | 0.01-0.5 | Max vertical displacement in meters |
| PositionLimitZ | 0.40 | 0.01-0.5 | Max depth displacement in meters |
| PositionSmoothing | 0.15 | 0.0-1.0 | Position smoothing factor |

### Neck Model
| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| NeckModelEnabled | true | | Simulate head rotating around neck pivot |
| NeckModelHeight | 0.10 | 0.0-0.25 | Vertical distance from neck to eyes (meters) |
| NeckModelForward | 0.05 | 0.0-0.20 | Forward distance from neck to eyes (meters) |

## OpenTrack Setup

1. Install [OpenTrack](https://github.com/opentrack/opentrack) and configure any compatible tracker as input (smartphone apps, webcam-based tracking, dedicated hardware, etc.)
2. Set output to **UDP over network**
3. Configure remote IP: `127.0.0.1` and port: `4242`
4. Start tracking before launching the game

### Phone App Setup

This mod includes built-in smoothing to handle network jitter, so if your tracking app already provides a filtered signal, you can send directly from your phone to the mod on port 4242 without needing OpenTrack on PC.

1. Install an OpenTrack-compatible head tracking app from your phone's app store
2. Configure your phone app to send to your PC's IP address on port 4242 (run `ipconfig` to find it, e.g. `192.168.1.100`)
3. Set the protocol to OpenTrack/UDP
4. Start tracking

**With OpenTrack (optional):** If you experience jerky motion, want curve mapping, or want a visual preview, route through OpenTrack instead. The mod already listens on port 4242, so OpenTrack's input must use a different port:
1. In OpenTrack, set Input to **UDP over network** on port **5252** (or any port other than 4242)
2. Set Output to **UDP over network** at `127.0.0.1:4242`
3. In your phone app, send to your PC's IP on port **5252** (matching OpenTrack's input port)
4. Make sure port 5252 is open in your PC's firewall for incoming UDP traffic

## Verifying Installation

1. Start OpenTrack and enable tracking
2. Launch Return of the Obra Dinn
3. Once in-game, move your head - the camera should follow
4. Press **Home** to recenter if needed

Check `BepInEx/LogOutput.log` (or the console if the game is running) for:
```
[Info   :   BepInEx] Loading [Obra Dinn Head Tracking 1.0.0]
[Info   :Obra Dinn Head Tracking] Obra Dinn Head Tracking v1.0.0 initializing...
[Info   :Obra Dinn Head Tracking] Listening on UDP port 4242
```

## Troubleshooting

### "Player type not found" error

This indicates a game version mismatch. The mod uses reflection to access game internals. If the game updated, the mod may need updates.

### BepInEx not loading

1. Ensure `winhttp.dll` exists in the game folder
2. Try running the game as administrator once
3. Verify the game isn't running from a path with special characters

### Camera not responding

1. Verify OpenTrack is running and tracking is active
2. Check UDP output is set to `127.0.0.1:4242`
3. Press **End** to make sure tracking is enabled
4. Press **Home** to recenter
5. Check firewall isn't blocking UDP port 4242

## Updating

1. Download the new release
2. Run `install.cmd` again - it will update the mod files

## Uninstallation

Run `uninstall.cmd` from the release folder. This removes the mod and only removes BepInEx if it was installed by this mod. To force remove BepInEx even with other plugins present:
```
uninstall.cmd /force
```

## Building from Source

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (any recent version)
- [pixi](https://pixi.sh) task runner
- Return of the Obra Dinn installed (for Unity/BepInEx DLL references)

### Build Steps

```bash
# Clone with submodules
git clone --recurse-submodules https://github.com/itsloopyo/obra-dinn-headtracking.git
cd obra-dinn-headtracking

# Build and install
pixi run install

# Or just build
pixi run build

# Package for release
pixi run package
```

### Available Commands

| Command | Description |
|---------|-------------|
| `pixi run build` | Build the mod (Release configuration) |
| `pixi run install` | Build and install to game directory |
| `pixi run uninstall` | Remove the mod from the game |
| `pixi run uninstall -- --force` | Remove the mod and BepInEx |
| `pixi run package` | Create release ZIP |
| `pixi run clean` | Clean build artifacts |
| `pixi run release` | Version bump, build, tag, and push |

## License

MIT License - see [LICENSE](LICENSE) for details.

## Credits

- [Lucas Pope](https://dukope.com/) - Return of the Obra Dinn
- [BepInEx](https://github.com/BepInEx/BepInEx) - Unity modding framework
- [OpenTrack](https://github.com/opentrack/opentrack) - Head tracking software (UDP protocol and Accela filter)
- [Harmony](https://github.com/pardeike/Harmony) - Runtime patching library
