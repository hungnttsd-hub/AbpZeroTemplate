# webHoanTien.com

Phase 1 của nền tảng chia sẻ hoa hồng affiliate, xây trên .NET 8, ABP.IO 8.3.4, MVC/Razor Pages và PostgreSQL.

## Chạy local

Yêu cầu: .NET 8 SDK và Docker Desktop.

1. Sao chép `.env.example` thành `.env`, thay toàn bộ giá trị `replace-with-*`.
2. Tạo certificate OpenIddict tại `secrets/openiddict.pfx`; không commit certificate hoặc mật khẩu.
3. Chạy `docker compose up --build`.
4. Mở `http://localhost:8080/health/ready`, sau đó mở ứng dụng.

Development mặc định dùng `MockShopeeAffiliateProvider`. Production mặc định dùng Shopee thật và không tự fallback sang Mock. Credential chỉ được truyền qua environment hoặc user-secrets:

```powershell
dotnet user-secrets --project src/WebHoanTien.Web set "Shopee:AppId" "..."
dotnet user-secrets --project src/WebHoanTien.Web set "Shopee:Secret" "..."
```

## Database và test

- Database mới: `webhoantien`; schema nghiệp vụ: `affiliate`; schema job: `hangfire`.
- Migration duy nhất: `InitialWebHoanTien`.
- Integration tests dùng PostgreSQL Testcontainers, không dùng SQLite. Docker Engine phải đang chạy.

```powershell
dotnet build WebHoanTien.sln -c Release
dotnet test WebHoanTien.sln -c Release
```

## Vận hành

- Hangfire Dashboard: `/hangfire`, chỉ role `admin`.
- Health: `/health/live` và `/health/ready`.
- Conversion sync mỗi giờ; reconciliation và retention chạy hàng ngày.
- Lần sync đầu bị khóa đến khi Admin chọn ngày bắt đầu trong ba tháng gần nhất.
- Raw payload đã lọc cùng IP/User-Agent được giữ tối đa 90 ngày.

Xem [runbook triển khai](docs/DEPLOYMENT-RUNBOOK.md) và [ADR Product Search Phase 2](docs/adr/0001-product-search-phase-2.md).
