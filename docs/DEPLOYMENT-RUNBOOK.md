# Deployment runbook — Phase 1

## Secret bắt buộc

Không đặt secret trong `appsettings.json`. Production cần PostgreSQL password, `StringEncryption:DefaultPassPhrase`, OpenIddict certificate/password, initial admin email/password, Shopee AppId/Secret, Google ClientId/Secret và SMTP credential.

Initial admin được seed từ environment và có extra property `MustChangePassword=true`. Hãy đổi mật khẩu tạm ngay khi bàn giao và không tái sử dụng credential cũ.

## Google OAuth trên IIS

Nút Google chỉ hiển thị khi cả hai biến cấu hình này có giá trị:

```xml
<environmentVariable name="Authentication__Google__ClientId" value="<Google OAuth Web client ID>" />
<environmentVariable name="Authentication__Google__ClientSecret" value="<Google OAuth client secret>" />
```

Thêm hai biến vào `environmentVariables` bên trong thẻ `aspNetCore` của `web.config` trên server, không commit secret vào source control. Trong Google Cloud Console, tạo OAuth client loại **Web application** và đăng ký callback chính xác là `https://<domain-cua-ban>/signin-google`. Sau khi thay đổi biến môi trường, recycle App Pool.

## Triển khai

1. Tạo database/volume mới `webhoantien`; không mount volume của hệ thống tiền nhiệm.
2. Chạy service `migrator` một lần và kiểm tra exit code 0.
3. Chạy đúng một instance `web` ở Phase 1.
4. Kiểm tra `/health/ready`, đăng nhập Admin và mở `/hangfire`.
5. Kiểm tra connection health; chọn ngày sync đầu trong ba tháng gần nhất.
6. Chạy Sync Now, kiểm tra Sync Runs và unmatched conversions trước khi bật lịch tự động.

## Shopee smoke test

Chỉ chạy khi có credential thật và opt-in rõ ràng. Kiểm tra tuần tự: short link, `productOfferV2`, `conversionReport`, sau đó `validatedReport`. Không log AppId/Secret, Authorization header hoặc payload chứa dữ liệu ngoài field đã allowlist.

`validatedReport` không hỗ trợ lọc ngày theo schema hiện tại và `validationId` là tùy chọn trong tài liệu. Production cần xác minh hành vi tài khoản thật trước khi tự động hóa đối soát toàn bộ.

## Rollback

Rollback image ứng dụng, không trỏ về database tiền nhiệm. Migration Phase 1 là clean-slate; backup PostgreSQL trước thay đổi schema. Hangfire dùng schema riêng và có thể tạm dừng bằng cách dừng web instance.
