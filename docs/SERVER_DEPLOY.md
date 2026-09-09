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
sudo git clone https://github.com/YOUR_ORG/request-coding-service.git requestcodingservice-repo
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
sudo ./deploy/deploy.sh
```

اسکریپت:
1. از پوشه `publish/` (بیلد از قبل) یا `dotnet publish` استفاده می‌کند
2. Migration دیتابیس را اجرا می‌کند
3. nginx و systemd را تنظیم می‌کند

### ۴. بررسی سلامت

```bash
curl -k https://apiweb-137requestcoding.sabzevar.ir:5019/health
curl -k https://apiweb-137requestcoding.sabzevar.ir:5019/api/v1/systems
```

### ۵. لاگ

```bash
sudo journalctl -u requestcodingservice -f
tail -f /var/log/requestcodingservice/app-*.log
```

## پورت‌ها

| سرویس | پورت |
|--------|------|
| Kestrel (داخلی) | `127.0.0.1:5019` |
| nginx (عمومی) | `5019` (HTTPS) |

## نمونه درخواست (تخصیص کد)

```bash
curl -X POST https://apiweb-137requestcoding.sabzevar.ir:5019/api/v1/tracking-codes \
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
curl "https://apiweb-137requestcoding.sabzevar.ir:5019/api/v1/tracking-codes/search?systemId=1&counter=12345&lastName=محمدی" \
  -H "Authorization: Bearer YOUR_JWT"
```

## ری‌استارت

```bash
sudo systemctl restart requestcodingservice
sudo systemctl status requestcodingservice
```
