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

# Framework languages (CSLModsCommon): always publish the full Common JSON set from source.
# bin\ may only contain a partial copy when Content/CopyToOutput was incomplete — that made
# half the language dropdown vanish in Options (max-priority for 4.9 prep).
$commonSource = Join-Path $projectRoot "CSLModsCommonShared\Localization\Common"
$commonDest = Join-Path $destination "Localization\Common"
if (Test-Path -LiteralPath $commonSource -PathType Container) {
    New-Item -ItemType Directory -Path $commonDest -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $commonSource "*") -Destination $commonDest -Force
    $jsonCount = (Get-ChildItem -LiteralPath $commonDest -Filter "*.json" -File).Count
    Write-Host "Localization/Common: deployed $jsonCount JSON locale file(s) from CSLModsCommonShared"
}
else {
    Write-Warning "CSLModsCommon locale folder missing: $commonSource"
}

# IPT UI strings: ensure root Translations packs are complete from project source.
$translationsSource = Join-Path $projectRoot "Translations"
$translationsDest = Join-Path $destination "Translations"
if (Test-Path -LiteralPath $translationsSource -PathType Container) {
    New-Item -ItemType Directory -Path $translationsDest -Force | Out-Null
    Get-ChildItem -LiteralPath $translationsSource -File | ForEach-Object {
        # Skip backup / non-live packs that must not ship into the mod folder.
        if ($_.Name -like "*.fixed.txt" -or $_.Name -like "*.bak" -or $_.Name -like "*.backup") {
            return
        }
        Copy-Item -LiteralPath $_.FullName -Destination $translationsDest -Force
    }
    # bin\ copy may have left *.fixed.txt / backups; strip them so only live packs remain.
    Get-ChildItem -LiteralPath $translationsDest -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "*.fixed.txt" -or $_.Name -like "*.bak" -or $_.Name -like "*.backup" } |
        Remove-Item -Force
    $txtCount = (Get-ChildItem -LiteralPath $translationsDest -Filter "*.txt" -File).Count
    Write-Host "Translations: deployed $txtCount .txt pack(s) from project source"
}

Write-Host "Installed clean local build to: $destination"
