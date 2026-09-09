# راه‌اندازی دیتابیس SQL Server

این اطلاعات را به کارفرما/DBA بدهید تا روی سرور SQL (`185.255.91.242,2019`) اجرا شود.

## ۱. ساخت دیتابیس

```sql
CREATE DATABASE [apiweb-137requestcoding];
GO
```

## ۲. ساخت Login و User

```sql
USE [master];
GO

CREATE LOGIN [apiweb137requestcodinguser]
WITH PASSWORD = N'Rc#9kLm2@xQ7',
     CHECK_POLICY = ON,
     CHECK_EXPIRATION = OFF;
GO

USE [apiweb-137requestcoding];
GO

CREATE USER [apiweb137requestcodinguser] FOR LOGIN [apiweb137requestcodinguser];
GO

ALTER ROLE [db_owner] ADD MEMBER [apiweb137requestcodinguser];
GO
```

## ۳. خلاصه اطلاعات اتصال

| پارامتر | مقدار |
|---------|--------|
| **Server** | `185.255.91.242,2019` |
| **Database** | `apiweb-137requestcoding` |
| **User Id** | `apiweb137requestcodinguser` |
| **Password** | `Rc#9kLm2@xQ7` |

## ۴. Connection String

```
Server=185.255.91.242,2019;Database=apiweb-137requestcoding;User Id=apiweb137requestcodinguser;Password=Rc#9kLm2@xQ7;Encrypt=True;TrustServerCertificate=True;Connection Timeout=15;Connect Retry Count=3;Pooling=true;Max Pool Size=100;MultipleActiveResultSets=true
```

## ۵. ایجاد جداول (Migration)

پس از ساخت دیتابیس، روی سرور لینوکس:

```bash
export ConnectionStrings__RequestCoding='Server=185.255.91.242,2019;Database=apiweb-137requestcoding;User Id=apiweb137requestcodinguser;Password=Rc#9kLm2@xQ7;Encrypt=True;TrustServerCertificate=True;TrustServerCertificate=True'

cd /opt/requestcodingservice-repo   # مسیر clone شده
dotnet ef database update \
  --project src/RequestCodingService.Infrastructure/RequestCodingService.Infrastructure.csproj \
  --startup-project src/RequestCodingService.Api/RequestCodingService.Api.csproj \
  --context RequestCodingDbContext
```

یا با اجرای `deploy/deploy.sh` که migration را خودکار انجام می‌دهد.

## ۶. جداول ایجادشده

- **Systems** — سامانه‌ها (seed: 137، FIRE)
- **RequestCounters** — شمارنده اتمیک per system + national code
- **TrackingRequests** — درخواست‌ها با کد ۵ رقمی

## ۷. ایندکس یکتا

```sql
UNIQUE (SystemId, NationalCode, Counter)
```

## ۸. تغییر رمز

پس از استقرار، رمز را در SQL Server و فایل `/etc/requestcodingservice.env` همزمان تغییر دهید.
