# Project Instructions

- Không sử dụng subagent.
- Chỉ chạy test khi người dùng yêu cầu rõ ràng.
- Chỉ chạy Playwright hoặc kiểm thử trình duyệt tự động khi người dùng yêu cầu rõ ràng.

## TikTok Shop / CatBack

- Tích hợp Affiliate Creator API theo tài liệu chính thức TikTok Shop Partner Center. Xem bảng đối chiếu và nguồn tại `docs/tiktok-shop-creator-api.md`.
- Chỉ dùng API phù hợp scope app đã được cấp và `granted_scopes` của Creator token; không dùng Seller token thay Creator token.
- Kiểm tra API Reference hiện hành trước khi viết request/DTO; không suy đoán endpoint, parameter, version hoặc alias scope; không dùng API deprecated.
- Scope collaboration ID 1021508 có key chính xác `creator.affiliate_collaboration.read`, đã xác minh từ HTML Manage scope; không dùng key `creator.affiliate.collaboration.read` hoặc tự coi là alias.
- Scope tracking link ID 1873988 / `creator.affiliate.link.write` được gán API `Generate Affiliate Sharing Link`; không tự thay bằng Creator Generate General Link, Creator Generate Publisher Link hoặc API Seller. Cần API Reference riêng để xác minh endpoint/version/schema.
- Chủ dự án bổ sung scope Active `creator.affiliate.share_link.read` cho kế hoạch Creator Generate General Link `202505`. Module Review Demo dùng `TikTokAffiliate:Mode = Mock`, phải ghi rõ Demo Mode; adapter Api chưa gọi thật và không fallback sang mock.
- Không yêu cầu thêm scope khi chức năng thực hiện được với scope hiện tại. Mapping endpoint tracking link ID 1873988 phải được xác minh trước khi bật chức năng tracking link; không chặn các API đọc đã được xác minh vì thiếu mapping này.
