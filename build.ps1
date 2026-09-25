<#
.SYNOPSIS
    Script de build pour WankulCrazy : mode Dev (rapide, injecte la DLL) ou mode Release (package zip complet).
.PARAMETER Mode
    'dev' : Compile le code et copie directement la DLL dans le dossier BepInEx du jeu.
    'release' : Compile en Release, prépare un dossier propre et crée une archive zip prête pour les joueurs.
#>
param (
    [ValidateSet('dev', 'release')]
    [string]$Mode = 'dev',
    [string]$GamePluginDir = 'E:\jeux\TCG Card Shop Simulator\BepInEx\plugins\WankulCrazy'
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Set-Location $ScriptDir

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   WankulCrazy Build System - Mode: $Mode" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

if ($Mode -eq 'dev') {
    Write-Host "[Dev] Compilation rapide (Release DLL)..." -ForegroundColor Yellow
    dotnet build -c Release --no-incremental

    $distDll = Join-Path $ScriptDir "dist\WankulCrazy\WankulCrazyPlugin.dll"
    if (-not (Test-Path $distDll)) {
        throw "DLL introuvable après la build : $distDll"
    }

    if (Test-Path $GamePluginDir) {
        Write-Host "[Dev] Copie de la DLL dans le jeu -> $GamePluginDir" -ForegroundColor Green
        try {
            Copy-Item -Path $distDll -Destination (Join-Path $GamePluginDir "WankulCrazyPlugin.dll") -Force
            Write-Host "[Dev] Déploiement terminé avec succès !" -ForegroundColor Green
        } catch {
            Write-Warning "Le jeu est actuellement ouvert et verrouille 'WankulCrazyPlugin.dll'. Ferme le jeu pour que la nouvelle DLL soit appliquée."
        }
    } else {
        Write-Warning "Dossier du jeu non trouvé : $GamePluginDir. La DLL est disponible dans dist/WankulCrazy/."
    }
}
elseif ($Mode -eq 'release') {
    Write-Host "[Release] Compilation propre..." -ForegroundColor Yellow
    dotnet build -c Release

    $releaseRoot = Join-Path $ScriptDir "release_build"
    $pluginReleaseDir = Join-Path $releaseRoot "WankulCrazy"
    $zipOutputDir = Join-Path $ScriptDir "releases"

    if (Test-Path $releaseRoot) {
        Remove-Item -Recurse -Force $releaseRoot
    }
    New-Item -ItemType Directory -Path $pluginReleaseDir -Force | Out-Null
    if (-not (Test-Path $zipOutputDir)) {
        New-Item -ItemType Directory -Path $zipOutputDir -Force | Out-Null
    }

    Write-Host "[Release] Copie des fichiers essentiels..." -ForegroundColor Yellow

    # 1. DLL principale
    Copy-Item (Join-Path $ScriptDir "dist\WankulCrazy\WankulCrazyPlugin.dll") $pluginReleaseDir -Force

    # 2. Textes & Docs
    if (Test-Path (Join-Path $ScriptDir "INSTALL.txt")) {
        Copy-Item (Join-Path $ScriptDir "INSTALL.txt") $pluginReleaseDir -Force
    }
    if (Test-Path (Join-Path $ScriptDir "LICENCE.txt")) {
        Copy-Item (Join-Path $ScriptDir "LICENCE.txt") $pluginReleaseDir -Force
    }

    # 3. Dossier data propre (sans saves locales ni SeasonTest)
    $destData = Join-Path $pluginReleaseDir "data"
    Copy-Item -Path (Join-Path $ScriptDir "data") -Destination $destData -Recurse -Force

    # Nettoyage des éléments temporaires / de dev dans le package joueur
    Get-ChildItem -Path $destData -Recurse -Include "save_*.json", "*.bckp", "*SeasonTest*" -File | Remove-Item -Force
    Get-ChildItem -Path $destData -Recurse -Filter "*SeasonTest*" -Directory | Remove-Item -Recurse -Force

    # 4. Création de l'archive Zip
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $zipPath = Join-Path $zipOutputDir "WankulCrazy_Release_$timestamp.zip"

    Write-Host "[Release] Compression du package zip -> $zipPath" -ForegroundColor Green
    Compress-Archive -Path $pluginReleaseDir -DestinationPath $zipPath -Force

    # Nettoyage dossier temporaire
    Remove-Item -Recurse -Force $releaseRoot

    Write-Host "==========================================" -ForegroundColor Green
    Write-Host "   Release prête : $zipPath" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
}
