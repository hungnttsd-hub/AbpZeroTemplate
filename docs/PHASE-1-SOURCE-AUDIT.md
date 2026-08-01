# Giai đoạn 1 — Khảo sát source ABP.IO hiện có

## 1. Mục tiêu

Xác định thành phần có thể tái sử dụng để chuyển source hiện tại thành hệ thống quản lý trung tâm giáo dục, không thay đổi .NET, ABP, namespace, database provider hoặc cơ chế xác thực.

## 2. Kết quả khảo sát

| Hạng mục | Kết quả |
|---|---|
| Solution | ABP layered application đầy đủ: Domain.Shared, Domain, Application.Contracts, Application, EntityFrameworkCore, HttpApi, HttpApi.Client, Web, DbMigrator và test projects. |
| Runtime | .NET 8.0, C# `latest`. |
| ABP | 8.3.4 (trừ theme LeptonX Lite 3.2.0). |
| Web | ASP.NET Core Razor Pages/MVC trong `src/AbpIoTemplateProject.Web`. |
| Database | SQL Server qua EF Core; `AbpIoTemplateProjectDbContext`; LocalDB cho development, Docker Compose đã có SQL Server 2022. |
| Authentication | ABP Identity + OpenIddict; dynamic claims và Authorization pipeline đã bật. |
| Theme | LeptonX Lite cho khối quản trị; storefront Razor layout hiện có. |
| Localization | ABP localization có sẵn, bao gồm tiếng Việt. |
| Logging/audit | Serilog, correlation ID và ABP Audit Logging đã bật. |
| Background jobs | ABP Background Jobs EF Core đã có bảng và module. |
| API | Conventional controllers từ Application layer và Swagger v1 đã cấu hình. |
| Delivery | Dockerfile đa stage cùng `docker-compose.yml` (web + DbMigrator + SQL Server). |

## 3. Thành phần có thể tái sử dụng

- Identity, OpenIddict, roles, permissions, audit log, background jobs, localization, database migration và Docker deployment.
- Entity và services hiện có cho customer, order, payment, promotion, banner, location, article, homepage section, site settings.
- Public pages, checkout/order pages và khu admin Store là nguồn tham khảo về Razor view models, validation, phân quyền và CRUD.

## 4. Hạn chế/gap hiện tại

- Domain hiện tại là e-commerce/thủy sinh, không có Course, Class, Teacher, Student, Lead, PlacementTest hay LearningContent.
- `IEmailSender` bị thay bằng `NullEmailSender` ở DEBUG; chưa có cấu hình SMTP/transactional email production.
- Không có SMS, CAPTCHA, blob/file-storage, xử lý upload hay cổng thanh toán thật; gateway hiện tại chỉ trả thông tin hướng dẫn giả lập.
- Không có portal học viên/giảng viên/tư vấn; chưa có permission theo nghiệp vụ giáo dục.
- Có hai migrations hiện có (`Initial`, `AddedStoreCommerce`), cần giữ nguyên và thêm migration giáo dục độc lập.

## 5. Quyết định kiến trúc đề xuất

- Giữ nguyên layered architecture và `AbpIoTemplateProjectDbContext`; bổ sung bounded context `Education` song song với `Store` trước khi dọn dẹp phần e-commerce.
- Tái sử dụng IdentityUser làm tài khoản; tạo `Student` riêng, không buộc một lead phải có tài khoản.
- Tái sử dụng Banner, Article, SiteSetting có chọn lọc hoặc mở rộng có kiểm soát; các aggregate đào tạo mới sẽ tách rõ namespace/table prefix `Education`.
- Cổng thanh toán, email/SMS, file storage và placement test phải được cấu hình qua user secrets/environment variables, không đưa credential vào source.

## 6. Rủi ro

- Cần quyết định lộ trình xử lý module Store: giữ để chạy song song, chuyển đổi dần sang Education, hoặc loại bỏ trong một đợt có migration/back-up riêng.
- Đề kiểm tra, quy tắc xếp lớp và nội dung đào tạo là dữ liệu nghiệp vụ cần chủ sở hữu xác nhận trước khi seed production.
- Website tham chiếu có khuyến mại/popup biến động theo thời gian; không nên sao chép nội dung chiến dịch động vào dữ liệu mặc định.

## 7. Kết luận

Source phù hợp để mở rộng thành hệ thống giáo dục thực tế mà không nâng package hoặc tạo solution mới. Giai đoạn 2 là khảo sát UI/UX và public flows trước khi tạo domain model, permissions hay migration.
