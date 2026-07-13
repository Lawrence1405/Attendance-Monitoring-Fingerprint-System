@echo off
setlocal enabledelayedexpansion

REM ════════════════════════════════════════════════════════════════
REM  FPTester — Build Script  (.NET 10 / x86)
REM ════════════════════════════════════════════════════════════════

cd /d "%~dp0"
echo.
echo Working directory: %CD%
echo.

where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] dotnet.exe not found on PATH.
    echo         Install .NET 10 SDK from https://dotnet.microsoft.com/download
    pause & exit /b 1
)

echo dotnet version:
dotnet --version
echo.

if not exist "FPTester.csproj" (
    echo [ERROR] FPTester.csproj not found in %CD%
    echo         Make sure ALL project files are in the same folder as build.bat
    pause & exit /b 1
)

echo [1/3] Restoring NuGet packages...
dotnet restore FPTester.csproj
if %ERRORLEVEL% NEQ 0 ( echo. & echo [ERROR] Restore failed. & pause & exit /b 1 )

echo.
echo [2/3] Building Release x86...
dotnet build FPTester.csproj -c Release -r win-x86 --no-restore
if %ERRORLEVEL% NEQ 0 ( echo. & echo [ERROR] Build failed. & pause & exit /b 1 )

echo.
echo [3/3] Publishing single-file executable...
dotnet publish FPTester.csproj ^
    -c Release ^
    -r win-x86 ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%~dp0publish"
if %ERRORLEVEL% NEQ 0 ( echo. & echo [ERROR] Publish failed. & pause & exit /b 1 )

REM ── Copy ftrScanAPI.dll next to the exe ──────────────────────────
set "FTRDLL=C:\Users\Dale Barro\Downloads\FS6x_Enrollment_Kit_2025.11.06\FS6x_Enrollment_Kit_2025.11.06\ftrScanAPI.dll"

echo.
if exist "!FTRDLL!" (
    echo Copying ftrScanAPI.dll to publish\...
    copy /Y "!FTRDLL!" "%~dp0publish\ftrScanAPI.dll" >nul
    echo   Done.
) else (
    echo [WARNING] ftrScanAPI.dll not found at:
    echo   !FTRDLL!
    echo   Manually copy ftrScanAPI.dll from the Enrollment Kit
    echo   into the publish\ folder next to FPTester.exe.
)

echo.
if exist "%~dp0publish\FPTester.exe" (
    echo ════════════════════════════════════════════════════
    echo  BUILD SUCCESSFUL
    echo  EXE: %~dp0publish\FPTester.exe
    echo  DLL: %~dp0publish\ftrScanAPI.dll
    echo ════════════════════════════════════════════════════
    explorer "%~dp0publish"
) else (
    echo [ERROR] FPTester.exe was not created.
)

pause
