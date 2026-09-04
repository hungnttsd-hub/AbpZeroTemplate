[CmdletBinding()]
param(
  [switch]$Elevated
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$TaskName = "CatsBack Shopee Sync Helper"
$HelperDir = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
$EntryPoint = Join-Path $HelperDir "index.js"
$PackagePath = Join-Path $HelperDir "package.json"
$HiddenLauncher = Join-Path $HelperDir "start-helper-hidden.vbs"
$LogPath = Join-Path $HelperDir "logs\helper.log"
$DefaultPort = 32145

function Write-Step {
  param([int]$Number, [string]$Message)
  Write-Host "[$Number/5] $Message" -ForegroundColor Cyan
}

function Get-ConfiguredPort {
  param([string]$Directory)

  $configPath = Join-Path $Directory "config.json"
  if (-not (Test-Path -LiteralPath $configPath)) {
    return $DefaultPort
  }

  try {
    $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    $settingsPortProperty = $config.PSObject.Properties["settingsPort"]
    if ($null -ne $settingsPortProperty -and $null -ne $settingsPortProperty.Value) {
      $port = [int]$settingsPortProperty.Value
      if ($port -lt 1 -or $port -gt 65535) {
        throw "settingsPort must be between 1 and 65535."
      }
      return $port
    }
  }
  catch {
    throw "Cannot read $configPath. $($_.Exception.Message)"
  }

  return $DefaultPort
}

function Get-ListenerProcessIds {
  param([int]$Port)

  if (Get-Command Get-NetTCPConnection -ErrorAction SilentlyContinue) {
    return @(
      Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    )
  }

  $escapedPort = [regex]::Escape([string]$Port)
  $processIds = foreach ($line in (& "$env:WINDIR\System32\netstat.exe" -ano -p tcp 2>$null)) {
    if ($line -match "^\s*TCP\s+\S+:$escapedPort\s+\S+\s+LISTENING\s+(\d+)\s*$") {
      [int]$Matches[1]
    }
  }
  return @($processIds | Select-Object -Unique)
}

function Get-HelperHealth {
  param([int]$Port)

  try {
    return Invoke-RestMethod -Uri "http://127.0.0.1:$Port/health" -Method Get -TimeoutSec 2
  }
  catch {
    return $null
  }
}

function Test-HelperHealth {
  param($Health)

  if ($null -eq $Health) {
    return $false
  }

  $propertyNames = @($Health.PSObject.Properties.Name)
  if ($propertyNames -notcontains "ok" -or
    $propertyNames -notcontains "version" -or
    $propertyNames -notcontains "watchDir") {
    return $false
  }

  return ($Health.ok -eq $true)
}

function Wait-ForPortToClose {
  param([int]$Port, [int]$TimeoutSeconds = 8)

  $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
  do {
    if (@(Get-ListenerProcessIds -Port $Port).Count -eq 0) {
      return $true
    }
    Start-Sleep -Milliseconds 250
  } while ([DateTime]::UtcNow -lt $deadline)

  return $false
}

function Stop-HelperOnPort {
  param([int]$Port)

  $processIds = @(Get-ListenerProcessIds -Port $Port)
  if ($processIds.Count -eq 0) {
    return @()
  }

  $health = Get-HelperHealth -Port $Port
  if (-not (Test-HelperHealth -Health $health)) {
    throw "Port $Port is occupied by PID(s) $($processIds -join ', '), but it is not a recognizable CatsBack Local Helper. It was not stopped."
  }

  foreach ($processId in $processIds) {
    $process = Get-Process -Id $processId -ErrorAction Stop
    if ($process.ProcessName -ine "node") {
      throw "Port $Port belongs to $($process.ProcessName) (PID $processId), not node.exe. It was not stopped."
    }
  }

  foreach ($processId in $processIds) {
    Stop-Process -Id $processId -Force -ErrorAction Stop
  }

  if (-not (Wait-ForPortToClose -Port $Port)) {
    throw "Port $Port did not close after stopping PID(s) $($processIds -join ', ')."
  }

  return $processIds
}

function Resolve-HelperDirectoryFromTask {
  param($Task)

  if ($null -eq $Task) {
    return $null
  }

  foreach ($action in @($Task.Actions)) {
    $candidates = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace([string]$action.WorkingDirectory)) {
      $candidates.Add([string]$action.WorkingDirectory)
    }

    $arguments = [string]$action.Arguments
    if (-not [string]::IsNullOrWhiteSpace($arguments)) {
      $candidates.Add($arguments.Trim().Trim('"'))
      foreach ($match in [regex]::Matches($arguments, '"([^\"]+)"')) {
        $candidates.Add($match.Groups[1].Value)
      }
      foreach ($part in ($arguments -split '\s+')) {
        $candidates.Add($part.Trim().Trim('"'))
      }
    }

    foreach ($candidate in $candidates) {
      if ([string]::IsNullOrWhiteSpace($candidate)) {
        continue
      }

      $expanded = [Environment]::ExpandEnvironmentVariables($candidate)
      if (Test-Path -LiteralPath $expanded -PathType Container) {
        $directory = [IO.Path]::GetFullPath($expanded).TrimEnd('\')
      }
      elseif (Test-Path -LiteralPath $expanded -PathType Leaf) {
        $directory = [IO.Path]::GetFullPath((Split-Path -Parent $expanded)).TrimEnd('\')
      }
      else {
        continue
      }

      if (Test-Path -LiteralPath (Join-Path $directory "index.js") -PathType Leaf) {
        return $directory
      }
    }
  }

  return $null
}

function Copy-RuntimeFileIfMissing {
  param([string]$SourceDirectory, [string]$Name)

  if ([string]::IsNullOrWhiteSpace($SourceDirectory) -or
    $SourceDirectory.Equals($HelperDir, [StringComparison]::OrdinalIgnoreCase)) {
    return
  }

  $source = Join-Path $SourceDirectory $Name
  $destination = Join-Path $HelperDir $Name
  if ((Test-Path -LiteralPath $source -PathType Leaf) -and
    -not (Test-Path -LiteralPath $destination)) {
    Copy-Item -LiteralPath $source -Destination $destination
    Write-Host "      Migrated $Name from the previous helper directory."
  }
}

function Test-TaskDefinition {
  param($Task, [string]$ExpectedArguments)

  if ($null -eq $Task -or [string]$Task.State -eq "Disabled") {
    return $false
  }

  $actions = @($Task.Actions)
  if ($actions.Count -ne 1) {
    return $false
  }

  $expectedWscript = [IO.Path]::GetFullPath((Join-Path $env:WINDIR "System32\wscript.exe"))
  try {
    $actualWscript = [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables([string]$actions[0].Execute))
    $actualWorkingDirectory = [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables([string]$actions[0].WorkingDirectory)).TrimEnd('\')
  }
  catch {
    return $false
  }

  $actionMatches = $actualWscript.Equals($expectedWscript, [StringComparison]::OrdinalIgnoreCase) -and
    ([string]$actions[0].Arguments).Equals($ExpectedArguments, [StringComparison]::Ordinal) -and
    $actualWorkingDirectory.Equals($HelperDir, [StringComparison]::OrdinalIgnoreCase)

  $hasEnabledLogonTrigger = @(
    $Task.Triggers | Where-Object {
      $_.CimClass.CimClassName -eq "MSFT_TaskLogonTrigger" -and $_.Enabled -ne $false
    }
  ).Count -gt 0

  return ($actionMatches -and $hasEnabledLogonTrigger)
}

function Register-HelperTask {
  param([string]$ExpectedArguments)

  $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
  $action = New-ScheduledTaskAction `
    -Execute (Join-Path $env:WINDIR "System32\wscript.exe") `
    -Argument $ExpectedArguments `
    -WorkingDirectory $HelperDir
  $trigger = New-ScheduledTaskTrigger -AtLogOn -User $currentUser
  $principal = New-ScheduledTaskPrincipal `
    -UserId $currentUser `
    -LogonType Interactive `
    -RunLevel Limited
  $settings = New-ScheduledTaskSettingsSet `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -MultipleInstances IgnoreNew `
    -StartWhenAvailable `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries

  Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Description "Runs the CatsBack Shopee Sync Local Helper at Windows logon and restarts it after failures." `
    -Force | Out-Null
}

function Restart-AsAdministrator {
  $powerShellPath = Join-Path $PSHOME "powershell.exe"
  $arguments = "-NoLogo -NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Elevated"
  Write-Host "      Windows administrator permission is required to repair the existing task." -ForegroundColor Yellow
  $process = Start-Process -FilePath $powerShellPath -ArgumentList $arguments -Verb RunAs -Wait -PassThru
  exit $process.ExitCode
}

try {
  Write-Step 1 "Checking the current helper code and Node.js..."
  foreach ($requiredPath in @($EntryPoint, $PackagePath, $HiddenLauncher)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
      throw "Required file was not found: $requiredPath"
    }
  }

  $nodeCommand = Get-Command node.exe -CommandType Application -ErrorAction Stop | Select-Object -First 1
  $nodePath = [IO.Path]::GetFullPath($nodeCommand.Source)
  $nodeVersionText = [string](& $nodePath --version)
  if ($LASTEXITCODE -ne 0) {
    throw "Could not read the Node.js version from $nodePath."
  }

  try {
    $nodeVersion = [version]$nodeVersionText.Trim().TrimStart([char]'v')
  }
  catch {
    throw "Unrecognized Node.js version: $nodeVersionText"
  }
  if ($nodeVersion -lt [version]"18.0.0") {
    throw "Node.js 18+ is required. Found $nodeVersionText at $nodePath."
  }

  $syntaxOutput = @(& $nodePath --check $EntryPoint 2>&1)
  if ($LASTEXITCODE -ne 0) {
    throw "The current index.js failed the syntax check. The running helper was not changed.`n$($syntaxOutput -join [Environment]::NewLine)"
  }

  $package = Get-Content -LiteralPath $PackagePath -Raw | ConvertFrom-Json
  $expectedVersion = [string]$package.version
  if ([string]::IsNullOrWhiteSpace($expectedVersion)) {
    throw "package.json does not contain a version."
  }
  Write-Host "      Code v$expectedVersion; Node $nodeVersionText"

  $existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
  $previousHelperDir = Resolve-HelperDirectoryFromTask -Task $existingTask
  $portsToStop = New-Object System.Collections.Generic.List[int]
  $portsToStop.Add((Get-ConfiguredPort -Directory $HelperDir))
  if (-not [string]::IsNullOrWhiteSpace($previousHelperDir)) {
    $previousPort = Get-ConfiguredPort -Directory $previousHelperDir
    if (-not $portsToStop.Contains($previousPort)) {
      $portsToStop.Add($previousPort)
    }
  }

  Write-Step 2 "Stopping any existing Local Helper instance..."
  if ($null -ne $existingTask -and [string]$existingTask.State -eq "Running") {
    Stop-ScheduledTask -TaskName $TaskName
    foreach ($port in $portsToStop) {
      [void](Wait-ForPortToClose -Port $port -TimeoutSeconds 5)
    }
  }

  $stoppedProcessIds = New-Object System.Collections.Generic.List[int]
  foreach ($port in $portsToStop) {
    foreach ($processId in @(Stop-HelperOnPort -Port $port)) {
      if (-not $stoppedProcessIds.Contains([int]$processId)) {
        $stoppedProcessIds.Add([int]$processId)
      }
    }
  }
  if ($stoppedProcessIds.Count -gt 0) {
    Write-Host "      Stopped PID(s): $($stoppedProcessIds -join ', ')"
  }
  else {
    Write-Host "      No running helper process was found."
  }

  Write-Step 3 "Preserving settings and ensuring Windows startup..."
  Copy-RuntimeFileIfMissing -SourceDirectory $previousHelperDir -Name "config.json"
  Copy-RuntimeFileIfMissing -SourceDirectory $previousHelperDir -Name "state.json"

  $port = Get-ConfiguredPort -Directory $HelperDir
  $expectedArguments = "`"$HiddenLauncher`" `"$nodePath`""
  $taskIsCurrent = Test-TaskDefinition -Task $existingTask -ExpectedArguments $expectedArguments
  if (-not $taskIsCurrent) {
    try {
      Register-HelperTask -ExpectedArguments $expectedArguments
    }
    catch {
      if (-not $Elevated -and $_.Exception.Message -match "(?i)access.*denied|0x80070005") {
        Restart-AsAdministrator
      }
      throw
    }
    Write-Host "      Startup task installed/updated for the current code directory."
  }
  else {
    Write-Host "      Startup task is already current."
  }

  Write-Step 4 "Starting the Local Helper..."
  Start-ScheduledTask -TaskName $TaskName

  Write-Step 5 "Waiting for the health check..."
  $deadline = [DateTime]::UtcNow.AddSeconds(20)
  $health = $null
  $listenerProcessIds = @()
  do {
    Start-Sleep -Milliseconds 400
    $health = Get-HelperHealth -Port $port
    $listenerProcessIds = @(Get-ListenerProcessIds -Port $port)
    if ((Test-HelperHealth -Health $health) -and $listenerProcessIds.Count -eq 1) {
      break
    }
  } while ([DateTime]::UtcNow -lt $deadline)

  if (-not (Test-HelperHealth -Health $health)) {
    $taskState = (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue).State
    throw "The helper did not become healthy on port $port within 20 seconds. Scheduled task state: $taskState."
  }
  if ([string]$health.version -ne $expectedVersion) {
    throw "The running helper reports v$($health.version), but the current code is v$expectedVersion."
  }
  if ($listenerProcessIds.Count -ne 1) {
    throw "Expected exactly one listener on port $port; found $($listenerProcessIds.Count)."
  }

  $runningProcess = Get-Process -Id $listenerProcessIds[0] -ErrorAction Stop
  if ($runningProcess.ProcessName -ine "node") {
    throw "The healthy port belongs to $($runningProcess.ProcessName), not node.exe."
  }

  Write-Host ""
  Write-Host "Local Helper is ready." -ForegroundColor Green
  Write-Host "  Version : $($health.version)"
  Write-Host "  PID     : $($listenerProcessIds[0])"
  Write-Host "  Startup : $TaskName"
  Write-Host "  Health  : http://127.0.0.1:$port/health"
  Write-Host "  Settings: http://127.0.0.1:$port/settings"
  exit 0
}
catch {
  Write-Host ""
  Write-Host "Local Helper setup/start failed:" -ForegroundColor Red
  Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
  if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
    Write-Host ""
    Write-Host "Latest helper log lines:"
    Get-Content -LiteralPath $LogPath -Tail 10 | ForEach-Object { Write-Host "  $_" }
  }
  exit 1
}
