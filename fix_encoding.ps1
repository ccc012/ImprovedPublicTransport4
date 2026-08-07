$replacements = @{
    'Ã‰' = 'É'; 'Ã€' = 'À'; 'Ã–' = 'Ö'; 'ÃŸ' = 'ß'; 'Ãœ' = 'Ü'; 'Ã§' = 'ç'; 'Ã¡' = 'á'; 'Ã©' = 'é'; 'Ã­' = 'í'; 'Ã³' = 'ó'; 'Ãº' = 'ú'; 'Ã±' = 'ñ';
    'â€"' = '—'; 'â€"' = '–'; 'â€œ' = '"'; 'â€' = '"'; 'â€™' = ''''; 'â€¢' = '•'; 'â˜…' = '★'; 'â†'' = '→'; 'â‰¤' = '≤'; 'â€˜' = ''''; 'â€š' = '‚'; 'â€¦' = '…'; 'â„¢' = '™'; 'â€º' = '›'; 'â–¡' = '▪'
}

$files = Get-ChildItem 'C:\Users\Lucas\source\repos\cs1_ipt4\Translations\*.txt'
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content
    foreach ($pair in $replacements.GetEnumerator()) {
        $content = $content -replace [regex]::Escape($pair.Key), $pair.Value
    }
    if ($content -ne $original) {
        Set-Content $file.FullName -Value $content -Encoding UTF8
        Write-Host "Fixed: $($file.Name)"
    }
}