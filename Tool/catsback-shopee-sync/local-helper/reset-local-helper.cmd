@echo off
setlocal

set "CB_TASK_NAME=CatsBack Shopee Sync Helper"
set "CB_HELPER_DIR=E:\HungNT\catsback-shopee-sync\local-helper"
if not "%~1"=="" set "CB_HELPER_DIR=%~f1"
set "CB_ENTRY_POINT=%CB_HELPER_DIR%\index.js"

if not exist "%CB_ENTRY_POINT%" (
  echo [ERROR] Helper entry point was not found: %CB_ENTRY_POINT%
  echo Usage: %~nx0 [helper-directory]
  exit /b 1
)

where node.exe >nul 2>&1
if errorlevel 1 (
  echo [ERROR] node.exe was not found in PATH.
  exit /b 1
)

echo [1/3] Checking the published helper...
node --check "%CB_ENTRY_POINT%"
if errorlevel 1 (
  echo [ERROR] Syntax check failed. The running helper was not changed.
  exit /b 1
)

echo [2/3] Restarting scheduled task "%CB_TASK_NAME%"...
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "$taskName = $env:CB_TASK_NAME;" ^
  "$helperDir = [IO.Path]::GetFullPath($env:CB_HELPER_DIR).TrimEnd('\');" ^
  "$entryPoint = [IO.Path]::GetFullPath($env:CB_ENTRY_POINT);" ^
  "try {" ^
  "  $task = Get-ScheduledTask -TaskName $taskName -ErrorAction Stop;" ^
  "  $action = @($task.Actions)[0];" ^
  "  $actionText = (($action.Execute, $action.Arguments, $action.WorkingDirectory) -join ' ');" ^
  "  if ($actionText.IndexOf($helperDir, [StringComparison]::OrdinalIgnoreCase) -lt 0) { throw ('Scheduled task points somewhere else: ' + $actionText) };" ^
  "  $oldProcesses = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -ieq 'node.exe' -and $_.CommandLine -and $_.CommandLine.IndexOf($entryPoint, [StringComparison]::OrdinalIgnoreCase) -ge 0 });" ^
  "  if ($task.State -eq 'Running') { Stop-ScheduledTask -TaskName $taskName };" ^
  "  foreach ($process in $oldProcesses) { if (Get-Process -Id $process.ProcessId -ErrorAction SilentlyContinue) { Stop-Process -Id $process.ProcessId -Force } };" ^
  "  Start-ScheduledTask -TaskName $taskName;" ^
  "  Start-Sleep -Seconds 3;" ^
  "  $newProcesses = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -ieq 'node.exe' -and $_.CommandLine -and $_.CommandLine.IndexOf($entryPoint, [StringComparison]::OrdinalIgnoreCase) -ge 0 });" ^
  "  if ($newProcesses.Count -ne 1) { throw ('Expected one helper process but found ' + $newProcesses.Count) };" ^
  "  $port = 32145;" ^
  "  $configPath = Join-Path $helperDir 'config.json';" ^
  "  if (Test-Path -LiteralPath $configPath) { $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json; if ($config.settingsPort) { $port = [int]$config.settingsPort } };" ^
  "  $health = Invoke-RestMethod -Uri ('http://127.0.0.1:' + $port + '/health') -Method Get -TimeoutSec 5;" ^
  "  if (-not $health.ok) { throw 'Health endpoint did not report OK.' };" ^
  "  $listener = Get-NetTCPConnection -LocalAddress '127.0.0.1' -LocalPort $port -State Listen -ErrorAction Stop;" ^
  "  if ($listener.OwningProcess -notcontains $newProcesses[0].ProcessId) { throw 'The settings port belongs to another process.' };" ^
  "  Write-Host ('Stopped PID(s): ' + (($oldProcesses.ProcessId -join ', ') -replace '^$', '(none)'));" ^
  "  Write-Host ('Started PID: ' + $newProcesses[0].ProcessId);" ^
  "  Write-Host ('Health: OK on http://127.0.0.1:' + $port + '/health');" ^
  "} catch { Write-Error $_; exit 1 }"

if errorlevel 1 (
  echo [ERROR] Local Helper reset failed.
  exit /b 1
)

echo [3/3] Local Helper reset completed successfully.
exit /b 0
