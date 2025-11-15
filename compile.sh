#!/bin/bash

# ====================================================================
# CloverAddictivePatches Build Script
# ====================================================================
# UTILITY FILES (Always included)
# ====================================================================
UTILITY_FILES=(
    "Utilities/ReflectionCache.cs"
    "Utilities/CameraAccessors.cs"
    "Utilities/DeathHandlingUtils.cs"
    "Utilities/MenuHelpers.cs"
    "Utilities/ItemPrimitives.cs"
)

# ====================================================================
# PATCH FILE SELECTION
# ====================================================================
# To exclude specific patch files from compilation, comment them out below.
# This is useful for quick debugging/testing during development.
# For runtime toggling, use the config file (BepInEx/config/io.github.failspy.qualityclover.cfg)

PATCH_FILES=(
    # Core/Debug
    "Patches/Debug.cs"
    # "Patches/DebugBlackScreenTest.cs"
    "Patches/SkipIntro.cs"

    # Camera & FOV
    "Patches/CameraUtils.cs"
    "Patches/MainMenuCameraFix.cs"

    # Quality of Life (Alphabetical)
    "Patches/ATMCutsceneFreeroamPatch.cs"          # ATM and interests cutscene freeroam
    "Patches/ControllerFix.cs"
    "Patches/DrawerPeek.cs"
    "Patches/ExtendedTransitionSpeeds.cs"          # Renamed from TransitionSpeedIncrease
    "Patches/FastInterestsPatch.cs"                # Fast interests (skip trapdoor)
    "Patches/InstantRestartPatch.cs"               # Instant restart on death
    "Patches/InventoryDrawerSwap.cs"
    "Patches/MainMenuAdditions.cs"
    "Patches/MemoryCardMenuAccess.cs"              # Renamed from DeckBoxMenuFix
    "Patches/NewRunConfirmation.cs"
    "Patches/QuietDrawersPatch.cs"                 # Quiet drawer opening (no corpse horror)
    "Patches/ReduceSkipDelays.cs"
    "Patches/ReducedMotion.cs"                     # Reduced Motion accessibility patch
    "Patches/SkipRepetitiveWarnings.cs"            # Skip repetitive warnings and restart anecdotes
    "Patches/SmartDeposit.cs"
    "Patches/NoVertigoInducersPatch.cs"            # No vertigo-inducing effects
)

# Example: To exclude a patch during development:
# Comment it out like this:
#   # "Patches/MainMenuAdditions.cs"

# ====================================================================
# BUILD CONFIGURATION
# ====================================================================

# Function to find Steam library folders
find_steam_libraries() {
    local steam_paths=(
        "$HOME/.steam/steam"
        "$HOME/.local/share/Steam"
        "/mnt/c/Program Files (x86)/Steam"  # WSL
    )

    local library_folders=()

    for steam_path in "${steam_paths[@]}"; do
        if [ -f "$steam_path/steamapps/libraryfolders.vdf" ]; then
            # Parse VDF file to find library paths
            while IFS= read -r line; do
                if [[ $line =~ \"path\"[[:space:]]*\"([^\"]+)\" ]]; then
                    library_folders+=("${BASH_REMATCH[1]}/steamapps/common")
                fi
            done < "$steam_path/steamapps/libraryfolders.vdf"

            # Add default steamapps
            library_folders+=("$steam_path/steamapps/common")
        fi
    done

    printf '%s\n' "${library_folders[@]}"
}

# Function to find CloverPit game directory
find_game_dir() {
    # Check if CLOVERPIT_DIR environment variable is set
    if [ -n "$CLOVERPIT_DIR" ]; then
        if [ -d "$CLOVERPIT_DIR" ]; then
            echo "$CLOVERPIT_DIR"
            return 0
        else
            echo "Warning: CLOVERPIT_DIR is set but directory doesn't exist: $CLOVERPIT_DIR" >&2
        fi
    fi

    # Search Steam libraries
    while IFS= read -r library; do
        if [ -d "$library/CloverPit" ]; then
            echo "$library/CloverPit"
            return 0
        fi
    done < <(find_steam_libraries)

    return 1
}

# Auto-detect or use environment variable
if ! GAME_DIR=$(find_game_dir); then
    echo "==============================================="
    echo "ERROR: Could not find CloverPit installation!"
    echo "==============================================="
    echo ""
    echo "Please set the CLOVERPIT_DIR environment variable:"
    echo "  export CLOVERPIT_DIR=\"/path/to/CloverPit\""
    echo ""
    echo "Or add it to this script, or run with:"
    echo "  CLOVERPIT_DIR=\"/path/to/CloverPit\" ./comp.sh"
    echo ""
    exit 1
fi

MANAGED_DIR="$GAME_DIR/CloverPit_Data/Managed"
BEPINEX_DIR="$GAME_DIR/BepInEx"

# Verify required directories exist
if [ ! -d "$MANAGED_DIR" ]; then
    echo "ERROR: Managed directory not found: $MANAGED_DIR" >&2
    exit 1
fi

if [ ! -d "$BEPINEX_DIR" ]; then
    echo "ERROR: BepInEx directory not found: $BEPINEX_DIR" >&2
    echo "Make sure BepInEx is installed in the game directory." >&2
    exit 1
fi

# ====================================================================
# COMPILATION
# ====================================================================
echo "==============================================="
echo "Compiling CloverAddictivePatches.dll"
echo "==============================================="
echo "Game directory: $GAME_DIR"
echo "Utilities: ${#UTILITY_FILES[@]} file(s)"
echo "Patches: ${#PATCH_FILES[@]} file(s)"
echo ""

mcs -target:library \
    -r:$BEPINEX_DIR/core/BepInEx.dll \
    -r:$BEPINEX_DIR/core/0Harmony.dll \
    -r:$MANAGED_DIR/Assembly-CSharp.dll \
    -r:$MANAGED_DIR/Assembly-CSharp-firstpass.dll \
    -r:$MANAGED_DIR/UnityEngine.CoreModule.dll \
    -r:$MANAGED_DIR/UnityEngine.dll \
    -r:$MANAGED_DIR/UnityEngine.PhysicsModule.dll \
    -r:$MANAGED_DIR/UnityEngine.UI.dll \
    -r:$MANAGED_DIR/UnityEngine.UIModule.dll \
    -r:$MANAGED_DIR/UnityEngine.InputLegacyModule.dll \
    -r:$MANAGED_DIR/Unity.TextMeshPro.dll \
    -r:$MANAGED_DIR/netstandard.dll \
    -r:$MANAGED_DIR/UniTask.dll \
    -r:$MANAGED_DIR/Rewired_Core.dll \
    -r:$MANAGED_DIR/System.Numerics.dll \
    -out:CloverAddictivePatches.dll \
    Plugin.cs \
    "${UTILITY_FILES[@]}" \
    "${PATCH_FILES[@]}"

if [ $? -eq 0 ]; then
    echo ""
    echo "==============================================="
    echo "Compilation successful!"
    echo "==============================================="
    echo "Copying to BepInEx plugins folder..."
    mkdir "$BEPINEX_DIR/plugins" 2>/dev/null
    cp CloverAddictivePatches.dll $BEPINEX_DIR/plugins/
    echo "Done!"
else
    echo ""
    echo "==============================================="
    echo "Compilation failed!"
    echo "==============================================="
    exit 1
fi
