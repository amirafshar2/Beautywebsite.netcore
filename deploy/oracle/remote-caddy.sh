cd ~/negin/deploy/oracle
echo "== Caddyfile on server has portfolio block:"; grep -c portfolio-web Caddyfile
echo "== Caddyfile inside container has portfolio block:"; sudo docker exec caddy grep -c portfolio-web /etc/caddy/Caddyfile || true
echo "== recreate caddy (picks up the new file)"
sudo docker compose up -d --force-recreate caddy 2>&1 | tail -3
sleep 45
sudo docker exec caddy grep -c portfolio-web /etc/caddy/Caddyfile
sudo docker logs --since 2m caddy 2>&1 | grep -oE '"(msg|identifier|error)":"[^"]{0,140}"' | paste -sd' ' | sed 's/"msg"/\n"msg"/g' | grep -E 'obtained|error|failed' | tail -15
for h in amirrezaafshar.de www.amirrezaafshar.de afsharweb.com www.afsharweb.com beautybynegin.com; do
  curl -s -o /dev/null -m 15 -w "$h https -> %{http_code} (ssl verify %{ssl_verify_result})\n" --resolve $h:443:127.0.0.1 https://$h/robots.txt || echo "$h FAILED"
done
