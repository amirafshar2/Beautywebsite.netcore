# Run from C:\xampp\htdocs\Negin :  powershell -ExecutionPolicy Bypass -File deploy\oracle\diagnose.ps1
# Writes the server state to deploy\oracle\diagnose.txt (read by Claude)
$key = Join-Path $env:USERPROFILE ".ssh\oracle_web_server"
$opts = @("-i", $key, "-o", "StrictHostKeyChecking=accept-new")
scp @opts "$PSScriptRoot\remote-diagnose.sh" "ubuntu@130.61.184.171:remote-diagnose.sh" | Out-Null
ssh @opts ubuntu@130.61.184.171 "bash remote-diagnose.sh" 2>&1 | Out-File -Encoding utf8 "$PSScriptRoot\diagnose.txt"
Write-Host "OK - diagnose.txt"
