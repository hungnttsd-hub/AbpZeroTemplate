# Deploy Wordy Wings

Dockerfile gốc đã bổ sung bước `npm ci` / `npm run build:host` để phục vụ frontend tại `/wordy-wings/index.html` trên cùng ABP host. Cách này dùng session Identity/OpenIddict hiện có mà không cần cấu hình cookie cross-origin. React/Phaser vẫn độc lập để bọc Capacitor về sau.

## Render Docker Web Service

- Build context: repository root; Dockerfile: `Dockerfile`; target cuối `render`.
- Runtime port: 10000; health endpoint: `/health/live` (readiness database: `/health/ready`).
- Entrypoint hiện có chạy DbMigrator trước Web: cần quyền tạo schema/bảng cho PostgreSQL.
- Khai báo `ConnectionStrings__Default`, `App__SelfUrl`, các secret `OpenIddict__CertificatePassword` / `OpenIddict__CertificateBase64` và cấu hình DataProtection theo deployment hiện tại.
- `InitialAdmin__Email` / `InitialAdmin__Password` chỉ dùng khi bootstrap admin, lấy từ secret store.
- Không commit secret. Không dùng disk ephemeral để lưu asset hoặc progress.
- URL frontend: `https://<your-service>/wordy-wings/index.html`.

Giữ PostgreSQL và deployment theo cấu hình backend gốc; migration chỉ thêm schema `wordy` và không xóa dữ liệu của module cũ.

## Render Static Site (chế độ trên thiết bị)

Build command từ repository root: `cd react && npm ci && npm run build`.

Publish directory: `react/dist`.

Vite đang dùng base `/wordy-wings/`. Cấu hình rewrite `/wordy-wings/assets/*` -> `/assets/*`, và `/wordy-wings/*` -> `/index.html`, hoặc phục vụ dist dưới đúng subpath này. Static site không có backend sẽ dùng chế độ chơi trên thiết bị.

Muốn tách static site và API cho tài khoản production: cần OIDC Authorization Code + PKCE, client registration, redirect URI và CORS cho domain cụ thể. Adapter HTTP hiện tại chủ ý dùng cookie same-origin; không bật wildcard credentials hoặc đưa client secret vào JavaScript. Đường deploy tích hợp đã triển khai là Docker same-origin.

## Sau triển khai

Khi được yêu cầu kiểm thử, xác nhận đăng nhập, tạo hồ sơ, chơi W01-L01, tải lại vẫn giữ sao, thử queue khi API gián đoạn và gửi lại cùng `attemptId` không tăng số lượt lần hai. Kiểm tra boss W01-L10/W02-L10 mở world kế tiếp và ownership giữa hai parent account. Chỉ công bố MVP đạt acceptance sau khi đã xác nhận các mục P0 trong GDD.
