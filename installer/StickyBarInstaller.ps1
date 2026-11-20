<#
.SYNOPSIS
    Installs or uninstalls StickyBar for the current user (E8-CA5).
#>
[CmdletBinding()]
param(
    [ValidateSet('Install', 'Uninstall')]
    [string]$Action = 'Install',

    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'StickyBar'),

    [switch]$AutoStart,

    [string]$RegistryRunPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run',

    [string]$UninstallRegistryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\StickyBar',

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

    $defaultDir = [System.IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'StickyBar'))
    $resolvedDir = [System.IO.Path]::GetFullPath($TargetDir)
    return $resolvedDir.Equals($defaultDir, [System.StringComparison]::OrdinalIgnoreCase)
}

function Stop-StickyBarIfRealInstall {
    param([string]$TargetDir)

    if (Test-IsDefaultInstallDir -TargetDir $TargetDir) {
        Get-Process -Name 'StickyBar' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 200
    }
}

$runPath = Resolve-RegistryPath -Path $RegistryRunPath
$uninstallPath = Resolve-RegistryPath -Path $UninstallRegistryPath
$shortcutPath = Join-Path $StartMenuDir 'StickyBar.lnk'
$runValueName = 'StickyBar'

if ($Action -eq 'Install') {
    if (-not (Test-Path -LiteralPath $SourceDir)) {
        throw "Source directory '$SourceDir' does not exist."
    }

    $sourceExe = Join-Path $SourceDir 'StickyBar.exe'
    if (-not (Test-Path -LiteralPath $sourceExe)) {
        throw "Application executable '$sourceExe' was not found."
    }

    Stop-StickyBarIfRealInstall -TargetDir $InstallDir

    if (-not (Test-Path -LiteralPath $InstallDir)) {
        New-Item -Path $InstallDir -ItemType Directory -Force | Out-Null
    }

    Copy-Item -Path (Join-Path $SourceDir '*') -Destination $InstallDir -Recurse -Force

    $installedScript = Join-Path $InstallDir 'StickyBarInstaller.ps1'
    if (-not [System.IO.Path]::GetFullPath($PSCommandPath).Equals([System.IO.Path]::GetFullPath($installedScript), [System.StringComparison]::OrdinalIgnoreCase)) {
        Copy-Item -LiteralPath $PSCommandPath -Destination $installedScript -Force
    }

    $installedExe = Join-Path $InstallDir 'StickyBar.exe'

    if (-not (Test-Path -LiteralPath $StartMenuDir)) {
        New-Item -Path $StartMenuDir -ItemType Directory -Force | Out-Null
    }

    $shell = New-Object -ComObject WScript.Shell
    try {
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $installedExe
        $shortcut.WorkingDirectory = $InstallDir
        $shortcut.IconLocation = "$installedExe,0"
        $shortcut.Description = 'StickyBar'
        $shortcut.Save()
    }
    finally {
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($shell)
    }

    New-Item -Path $uninstallPath -Force | Out-Null
    Set-ItemProperty -Path $uninstallPath -Name 'DisplayName' -Value 'StickyBar'
    Set-ItemProperty -Path $uninstallPath -Name 'DisplayVersion' -Value '1.0.0'
    Set-ItemProperty -Path $uninstallPath -Name 'Publisher' -Value 'StickyBar'
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

    Write-Output "StickyBar installed at '$InstallDir'."
}
elseif ($Action -eq 'Uninstall') {
    Stop-StickyBarIfRealInstall -TargetDir $InstallDir

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

    Write-Output 'StickyBar uninstalled.'
}
