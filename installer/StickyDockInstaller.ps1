<#
.SYNOPSIS
    Installs or uninstalls Sticky Dock Utility for the current user (E8-CA5).
#>
[CmdletBinding()]
param(
    [ValidateSet('Install', 'Uninstall')]
    [string]$Action = 'Install',

    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'StickyDock'),

    [switch]$AutoStart,

    [string]$RegistryRunPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run',

    [string]$UninstallRegistryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\StickyDock',

    [string]$StartMenuDir = ([Environment]::GetFolderPath('Programs')),

    [string]$SourceDir = (Join-Path $PSScriptRoot 'app')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RegistryPath {
    param([string]$Path)

    if ($Path -match '^HKCU\\') {
        return 'HKCU:\' + $Path.Substring(5)
    }

    return $Path
}

function Test-IsDefaultInstallDir {
    param([string]$TargetDir)

    $defaultDir = [System.IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'StickyDock'))
    $resolvedDir = [System.IO.Path]::GetFullPath($TargetDir)
    return $resolvedDir.Equals($defaultDir, [System.StringComparison]::OrdinalIgnoreCase)
}

function Stop-StickyDockIfRealInstall {
    param([string]$TargetDir)

    if (Test-IsDefaultInstallDir -TargetDir $TargetDir) {
        Get-Process -Name 'StickyDock' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 200
    }
}

$runPath = Resolve-RegistryPath -Path $RegistryRunPath
$uninstallPath = Resolve-RegistryPath -Path $UninstallRegistryPath
$shortcutPath = Join-Path $StartMenuDir 'Sticky Dock Utility.lnk'
$runValueName = 'StickyDock'

if ($Action -eq 'Install') {
    if (-not (Test-Path -LiteralPath $SourceDir)) {
        throw "Source directory '$SourceDir' does not exist."
    }

    $sourceExe = Join-Path $SourceDir 'StickyDock.exe'
    if (-not (Test-Path -LiteralPath $sourceExe)) {
        throw "Application executable '$sourceExe' was not found."
    }

    Stop-StickyDockIfRealInstall -TargetDir $InstallDir

    if (-not (Test-Path -LiteralPath $InstallDir)) {
        New-Item -Path $InstallDir -ItemType Directory -Force | Out-Null
    }

    Copy-Item -Path (Join-Path $SourceDir '*') -Destination $InstallDir -Recurse -Force

    $installedScript = Join-Path $InstallDir 'StickyDockInstaller.ps1'
    if (-not [System.IO.Path]::GetFullPath($PSCommandPath).Equals([System.IO.Path]::GetFullPath($installedScript), [System.StringComparison]::OrdinalIgnoreCase)) {
        Copy-Item -LiteralPath $PSCommandPath -Destination $installedScript -Force
    }

    $installedExe = Join-Path $InstallDir 'StickyDock.exe'

    if (-not (Test-Path -LiteralPath $StartMenuDir)) {
        New-Item -Path $StartMenuDir -ItemType Directory -Force | Out-Null
    }

    $shell = New-Object -ComObject WScript.Shell
    try {
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $installedExe
        $shortcut.WorkingDirectory = $InstallDir
        $shortcut.IconLocation = "$installedExe,0"
        $shortcut.Description = 'Sticky Dock Utility'
        $shortcut.Save()
    }
    finally {
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($shell)
    }

    New-Item -Path $uninstallPath -Force | Out-Null
    Set-ItemProperty -Path $uninstallPath -Name 'DisplayName' -Value 'Sticky Dock Utility'
    Set-ItemProperty -Path $uninstallPath -Name 'DisplayVersion' -Value '1.0.0'
    Set-ItemProperty -Path $uninstallPath -Name 'Publisher' -Value 'Sticky Dock Utility'
    Set-ItemProperty -Path $uninstallPath -Name 'InstallLocation' -Value $InstallDir
    Set-ItemProperty -Path $uninstallPath -Name 'DisplayIcon' -Value $installedExe
    Set-ItemProperty -Path $uninstallPath -Name 'UninstallString' -Value "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$installedScript`" -Action Uninstall"
    Set-ItemProperty -Path $uninstallPath -Name 'NoModify' -Value 1 -Type DWord
    Set-ItemProperty -Path $uninstallPath -Name 'NoRepair' -Value 1 -Type DWord

    if ($AutoStart.IsPresent) {
        New-Item -Path $runPath -Force | Out-Null
        Set-ItemProperty -Path $runPath -Name $runValueName -Value "`"$installedExe`""
    }
    else {
        if (Test-Path -LiteralPath $runPath) {
            Remove-ItemProperty -Path $runPath -Name $runValueName -ErrorAction SilentlyContinue
        }
    }

    Write-Output "Sticky Dock Utility installed at '$InstallDir'."
}
elseif ($Action -eq 'Uninstall') {
    Stop-StickyDockIfRealInstall -TargetDir $InstallDir

    if (Test-Path -LiteralPath $runPath) {
        Remove-ItemProperty -Path $runPath -Name $runValueName -ErrorAction SilentlyContinue
    }

    if (Test-Path -LiteralPath $shortcutPath) {
        Remove-Item -LiteralPath $shortcutPath -Force
    }

    if (Test-Path -LiteralPath $uninstallPath) {
        Remove-Item -LiteralPath $uninstallPath -Recurse -Force
    }

    if (Test-Path -LiteralPath $InstallDir) {
        Remove-Item -LiteralPath $InstallDir -Recurse -Force
    }

    Write-Output 'Sticky Dock Utility uninstalled.'
}
