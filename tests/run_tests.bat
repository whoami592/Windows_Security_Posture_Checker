@echo off
setlocal
cd /d "%~dp0.."
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" exit /b 1
if not exist bin mkdir bin
"%CSC%" /nologo /target:exe /langversion:5 /out:bin\ReportTests.exe /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll src\Models.cs src\Export.cs tests\Tests.cs
if errorlevel 1 exit /b 1
bin\ReportTests.exe
if errorlevel 1 exit /b 1
powershell.exe -NoProfile -NonInteractive -Command "$t=$null;$e=$null;[void][System.Management.Automation.Language.Parser]::ParseFile((Join-Path (Get-Location) 'src\Collect.ps1'),[ref]$t,[ref]$e); if($e.Count){$e | Format-List;exit 1}else{'PASS: PowerShell parser'}"
if errorlevel 1 exit /b 1
echo All local checks passed.
pause
