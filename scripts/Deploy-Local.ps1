param(
    [string]$Configuration = "Debug",
    [string]$GameDataRoot = "$env:LOCALAPPDATA\Colossal Order\Cities_Skylines"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$buildOutput = Join-Path $projectRoot "bin\$Configuration\net35"
$destination = Join-Path $GameDataRoot "Addons\Mods\ImprovedPublicTransport4"

if (-not (Test-Path -LiteralPath $buildOutput -PathType Container)) {
    throw "Build output not found: $buildOutput"
}

$resolvedGameDataRoot = [System.IO.Path]::GetFullPath($GameDataRoot)
$resolvedDestination = [System.IO.Path]::GetFullPath($destination)
if (-not $resolvedDestination.StartsWith($resolvedGameDataRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to deploy outside the configured Cities: Skylines data root: $resolvedDestination"
}

if (Test-Path -LiteralPath $destination) {
    Remove-Item -LiteralPath $destination -Recurse -Force
}

New-Item -ItemType Directory -Path $destination -Force | Out-Null

foreach ($fileName in @("ImprovedPublicTransport4.dll", "ImprovedPublicTransport4.pdb", "Newtonsoft.Json.dll")) {
    $sourceFile = Join-Path $buildOutput $fileName
    if (Test-Path -LiteralPath $sourceFile -PathType Leaf) {
        Copy-Item -LiteralPath $sourceFile -Destination $destination -Force
    }
}

foreach ($directoryName in @("Localization", "Resources", "Translations")) {
    $sourceDirectory = Join-Path $buildOutput $directoryName
    if (Test-Path -LiteralPath $sourceDirectory -PathType Container) {
        Copy-Item -LiteralPath $sourceDirectory -Destination $destination -Recurse -Force
    }
}

Write-Host "Installed clean local build to: $destination"
