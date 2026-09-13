# Technical SEO implementation — 13/09/2026

Đã triển khai theo phân tích được xác nhận trước đó.

## Thành phần đã thêm

- `src/WebHoanTien.Web/Seo/SeoOptions.cs`: cấu hình domain, title, description, ảnh Open Graph, verification token và cờ indexing.
- `SeoMetadata.cs`, `SeoPageCatalog.cs`, `SeoMetadataProvider.cs`: catalog public và metadata dùng chung.
- `_SeoHead.cshtml`: title, description, robots, canonical, Open Graph, Twitter card, verification tùy config và JSON-LD.
- `SeoResponseMiddleware.cs`: robots/sitemap server-rendered, canonical redirect, X-Robots-Tag và cache HTML riêng tư.
- `OptimizedAssetTagHelper.cs`, `scripts/build-assets.mjs`: Production minify CSS/JS bằng esbuild; khi chạy Node 20+ sẽ tạo thêm WebP cho hero/mascot bằng Sharp, còn môi trường Node cũ vẫn fallback an toàn về ảnh PNG đã duyệt.
- `test/WebHoanTien.Web.Tests/Seo/SeoPolicyTests.cs`, `SeoRazorTests.cs`: kiểm tra policy metadata, sitemap, redirect và HTML đầu ra.

## URL policy

Sitemap Production gồm đúng 7 URL public:

`/`, `/huong-dan`, `/huong-dan/video/cai-dat`, `/huong-dan/video/tao-link-hoan-tien`, `/huong-dan/video/dang-ky`, `/Legal/Terms`, `/Legal/Privacy`.

Trang Account, Orders, Links, Wallet, Withdrawal, Notifications, Admin, API, redirect affiliate và URL có query chức năng không có canonical/index trong sitemap. Production dùng `noindex` cho các trang này; Development/Staging dùng `noindex, nofollow` toàn site. Tracking query được phép giữ lại khi redirect nhưng canonical vẫn là URL sạch.

`http` và `www` được chuẩn hóa về `https://catback.id.vn`; alias trailing slash và `/Index` chỉ redirect ở các route public catalog, bảo toàn query string. Slug hướng dẫn không tồn tại trả 404 thay vì redirect về danh sách.

## Cache và hiệu năng

HTML luôn `private, no-store` vì layout chứa tên tài khoản, badge thông báo và menu theo quyền. Static files Production có cache dài hạn khi URL đã fingerprint bằng `asp-append-version`; manifest, PWA launch và service worker vẫn no-cache. CSS/JS được minify khi `dotnet publish -c Release`; Dockerfile dùng Node 22 để tạo WebP tối ưu, trong khi Node 18 local tự bỏ qua bước WebP và vẫn publish thành công với PNG fallback. Ảnh CatBack có `width`/`height` cho hero/mascot; ảnh ngoài màn hình đã lazy-load theo template hiện tại.

## Kiểm tra

- `dotnet build ... -p:OutDir=artifacts/seo-final-build/`: thành công, 0 warning, 0 error.
- `dotnet publish -c Release`: thành công; npm/esbuild tạo asset minify trong output publish và pipeline đã kiểm tra fallback Node 18. Bundled Node 24 cũng tạo thành công `hero.webp` và `mascot.webp`.
- `dotnet test ... --filter FullyQualifiedName~SeoPolicyTests`: 11 passed, 0 failed.
- `git diff --check`, kiểm tra XML sitemap/SVG và đường dẫn asset: đạt.

Chưa chạy toàn bộ test suite vì fixture web yêu cầu Docker PostgreSQL nhưng Docker Engine không chạy trên máy hiện tại. Chưa chạy Playwright hoặc kiểm thử trình duyệt tự động. Sau deploy cần kiểm tra View Source, Search Console, PageSpeed và xác nhận Cloudflare/Render giữ forwarding HTTPS đúng cấu hình.
