# Master Prompt — Web Affiliate Cashback bằng ABP.IO

Bạn là **Senior Solution Architect + Senior .NET/ABP.IO Developer**.

Hãy xây dựng một web application Affiliate Cashback sử dụng **ABP.IO Application Template**.
Web tham chiếu: https://sharehoahong.com/
Tên web: webHoanTien.com
Sử dụng AbpIo Tempate tại project hiện tại. Xóa hết tính năng của izone đi

## Mục tiêu hệ thống

- Người dùng có thể tự tạo tài khoản bằng email/password hoặc đăng nhập bằng Google.
- Người dùng cung cấp link sản phẩm Shopee.
- Hệ thống tạo affiliate tracking riêng cho người dùng.
- Người dùng mua hàng thông qua affiliate link.
- Backend định kỳ đồng bộ conversion/order từ Shopee Affiliate.
- Hệ thống map đơn affiliate về đúng user.
- Hệ thống tính phần commission/cashback mà user được hưởng.
- Kiến trúc phải cho phép tích hợp thêm TikTok Shop trong tương lai.
- Phase 2 sẽ bổ sung khả năng tìm kiếm sản phẩm trực tiếp trên hệ thống tương tự trải nghiệm search của Shopee.
- Website customer-facing tham khảo sự đơn giản về UX của https://sharehoahong.com nhưng không sao chép giao diện, thương hiệu, nội dung hoặc source code.

---

# 1. Nguyên tắc kiến trúc

Sử dụng:

- ABP.IO MVC Application Template
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- memorycache
- Hangfire cho background job
- Modular Monolith
- Không sử dụng Microservice ở giai đoạn hiện tại

Tuân thủ kiến trúc ABP:

```text
Domain
Domain.Shared
Application
Application.Contracts
EntityFrameworkCore
HttpApi
HttpApi.Client
Web/Angular
```

Business logic quan trọng phải nằm trong Domain/Application layer phù hợp.

Không đưa business logic vào Controller.

Không phụ thuộc trực tiếp business domain vào Shopee SDK/API implementation.

---

# 2. Thiết kế theo Provider

Hệ thống ban đầu chỉ tích hợp Shopee nhưng architecture phải hỗ trợ thêm:

```text
Shopee
TikTok Shop
Lazada
...
```

Không viết code kiểu:

```csharp
if (platform == "Shopee")
{
    ...
}
```

rải rác trong business logic.

Thiết kế abstraction:

```csharp
public interface IAffiliateProvider
{
    AffiliatePlatform Platform { get; }

    Task<AffiliateLinkResult> CreateAffiliateLinkAsync(
        CreateAffiliateLinkRequest request);

    Task<AffiliateOrderSyncResult> GetOrdersAsync(
        AffiliateOrderSyncRequest request);
}
```

Implementation Phase 1:

```text
ShopeeAffiliateProvider
```

Tương lai:

```text
TikTokAffiliateProvider
```

Business layer chỉ phụ thuộc `IAffiliateProvider`, không phụ thuộc trực tiếp Shopee.

---

# 3. Affiliate Platform

Tạo enum:

```csharp
public enum AffiliatePlatform
{
    Shopee = 1,
    TikTok = 2
}
```

Phase 1:

```text
Shopee = Enabled
TikTok = NotImplemented
```

Không implement TikTok ở Phase 1. Chỉ chuẩn bị architecture để thêm provider sau này.

---

# 4. PHASE 1 SCOPE

Phase 1 gồm:

```text
Authentication
├── Google Login
├── Email Registration
├── Email/Password Login
├── Forgot Password
└── User Profile

Shopee Affiliate
├── Paste Shopee URL
├── Validate URL
├── Generate Tracking Token
├── Generate Affiliate Link
└── Redirect to Shopee

Order Synchronization
├── Background Job
├── Incremental Sync
├── Pagination
├── Mapping sub_id → User
├── Idempotent UPSERT
└── Reconciliation

Commission
├── Platform Commission
├── User Share Rate
├── User Commission
└── Platform Revenue

Customer UI
├── Landing
├── Create Affiliate Link
├── My Orders
└── Account

Admin
├── Affiliate Settings
├── Commission Rule
├── Orders
└── Sync Monitoring
```

Không implement trong Phase 1:

```text
Product Search
TikTok integration
Wallet
Withdrawal
Bank Account
Automatic Payout
Referral
VIP
Membership
```

---

# 5. Authentication & User Account

Phase 1 bắt buộc có hệ thống tài khoản người dùng.

User có 2 cách đăng nhập:

```text
1. Đăng nhập / đăng ký bằng Google

2. Tự tạo tài khoản bằng:
   - Email
   - Password
```

Sử dụng hệ thống Identity/Account có sẵn của ABP.IO.

Không tự xây authentication framework mới nếu ABP đã hỗ trợ.

---

# 6. Authentication Flow

Landing page phải cho phép user nhìn thấy chức năng chính trước khi đăng nhập.

Flow ưu tiên:

```text
Guest
  ↓
Landing Page
  ↓
Paste Shopee Link
  ↓
Hệ thống validate link
  ↓
User muốn tạo Affiliate Link
  ↓
Nếu chưa Login
  ↓
Hiển thị Authentication
  ↓
┌────────────────────────────┐
│ [ Tiếp tục với Google ]    │
│                            │
│ -------- hoặc --------     │
│                            │
│ Email                      │
│ Password                   │
│                            │
│ [ Đăng nhập ]              │
│                            │
│ Chưa có tài khoản?         │
│ [ Đăng ký ]                │
└────────────────────────────┘
```

Sau khi login/register thành công:

```text
Không làm mất link Shopee user vừa nhập
        ↓
Khôi phục pending action
        ↓
Generate AffiliateTracking
        ↓
Generate Affiliate Link
        ↓
Hiển thị CTA "Mua trên Shopee"
```

Không bắt user:

```text
Paste Link
→ Login
→ quay lại Home
→ Paste Link lần nữa
```

---

# 7. Google Login

Implement:

```text
Continue with Google
```

Sử dụng Google OAuth/OIDC thông qua authentication infrastructure của ABP/ASP.NET Core.

Configuration:

```text
GoogleAuthenticationOptions
----------------------------
ClientId
ClientSecret
```

Không hard-code Google ClientId/ClientSecret trong source code.

Production secrets phải lấy từ Environment Variables, Secret Manager hoặc deployment secret store.

---

# 8. Local Account Registration

User có thể tự tạo account bằng:

```text
Email
Password
Confirm Password
```

Có:

```text
Register
Login
Logout
Forgot Password
Reset Password
```

Nếu project bật email confirmation thì support:

```text
Register
   ↓
Verification Email
   ↓
Confirm Email
   ↓
Account Active
```

Behavior này phải cấu hình theo environment/configuration.

---

# 9. Google Account vs Local Account

Cần xử lý trường hợp cùng một email.

Ví dụ:

```text
User trước đó đăng ký:
abc@gmail.com + Password

Sau đó bấm:
Continue with Google

Google trả:
abc@gmail.com
```

Không được tự động tạo hai customer profile khác nhau một cách mất kiểm soát.

Sử dụng cơ chế account/external-login của ABP/ASP.NET Identity phù hợp.

Phải có strategy rõ ràng cho:

```text
Local account
Google external login
Account linking
Duplicate email
```

Nếu không thể link an toàn tự động, yêu cầu user xác thực account hiện tại trước khi link Google identity.

---

# 10. Identity Rule

Affiliate domain sử dụng ABP User ID làm internal ownership:

```text
IdentityUser
    │
    │ UserId
    ▼
AffiliateTracking
    │
    ▼
AffiliateOrder
```

Affiliate URL chỉ chứa TrackingToken.

Không chứa:

```text
Email
Google ID
Phone
Internal UserId
```

---

# 11. User Profile

Phase 1 profile chỉ cần:

```text
Id
Email
Display Name
Avatar nullable
Authentication Provider
CreatedAt
```

Không tạo entity User riêng chỉ để duplicate toàn bộ `IdentityUser`.

Không lưu Google access token nếu không có business requirement.

---

# 12. Authorization

Có ít nhất 2 role:

```text
User
Admin
```

## User

```text
Generate Affiliate Link
View Own Affiliate Links
View Own Affiliate Orders
View Own Commission
```

## Admin

```text
View All Affiliate Orders
View Affiliate Tracking
View Users
View Sync Status
Manage Commission Rules
Manage Affiliate Settings
```

Áp dụng ABP Permission Management.

---

# 13. Current User

Mọi application service dành cho customer phải sử dụng:

```csharp
ICurrentUser
```

Không nhận `userId` từ frontend để xác định ownership.

Backend phải tự lấy `ICurrentUser.Id`.

---

# 14. Affiliate Link Flow

User nhập:

```text
https://shopee.vn/...
```

Frontend gọi:

```text
POST /api/app/affiliate-links
```

Request:

```json
{
  "url": "https://shopee.vn/..."
}
```

Backend:

```text
Validate URL
    ↓
Detect platform = SHOPEE
    ↓
Normalize URL
    ↓
Parse product information nếu có thể
    ↓
Generate tracking token
    ↓
Create AffiliateTracking
    ↓
Generate Shopee affiliate URL
    ↓
Return data
```

Response concept:

```json
{
  "platform": "Shopee",
  "originalUrl": "...",
  "affiliateUrl": "...",
  "trackingToken": "...",
  "estimatedCommission": null,
  "estimatedUserCashback": null
}
```

Không expose internal UserId trong affiliate URL.

Sinh opaque tracking token, ví dụ:

```text
AFF_H7K29DXF4P
```

Database lưu:

```text
AFF_H7K29DXF4P
        ↓
UserId
```

---

# 15. AffiliateTracking Entity

```text
AffiliateTracking
-----------------------------------
Id
UserId
Platform
TrackingToken
OriginalUrl
NormalizedUrl
AffiliateUrl
ShopId nullable
ProductId nullable
CreatedTime
LastClickTime nullable
ClickCount
Status
ExtraProperties
```

Rule:

```text
TrackingToken UNIQUE
```

Một user có thể tạo nhiều AffiliateTracking.

---

# 16. Affiliate Click

Có thể lưu click để analytics/debugging:

```text
AffiliateClick
-----------------------------------
Id
AffiliateTrackingId
UserId
Platform
ClickedAt
IpAddress nullable
UserAgent nullable
Referer nullable
```

Không dùng AffiliateClick làm source of truth cho commission.

Source of truth commission phải là Shopee Affiliate Conversion/Order.

---

# 17. Redirect / Buy Flow

Ưu tiên affiliate HTTPS URL chuẩn của Shopee.

Không tự tạo `shopee://...` trừ khi sau này đã xác minh attribution.

UI có CTA:

```text
[Mua trên Shopee]
```

Khi user click:

```text
Log click vào hệ thống
        ↓
Navigate sang Shopee Affiliate URL
```

Không dùng iframe để hiển thị Shopee.

Không proxy HTML của Shopee.

Không scrape Shopee.

---

# 18. Shopee API Integration

Tạo integration layer riêng:

```text
Integrations
└── Shopee
    ├── ShopeeAffiliateClient
    ├── ShopeeAffiliateProvider
    ├── DTO
    ├── Authentication
    ├── Mapping
    └── Configuration
```

Configuration:

```text
ShopeeAffiliateOptions
--------------------------------
AffiliateId
AppId / Credential
Secret
ApiBaseUrl
Enabled
```

Secret không hard-code.

Nếu chưa có official API documentation hoặc credential:

- Tạo interface.
- Tạo DTO nội bộ.
- Tạo Fake/Mock provider phục vụ development.
- Đánh dấu TODO rõ ràng.
- Không gọi unofficial/private Shopee API.
- Không scrape Shopee website.

---

# 19. Affiliate Order Sync

Sử dụng Hangfire.

Tạo job:

```text
ShopeeAffiliateOrderSyncJob
```

Flow:

```text
Hangfire
    ↓
ShopeeAffiliateProvider
    ↓
Get conversions/orders
    ↓
Pagination
    ↓
For each record
    ↓
Read sub_id/tracking value
    ↓
Find AffiliateTracking
    ↓
Determine User
    ↓
UPSERT AffiliateOrder
```

Không tạo một job Shopee cho mỗi user.

---

# 20. Incremental Sync

Không quét toàn bộ lịch sử mỗi lần.

Entity:

```text
AffiliateSyncState
-----------------------------------
Id
Platform
LastSuccessfulSyncAt
LastAttemptAt
LastError
Status
```

Job sử dụng watermark và overlap window để tránh eventual consistency.

Ví dụ configurable:

```text
SyncIntervalMinutes = 5
OverlapMinutes = 15
```

Không hard-code.

---

# 21. Idempotency

Sync job bắt buộc idempotent.

Shopee trả cùng conversion nhiều lần không được tạo duplicate order.

Thiết kế unique business key phù hợp dựa trên dữ liệu thực tế Shopee cung cấp.

Ví dụ conceptual:

```text
Platform
+
ExternalOrderId
+
ExternalItemId
```

Không assume schema Shopee nếu chưa có documentation.

---

# 22. AffiliateOrder

```text
AffiliateOrder
-----------------------------------
Id
Platform
ExternalOrderId
ExternalItemId nullable
AffiliateTrackingId nullable
UserId nullable
TrackingToken
ProductName nullable
ProductId nullable
ShopId nullable
OrderAmount nullable
PlatformCommission
UserShareRate
UserCommission
PlatformRevenue
Currency
OrderStatus
CommissionStatus
OrderedAt nullable
CompletedAt nullable
CancelledAt nullable
PlatformCreatedAt nullable
PlatformUpdatedAt nullable
FirstSyncedAt
LastSyncedAt
RawReference nullable
```

Nếu không map được TrackingToken:

```text
AffiliateTrackingId = null
UserId = null
Status = Unmatched
```

Không bỏ mất record.

---

# 23. Order Status

Internal enum:

```text
Pending
Confirmed
Completed
Cancelled
Refunded
Rejected
Unknown
```

Commission status:

```text
Estimated
Pending
Approved
Rejected
PaidByPlatform
```

Mapping Shopee status → internal status phải nằm trong `ShopeeOrderMapper`.

---

# 24. Commission Calculation

Phase 1:

```text
UserCommission
=
PlatformCommission
×
UserShareRate
```

Ví dụ:

```text
Shopee trả: 100,000
UserShareRate = 70%
UserCommission = 70,000
PlatformRevenue = 30,000
```

Actual Shopee commission là source of truth.

Không tính user commission trực tiếp từ order value nếu đã có actual commission.

---

# 25. Commission Rule

Không hard-code 70%.

Entity:

```text
AffiliateCommissionRule
--------------------------------
Id
Platform
UserShareRate
EffectiveFrom
EffectiveTo nullable
IsActive
```

Phase 1 chỉ cần:

```text
Shopee = 70%
```

Architecture phải cho phép sau này:

```text
Shopee = 70%
TikTok = 65%
VIP = 80%
Normal = 70%
```

Chưa implement VIP ở Phase 1.

---

# 26. Commission Snapshot

Khi tính commission cho một order, phải lưu snapshot tỷ lệ đã sử dụng:

```text
PlatformCommission = 100000
UserShareRate = 0.70
UserCommission = 70000
PlatformRevenue = 30000
```

Nếu Admin đổi 70% → 80%, không tự động thay đổi lịch sử order đã tính trước đó.

---

# 27. Reconciliation

Order có thể thay đổi sau sync đầu tiên:

```text
Pending → Completed
Pending → Cancelled
Completed → Refunded
```

Job phải update order hiện có.

Không assume conversion là immutable.

---

# 28. User API

```text
POST /api/app/affiliate-links
GET  /api/app/affiliate-orders
GET  /api/app/affiliate-orders/{id}
```

Danh sách order chỉ trả order thuộc current authenticated user.

---

# 29. UI/UX Reference

Sử dụng:

```text
https://sharehoahong.com
```

làm reference về UX simplicity và information hierarchy.

Không clone:

```text
Brand
Logo
Copywriting
Source code
Assets
CSS
Illustrations
```

Mục tiêu UX:

```text
Simple
Trustworthy
Mobile-first
Fast
Consumer-oriented
Ít bước
CTA rõ ràng
```

Customer-facing website không được mang phong cách Enterprise Admin Dashboard.

---

# 30. Landing Page

Homepage Phase 1 tập trung vào một hành động:

```text
DÁN LINK SHOPEE
```

Cho phép guest Paste Link + Validate URL.

Chỉ bắt authentication trước khi tạo personalized AffiliateTracking.

---

# 31. Login UX

Có:

```text
[ Tiếp tục với Google ]

hoặc

Email
Password
[ Đăng nhập ]

Quên mật khẩu?
Chưa có tài khoản? Đăng ký
```

Google login là CTA nổi bật, nhưng email/password vẫn được hỗ trợ đầy đủ.

---

# 32. Registration UX

Có:

```text
[ Đăng ký bằng Google ]

hoặc

Email
Password
Confirm Password

[ Tạo tài khoản ]
```

Không yêu cầu Phase 1:

```text
Ngày sinh
Địa chỉ
CCCD
Tài khoản ngân hàng
Giới tính
```

---

# 33. Authenticated Home

Navigation customer:

```text
Trang chủ
Đơn của tôi
Tài khoản
```

Có thể thêm:

```text
Cách hoạt động
```

Không hiển thị menu cho feature chưa implement:

```text
Wallet
Withdrawal
TikTok
Product Search
VIP
Referral
```

---

# 34. How It Works

Landing page:

```text
1. Dán link Shopee
2. Hệ thống tạo Affiliate Link riêng
3. Mua qua link
4. Khi Shopee ghi nhận đơn, đơn và hoa hồng tự động xuất hiện
```

Phase 1 chưa có payout nên không viết claim về tự động chuyển tiền ngân hàng.

---

# 35. Product Link Result

Sau khi validate Shopee URL:

```text
Shopee
Link sản phẩm hợp lệ

Hoa hồng dự kiến:
- Nếu API có dữ liệu: hiển thị estimate
- Nếu chưa có dữ liệu: "Hoa hồng sẽ được cập nhật khi Shopee ghi nhận giao dịch."

[ Mua trên Shopee ]
```

Không fake commission.

---

# 36. Mobile First

Design mobile-first.

Phải test:

```text
Android Chrome
iPhone Safari
Responsive Desktop
```

CTA `Mua trên Shopee` phải lớn, rõ ràng và touch-friendly.

---

# 37. In-App Browser Warning

Chuẩn bị capability phát hiện:

```text
Facebook Browser
Messenger Browser
Zalo Browser
TikTok Browser
```

Không tự block các browser này ở Phase 1 nếu chưa có kết quả attribution test.

Feature cảnh báo phải configuration-driven.

---

# 38. Customer UI vs Admin UI

Tách rõ Customer Website và Admin.

Customer:

```text
Consumer friendly
Simple
Minimal
Mobile first
```

Admin:

```text
Data dense
Filters
Tables
Operations
Monitoring
```

Không tái sử dụng nguyên giao diện ABP Admin cho customer-facing home nếu làm UX giống hệ thống quản trị.

---

# 39. Affiliate Orders UI

Trang:

```text
Đơn hàng của tôi
```

Hiển thị:

```text
Order
Product
Order Amount
Shopee Commission
Bạn nhận
Status
Order Date
Last Updated
```

---

# 40. Empty State

Nếu user vừa mua nhưng conversion chưa sync:

```text
Chưa có đơn hàng mới.

Shopee có thể cần một khoảng thời gian để ghi nhận đơn Affiliate.

Đơn hàng sẽ tự động xuất hiện sau khi Shopee xác nhận.
```

Không cam kết thời gian nếu Shopee không guarantee.

---

# 41. Refresh Order

Có thể có button:

```text
[Kiểm tra đơn mới]
```

Nhưng KHÔNG call Shopee API trực tiếp cho từng click.

MVP có thể refresh DB của hệ thống hoặc enqueue priority sync request.

Phải có cooldown/debounce.

---

# 42. Admin Phase 1

Admin cần:

```text
Affiliate Settings
- Shopee Enabled
- Affiliate ID
- User Share Rate
```

Affiliate Orders:

```text
Search External Order Id
Search Tracking Token
Search User
Platform
Status
Commission Status
Date
```

Sync Monitoring:

```text
Last Sync
Sync Status
Fetched
Inserted
Updated
Unmatched
Errors
```

Phase 1 chưa cần payout.

---

# 43. Audit

Sử dụng ABP Audit Logging khi phù hợp.

Log:

```text
Affiliate link generated
Commission rule changed
Shopee sync failed
Shopee sync recovered
```

Không log Secret/API key/Access token.

---

# 44. Error Handling

Phân biệt:

```text
InvalidShopeeUrl
UnsupportedPlatform
AffiliateProviderUnavailable
ShopeeApiRateLimited
ShopeeAuthenticationFailed
ShopeeApiTimeout
AffiliateTrackingNotFound
DuplicateConversion
```

External API failure không được làm crash application.

Retry có giới hạn, không infinite retry.

---

# 45. Rate Limit Protection

Shopee API phải đi qua:

```text
ShopeeAffiliateClient
```

Chuẩn bị:

```text
Rate limit
Retry
Backoff
Timeout
Circuit breaker
```

User traffic không được translate trực tiếp thành Shopee API traffic.

---

# 46. Distributed Deployment

Nếu chạy nhiều instance:

```text
Web 1
Web 2
Web 3
```

không được để tất cả instance cùng sync mất kiểm soát.

Sử dụng Hangfire distributed processing hoặc ABP distributed lock.

---

# 47. Security

Bắt buộc:

- Authentication
- Authorization
- Không expose secret
- Không expose internal UserId qua affiliate URL
- Validate external URLs
- Chỉ cho phép supported Shopee domain
- Chống open redirect
- Encode URL đúng cách
- Không nhận arbitrary redirect destination
- Không cho user sửa tracking token của user khác
- Rate-limit endpoint generate link khi cần

---

# 48. Observability

Structured logs:

```text
AffiliateProvider
SyncStart
SyncEnd
Page
FetchedCount
InsertedCount
UpdatedCount
UnmatchedCount
ErrorCount
Duration
```

Không log sensitive credential.

---

# 49. PHASE 2 — PRODUCT SEARCH

Không implement Phase 2 ngay.

Goal:

```text
User vào web app
    ↓
Search "iphone 17"
    ↓
Hiển thị sản phẩm Shopee
    ↓
Price
Image
Shop
Commission
Estimated Cashback
    ↓
User chọn
    ↓
Affiliate Link
    ↓
Shopee
```

Architecture dự kiến:

```text
ProductCatalog
ProductOffer
ProductSearch
```

Nếu Shopee Affiliate API hỗ trợ keyword search:

```text
Our API
    ↓
Shopee Product API
    ↓
Cache
```

Nếu Shopee chỉ cho Product Offer:

```text
Shopee Product Offer
        ↓
Catalog Sync Job
        ↓
Our Product DB
        ↓
Search Engine
        ↓
User
```

Không scrape Shopee search page.

Không sử dụng private Shopee endpoint.

Không implement cho đến khi official API capability được xác minh.

---

# 50. Chuẩn bị cho TikTok

Không implement TikTok trong Phase 1.

Đảm bảo:

```text
AffiliateTracking
AffiliateOrder
CommissionRule
AffiliateProvider
```

không phụ thuộc Shopee.

Shopee-specific fields chỉ nằm trong Shopee integration layer.

Nếu TikTok không hỗ trợ tracking `sub_id` giống Shopee thì TikTok Provider được phép có attribution strategy riêng.

---

# 51. Coding Standard

Tuân thủ:

- SOLID
- Clean Architecture theo ABP
- DDD ở mức hợp lý
- Async/await
- CancellationToken
- Repository abstraction của ABP
- UnitOfWork
- AutoMapper khi phù hợp
- DTO validation
- Localization
- Permission definition
- Avoid magic string
- Avoid magic number

Không over-engineering.

---

# 52. Testing

## Authentication

```text
Register bằng email/password
Login thành công
Login sai password
Google login flow
Duplicate email handling
Authorization
```

## Affiliate link

```text
Valid Shopee URL
Invalid URL
Unsupported platform
Tracking token unique
User mapping đúng
```

## Commission

```text
100000 × 70% = 70000
PlatformRevenue = 30000
```

## Sync

```text
New order
Existing order update
Duplicate conversion
Unknown tracking token
Cancelled order
Refunded order
Provider timeout
Rate limit
```

## Authorization

```text
User A không được xem order User B
```

---

# 53. Acceptance Criteria Phase 1

### AC01
Guest có thể truy cập Landing Page mà không cần đăng nhập.

### AC02
Guest có thể paste một Shopee URL và hệ thống validate URL.

### AC03
Authentication bắt buộc trước khi tạo personalized AffiliateTracking.

### AC04
User có thể đăng ký bằng email/password.

### AC05
User có thể đăng nhập bằng email/password.

### AC06
User có Forgot Password / Reset Password.

### AC07
User có thể đăng nhập/đăng ký bằng Google.

### AC08
Google ClientSecret không tồn tại trong frontend hoặc source repository.

### AC09
Sau login thành công, pending Shopee URL/action được khôi phục; user không phải paste lại link.

### AC10
System tạo AffiliateTracking + TrackingToken + Shopee affiliate URL.

### AC11
Tracking token map chính xác về user tạo link.

### AC12
User có thể click CTA và được chuyển sang Shopee affiliate URL.

### AC13
Backend có scheduled job lấy conversion/order từ Shopee provider.

### AC14
Job hỗ trợ pagination.

### AC15
Job hỗ trợ incremental synchronization.

### AC16
Job idempotent.

### AC17
Conversion có tracking token hợp lệ được map về đúng user.

### AC18
AffiliateOrder được insert/update đúng.

### AC19
Actual commission từ provider được lưu.

### AC20
User commission được tính:

```text
Actual Platform Commission
×
UserShareRate
```

### AC21
User chỉ xem được order của chính mình.

### AC22
Admin xem được tất cả affiliate orders.

### AC23
Order cancelled/refunded được cập nhật trong các lần sync tiếp theo.

### AC24
Shopee API lỗi không làm hệ thống crash.

### AC25
Không có Shopee credential trong frontend/source/log.

### AC26
AffiliateTracking map vào đúng authenticated `UserId`.

### AC27
User A không thể xem AffiliateTracking/AffiliateOrder của User B.

### AC28
Logout phải hủy authenticated session/token đúng cách.

### AC29
Nếu cùng email xuất hiện ở local account và Google external login, hệ thống không tự tạo duplicate customer mất kiểm soát.

---

# 54. Cách thực hiện

Không code toàn bộ hệ thống trong một lần.

Thực hiện tuần tự:

```text
Step 1
Review ABP solution hiện tại.

Step 2
Đề xuất module/folder/entity design.

Step 3
Liệt kê assumptions và technical unknowns.

Step 4
Implement Authentication.

Step 5
Implement Affiliate domain.

Step 6
Implement Generate Affiliate Link.

Step 7
Implement Shopee Provider abstraction.

Step 8
Implement Mock Shopee Provider nếu chưa có API credentials.

Step 9
Implement AffiliateOrder.

Step 10
Implement Hangfire sync.

Step 11
Implement commission calculation.

Step 12
Implement Customer UI.

Step 13
Implement Admin UI.

Step 14
Tests.

Step 15
Review architecture để chắc chắn TikTok có thể thêm sau này.
```

Sau mỗi step:

- Build project.
- Fix compilation error.
- Chạy test liên quan.
- Không để broken build rồi tiếp tục step khác.

---

# 55. Quan trọng

Nếu Shopee official API documentation chưa xác nhận một capability thì KHÔNG được tự giả định.

Ví dụ:

```text
Product search
Product commission lookup
Conversion field
sub_id schema
Pagination
Rate limit
```

Hãy:

1. Tạo abstraction phù hợp.
2. Đánh dấu technical discovery.
3. Mock dữ liệu nếu cần phát triển tiếp.
4. Chờ official documentation/credential trước khi implement production integration.

Không sử dụng scraping/private API như shortcut.

---

# 56. Output đầu tiên cần trả về

Trước khi bắt đầu code, hãy trả về:

```text
1. Current solution assessment
2. Proposed modules
3. Domain entities
4. Entity relationships
5. Application services
6. Authentication architecture
7. Shopee integration architecture
8. Background job architecture
9. Database tables
10. API endpoints
11. Customer sitemap
12. Landing page wireframe
13. Login/Register flow
14. My Orders wireframe
15. Phase 1 implementation plan
16. Technical unknowns cần xác minh với Shopee
17. Các điểm trong kiến trúc đã chuẩn bị cho TikTok
18. Những phần cố tình để sang Phase 2
```

Sau đó mới bắt đầu implement Phase 1.
