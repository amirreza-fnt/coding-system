#!/bin/bash
set -e

APP_NAME="requestcodingservice"
REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
API_DIR="/opt/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
NGINX_CONF="/etc/nginx/conf.d/apiweb-requestcoding.conf"
PUBLISH_DIR="$REPO_DIR/publish"
PORT_FILE="/etc/requestcodingservice.port"

port_in_use() {
  local port="$1"
  if command -v ss >/dev/null 2>&1; then
    ss -tlnH 2>/dev/null | awk '{print $4}' | grep -qE ":${port}$"
    return $?
  fi
  if command -v netstat >/dev/null 2>&1; then
    netstat -tln 2>/dev/null | grep -q ":${port} "
    return $?
  fi
  return 1
}

pick_ports() {
  PUBLIC_PORT=5021
  KESTREL_PORT=15021
  if port_in_use 5021; then
    echo "  Port 5021 is in use — falling back to 5025."
    PUBLIC_PORT=5025
    KESTREL_PORT=15025
    if port_in_use 5025; then
      echo "  ERROR: Both 5021 and 5025 are in use. Free a port and retry."
      exit 1
    fi
  fi
}

resolve_ssl_certs() {
  : # HTTP only — no SSL
}

render_template() {
  local src="$1"
  local dest="$2"
  sed \
    -e "s/__PUBLIC_PORT__/${PUBLIC_PORT}/g" \
    -e "s/__KESTREL_PORT__/${KESTREL_PORT}/g" \
    "$src" | sudo tee "$dest" >/dev/null
}

open_firewall_port() {
  if command -v firewall-cmd >/dev/null 2>&1 && systemctl is-active firewalld >/dev/null 2>&1; then
    echo "  Opening firewalld port ${PUBLIC_PORT}/tcp ..."
    sudo firewall-cmd --permanent --add-port="${PUBLIC_PORT}/tcp" || true
    sudo firewall-cmd --reload || true
  fi
}

allow_selinux_http_port() {
  if command -v semanage >/dev/null 2>&1; then
    echo "  Allowing SELinux http_port_t on ${PUBLIC_PORT}/tcp ..."
    sudo semanage port -a -t http_port_t -p tcp "${PUBLIC_PORT}" 2>/dev/null \
      || sudo semanage port -m -t http_port_t -p tcp "${PUBLIC_PORT}" 2>/dev/null \
      || true
  fi
}

verify_deploy() {
  echo "  Verifying listeners..."
  if sudo nginx -T 2>/dev/null | grep -q "listen ${PUBLIC_PORT}"; then
    echo "  nginx config includes listen ${PUBLIC_PORT}"
  else
    echo "  WARNING: nginx config missing listen ${PUBLIC_PORT} — check ${NGINX_CONF}"
  fi
  ss -tln | grep -E ":${PUBLIC_PORT}|:${KESTREL_PORT}" || echo "  WARNING: expected ports not listening yet."
  sleep 2
  if curl -sf "http://127.0.0.1:${KESTREL_PORT}/health" >/dev/null; then
    echo "  Kestrel health OK on 127.0.0.1:${KESTREL_PORT}"
  else
    echo "  WARNING: Kestrel health failed — check: journalctl -u ${APP_NAME} -n 50"
  fi
  if curl -ksf "https://127.0.0.1:${PUBLIC_PORT}/health" >/dev/null; then
    echo "  nginx health OK on https://127.0.0.1:${PUBLIC_PORT}"
  else
    echo "  WARNING: nginx HTTPS health failed on port ${PUBLIC_PORT}"
  fi
}

echo "============================================"
echo "   Deploying $APP_NAME (offline-ready)"
echo "============================================"

pick_ports
echo "  Public HTTP port:  ${PUBLIC_PORT}"
echo "  Kestrel port:      ${KESTREL_PORT}"
echo "${PUBLIC_PORT} ${KESTREL_PORT}" | sudo tee "$PORT_FILE" >/dev/null

if [ -d "$PUBLISH_DIR" ] && [ -f "$PUBLISH_DIR/RequestCodingService.Api.dll" ]; then
  echo "[1/5] Copying pre-built publish output..."
  sudo mkdir -p "$API_DIR"
  sudo rsync -a --delete "$PUBLISH_DIR/" "$API_DIR/"
else
  echo "[1/5] Publishing application..."
  dotnet publish "$REPO_DIR/src/RequestCodingService.Api/RequestCodingService.Api.csproj" \
    -c Release \
    -o "$API_DIR" \
    --self-contained false
fi

echo "[2/5] Database schema..."
if command -v dotnet >/dev/null 2>&1 && dotnet ef --version >/dev/null 2>&1; then
  export ConnectionStrings__RequestCoding="$(grep -E '^ConnectionStrings__RequestCoding=' /etc/requestcodingservice.env 2>/dev/null | cut -d= -f2- | tr -d '"' || true)"
  if [ -n "$ConnectionStrings__RequestCoding" ]; then
    dotnet ef database update \
      --project "$REPO_DIR/src/RequestCodingService.Infrastructure/RequestCodingService.Infrastructure.csproj" \
      --startup-project "$REPO_DIR/src/RequestCodingService.Api/RequestCodingService.Api.csproj" \
      --context RequestCodingDbContext
  else
    echo "  WARNING: /etc/requestcodingservice.env missing — skip EF migration."
  fi
else
  echo "  dotnet-ef not installed on this server (normal for offline deploy)."
  echo "  Run deploy/initial-schema.sql once in SSMS on apiweb-codingsystem."
fi

echo "[3/5] nginx..."
render_template "$REPO_DIR/deploy/nginx.conf.template" "$NGINX_CONF"
sudo rm -f /etc/nginx/sites-enabled/apiweb-requestcoding 2>/dev/null || true
if ! sudo nginx -t; then
  echo "  ERROR: nginx config invalid — fix ${NGINX_CONF} and retry."
  exit 1
fi
sudo systemctl reload nginx
open_firewall_port
allow_selinux_http_port

echo "[4/5] systemd + env..."
render_template "$REPO_DIR/deploy/requestcodingservice.service.template" "$SERVICE_FILE"
sudo useradd -r -s /usr/sbin/nologin requestcodingservice || true
sudo mkdir -p /var/log/requestcodingservice
sudo chown -R requestcodingservice:requestcodingservice /var/log/requestcodingservice "$API_DIR"
if [ -f /etc/requestcodingservice.env ]; then
  sudo chown root:requestcodingservice /etc/requestcodingservice.env
  sudo chmod 640 /etc/requestcodingservice.env
fi

echo "[5/5] start..."
sudo systemctl daemon-reload
sudo systemctl enable "$APP_NAME"
sudo systemctl restart "$APP_NAME"
sudo systemctl status "$APP_NAME" --no-pager || true
verify_deploy

echo ""
echo "============================================"
echo "   Deploy complete!"
echo "   Kestrel: http://127.0.0.1:${KESTREL_PORT}"
echo "   Health:  curl -k https://SERVER_IP:${PUBLIC_PORT}/health"
echo "   Swagger: curl -k https://SERVER_IP:${PUBLIC_PORT}/swagger"
echo "   Ports saved in: ${PORT_FILE}"
echo "============================================"
echo "Logs: sudo journalctl -u $APP_NAME -f"
