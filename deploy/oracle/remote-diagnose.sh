echo "== containers"; sudo docker ps --format '{{.Names}} {{.Status}}'
echo "== caddy certificates / errors (last 15 min)"
sudo docker logs --since 15m caddy 2>&1 | grep -oE '"(level|msg|identifier|error|detail)":"[^"]{0,160}"' | paste -sd' ' | sed 's/"level"/\n"level"/g' | grep -E 'obtained|error|challenge failed|certificate' | tail -25
echo "== local tests"
for h in amirrezaafshar.de afsharweb.com beautybynegin.com; do
  curl -s -o /dev/null -m 15 -w "$h https -> %{http_code} (ssl verify %{ssl_verify_result})\n" --resolve $h:443:127.0.0.1 https://$h/robots.txt || echo "$h https FAILED"
done
