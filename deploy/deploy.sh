#!/bin/bash
set -e

APP_NAME="requestcodingservice"
REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
API_DIR="/opt/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
NGINX_CONF="/etc/nginx/sites-available/apiweb-requestcoding"
PUBLISH_DIR="$REPO_DIR/publish"

echo "============================================"
echo "   Deploying $APP_NAME (offline-ready)"
echo "============================================"

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
if [ ! -f "$NGINX_CONF" ]; then
  sudo cp "$REPO_DIR/deploy/nginx.conf" "$NGINX_CONF"
  sudo ln -sf "$NGINX_CONF" /etc/nginx/sites-enabled/
  sudo nginx -t && sudo systemctl reload nginx
else
  echo "  nginx config already exists — skipped."
fi

echo "[4/5] systemd + env..."
sudo cp "$REPO_DIR/deploy/requestcodingservice.service" "$SERVICE_FILE"
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

echo ""
echo "============================================"
echo "   Deploy complete!"
echo "   Kestrel: http://127.0.0.1:1519"
echo "   Health (HTTPS): curl -k https://SERVER_IP:5019/health"
echo "   Swagger:        curl -k https://SERVER_IP:5019/swagger"
echo "============================================"
echo "Logs: sudo journalctl -u $APP_NAME -f"
