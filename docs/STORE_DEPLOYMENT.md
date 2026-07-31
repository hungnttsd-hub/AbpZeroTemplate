# Triển khai Aqua Garden Store

Ứng dụng là ABP.IO modular monolith trên .NET 8 và SQL Server. Luôn chạy `DbMigrator` đúng phiên bản trước khi chuyển traffic sang Web.

## Biến môi trường bắt buộc

- `ConnectionStrings__Default`: chuỗi kết nối SQL Server.
- `App__SelfUrl`: URL HTTPS công khai, không có dấu `/` cuối.
- `StringEncryption__DefaultPassPhrase`: secret riêng theo môi trường.
- Các secret OpenIddict/SMTP/payment gateway phải lưu trong secret store, không commit vào source.

## Docker Compose

```powershell
$env:MSSQL_SA_PASSWORD = "<strong-password>"
$env:STRING_ENCRYPTION_PASSPHRASE = "<random-passphrase>"
$env:APP_SELF_URL = "https://shop.example.com"
docker compose up --build -d
docker compose ps
```

Compose khởi động SQL Server, chạy migration/seed một lần, sau đó mới chạy Web ở cổng `8080`. Đặt reverse proxy (IIS, Nginx hoặc load balancer) phía trước để kết thúc TLS.

## IIS

1. Cài .NET 8 Hosting Bundle và URL Rewrite.
2. Publish:

   ```powershell
   dotnet publish .\src\AbpIoTemplateProject.Web\AbpIoTemplateProject.Web.csproj -c Release -o C:\Deploy\AquaGarden\Web
   dotnet publish .\src\AbpIoTemplateProject.DbMigrator\AbpIoTemplateProject.DbMigrator.csproj -c Release -o C:\Deploy\AquaGarden\Migrator
   ```

3. Backup database, đặt biến môi trường cho app pool, rồi chạy `dotnet AbpIoTemplateProject.DbMigrator.dll` trong thư mục Migrator.
4. Trỏ IIS site vào thư mục Web, dùng app pool `No Managed Code`, bật HTTPS, HSTS và forward headers.
5. Kiểm tra `/`, `/products`, `/robots.txt`, `/sitemap.xml` và luồng checkout COD trước khi mở traffic.

## Rollback

- Giữ lại artifact Web của phiên bản trước.
- Nếu chỉ lỗi ứng dụng, chuyển IIS/reverse proxy về artifact trước; không tự động hạ migration.
- Nếu migration có thay đổi phá vỡ tương thích, phục hồi bản backup SQL đã tạo trước deploy hoặc dùng migration rollback đã được thử nghiệm ở staging.
- Seed thương mại idempotent: nếu catalog đã tồn tại sẽ không chèn lại dữ liệu mẫu.

## Vận hành

- Theo dõi log lỗi HTTP, thời gian checkout, deadlock và lỗi unique index idempotency.
- Backup SQL theo lịch và thử phục hồi định kỳ.
- Giới hạn quyền admin theo các permission `Store.*`; không cấp toàn bộ quyền cho tài khoản vận hành thông thường.
- Cấu hình payment gateway thật bằng adapter riêng trước khi bật thanh toán online ở production.
