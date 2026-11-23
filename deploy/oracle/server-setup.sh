#!/usr/bin/env bash
# Runs ON the Oracle server (Ubuntu 24.04, ARM). Idempotent: safe to run again.
set -euo pipefail
cd ~
echo "== 1. packages + Docker"
if ! command -v docker >/dev/null; then
  sudo apt-get update -y
  sudo apt-get install -y ca-certificates curl unzip
  curl -fsSL https://get.docker.com | sudo sh
fi
sudo apt-get install -y unzip iptables-persistent >/dev/null 2>&1 || sudo DEBIAN_FRONTEND=noninteractive apt-get install -y unzip iptables-persistent
echo "== 2. firewall: open 80/443 (Oracle Ubuntu images block them with iptables)"
for p in 80 443; do
  sudo iptables -C INPUT -p tcp --dport $p -j ACCEPT 2>/dev/null || sudo iptables -I INPUT 5 -p tcp --dport $p -j ACCEPT
done
sudo iptables -C INPUT -p udp --dport 443 -j ACCEPT 2>/dev/null || sudo iptables -I INPUT 5 -p udp --dport 443 -j ACCEPT
sudo netfilter-persistent save
echo "== 3. source code"
rm -rf ~/negin.new && mkdir -p ~/negin.new && tar -xzf ~/negin-src.tar.gz -C ~/negin.new
# files packed on Windows can arrive as 700: the app user in the container must be able to read them
chmod -R u+rwX,go+rX ~/negin.new
rm -rf ~/negin.old; [ -d ~/negin ] && mv ~/negin ~/negin.old; mv ~/negin.new ~/negin
echo "== 4. data (only on first run: never overwrites a live database)"
if [ ! -f /srv/negin/data/site.db ]; then
  sudo bash ~/negin/deploy/oracle/restore-backup.sh ~/negin-backup.zip
else
  echo "data already present, not restored"
fi
echo "== 5. build + start"
cd ~/negin/deploy/oracle
sudo docker compose up -d --build
echo "== 6. test"
for i in $(seq 1 30); do
  code=$(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:8080/robots.txt || true)
  [ "$code" = "200" ] && break; sleep 3
done
echo "robots.txt -> $code"
curl -s -o /dev/null -w 'home -> %{http_code}\n' http://127.0.0.1:8080/fa
sudo docker compose ps
free -h | head -2; df -h / | tail -1
echo "== DONE"
