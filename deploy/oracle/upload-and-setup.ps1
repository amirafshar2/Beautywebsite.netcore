# Run on Windows from C:\xampp\htdocs\Negin :
#   powershell -ExecutionPolicy Bypass -File deploy\oracle\upload-and-setup.ps1
# Copies code + backup to the Oracle server and runs server-setup.sh there. Log: deploy\oracle\last-run.log
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $root
Start-Transcript -Path "$PSScriptRoot\last-run.log" -Force | Out-Null
$ip = "130.61.184.171"
# Windows ssh refuses keys that other users can read: keep a private copy in %USERPROFILE%\.ssh
$sshDir = Join-Path $env:USERPROFILE ".ssh"; New-Item -ItemType Directory -Force $sshDir | Out-Null
$key = Join-Path $sshDir "oracle_web_server"
if (-not (Test-Path $key)) {
  Copy-Item "$root\_oracle-ssh\oracle_web_server" $key
  icacls $key /inheritance:r /grant:r "$($env:USERNAME):(R)" | Out-Null
}
$opts = @("-i", $key, "-o", "StrictHostKeyChecking=accept-new")
$backup = Get-ChildItem "$root\*backup-*.zip" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "Backup: $($backup.Name)"
Write-Host "Uploading..."
scp @opts "$root\_deploy\negin-src.tar.gz" "ubuntu@${ip}:negin-src.tar.gz"
scp @opts $backup.FullName "ubuntu@${ip}:negin-backup.zip"
Write-Host "Running setup on the server (Docker build takes a few minutes)..."
ssh @opts "ubuntu@$ip" "tar -xzf negin-src.tar.gz ./deploy/oracle/server-setup.sh -O | bash"
Stop-Transcript | Out-Null
Write-Host "Fertig / تمام شد"
