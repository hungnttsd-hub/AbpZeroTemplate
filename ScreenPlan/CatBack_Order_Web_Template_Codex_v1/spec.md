# spec.md – CatBack Web Order Page Handoff

## 1. Mục tiêu
Thiết kế lại trang **Đơn hàng** bản web theo hướng:
- rõ ràng hơn
- sắc nét hơn
- đồng bộ với glossy dashboard đã chốt
- tăng cảm giác hiện đại nhưng vẫn dễ dùng

## 2. Phạm vi
Chỉ áp dụng cho **trang Đơn hàng bản web**.
Không thay đổi nghiệp vụ backend trong gói này.

## 3. Bố cục tổng thể
### 3.1 Sidebar trái
- nền navy gradient đậm
- logo CatBack ở đầu trang
- active item: `Đơn hàng`
- nhóm menu chính:
  - Trang chủ
  - Đơn hàng
  - Link đã tạo
  - Ví hoàn tiền
  - Rút tiền
  - Lịch sử ví
  - Thông báo
  - Hướng dẫn
  - Tài khoản & cài đặt
  - Quản trị
- cuối sidebar có 2 block:
  - block hỗ trợ
  - block promo / mascot

### 3.2 Vùng nội dung phải
Thứ tự từ trên xuống:
1. topbar
2. hero header của trang Đơn hàng
3. 3 KPI cards
4. filter tabs
5. danh sách order cards

## 4. Topbar
Bao gồm:
- icon chuông
- avatar tròn chữ A
- text `Xin chào, admin`
- nút `Đăng xuất`

## 5. Hero header của trang Đơn hàng
- title lớn: `Đơn hàng`
- subtitle: `Quản lý đơn hàng và theo dõi hoàn tiền của bạn`
- góc phải có mascot / shopping bag / coin / speech bubble
- nền xanh rất nhạt, có wave/cloud gloss mềm

## 6. KPI cards
3 card nằm ngang, đồng chiều cao:
1. `6` – `Chờ xử lý`
2. `4` – `Đã xác nhận`
3. `135.681đ` – `Hoàn tiền dự kiến`

Yêu cầu style:
- card trắng bo tròn 22px
- border xanh nhạt
- shadow rất nhẹ
- có icon tròn bên trái
- có mũi tên tròn bên phải

## 7. Filter tabs
Tabs dạng pill ngang:
- Tất cả (active)
- Chờ xử lý
- Đã xác nhận
- Đã hủy

Active tab:
- gradient cyan
- glow nhẹ
- text trắng

Inactive tab:
- nền trắng
- border xanh nhạt
- text xanh xám

## 8. Order card
Mỗi order card là 1 container trắng bo tròn 22px.

### 8.1 Phần đầu card
Bố cục 3 vùng:
- trái: thumbnail sản phẩm
- giữa: thông tin sản phẩm
- phải: số tiền / hoàn tiền / mã đơn / CTA

### 8.2 Thông tin trái – giữa
- ảnh thumbnail bo góc vừa
- title sản phẩm đậm, tối đa 2 dòng
- dòng nguồn: `Sản phẩm từ Shopee`
- dòng thời gian
- status pill vàng nhạt

### 8.3 Thông tin phải
- cột 1: `Số tiền đơn hàng`
- cột 2: `Hoàn tiền dự kiến`
- bên dưới có `Mã đơn hàng` + icon copy
- ngoài cùng phải có nút `Xem chi tiết`

### 8.4 Hàng dưới cùng
- nền xanh rất nhạt
- có label `Người nhận`
- value `Chưa xác định`
- cảnh báo token chưa ghép / xung đột

## 9. Style guide
### 9.1 Màu
- primary navy: `#0A3668`
- primary text: `#123C74`
- page background: `#E8F9FF`
- accent cyan: `#1ED7E6`
- active cyan glow: `#52F0FF`
- border: `#D4EAF3`
- muted: `#6F8BAA`
- success: `#31C76E`
- warning bg: `#FFF1BF`

### 9.2 Bóng / gloss
- card shadow: `0 8px 24px rgba(10, 54, 104, 0.08)`
- glossy button shadow: `0 10px 30px rgba(15, 177, 196, 0.22)`
- active glow: `0 0 0 1px rgba(101,239,255,.8), 0 10px 30px rgba(30,215,230,.35)`

### 9.3 Bo góc
- page block: 26px
- main card: 22–24px
- pill: 16–20px
- CTA button: 24px

## 10. Typography
- font ưu tiên: `Inter`, fallback `Be Vietnam Pro`, `system-ui`, `sans-serif`
- title page: 34px / 800
- section title: 18px / 800
- body: 16px / 500
- caption: 13–14px / 500–700

## 11. Reusable components cần tạo
- `OrderSidebar`
- `OrderTopbar`
- `OrdersHeroHeader`
- `OrderKpiCard`
- `OrderFilterTabs`
- `OrderCard`
- `StatusPill`
- `RecipientInfoRow`
- `CopyCodeButton`
- `OutlineArrowButton`

## 12. Responsive notes
Package này tối ưu cho desktop / web.
Khi responsive xuống tablet:
- KPI có thể xuống 2 hàng
- order card có thể chuyển thành layout dọc
- sidebar có thể thu gọn nếu hệ thống hiện có hỗ trợ

## 13. Rule với Codex
- đọc SVG + PNG reference trước khi code
- không tự đổi màu theme sang phong cách khác
- không thay logo / mascot CatBack bằng icon generic
- không dùng ảnh screenshot làm UI thật
- phải dựng bằng HTML/CSS/component thật

## 14. Nguồn truth khi có mâu thuẫn
1. `reference/01_final_order_template.png`
2. `blueprints/00_order_page_blueprint.svg`
3. `svg/09_master_order_page.svg`
4. `design-tokens.json`
5. file này (`spec.md`)
