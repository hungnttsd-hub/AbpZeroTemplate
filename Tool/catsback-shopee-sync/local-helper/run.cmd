@echo off
setlocal

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0ensure-local-helper.ps1"
set "CB_EXIT_CODE=%ERRORLEVEL%"

echo.
if "%CB_EXIT_CODE%"=="0" (
  echo Done. Local Helper is running with the current code.
) else (
  echo Failed. Review the error above.
)

if /i not "%~1"=="--no-pause" pause
exit /b %CB_EXIT_CODE%
