<#
.SYNOPSIS
    Publishes TFactor and builds the installer in one step.

.DESCRIPTION
    Runs `dotnet publish` with the SelfContained profile to produce a single-file TFactor.exe, then compiles
    Installer\TFactor.iss with Inno Setup to produce TFactorSetup.exe. Looks for ISCC.exe on PATH first, then
    falls back to the usual Inno Setup install locations.

.PARAMETER Version
    Optional. If given, updates MyAppVersion in TFactor.iss before compiling, e.g. -Version 1.1.0.

.EXAMPLE
    .\Installer\build.ps1

.EXAMPLE
    .\Installer\build.ps1 -Version 1.1.0
#>

param(
    [string]$Version
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $repoRoot "TFactor\TFactor.csproj"
$issFile = Join-Path $repoRoot "Installer\TFactor.iss"

# 1. Bump the version baked into the installer, if requested
if ($Version) {
    Write-Host "Setting installer version to $Version"
    $content = Get-Content $issFile -Raw
    $content = $content -replace '#define MyAppVersion "[^"]+"', ('#define MyAppVersion "' + $Version + '"')
    Set-Content -Path $issFile -Value $content -NoNewline
}

# 2. Publish a self-contained, single-file TFactor.exe
Write-Host "Publishing TFactor..."
dotnet publish $csproj -c Release -p:PublishProfile=SelfContained
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

# 3. Find ISCC.exe - PATH first, then the usual install locations
$isccCommand = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
if ($isccCommand) {
    $isccPath = $isccCommand.Source
}
else {
    $candidates = @(
        "$env:ProgramFiles\Inno Setup 7\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    )
    $isccPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $isccPath) {
        throw "Couldn't find ISCC.exe. Install Inno Setup (https://jrsoftware.org/isdl.php) or add it to your PATH, then try again."
    }
}

# 4. Compile the installer
Write-Host "Compiling installer with $isccPath..."
& $isccPath $issFile
if ($LASTEXITCODE -ne 0) {
    throw "ISCC.exe failed with exit code $LASTEXITCODE"
}

Write-Host "Done. Installer at Installer\Output\TFactorSetup.exe"
