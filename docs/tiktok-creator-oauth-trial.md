# CatBack — thử Creator OAuth thật

Ngày đối chiếu: 08/10/2026 (Asia/Bangkok).

## Kết quả hiện tại

Đã mở trang authorization thật của TikTok bằng App Key CatBack. Trang hiển thị tên CatBack và bốn nhóm quyền: Affiliate Information, Read Creator Affiliate Collaborations, Manage Affiliate Tracking Links, Read Affiliate Share Link; có QR và Login with TikTok. Đây chỉ là kiểm tra mở màn hình cấp quyền, chưa phải xác nhận Creator đủ điều kiện beta, callback thành công hoặc đã nhận token. Không gọi token API với code giả.

## Phạm vi triển khai

- Nút **Kết nối TikTok Creator thật** riêng trên `/tiktok-affiliate`, chỉ cho user được policy IsTiktokDemo cho phép. Nút demo và dữ liệu demo giữ độc lập.
- POST `/api/tiktok-shop/oauth/connect` nhận Username TikTok từ form workspace có antiforgery. `hungnttsd` đi qua hai trang kết nối cục bộ; username khác dùng luồng thật. State ngẫu nhiên 256 bit gắn với tenant/user/app/username và cookie HttpOnly/Secure/Lax. State hết hạn 15 phút và được xóa trước khi đổi code. Bắt đầu lại hủy state trước đó của trình duyệt.
- GET `/api/tiktok-shop/oauth/callback` vẫn cho TikTok kiểm tra URL bằng HEAD hoặc GET không query. Callback có code phải kiểm tra phiên CatBack, policy và state trước khi gửi code tới TikTok.
- Đổi code bằng Get Access Token, đúng `grant_type=authorized_code`. Chỉ nhận `user_type=1` và `granted_scopes` chứa `creator.affiliate.info`.
- Gọi Get Creator Profile `GET /affiliate_creator/202508/profiles`, ký HMAC-SHA256 theo tài liệu, đọc `creator_user_open_id`, `username`, `selection_region`. Chỉ xác nhận kết nối khi profile API thành công, region VN và username khớp lựa chọn. Username khác thì xóa token và báo lỗi. Dùng username làm nhãn tên vì response hiện hành không cung cấp displayName theo DTO CatBack. Callback thành công về `/tiktok-affiliate`; endpoint Result chỉ còn dùng cho thông báo lỗi.
- Token và refresh token được mã hóa bằng Data Protection, purpose gắn tenant/user/app key, lưu trong cache đã cấu hình. Phiên thử tối đa 8 giờ mỗi lần lưu, giới hạn bởi refresh expiry; cache không bền vững sẽ mất khi restart. Refresh kiểm tra lại user type, scope và open_id. Đây chưa phải token store lâu dài phục vụ production.
- Kết quả callback chuyển về URL không có query. Không xuất token/code/secret ra UI hoặc log. Client HTTP riêng tắt logger mặc định và redirect, chỉ log stage/HTTP/code/request_id, không ghi response message hoặc exception message có thể chứa thông tin nhạy cảm.
- Ngắt kết nối tại CatBack xóa token cục bộ và pending state; không thu hồi authorization tại TikTok. Muốn thu hồi quyền cần thao tác trên TikTok.
- Adapter Api đọc hồ sơ thật; tra sản phẩm/tạo link/orders thật chưa triển khai. Không dùng mock để lấp dữ liệu thiếu. Phần card OAuth mới và trang kết quả dùng tiếng Việt; toàn bộ workflow demo cũ chưa được Việt hóa trong bước này.

## Cấu hình và cách thử sau triển khai

Điền `TikTokShop.AppKey`, `TikTokShop.AppSecret` trong file secrets trên máy chủ đang chạy. App Secret không đưa vào chat, Git hoặc trình duyệt. File secrets cục bộ có giá trị không chứng minh server production đã nạp cùng cấu hình; application nạp secrets lúc startup.

Redirect URL trong Partner Center phải trùng `https://catback.id.vn/api/tiktok-shop/oauth/callback`. Code đọc `TikTokShop.RedirectUrl` nếu đặt, nếu không dùng `App.SelfUrl` + callback path. Nếu bắt đầu từ localhost hoặc host/port khác, POST kết nối chuyển sang workspace HTTPS trên domain callback và điền sẵn username; đăng nhập CatBack trên domain đó nếu cần rồi bấm Kết nối để mở TikTok Creator authorization thật. Không tạo state/token ở localhost cho callback production. Khi host/port đã khớp, không chặn chỉ vì backend nhận HTTP sau proxy kết thúc TLS: state cookie vẫn Secure và callback cấu hình vẫn bắt buộc HTTPS. Không sửa Redirect URL đã đăng ký bằng suy đoán.

1. Triển khai code mới lên domain callback đã đăng ký và restart với cấu hình secrets đúng.
2. Đăng nhập CatBack bằng tài khoản IsTiktokDemo=true.
3. Mở `/tiktok-affiliate`, chọn **Kết nối TikTok Creator thật**.
4. Chủ tài khoản tự đăng nhập TikTok Creator Vietnam và kiểm tra/cấp quyền trên TikTok.
5. Nếu thành công, trang kết quả chỉ xác nhận sau khi đổi token và đọc profile API thật. Quay về module để thấy hồ sơ thật ở card riêng.
6. Nếu lỗi trước khi callback, chụp thông báo TikTok (không gửi QR/code/token). Nếu lỗi tại token/profile API, dùng stage, code và request_id hiển thị để hỏi Partner Manager về allowlist/Creator eligibility/scopes. Không tự kết luận lỗi beta chỉ dựa vào một code chung.

Không bấm cấp quyền từ URL preflight mở trực tiếp để hoàn tất thử nghiệm: URL đó không tạo state/cookie/backend session của CatBack. Phải bắt đầu từ nút POST trên bản đã triển khai.

Luồng thử chưa đáp ứng toàn bộ App Review: cần bổ sung API sản phẩm, link và orders thật, token store lâu dài, cùng tiếng Việt cho toàn bộ trải nghiệm. Không có bằng chứng đã nhận token hoặc API profile thành công ở lần này.

## Nguồn chính thức đã đọc

- [Creator Authorization Guide](https://partner.tiktokshop.com/docv2/page/creator-authorization-guide): authorization URL, state, prerequisite beta/market và user_type/granted_scopes.
- [Authorization overview / Token API](https://partner.tiktokshop.com/docv2/page/authorization-overview-202407): token/get, token/refresh, thời hạn là Unix timestamp, code dùng một lần.
- [Get Creator Profile 202508](https://partner.tiktokshop.com/docv2/page/get-creator-profile-202508): endpoint, scope và response creator_user_open_id.
- [Sign your API request](https://partner.tiktokshop.com/docv2/page/sign-your-api-request): sort query, exclude sign/access_token, wrap app_secret và HMAC-SHA256.

Đã đọc nội dung trang chính thức sau khi tải JavaScript. Không dùng mẫu API profile 202405 trong guide cũ làm hợp đồng cho endpoint 202508.
