$files = Get-ChildItem "C:\Users\Lucas\source\repos\cs1_ipt4\Translations\*.txt" | Where-Object { $_.Name -ne "pt-br.fixed.txt" -and $_.Name -ne "en.txt" }
foreach($f in $files) {
    $content = Get-Content $f.FullName -Raw
    $lines = $content -split "`n"
    $newLines = @()
    foreach($line in $lines) {
        $newLines += $line
        if($line.StartsWith("SETTINGS_STOPSANDSTATIONS_DESCRIPTION ")) {
            $desc = $line.Substring("SETTINGS_STOPSANDSTATIONS_DESCRIPTION ".Length)
            $newLines += "SETTINGS_STOPSANDSTATIONS_ENABLE " + $desc
            $newLines += "SETTINGS_STOPSANDSTATIONS_ENABLE_TOOLTIP " + $desc
        }
    }
    $out = ($newLines -join "`n").TrimEnd() + "`n"
    [System.IO.File]::WriteAllText($f.FullName, $out, [System.Text.Encoding]::UTF8)
}
Write-Host "Added keys to all translation packs"