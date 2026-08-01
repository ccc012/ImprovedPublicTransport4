# Emits all nine playtest language packs to Translations/*.txt
Set-Location $PSScriptRoot
python emit_all_nine.py
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Done. Non-CHANGELOG strings should now be native for da/fi/no/sv/hu/ro/el/vi/ms."
