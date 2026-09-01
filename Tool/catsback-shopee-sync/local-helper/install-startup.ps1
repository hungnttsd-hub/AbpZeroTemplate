$ErrorActionPreference = "Stop"
$TaskName = "CatsBack Shopee Sync Helper"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$CmdPath = Join-Path $ScriptDir "start-helper.cmd"

$Action = New-ScheduledTaskAction -Execute "cmd.exe" -Argument "/c `"$CmdPath`"" -WorkingDirectory $ScriptDir
$Trigger = New-ScheduledTaskTrigger -AtLogOn
$Settings = New-ScheduledTaskSettingsSet -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 5) -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskName $TaskName -Action $Action -Trigger $Trigger -Settings $Settings -Description "Watches Shopee conversion report downloads and uploads them to CatsBack using short-lived machine tokens." -Force | Out-Null
Write-Host "Installed scheduled task: $TaskName"
