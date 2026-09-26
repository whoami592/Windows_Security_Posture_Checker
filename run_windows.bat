@echo off
cd /d "%~dp0"
if not exist bin\SecurityPostureChecker.exe (
  echo Build first by running build_windows.bat, or open the project in Visual Studio.
  pause
  exit /b 1
)
start "" "bin\SecurityPostureChecker.exe"
