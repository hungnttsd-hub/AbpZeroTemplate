# CatBack desktop template

## Phạm vi

Giao diện phía khách dùng template mới từ 1024px. Dưới 1024px dùng CSS và hành vi hiện tại. Không thay đổi API, DTO, route, quyền hoặc nghiệp vụ hoàn tiền.

Các trang `/Admin`, `/Identity`, `/SettingManagement` và trang đổi mật khẩu quản trị ban đầu được loại khỏi template. `/Orders` và chi tiết đơn hàng dùng template desktop cho mọi tài khoản, kể cả tài khoản có quyền quản trị đơn hàng; phạm vi dữ liệu và thông tin người nhận vẫn theo quyền hiện tại. Menu tài khoản hiện tại tiếp tục cung cấp các liên kết quản trị theo quyền.

## Cách mở rộng

- `_CustomerLayout.cshtml` gắn `cb-desktop-enabled` cho trang khách. Trang đặc thù có thể đặt `ViewData["DisableCustomerDesktop"] = true` trước khi render layout.
- `customer-desktop.css` được tải sau CSS cũ, có `data-turbo-track="reload"` và version theo nội dung. Tất cả rule/token nằm trong `@media (min-width: 1024px)` và giới hạn bởi `body.cb-desktop-enabled`.
- Sidebar rộng 224px ở 1024–1439px và 244px từ 1440px theo reference Glossy v1. Khung nội dung tối đa 1600px, padding tương ứng 24px/26px.
- Dùng `cb-card` cho khối form/nội dung; `cb-page-header` cho header trang; `_CustomerSectionHeader` với `CustomerSectionHeaderModel` cho tiêu đề khối và liên kết xem thêm.
- Khối chỉ dành cho desktop có `class="cb-desktop-only" hidden`. Stylesheet desktop mới mở hiển thị; giữ thuộc tính `hidden` để khối không xuất hiện ở mobile khi CSS chưa tải.
- Giữ class cũ, ID, `data-*`, form action/handler và antiforgery. Không nhân bản form theo thiết bị. Các template kết quả AJAX và vòng đời Turbo giữ nguyên. Drawer tài khoản chỉ dùng trên mobile của template khách; desktop truy cập tài khoản/đăng xuất ở topbar và quản trị theo quyền ở sidebar trái. Ô tìm kiếm chưa có chức năng đã được gỡ theo phản hồi người dùng.
- Sidebar và hero dùng đúng `catback-mascot-reference.png`; không dùng mascot cũ trong `catback-logo.png`. Icon lấy từ `catback/icons`, `orders-icons`, `notification-icons` và Font Awesome hiện có. Ví dùng `catback/wallet-illustration.svg`. Không thay đổi file asset gốc.

## Trang chủ

Hero, tạo link, dải lợi ích, 5 đơn hàng mới nhất, link đã tạo, ví và 5 lưu ý ghi nhận hoa hồng dùng cùng template desktop. Các wrapper cũ chỉ thay đổi bố cục bằng CSS desktop; nội dung mobile giữ thứ tự cũ.

`IndexModel` gọi `IAffiliateOrderAppService.GetListAsync` với `MaxResultCount = 5`; service giữ thứ tự mới nhất và phạm vi quyền hiện có. Người có quyền xem toàn bộ đơn thấy nhãn phạm vi quản trị. Không tự tính tỷ lệ hoặc giá trị hoàn tiền; bảng sử dụng `CustomerOrderUi` như trang Đơn hàng.

Lỗi tải đơn gần đây được ghi log và hiển thị tại riêng khối này. Khách chưa đăng nhập thấy lời mời đăng nhập; không có số dư/đơn mẫu. Truy vấn bổ sung chạy khi render trang chủ đã đăng nhập, kể cả viewport mobile; khối mới chỉ hiển thị ở desktop.

## File thay đổi

Các đường dẫn bên dưới tương đối với `src/WebHoanTien.Web`:

| Nhóm | File |
| --- | --- |
| Template chung | `Pages/Shared/_CustomerLayout.cshtml`, `Pages/Shared/_CustomerDesktopSidebar.cshtml`, `Pages/Shared/_CustomerDesktopTopbar.cshtml`, `Pages/Shared/CustomerDesktopShellModel.cs` |
| Component | `Pages/Shared/_CustomerSectionHeader.cshtml`, `Pages/Shared/CustomerSectionHeaderModel.cs`, `Pages/Shared/_CustomerBenefits.cshtml`, `Pages/Shared/_CustomerHeroTrust.cshtml`, `Pages/Shared/_CustomerRecentOrders.cshtml` |
| Style | `wwwroot/customer-desktop.css` |
| Trang chủ/link | `Pages/Index.cshtml`, `Pages/Index.cshtml.cs`, `Pages/Links.cshtml`, `Pages/LinkResult.cshtml`, `Pages/PendingAffiliate.cshtml` |
| Đơn hàng | `Pages/Orders.cshtml`, `Pages/OrderDetails.cshtml` |
| Ví | `Pages/Wallet/Index.cshtml`, `Pages/Wallet/Withdraw.cshtml`, `Pages/Wallet/History.cshtml` |
| Thông báo/hướng dẫn | `Pages/Notifications/Index.cshtml`, `Pages/Notifications/Detail.cshtml`, `Pages/Guide/Index.cshtml`, `Pages/Guide/Video.cshtml` |
| Tài khoản | `Pages/Account/AnonymousSuccess.cshtml`, `Pages/Account/ChangePassword.cshtml`, `Pages/Account/Choice.cshtml`, `Pages/Account/ConfirmEmail.cshtml`, `Pages/Account/ConfirmEmailSent.cshtml`, `Pages/Account/ForgotPassword.cshtml`, `Pages/Account/InitialPassword.cshtml`, `Pages/Account/Login.cshtml`, `Pages/Account/Profile.cshtml`, `Pages/Account/Recovery.cshtml`, `Pages/Account/Register.cshtml`, `Pages/Account/Upgrade.cshtml`, `Pages/Account/UpgradeConfirmation.cshtml` |
| Nội dung dùng chung | `Pages/Shared/_AnonymousProfile.cshtml`, `Pages/Legal/Consent.cshtml` |

Các trang pháp lý còn lại được áp dụng style qua layout và class hiện có, không cần sửa nội dung.

## Xác minh

- Build dự án Web bằng `dotnet build src/WebHoanTien.Web/WebHoanTien.Web.csproj --no-restore -p:UseSharedCompilation=false -v:minimal`: thành công, 0 warning, 0 lỗi.
- Rà soát diff: các view ngoài trang chủ/layout và cờ loại trừ quản trị chỉ thêm class chung. Không sửa CSS hoặc JavaScript hiện có.
- Rà soát các đường dẫn asset cố định trong component mới, form ID/selector cũ và `git diff --check`.
- Không chạy test, Playwright hoặc kiểm thử trình duyệt tự động. Chưa có screenshot hoặc xác minh trực quan tại các viewport.

Checklist trực quan còn cần thực hiện khi được yêu cầu:

- Desktop 1024/1280/1440/1920px: không tràn ngang toàn trang, sidebar không che nội dung; bảng hẹp cuộn trong khối bảng.
- Đối chiếu mobile 390/640/768/1023px: header, bottom nav, drawer, form và modal như trước.
- Khách chưa đăng nhập, tài khoản thường/ẩn danh, quản trị; có/không có dữ liệu, lỗi tải đơn và validation.
- Tạo link sản phẩm/shop, sao chép, mua hàng, ẩn/hiện link; lọc và tải thêm đơn/thông báo.
- Ví/rút tiền, thông tin ngân hàng, đăng nhập/khôi phục; menu bàn phím, Turbo back/forward và PWA cài đặt.
