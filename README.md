# Request Coding Service

سرویس متمرکز تولید **کد پیگیری ۵ رقمی** برای سامانه‌های سبزوار من (۱۳۷، آتش‌نشانی و ...).

## ایده اصلی

| مورد | توضیح |
|------|--------|
| کد پیگیری | عدد ۵ رقمی (مثلاً `00001`) |
| یکتایی منطقی | `SYSTEM_ID + NATIONAL_CODE + COUNTER` |
| شمارنده | برای هر سازمان و هر کد ملی مستقل |
| حذف | Soft Delete — کد آزاد نمی‌شود |
| همزمانی | افزایش شمارنده با `UPDLOCK` اتمیک |

## API Endpoints

| Method | Path | توضیح |
|--------|------|--------|
| POST | `/api/v1/tracking-codes` | تخصیص کد جدید |
| GET | `/api/v1/tracking-codes/{id}` | دریافت با شناسه |
| GET | `/api/v1/tracking-codes/by-code/{systemId}/{counter}` | دریافت با کد ۵ رقمی |
| GET | `/api/v1/tracking-codes/search` | جستجوی اپراتور |
| GET | `/api/v1/tracking-codes/my` | لیست درخواست‌های شهروند (SSO) |
| DELETE | `/api/v1/tracking-codes/{id}` | حذف منطقی |
| GET | `/api/v1/systems` | لیست سامانه‌ها |

## احراز هویت

- **شهروند / اپراتور:** Bearer JWT از `sso-login-service`
- **سرویس داخلی:** هدر `X-Api-Key`

## ساختار پروژه

```
src/
  RequestCodingService.Domain/
  RequestCodingService.Application/
  RequestCodingService.Infrastructure/
  RequestCodingService.Api/
deploy/
publish/          ← خروجی Release برای سرور آفلاین
docs/
```

## مستندات

- [راه‌اندازی دیتابیس](docs/DATABASE_SETUP.md)
- [راه‌اندازی سرور](docs/SERVER_DEPLOY.md)
