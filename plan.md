Kế hoạch Phase 1 — webHoanTien.com
Tóm tắt quyết định
Xóa toàn bộ tính năng, dữ liệu mẫu, migration, UI và asset iZone; tạo database mới, không migrate dữ liệu cũ.
Đổi toàn bộ solution/project/namespace thành WebHoanTien.
Giữ .NET 8, ABP.IO 8.3.4, MVC/Razor Pages, PostgreSQL; tắt Multi-Tenancy.
Tích hợp Shopee Open API thật theo tài liệu chính thức; Mock chỉ dùng development/test.
Phase 1 chạy một Web instance bằng Docker Compose; Hangfire dùng PostgreSQL.
UI tiếng Việt, mobile-first, nhận diện mới hoàn toàn.
Commission được Admin cấu hình theo rule hiệu lực, tính trên netCommission và chọn theo thời điểm đặt hàng.
1. Đánh giá solution hiện tại
Solution ABP layered monolith đầy đủ: Domain.Shared, Domain, Application.Contracts, Application, EF Core, HttpApi, HttpApi.Client, Web, DbMigrator và test.
Frontend là MVC/Razor Pages, không có Angular.
PostgreSQL đã được cấu hình; Multi-Tenancy hiện đang bật.
Toàn bộ nghiệp vụ hiện tại là hệ thống giáo dục iZone: entity, seed, permission, migration, Razor Pages, CSS, asset và tài liệu triển khai.
Chưa có Hangfire, Google OAuth, Shopee client hoặc affiliate domain.
Một số cấu hình mẫu nhạy cảm vẫn nằm trong appsettings.json; phải chuyển hoàn toàn sang environment/user-secrets.
Baseline hiện build thành công; EF tests 3/3 và Web test 1/1. Ba test project chưa phát hiện test thực thi và Web.Tests có warning CS7022.
Worktree không có thay đổi tracked; file master prompt đang untracked.
2. Module và ranh giới kiến trúc
Giữ số lượng project ABP hiện tại, tổ chức bounded context bằng namespace/folder thay vì tạo quá nhiều project:
Affiliates: tracking, conversion, order, click, commission.
Integrations: provider contract và registry.
Integrations.Shopee: GraphQL client, authentication, DTO, mapper.
Operations: Hangfire jobs, sync state, monitoring, retention.
IdentityExtensions: Google linking, profile extension, legal consent.
CustomerWeb: landing, link result, link history, orders, account.
Admin: settings, rules, orders, unmatched conversions, sync monitoring.
Legal: Terms, Privacy và versioned consent.
Business domain chỉ phụ thuộc IAffiliateProvider; Shopee-specific DTO/status không đi vào core domain.
3. Domain entities
AffiliateTracking: owner ABP UserId, platform, opaque token, original/normalized/affiliate URL, product/shop metadata, estimate, click counters và trạng thái.
AffiliateClick: tracking, user, thời điểm, IP thô, User-Agent và referer; tự xóa sau 90 ngày.
AffiliateConversion: external conversion ID, tracking/user mapping, purchase/click time, gross/net commission, tỷ lệ chia snapshot, user commission và platform revenue.
AffiliateOrder: conversion, external order ID, order status, shop type và các tổng đã phân bổ.
AffiliateOrderItem: item/model ID, sản phẩm, giá trị mua, số lượng, commission thành phần, allocated net commission, refund/fraud/status.
AffiliateCommissionRule: platform, UserShareRate, EffectiveFrom/To, active flag; không cho khoảng hiệu lực chồng lấn.
AffiliateSyncState: watermark và trạng thái theo platform/sync kind.
AffiliateSyncRun: thời gian chạy, fetched/inserted/updated/unmatched/error và lỗi tổng hợp.
AffiliateRawPayload: payload Shopee đã lọc/mask, liên kết sync/conversion và tự xóa sau 90 ngày.
UserLegalConsent: UserId, TermsVersion, PrivacyVersion, method và thời điểm chấp thuận.
Không tạo entity User trùng với IdentityUser; avatar/authentication provider dùng Identity extension/external login.
4. Quan hệ entity
IdentityUser
 ├─ AffiliateTracking ── AffiliateClick
 │          │
 │          └─ AffiliateConversion
 │                    └─ AffiliateOrder
 │                          └─ AffiliateOrderItem
 └─ UserLegalConsent

AffiliateCommissionRule ──> AffiliateConversion (snapshot theo purchaseTime)

AffiliateSyncState ── AffiliateSyncRun ── AffiliateRawPayload
Unique keys:
Tracking token toàn hệ thống.
Một tracking active cho UserId + Platform + NormalizedUrl.
Platform + ExternalConversionId.
ConversionId + ExternalOrderId.
OrderId + ExternalItemId + normalized ModelId.
5. Application services
AffiliateLinkAppService: validate guest URL, tạo/tái sử dụng tracking, tạo Shopee short link và lịch sử link.
AffiliateOrderAppService: danh sách/chi tiết order của ICurrentUser, yêu cầu sync ưu tiên.
CustomerProfileAppService: profile tổng hợp từ Identity và external login.
AdminAffiliateSettingsAppService: cài đặt không nhạy cảm và trạng thái kết nối.
AdminCommissionRuleAppService: rule version hóa theo thời gian.
AdminAffiliateOrderAppService: tìm kiếm conversion/order, unmatched mapping.
AdminAffiliateSyncAppService: Sync Now, Reconcile và lịch sử job.
Domain managers: URL normalization, tracking token, commission calculation/allocation và reconciliation.
Mọi customer service lấy owner từ ICurrentUser.Id; không nhận UserId từ frontend.
6. Authentication architecture
Dùng ABP Identity, Account và OpenIddict; single-tenant.
Email registration chỉ yêu cầu email/password/confirm; username nội bộ bằng normalized email.
Production bắt buộc email confirmation; development điều khiển bằng config.
Forgot/Reset Password dùng IEmailSender và SMTP.
Google OAuth dùng ClientId/Secret từ secret store; callback production dự kiến /signin-google.
Email Google trùng local account: yêu cầu đăng nhập local trước, sau đó mới liên kết Google.
User mới từ Google phải chấp thuận Terms/Privacy trước khi hoàn tất tạo tài khoản.
Pending Shopee action lưu trong cookie mã hóa, có nonce và TTL; sau login tiếp tục tạo link ngay.
Google avatar được dùng khi có; local account hiển thị initials, không triển khai upload.
Role User tự gán khi đăng ký; Admin đầu tiên seed từ environment secrets và buộc đổi mật khẩu.
7. Shopee integration architecture
Endpoint: POST https://open-api.affiliate.shopee.vn/graphql.
Authorization SHA-256 từ AppId + Timestamp + exact payload + Secret.
Production client hỗ trợ:generateShortLink(originUrl, subIds).
productOfferV2(itemId) để lấy estimate sau đăng nhập.
conversionReport.
validatedReport.

Chỉ truyền một opaque tracking token trong subIds; không truyền email, Google ID hoặc UserId.
utmContent được mapper tách và map về tracking.
netCommission là nguồn chia cashback; fallback sang totalCommission chỉ khi có policy rõ và ghi lại nguồn.
Net commission được phân bổ xuống item theo tỷ trọng itemTotalCommission; phần dư làm tròn gán có tính xác định để tổng luôn khớp.
Làm tròn VND đến một đồng theo MidpointRounding.AwayFromZero.
Product estimate được cache ngắn hạn; lỗi estimate không chặn tạo short link.
HTTP client có timeout, bounded retry/backoff, circuit breaker và xử lý GraphQL errors dù HTTP status là 200.
Production không bao giờ tự động fallback sang Mock; Mock chỉ được bật rõ ràng ở development/test.
8. Background job architecture
Hangfire dùng PostgreSQL storage và dashboard chỉ dành cho Admin.
Conversion sync mỗi 60 phút, overlap mặc định 15 phút.
Daily reconciliation rà lại cửa sổ tối đa ba tháng.
Lần sync đầu tiên bị khóa cho đến khi Admin chọn ngày bắt đầu trong phạm vi Shopee cho phép.
Pagination dùng scrollId, tối đa 500 record/trang và hoàn thành chuỗi cursor trước khi hết 30 giây.
validatedReport cập nhật commission từ Estimated/Pending sang Approved; không tự gán PaidByPlatform nếu chưa có bằng chứng payout chính thức.
Nút “Kiểm tra đơn mới” có cooldown 15 phút/user; các yêu cầu gần nhau được coalesce thành một job toàn hệ thống.
Admin được Sync Now và Reconcile khoảng ngày tối đa ba tháng.
Dùng Hangfire concurrency control/distributed lock để sẵn sàng scale-out.
Retention job xóa raw payload, IP và User-Agent sau 90 ngày.
9. Database
Database mới: webhoantien; schema nghiệp vụ affiliate, schema Hangfire riêng.
Xóa migration iZone và tạo InitialWebHoanTien mới.
Giữ các bảng ABP cần thiết: Identity, Permission, Setting, Audit Logging, OpenIddict.
Loại Tenant Management và toàn bộ bảng education/iZone.
Commission dùng numeric phù hợp tiền tệ; external IDs dùng kiểu không làm mất dữ liệu Shopee.
Concurrency token/index hỗ trợ idempotent UPSERT.
Database iZone/volume cũ không được ứng dụng mới sử dụng; không tự động xóa vật lý volume ngoài quy trình triển khai.
10. API endpoints
Customer/public:
POST /api/app/affiliate-links/validate — anonymous, chỉ validate/normalize cục bộ.
POST /api/app/affiliate-links — authenticated, tạo hoặc tái sử dụng tracking.
GET /api/app/affiliate-links
GET /api/app/affiliate-links/{id}
GET /go/{trackingToken} — log click và redirect 302 tới URL đã lưu.
GET /api/app/affiliate-orders
GET /api/app/affiliate-orders/{id}
POST /api/app/affiliate-orders/sync-requests
Admin:
Settings get/update và connection health.
Commission rule list/create/activate.
Conversion/order/search/unmatched endpoints.
Manual match chỉ nhận AffiliateTracking ID; UserId được suy ra.
Sync state/run list, Sync Now và Reconcile.
URL validation dùng exact-host allowlist, HTTPS, giới hạn redirect, timeout/response size, kiểm tra mọi hop và chặn private/reserved IP để chống SSRF/open redirect.
11. Customer sitemap
/
├─ Cách hoạt động
├─ Đăng nhập
├─ Đăng ký
├─ Quên/Đặt lại mật khẩu
├─ Kết quả tạo link
├─ Link đã tạo        (không chiếm menu chính)
├─ Đơn của tôi
├─ Tài khoản
├─ Điều khoản
└─ Chính sách riêng tư
Menu chính sau đăng nhập: Trang chủ, Đơn của tôi, Tài khoản.
Admin tiếp tục dùng LeptonX Lite; customer UI dùng layout riêng hoàn toàn.
12. Landing page wireframe
[Logo webHoanTien.com]        [Cách hoạt động] [Đăng nhập]

        Mua sắm Shopee,
        nhận lại một phần hoa hồng

 [ Dán link sản phẩm Shopee................ ]
 [ Kiểm tra link / Tạo link mua hàng         ]

 [Trạng thái URL hợp lệ hoặc lỗi rõ ràng]

  1. Dán link  →  2. Tạo link riêng  →  3. Mua qua link
                    → 4. Theo dõi đơn và cashback

 [Giải thích thời gian Shopee ghi nhận]
 [Không cam kết payout hoặc thời gian không được Shopee bảo đảm]
 [Disclaimer không phải sản phẩm chính thức của Shopee]

 [Điều khoản] [Riêng tư] [Liên hệ]
Guest được validate URL trước. Chỉ khi tạo tracking mới chuyển sang auth. Thiết kế mới không sao chép màu, logo, copy hoặc asset của ShareHoaHong/iZone.
13. Login/Register flow
Guest dán link → validate → bấm tạo link
→ lưu pending action trong cookie mã hóa
→ Login/Register tùy biến

[Tiếp tục với Google]
hoặc
[Email] [Password] [Đăng nhập]

Register:
[Email] [Password] [Confirm Password]
[Checkbox Terms/Privacy bắt buộc]

→ xác minh email nếu production
→ khôi phục pending action
→ tạo/reuse AffiliateTracking
→ gọi Shopee generateShortLink
→ hiển thị estimate nếu có
→ CTA “Mua trên Shopee”
Google trùng email sẽ chuyển sang flow xác thực local rồi liên kết, không tạo duplicate profile.
14. My Orders wireframe
ĐƠN HÀNG CỦA TÔI                  [Kiểm tra đơn mới]

[Bộ lọc trạng thái] [Khoảng ngày]

┌ Order ID · Ngày đặt · Trạng thái
│ Tổng giá trị
│ Hoa hồng Shopee thực nhận
│ Bạn được nhận
│ Cập nhật lần cuối
│ [Xem sản phẩm ▾]
└

Empty state:
“Shopee có thể cần thời gian để ghi nhận giao dịch.
Đơn sẽ xuất hiện sau khi Shopee xác nhận.”
Một card cho mỗi order; mở rộng để xem item. Cancelled/Refunded/Rejected giữ snapshot để audit nhưng payable cashback bằng 0.
15. Trình tự triển khai và kiểm thử
Đổi tên solution/project/namespace; loại iZone, Multi-Tenancy, seed, docs và asset cũ.
Làm sạch cấu hình/secrets; tạo Docker/PostgreSQL baseline mới.
Tạo domain entities, mapping, migration và permissions.
Hoàn thiện Identity, User/Admin roles, email confirmation, SMTP và legal consent.
Tích hợp Google login/account linking và pending-action flow.
Xây URL validator, tracking token, link reuse và redirect logging.
Tạo provider registry và production Shopee GraphQL client.
Implement short link và Product Offer estimate.
Implement conversion/order/item UPSERT và status mapper.
Implement commission rule, snapshot, net allocation và rounding.
Tích hợp Hangfire sync, reconciliation, priority request và retention.
Xây Customer UI.
Xây Admin UI và sync monitoring.
Viết PostgreSQL tests bằng Testcontainers; không dùng SQLite.
Hoàn thiện Docker Compose, health/readiness, structured logging và deployment runbook.
Sau mỗi bước: build toàn solution, chạy test liên quan và không tiếp tục nếu build đang hỏng.
Test bắt buộc:
Email/Google registration, duplicate email linking, consent, confirm/reset password và pending action.
Valid/invalid/short Shopee URL, SSRF/open redirect, token uniqueness và reuse.
SHA-256 signing fixtures, GraphQL error/rate-limit/timeout.
Pagination cursor, idempotent UPSERT, status reconciliation và unmatched mapping.
100000 × 70% = 70000, allocation nhiều item và rounding residual.
Cancel/refund payable bằng 0 nhưng giữ snapshot.
User A không xem được dữ liệu User B.
Admin permissions, manual match và sync audit.
Live Shopee smoke test chỉ chạy khi có credential cùng biến opt-in.
Manual QA trên Android Chrome, iPhone Safari và responsive desktop.
16. Technical unknowns cần xác minh bằng credential thật
Quyền API thực tế của AppId và khả năng gọi từng query.
Giới hạn độ dài/ký tự của subIds và định dạng thực tế của utmContent.
Domain short-link đầu vào/đầu ra và chuỗi redirect thực tế.
Hành vi validatedReport khi không truyền validationId.
Mapping refund một phần, fraud và displayItemStatus sang internal enum.
Precision thực tế của các commission string.
Việc một conversion chứa nhiều order/item trong dữ liệu thật.
Rate limit/quy định cursor có thể thay đổi so với tài liệu hiện tại.
SMTP sender/domain, Google ClientId/Secret và production callback domain.
PaidByPlatform chưa được tự động hóa nếu Open API không cung cấp trạng thái payout rõ ràng.
17. Chuẩn bị cho TikTok
Core entity chỉ dùng AffiliatePlatform, external IDs và generic status.
Provider registry chọn implementation theo platform; không có if Shopee rải rác.
Attribution strategy nằm trong provider contract vì TikTok có thể không dùng sub-ID giống Shopee.
Shopee GraphQL DTO, signature và mapper chỉ tồn tại trong integration layer.
Commission calculator nhận normalized provider data.
Domain/API/UI không hiển thị TikTok khi provider chưa được triển khai.
18. Cố ý để sang Phase 2
Product Search UI/API/database/cache/catalog sync.
Dù productOfferV2 đã xác nhận hỗ trợ keyword, Phase 1 chỉ ghi ADR và không tạo schema/code chết.
TikTok/Lazada provider.
Wallet, withdrawal, bank account và automatic payout.
Referral, VIP, membership.
Product catalog nội bộ/search engine.
Avatar upload/file storage.
Tự động xác nhận PaidByPlatform nếu chưa có API chính thức.



19. Quyết định thay thế Shopee Open API cho Phase 1
Nội dung phần này thay thế mọi mô tả mâu thuẫn ở các phần trước về `generateShortLink`, `conversionReport`, `validatedReport`, Shopee AppId/Secret, job sync và reconciliation tự động.

PRODUCT DATA
AddLiveTag Product Data API: `https://data.addlivetag.com/product-data/product-data.php`.
Gọi bằng `item_id` khi URL đã trích xuất được item ID; chỉ dùng link gốc khi không có item ID.
Chỉ dùng productName, imageUrl và commission trả về làm estimate hiển thị khi tạo link.
Estimate không tạo AffiliateConversion, không tạo AffiliateOrder và không được dùng để tính payable cashback.
Không gọi AddLiveTag để tạo link affiliate hoặc lấy actual order data.

AFFILIATE LINK
App tự tạo URL Shopee từ URL gốc đã chuẩn hóa bằng query `affiliate_id={SHOPEE_AFFILIATE_ID}` và `sub_id={trackingToken}`.
`sub_id` chỉ là opaque tracking token ngẫu nhiên; không chứa email, UserId hay thông tin định danh.
Khi tạo lại link, luôn ghi đè affiliate_id/sub_id có sẵn trong URL đầu vào bằng giá trị của app.
Không dùng Shopee Open API, AppId, API Secret hoặc thư viện tạo short link để tạo link affiliate.

ACTUAL ORDER DATA
Admin xuất báo cáo actual order/commission từ Shopee Affiliate Portal và import thủ công vào hệ thống.
Phase 1 hỗ trợ CSV/TXT UTF-8, tự nhận diện phân cách `,`, `;` hoặc tab.
Các cột bắt buộc (tên Việt/Anh đều được map): Mã đơn hàng/Order ID, Sub ID, Thời gian đặt hàng/Purchase Time và Hoa hồng thực nhận/Net Commission.
Các cột tùy chọn: Conversion ID, trạng thái đơn, giá trị đơn, Item ID, Model ID, tên sản phẩm, số lượng, tiền hoàn và fraud status.
Import idempotent theo Platform + Conversion ID và Conversion + Order + Item; report không ghép được sub_id vẫn lưu để Admin manual match.

PAYABLE COMMISSION
Chỉ AffiliateConversion/Order được upsert từ Shopee actual report mới có thể sinh payable commission.
Số payable tiếp tục dùng net commission thực tế và commission rule snapshot theo purchaseTime.
Cancelled, Refunded và Rejected giữ snapshot audit nhưng payable bằng 0.
Không có job hourly sync, priority sync, reconciliation API hoặc nút “Kiểm tra đơn mới” cho customer.

CONFIGURATION VÀ VẬN HÀNH
Production chỉ cần `SHOPEE_AFFILIATE_ID`; có thể override `SHOPEE_PRODUCT_DATA_ENDPOINT` khi AddLiveTag thay đổi endpoint.
Không lưu Shopee Open API/Open Platform credential trong source, `.env.example` hay Docker Compose.
Admin import dùng `POST /api/app/admin/shopee-reports/import` dạng multipart/form-data với field `report`, giới hạn 5 MB.
Mỗi lần import lưu audit run và metadata header/file; raw payload retention vẫn áp dụng 90 ngày.

KIỂM THỬ BẮT BUỘC BỔ SUNG
Affiliate URL phải chứa đúng affiliate_id và sub_id mới, đồng thời redirect giữ nguyên hai query này.
AddLiveTag lỗi/rate limit không chặn tạo link; chỉ bỏ trống estimate.
CSV báo cáo phải kiểm thử dấu phẩy/chấm tiền tệ, cột tiếng Việt/Anh, nhiều item trong một order, re-import idempotent, unmatched sub_id và status refund/cancel.
Không còn test cho Shopee Open API signature, permission check hay automatic report sync trong Phase 1.
