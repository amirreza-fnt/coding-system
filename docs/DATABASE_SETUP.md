# راه‌اندازی دیتابیس SQL Server

## ۱. ساخت دیتابیس

```sql
CREATE DATABASE [apiweb-codingsystem];
GO
```

## ۲. ساخت Login و User

```sql
USE [master];
GO

CREATE LOGIN [apiwebcodingsystemuser]
WITH PASSWORD = N'A#kHE%nm54UtD',
     CHECK_POLICY = ON,
     CHECK_EXPIRATION = OFF;
GO

USE [apiweb-codingsystem];
GO

CREATE USER [apiwebcodingsystemuser] FOR LOGIN [apiwebcodingsystemuser];
GO

ALTER ROLE [db_owner] ADD MEMBER [apiwebcodingsystemuser];
GO
```

## ۳. خلاصه اطلاعات اتصال

| پارامتر | مقدار |
|---------|--------|
| **Server** | `185.255.91.242,2019` |
| **Database** | `apiweb-codingsystem` |
| **User Id** | `apiwebcodingsystemuser` |
| **Password** | `A#kHE%nm54UtD` |

## ۴. Connection String

```
Server=185.255.91.242,2019;Database=apiweb-codingsystem;User Id=apiwebcodingsystemuser;Password=A#kHE%nm54UtD;Encrypt=True;TrustServerCertificate=True;Connection Timeout=15;Connect Retry Count=3;Pooling=true;Max Pool Size=100;MultipleActiveResultSets=true
```

## ۵. Migration روی سرور

```bash
sudo ./deploy/deploy.sh
```

## ۶. جداول

- **Systems** — سامانه‌ها (seed: 137، FIRE)
- **RequestCounters** — شمارنده اتمیک
- **TrackingRequests** — درخواست‌ها با کد ۵ رقمی
