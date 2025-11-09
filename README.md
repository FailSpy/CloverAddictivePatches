# CloverAddictivePatches

A collection of many toggleable quality-of-life improvements for CloverPit. Tweak FOV, skip repetitive dialogues, peek into drawers, and smooth out rough edges.

## What's Included

### Camera & FOV
- **Adjustable FOV**: Configurable in **Mod Options**, Accessibility, or through F1/F2 keys (60-110°)
- **Improved Dolly Zoom**: Scales naturally with your FOV preference
- **Main Menu Camera Fix**: Camera stays in free cam instead of snapping to drawer

### Speedrunner-Friendly
- **Skip Repetitive Warnings**: "1 round left" warnings and "welcome back" dialogues
- **Instant Restart**: No camera animations when manually restarting
- **Skip Trapdoor Warnings**: Skips trapdoor shake cutscene
- **Reduced Skip Delays**: Dialogues/cutscenes skippable faster (0.5s → 0.1s)
- **Extended Transition Speeds**: Max speed increased to 16x (up from 4x)
- **Skip Intro**: Bypass startup sequences

### Inventory & Drawers
- **Drawer Peek**: Hover over drawers to see them open slightly
- **Quick Swap**: "Swap with [Item]" options in equipped powerup menus
- **Smart Deposit**: Hold Shift at ATM for rapid deposits—automatically stops before critical thresholds
- **Quiet Drawers**: No horror sounds/effects when opening skeleton parts

### Accessibility
- **No Vertigo Effects**: Removes dolly zoom, FOV changes, and falling animations
- **Flashing Lights Reduction**: Toggle available in accessibility menu
- **Controller Disconnect Fix**: Prevents crashes when controller unplugged

### Other
- **ATM Cutscene Freeroam**: Free movement and camera control during ATM/interests cutscenes
- **New Run Confirmation**: Prevents accidental progress loss
- **Memory Card Menu Access**: Open main menu during card selection (quit without consuming a card)
- **In-Game Mod Options**: Configure patches on the fly from Main Menu → Settings

All features are toggleable. Most can be configured in-game via the new **Mod Options** menu (under Main Menu → Settings).

## Installation

### First Time Setup

1. **Install BepInEx**
   - Download [BepInEx 5 (x64)](https://github.com/BepInEx/BepInEx/releases) (get the `BepInEx_x64_*.zip`)
   - Extract the zip into your CloverPit game folder (where `CloverPit.exe` is)
   - Run the game once, then close it (this generates BepInEx folders)

2. **Install the Mod**
   - Download the latest `CloverAddictivePatches.dll` from [Releases](../../releases)
   - Place it in `BepInEx/plugins/` (create the `plugins` folder if it doesn't exist)

3. **Launch and Configure**
   - Start CloverPit
   - Open Main Menu (Esc) → Settings → **Mod Options** (new menu at the bottom)
   - Toggle patches as desired

### Configuration

**Recommended:** Use the in-game **Mod Options** menu (Main Menu → Settings). Changes apply immediately.

**Advanced:** Edit `BepInEx/config/io.github.failspy.qualityclover.cfg` directly. Requires game restart.

**Default FOV:** Set your preferred FOV in the config file (`PlayerFOV = 80` by default). F1/F2 keys adjust from there.

## Troubleshooting

- **Mod not loading?** Check `BepInEx/LogOutput.log` for errors. Make sure BepInEx initialized (you should see a console window on launch).
- **Specific patch not working?** Verify it's enabled in **Mod Options** or the config file.
- **Config changes not applying?** In-game **Mod Options** changes apply live. Manual config edits require restart.

## Building From Source

### Linux / WSL

If you want to compile the mod yourself:

1. Clone this repo
2. Install Mono (if not already installed): `sudo apt install mono-mcs` (Debian/Ubuntu) or `sudo pacman -S mono` (Arch)
3. Run `./compile.sh`

The script will automatically detect your CloverPit installation by searching Steam library folders. If auto-detection fails, set the `CLOVERPIT_DIR` environment variable:

```bash
export CLOVERPIT_DIR="/path/to/CloverPit"
./compile.sh
```

Or as a one-liner:
```bash
CLOVERPIT_DIR="/path/to/CloverPit" ./compile.sh
```

### Windows

A native Windows build script (`compile.bat`) is provided for compiling without WSL:

1. Clone this repo
2. Install Visual Studio Build Tools or the .NET SDK (for `csc.exe`)
3. Run `compile.bat` from Command Prompt or PowerShell

The batch script auto-detects your CloverPit installation. If needed, override with:
```cmd
set CLOVERPIT_DIR=C:\path\to\CloverPit
compile.bat
```

The script compiles all patches and copies the DLL to `BepInEx/plugins/`.
