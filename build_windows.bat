@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo 64-bit .NET Framework compiler not found. Use Visual Studio with .NET Framework 4.8 development tools.
  pause
  exit /b 1
)
if not exist bin mkdir bin
"%CSC%" /nologo /target:winexe /platform:anycpu /langversion:5 /optimize+ /out:bin\SecurityPostureChecker.exe /win32manifest:app.manifest /resource:src\Collect.ps1,Sabaz.Collect.ps1 /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll src\Program.cs src\MainForm.cs src\Models.cs src\Collector.cs src\Export.cs
if errorlevel 1 (
  echo Build failed. Read the compiler errors above.
  pause
  exit /b 1
)
copy /y SecurityPostureChecker.exe.config bin\SecurityPostureChecker.exe.config >nul
echo Built bin\SecurityPostureChecker.exe
pause
