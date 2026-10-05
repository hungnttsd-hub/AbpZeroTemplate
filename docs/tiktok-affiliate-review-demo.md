# TikTok Affiliate — Review Demo

## Mở module

Đăng nhập CatBack bằng luồng xác thực hiện có, chọn **TikTok Affiliate** trong sidebar desktop hoặc menu tài khoản trên mobile, hoặc mở `/tiktok-affiliate`.

### Cấp quyền theo user

- User phải có extra property bool **IsTiktokDemo = true** mới thấy navigation và được truy cập trang/handler/service TikTok Affiliate. Giá trị thiếu hoặc false đều bị từ chối; admin cũng không tự được bypass.
- Admin mở **Quản lý người dùng** (`/Identity/Users`) → tạo/sửa user → checkbox **Cho phép TikTok Affiliate demo (IsTiktokDemo)** → Lưu. Checkbox được dựng bởi form extension chuẩn của ABP và lưu cùng create/update user qua quyền quản lý user hiện có.
- Giá trị mặc định false. User không được chỉnh trường này qua profile/registration vì `AllowUserToEdit = false`.
- Sau khi đăng nhập thành công bằng mật khẩu hoặc Google, user có **IsTiktokDemo = true** được chuyển tới `/tiktok-affiliate`, ưu tiên hơn return URL của lần đăng nhập. User khác giữ nguyên luồng chuyển hướng hiện có. Đăng nhập thất bại không chuyển tới trang demo.
- Trường lưu trong `AbpUsers.ExtraProperties` bằng cơ chế extension ABP; không cần cột vật lý hoặc migration database. Backend kiểm tra giá trị đã lưu và user đang active, không lấy quyền từ cookie claim. Bỏ checkbox sẽ chặn ở request tiếp theo; refresh trang để cập nhật navigation đang mở.

`src/WebHoanTien.Web/appsettings.json` mặc định:

```json
"TikTokAffiliate": { "Mode": "Mock" }
```

Không cần app secret, CreatorAccessToken hoặc kết nối TikTok thật trong chế độ này. Đây là demo cho review, không phải bằng chứng TikTok đã cấp Creator testing account hoặc đã chấp thuận app.

## Walkthrough khoảng 1–2 phút

1. Overview: chỉ vào banner **Demo Mode** và giải thích toàn bộ profile/product/link/order đều là dữ liệu thử.
2. Bấm **Connect TikTok Creator**. Profile `CatBack Demo Creator`, `@catback_demo`, market VN xuất hiện. Giao diện ghi rõ đây là kết nối giả lập, không phải OAuth thật.
3. Bấm **Use demo product**, sau đó **Check Product**. Service nhận URL `https://shop.tiktok.com/vn/pdp/1736327643619493458` và trả Crocs Classic Clog / Demo Official Store / 1.290.000 VND / commission 8% (103.200 VND). Hình sản phẩm là minh họa cục bộ có nhãn DEMO PRODUCT.
4. Bấm **Generate Affiliate Link**. Có trạng thái “Generating TikTok affiliate link...”, rồi kết quả và **Copy Link**. Link chứa `affiliate_id=demo`, được ghi rõ không do TikTok phát hành và không dùng được để attribution thật.
5. Chọn **Generated Links** để xem lịch sử, thời gian, trạng thái, mở link demo hoặc sao chép. Mở link demo vẫn điều hướng sang URL TikTok; điều này không tạo tracking thật.
6. Chọn **Affiliate Orders**: ba đơn giả lập TK100001/Pending, TK100002/Settled, TK100003/Cancelled. Commission và trạng thái không có tác động tài chính.

Để làm lại walkthrough, bấm **Disconnect TikTok Creator** trên Overview: ngắt kết nối demo, xóa trạng thái product đã check và kết quả link đang hiển thị, ẩn orders cho tới khi kết nối lại. Không đăng xuất CatBack hoặc tác động tài khoản TikTok thật. Lịch sử link được giữ; bấm **Delete Link** trong Generated Links để xóa từng link demo của tài khoản hiện tại. Sau khi xóa có thể tạo lại link với thời gian mới. Hai thao tác dùng POST/antiforgery và vẫn yêu cầu IsTiktokDemo; adapter Api hiện chưa hỗ trợ hai thao tác này.

Các view nên chụp khi thực hiện review: Overview chưa kết nối; Overview đã có product và link; Generated Links; Affiliate Orders. Banner Demo Mode phải hiện trong mỗi ảnh. Chưa chụp screenshots hoặc chạy trình duyệt tự động trong lần triển khai này.

## Hành vi và giới hạn demo

- Page và service dùng authentication hiện có. POST handler sử dụng antiforgery của Razor Pages; không có API anonymous để tạo dữ liệu.
- Mỗi tenant/tài khoản có trạng thái Creator, product đã check và lịch sử link riêng trong memory cache. Refresh trang giữ trạng thái kết nối/lịch sử trong cùng app process.
- Trạng thái hết hạn sau 2 giờ không hoạt động, tối đa 8 giờ hoặc khi process restart. Nhiều instance không chia sẻ trạng thái mock. Chưa có database migration.
- Chỉ hỗ trợ fixture Product ID được cung cấp, với hai dạng URL PDP `/vn/pdp/{id}` và `/view/product/{id}` trên HTTPS `shop.tiktok.com`. Không lookup URL bên ngoài; không gán dữ liệu Crocs cho ID khác.
- Lặp lại Generate cho cùng fixture trả lại link đã tạo, không thêm lịch sử trùng. `createdAt` là thời gian thực của lần tạo đầu tiên, hiển thị múi giờ Việt Nam; thời gian ba order là fixture cố định.
- Chuyển tab dùng keyboard ArrowLeft/ArrowRight/Home/End. Kết quả, loading, lỗi, copy có status message. Link bên ngoài mở tab mới với noopener/noreferrer.
- Không ghi vào AffiliateTracking, AffiliateOrder hoặc wallet/payout. Không gọi TikTok thật.

## Đổi sang API sau này

`ITikTokAffiliateService` trong Application.Contracts định nghĩa GetCreatorProfile, GetProduct, GenerateAffiliateLink, GetGeneratedLinks, SearchAffiliateOrders, cùng metadata mode và ConnectCreator.

Application layer đăng ký `TikTokAffiliateMockService` hoặc `TikTokAffiliateApiService` theo `TikTokAffiliate:Mode`. Mode không hợp lệ làm startup thất bại thay vì chọn ngầm service. Hai adapter trả cùng DTO; Razor/JavaScript chỉ phụ thuộc DTO và metadata `IsDemo`, không chứa fixture data.

`Mode = Api` hiện trả dữ liệu trống và thông báo live integration chưa triển khai. Connect/lookup/generate trả lỗi có nội dung rõ ràng; không fallback sang mock, không tạo profile hoặc link giả. Việc hoàn thiện adapter thật cần OAuth, token store riêng cho Creator, signing, scope checks, mapping response và lưu lịch sử thật. UI giữ nguyên.

Scope `creator.affiliate.share_link.read` được chủ dự án xác nhận Active trong yêu cầu module này. API dự kiến: Creator Generate General Link `POST /affiliate_creator/202505/affiliate_sharing_links/general_publishers/generate_batch`. Required scope đã được đối chiếu trước đó; chưa có request thật thành công. Xem `tiktok-shop-creator-api.md` để đọc nguồn và các giới hạn đã xác minh.

## Kiểm tra triển khai

Không chạy test hoặc Playwright theo AGENTS.md. `dotnet build src/WebHoanTien.Web/WebHoanTien.Web.csproj --no-restore -p:SkipClientAssets=true --verbosity quiet` thành công, 0 warning / 0 error. `node --check` cho JavaScript mới thành công. Đây là kiểm tra biên dịch/cú pháp, không xác nhận giao diện đã được kiểm tra bằng trình duyệt. Screenshots/product walkthrough phải giữ nhãn demo trước khi dùng cho review.
