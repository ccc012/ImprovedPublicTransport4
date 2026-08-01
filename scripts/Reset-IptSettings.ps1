# Reset IPT4 settings to force Safe defaults on next game launch
$json = "$env:LOCALAPPDATA\Colossal Order\Cities_Skylines\ModsSettings\ImprovedPublicTransport4\ImprovedPublicTransportModSetting.json"
if (Test-Path $json) {
  $bak = $json + ".bak-" + (Get-Date -Format "yyyyMMdd-HHmmss")
  Copy-Item $json $bak
  Remove-Item $json -Force
  Write-Host "Removed settings (backup $bak). Next launch = Safe defaults."
} else { Write-Host "Already clean" }
