<#
.SYNOPSIS
    Publishes StickyBar and builds the distributable ZIP installer package in dist/ (E8-CA5).
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$distDir = Join-Path $repoRoot 'dist'
$packageDir = Join-Path $distDir 'StickyBar-Package'
$appDir = Join-Path $packageDir 'app'
$zipPath = Join-Path $distDir 'StickyBar-Setup-win-x64.zip'
$projectPath = Join-Path $repoRoot 'src\StickyBar.App\StickyBar.App.csproj'

if (Test-Path -LiteralPath $packageDir) {
    Remove-Item -LiteralPath $packageDir -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -Path $appDir -ItemType Directory -Force | Out-Null

& dotnet publish $projectPath -c Release -r win-x64 --self-contained false -o $appDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'StickyBarInstaller.ps1') -Destination $packageDir -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Instalar-StickyBar.bat') -Destination $packageDir -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Desinstalar-StickyBar.bat') -Destination $packageDir -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs\MANUAL-USUARIO.md') -Destination $packageDir -Force

Compress-Archive -Path (Join-Path $packageDir '*') -DestinationPath $zipPath -Force

Write-Output "Package generated at '$zipPath'."
