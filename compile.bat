@echo off
setlocal enabledelayedexpansion

REM ====================================================================
REM CloverAddictivePatches v1.0.0 - Windows Build Script
REM ====================================================================
REM This mod uses a shared utility architecture with cleaner patches.
REM Utilities provide reflection caching, camera accessors, death handling,
REM and menu helpers to reduce code duplication and improve maintainability.

REM ====================================================================
REM UTILITY FILES (Always included)
REM ====================================================================
set "UTILITY_FILES=Utilities\ReflectionCache.cs Utilities\CameraAccessors.cs Utilities\DeathHandlingUtils.cs Utilities\MenuHelpers.cs"

REM ====================================================================
REM PATCH FILE SELECTION
REM ====================================================================
REM To exclude specific patch files from compilation, comment them out below.
REM This is useful for quick debugging/testing during development.
REM For runtime toggling, use the config file (BepInEx/config/io.github.failspy.qualityclover.cfg)

set "PATCH_FILES=Patches\Debug.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\SkipIntro.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\CameraUtils.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\MainMenuCameraFix.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\ATMCutsceneFreeroamPatch.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\ControllerFix.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\DrawerPeek.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\ExtendedTransitionSpeeds.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\FastInterestsPatch.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\InstantRestartPatch.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\InventoryDrawerSwap.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\MainMenuAdditions.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\MemoryCardMenuAccess.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\NewRunConfirmation.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\QuietDrawersPatch.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\ReduceSkipDelays.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\SkipRepetitiveWarnings.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\SmartDeposit.cs"
set "PATCH_FILES=%PATCH_FILES% Patches\NoVertigoInducersPatch.cs"

REM ====================================================================
REM BUILD CONFIGURATION - Auto-detect or use CLOVERPIT_DIR
REM ====================================================================

REM Check if CLOVERPIT_DIR is already set
if not "%CLOVERPIT_DIR%"=="" (
    if exist "%CLOVERPIT_DIR%" (
        set "GAME_DIR=%CLOVERPIT_DIR%"
        goto :found_game
    ) else (
        echo Warning: CLOVERPIT_DIR is set but directory doesn't exist: %CLOVERPIT_DIR%
    )
)

REM Auto-detect Steam installation
set "GAME_DIR="

REM Check common Steam locations
for %%P in (
    "C:\Program Files (x86)\Steam\steamapps\common\CloverPit"
    "C:\Program Files\Steam\steamapps\common\CloverPit"
    "%ProgramFiles(x86)%\Steam\steamapps\common\CloverPit"
    "%ProgramFiles%\Steam\steamapps\common\CloverPit"
) do (
    if exist %%P (
        set "GAME_DIR=%%~P"
        goto :found_game
    )
)

REM Parse Steam library folders
set "STEAM_PATH=%ProgramFiles(x86)%\Steam"
if not exist "%STEAM_PATH%" set "STEAM_PATH=%ProgramFiles%\Steam"

if exist "%STEAM_PATH%\steamapps\libraryfolders.vdf" (
    for /f "usebackq tokens=2 delims=^" %%A in (`findstr /C:"\"path\"" "%STEAM_PATH%\steamapps\libraryfolders.vdf"`) do (
        set "line=%%A"
        REM Remove quotes and whitespace
        set "line=!line:~0,-1!"
        set "line=!line:*"=!"
        if exist "!line!\steamapps\common\CloverPit" (
            set "GAME_DIR=!line!\steamapps\common\CloverPit"
            goto :found_game
        )
    )
)

REM If still not found, error out
if "%GAME_DIR%"=="" (
    echo ===============================================
    echo ERROR: Could not find CloverPit installation!
    echo ===============================================
    echo.
    echo Please set the CLOVERPIT_DIR environment variable:
    echo   set CLOVERPIT_DIR=C:\path\to\CloverPit
    echo   comp.bat
    echo.
    echo Or add it to your system environment variables.
    echo.
    exit /b 1
)

:found_game
set "MANAGED_DIR=%GAME_DIR%\CloverPit_Data\Managed"
set "BEPINEX_DIR=%GAME_DIR%\BepInEx"

REM Verify required directories exist
if not exist "%MANAGED_DIR%" (
    echo ERROR: Managed directory not found: %MANAGED_DIR%
    exit /b 1
)

if not exist "%BEPINEX_DIR%" (
    echo ERROR: BepInEx directory not found: %BEPINEX_DIR%
    echo Make sure BepInEx is installed in the game directory.
    exit /b 1
)

REM ====================================================================
REM FIND C# COMPILER
REM ====================================================================

REM Try to find csc.exe in common locations
set "CSC="

REM Check if csc is in PATH
where csc.exe >nul 2>&1
if %errorlevel% equ 0 (
    set "CSC=csc.exe"
    goto :found_compiler
)

REM Check .NET Framework locations
for %%V in (4.8 4.7.2 4.7.1 4.7 4.6.2 4.6.1 4.6 4.5.2 4.5.1 4.5) do (
    if exist "%windir%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
        set "CSC=%windir%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
        goto :found_compiler
    )
)

REM Check Visual Studio locations
for %%Y in (2022 2019 2017) do (
    for %%E in (Community Professional Enterprise) do (
        if exist "%ProgramFiles%\Microsoft Visual Studio\%%Y\%%E\MSBuild\Current\Bin\Roslyn\csc.exe" (
            set "CSC=%ProgramFiles%\Microsoft Visual Studio\%%Y\%%E\MSBuild\Current\Bin\Roslyn\csc.exe"
            goto :found_compiler
        )
        if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\%%Y\%%E\MSBuild\Current\Bin\Roslyn\csc.exe" (
            set "CSC=%ProgramFiles(x86)%\Microsoft Visual Studio\%%Y\%%E\MSBuild\Current\Bin\Roslyn\csc.exe"
            goto :found_compiler
        )
    )
)

echo ===============================================
echo ERROR: C# compiler (csc.exe) not found!
echo ===============================================
echo.
echo Please install one of the following:
echo   - .NET SDK: https://dotnet.microsoft.com/download
echo   - Visual Studio Build Tools: https://visualstudio.microsoft.com/downloads/
echo.
exit /b 1

:found_compiler

REM ====================================================================
REM COMPILATION
REM ====================================================================
echo ===============================================
echo Compiling CloverAddictivePatches.dll v1.0.0
echo ===============================================
echo Game directory: %GAME_DIR%
echo Compiler: %CSC%
echo.

"%CSC%" /target:library ^
    /reference:"%BEPINEX_DIR%\core\BepInEx.dll" ^
    /reference:"%BEPINEX_DIR%\core\0Harmony.dll" ^
    /reference:"%MANAGED_DIR%\Assembly-CSharp.dll" ^
    /reference:"%MANAGED_DIR%\Assembly-CSharp-firstpass.dll" ^
    /reference:"%MANAGED_DIR%\UnityEngine.CoreModule.dll" ^
    /reference:"%MANAGED_DIR%\UnityEngine.dll" ^
    /reference:"%MANAGED_DIR%\UnityEngine.PhysicsModule.dll" ^
    /reference:"%MANAGED_DIR%\UnityEngine.UI.dll" ^
    /reference:"%MANAGED_DIR%\UnityEngine.UIModule.dll" ^
    /reference:"%MANAGED_DIR%\UnityEngine.InputLegacyModule.dll" ^
    /reference:"%MANAGED_DIR%\Unity.TextMeshPro.dll" ^
    /reference:"%MANAGED_DIR%\netstandard.dll" ^
    /reference:"%MANAGED_DIR%\UniTask.dll" ^
    /reference:"%MANAGED_DIR%\Rewired_Core.dll" ^
    /reference:"%MANAGED_DIR%\System.Numerics.dll" ^
    /out:CloverAddictivePatches.dll ^
    Plugin.cs ^
    %UTILITY_FILES% ^
    %PATCH_FILES%

if %errorlevel% equ 0 (
    echo.
    echo ===============================================
    echo Compilation successful!
    echo ===============================================
    echo Copying to BepInEx plugins folder...
    mkdir "%BEPINEX_DIR%\plugins" 2>nul
    copy /Y CloverAddictivePatches.dll "%BEPINEX_DIR%\plugins\" >nul
    echo Done!
) else (
    echo.
    echo ===============================================
    echo Compilation failed!
    echo ===============================================
    exit /b 1
)
