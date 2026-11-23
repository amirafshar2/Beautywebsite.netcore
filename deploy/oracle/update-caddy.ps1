# Run from C:\xampp\htdocs\Negin :  powershell -ExecutionPolicy Bypass -File deploy\oracle\update-caddy.ps1
# Copies the Caddyfile to the server and restarts Caddy so it reads it. Output: deploy\oracle\caddy-run.txt
$key = Join-Path $env:USERPROFILE ".ssh\oracle_web_server"
$opts = @("-i", $key, "-o", "StrictHostKeyChecking=accept-new")
scp @opts "$PSScriptRoot\Caddyfile" "ubuntu@130.61.184.171:negin/deploy/oracle/Caddyfile"
scp @opts "$PSScriptRoot\remote-caddy.sh" "ubuntu@130.61.184.171:remote-caddy.sh"
ssh @opts ubuntu@130.61.184.171 "bash remote-caddy.sh" 2>&1 | Out-File -Encoding utf8 "$PSScriptRoot\caddy-run.txt"
Write-Host "OK - caddy-run.txt"
