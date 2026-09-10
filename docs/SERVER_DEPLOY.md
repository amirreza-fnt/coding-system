# راه‌اندازی روی سرور (آفلاین)

سرور به اینترنت دسترسی ندارد؛ فقط GitHub در دسترس است.

## پیش‌نیازها

- .NET 8 Runtime (`dotnet --version` → 8.x)
- nginx + SSL certificate
- systemd
- دسترسی به SQL Server

## مراحل

### ۱. Clone از GitHub

```bash
cd /opt
sudo git clone https://github.com/amirreza-fnt/coding-system.git requestcodingservice-repo
cd requestcodingservice-repo
```

### ۲. تنظیم env

```bash
sudo cp deploy/requestcodingservice.env.example /etc/requestcodingservice.env
sudo chmod 640 /etc/requestcodingservice.env
sudo chown root:requestcodingservice /etc/requestcodingservice.env
```

### ۳. Deploy

```bash
chmod +x deploy/deploy.sh
sudo bash deploy/deploy.sh
```

اگر `command not found` دیدید، حتماً با `bash deploy/deploy.sh` اجرا کنید (مشکل line-ending ویندوز).

اسکریپت:
1. از پوشه `publish/` (بیلد از قبل) یا `dotnet publish` استفاده می‌کند
2. Migration دیتابیس را اجرا می‌کند
3. nginx و systemd را تنظیم می‌کند

### ۴. بررسی سلامت

روی شبکه داخلی (HTTP — بدون SSL):

```bash
curl http://192.168.1.12:5021/health
curl http://192.168.1.12:5021/api/v1/systems
```

> پورت **5021** با **HTTP** است (fallback deploy: **5025**).

اگر `dotnet-ef` روی سرور نیست، یک‌بار `deploy/initial-schema.sql` را در SSMS اجرا کنید، بعد `./deploy/deploy.sh` را دوباره بزنید.

### ۵. لاگ

```bash
sudo journalctl -u requestcodingservice -f
tail -f /var/log/requestcodingservice/app-*.log
```

## پورت‌ها

| سرویس | پورت |
|--------|------|
| Kestrel (داخلی) | `15021` (یا `15025` اگر 5021 اشغال باشد) |
| nginx (عمومی HTTPS) | `5021` (fallback: `5025`) |

## نمونه درخواست (تخصیص کد)

```bash
curl -X POST https://apiweb-requestcoding.sabzevar.ir:5021/api/v1/tracking-codes \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-internal-key-137" \
  -d '{
    "systemId": 1,
    "nationalCode": "0795032307",
    "firstName": "علی",
    "lastName": "محمدی",
    "mobile": "09151234567",
    "description": "درخواست نمونه"
  }'
```

پاسخ نمونه:

```json
{
  "id": "...",
  "systemId": 1,
  "systemCode": "137",
  "systemName": "سامانه ۱۳۷",
  "trackingCode": "00001",
  "counter": 1,
  "nationalCode": "0795032307",
  "createdAtUtc": "..."
}
```

## جستجوی اپراتور

```bash
curl "https://apiweb-requestcoding.sabzevar.ir:5021/api/v1/tracking-codes/search?systemId=1&counter=12345&lastName=محمدی" \
  -H "Authorization: Bearer YOUR_JWT"
```

## ری‌استارت

```bash
sudo systemctl restart requestcodingservice
sudo systemctl status requestcodingservice
```
