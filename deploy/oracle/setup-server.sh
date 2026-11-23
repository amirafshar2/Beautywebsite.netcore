#!/usr/bin/env bash
# One-time setup of a fresh Ubuntu server: Docker, firewall ports 80/443, swap.
set -euo pipefail
sudo apt-get update
sudo apt-get install -y ca-certificates curl unzip git
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker "$USER"
# Oracle's Ubuntu images block everything except SSH with iptables: open 80 and 443.
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
sudo iptables -I INPUT 6 -m state --state NEW -p udp --dport 443 -j ACCEPT
sudo netfilter-persistent save
sudo mkdir -p /srv/negin/data
echo "Done. Log out and in again so the docker group applies."
