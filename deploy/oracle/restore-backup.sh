#!/usr/bin/env bash
# Puts a backup zip from the admin panel (site.db, uploads/, keys/) into /srv/negin/data.
# Usage: sudo ./restore-backup.sh ~/backup.zip
set -euo pipefail
zip="${1:?path to backup zip}"
data=/srv/negin/data
docker stop negin-web 2>/dev/null || true
mkdir -p "$data"
if [ -f "$data/site.db" ]; then
  mv "$data" "$data.before-restore-$(date +%Y%m%d-%H%M%S)"
  mkdir -p "$data"
fi
unzip -o -q "$zip" -d "$data"
rm -f "$data/backup.json"
chown -R 1654:1654 "$data"   # "app" user inside the aspnet image
echo "Restored into $data:"; ls -la "$data"
