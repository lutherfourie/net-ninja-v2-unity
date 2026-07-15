# Per-machine UnityYAMLMerge configuration
$unity = "C:/Program Files/Unity/Hub/Editor/6000.4.3f1/Editor/Data/Tools/UnityYAMLMerge.exe"
if (-not (Test-Path $unity)) { Write-Host "Adjust Unity path in this script."; exit 1 }
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "`"$unity`" merge -p %O %B %A %A"
Write-Host "SmartMerge configured."
