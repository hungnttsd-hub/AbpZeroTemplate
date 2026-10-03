# webHoanTien.com

Phase 1 của nền tảng chia sẻ hoa hồng affiliate, xây trên .NET 8, ABP.IO 8.3.4, MVC/Razor Pages và PostgreSQL.

## Chạy local

Yêu cầu: .NET 8 SDK và Docker Desktop.

Khôi phục các thư viện giao diện ABP cho dự án MVC:

```powershell
cd src/WebHoanTien.Web
npm run create-bundles
```

1. Sao chép `src/WebHoanTien.Web/appsettings.secrets.example.json` thành `appsettings.secrets.json` trong cùng thư mục và điền cấu hình local.
2. Sao chép `src/WebHoanTien.DbMigrator/appsettings.secrets.example.json` thành `appsettings.secrets.json` trong cùng thư mục.
3. Tạo certificate OpenIddict khi chạy Production; không commit certificate hoặc `appsettings.secrets.json`.
4. Chạy DbMigrator rồi chạy Web từ Visual Studio hoặc `dotnet run`.

Docker Compose vẫn dùng `.env` để truyền cấu hình cho PostgreSQL và ghi đè appsettings theo chuẩn biến môi trường của ASP.NET Core.

Ứng dụng tạo link trực tiếp với `affiliate_id` và `sub_id`; không dùng Shopee Open API. AddLiveTag chỉ cung cấp product data/estimate, không dùng để tạo link hoặc tính cashback. Khi chạy local, điền Shopee Affiliate ID vào `src/WebHoanTien.Web/appsettings.secrets.json`:

```json
{
  "Shopee": { "AffiliateId": "AFFILIATE_ID_CUA_BAN" }
}
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

### Redirect URL cho TikTok Shop

Khai báo chính xác `https://catback.id.vn/api/tiktok-shop/oauth/callback` trong TikTok Shop Partner Center sau khi triển khai bản Web có endpoint này lên domain production.

- `GET`/`HEAD` không có query trả về trang thông tin công khai (HTTP 200), không cần đăng nhập hoặc chấp thuận điều khoản.
- Callback có query được chuyển đến `/tiktok-shop/connection-result`, không chuyển tiếp mã ủy quyền hoặc `state`. Trang kết quả trả HTTP 503 và nói rõ kết nối chưa khả dụng; không báo thành công giả.
- Đây là bước chuẩn bị Redirect URL, chưa phải tích hợp OAuth hoàn chỉnh. Endpoint không lưu mã, không đổi token và không liên kết tài khoản. Chỉ điền App Key/App Secret sẽ chưa kích hoạt kết nối.
- Để bật OAuth cần triển khai bước bắt đầu ủy quyền cho đúng loại ứng dụng (creator/seller), kiểm tra `state` một lần có thời hạn và ràng buộc phiên, trao đổi mã trên máy chủ, lưu token có mã hóa và xử lý làm mới/thu hồi. App Secret chỉ cấu hình qua secret hoặc biến môi trường, không commit vào mã nguồn.
- Callback tắt audit, cache và referrer; ứng dụng loại bỏ log request cho hai đường dẫn OAuth. Khi triển khai, cấu hình IIS/reverse proxy không ghi query string của các đường dẫn này để tránh lưu mã ủy quyền ở lớp hạ tầng.

Tài liệu nền tảng: [TikTok Shop authorization overview](https://partner.tiktokshop.com/docv2/page/678e3a362dccb8030ea6f98c).

### Tác vụ hiện có

- Hangfire Dashboard: `/hangfire`, chỉ role `admin`.
- Health: `/health/live` và `/health/ready`.
- Admin xuất báo cáo actual order từ Shopee rồi import CSV/TXT tại `/Admin/Affiliates`.
- Chỉ các dòng đã import từ báo cáo Shopee mới được dùng để tính cashback payable.
- Raw payload đã lọc cùng IP/User-Agent được giữ tối đa 90 ngày.

Xem [runbook triển khai](docs/DEPLOYMENT-RUNBOOK.md) và [ADR Product Search Phase 2](docs/adr/0001-product-search-phase-2.md).
