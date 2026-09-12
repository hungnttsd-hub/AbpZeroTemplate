# CatBack Orders desktop — 12/09/2026

## Thiết kế và phạm vi

Tham chiếu `ScreenPlan/CatBack_Order_Web_Template_Codex_v1`: README, spec, design tokens, styles, blueprint SVG, checklist và ảnh `reference/01_final_order_template.png` cùng các crop. Yêu cầu của người dùng ưu tiên hơn nội dung mẫu trong tài liệu.

Áp dụng từ 1024px, trong `body.cb-desktop-enabled`. Background SVG sóng cyan dùng chung cho tất cả trang phía khách qua `_CustomerLayout`; trang quản trị được loại trừ như trước. Các thông số desktop mới thay thế ghi chú tương ứng trong bản bàn giao Glossy Dashboard trước: font Inter, card 22px, sidebar 224px/252px và padding 24px/36px (mốc 1440px). Nội dung tối đa 1600px.

## Thay đổi

- Orders: header tiêu đề/mô tả, ba KPI nằm ngang có icon và liên kết, bộ lọc pill glossy, card chia cột sản phẩm/số tiền/thao tác. Hàng người nhận và cảnh báo token chỉ xuất hiện theo điều kiện quyền và dữ liệu hiện tại. Mô tả phạm vi quản trị rõ ràng khi xem toàn bộ đơn.
- Dùng chung `_CustomerPageHero` và `_CustomerKpiCard`, với model trình bày riêng. Header áp dụng cho Orders, chi tiết đơn, Link đã tạo, Ví, Rút tiền, Lịch sử ví, Thông báo và Hướng dẫn.
- Dùng chung class `cb-list-filters` cho Orders, Lịch sử ví và Thông báo. Đồng nhất bề mặt, viền, shadow và control cho các trang phía khách; căn lại cột ảnh ở Link và chi tiết đơn.
- `customer-page-background.svg` sử dụng đường sóng từ blueprint, không chứa ảnh chụp giao diện hay dữ liệu mẫu. Trang chủ giữ banner đã được yêu cầu trước đó. Header trang dùng vùng minh họa từ asset banner đã duyệt.
- Giữ `_OrderCards` cho cả render đầu và tải thêm, `data-order-id`, copy selector, route, filter và loader hiện tại. Không đổi service/API/DTO, PageModel Orders, authorization hoặc công thức tiền.
- Header mobile giữ nguyên; các khối thêm mới có `hidden`, chỉ hiển thị bằng CSS desktop. Nhãn KPI vẫn là `span:last-child` để tương thích stylesheet mobile. Không nhân đôi form, ID, nút cài đặt hay handler.
- Quy tắc không sử dụng subagent được ghi trong `AGENTS.md`; không sử dụng subagent trong lượt này.

## Kiểm tra đã thực hiện

- Build dự án Web bằng `dotnet build --no-restore -m:1 -p:UseSharedCompilation=false -p:OutDir=E:/HungNT/WebHoanTien/artifacts/orders-desktop-build/ -v:quiet`: thành công, 0 cảnh báo, 0 lỗi; bao gồm biên dịch Razor. Sau build chỉ tinh chỉnh CSS và tài liệu.
- `git diff --check` không có lỗi; kiểm tra tĩnh cặp ngoặc CSS, media query desktop, đường dẫn asset CSS và XML của background SVG.
- Rà soát binding thực, điều kiện hiển thị người nhận, selector tải thêm/sao chép, thứ tự phần tử KPI mobile và độ ưu tiên CSS filter đang chọn.

## Chưa kiểm tra

Không chạy test, Playwright hay trình duyệt tự động. Chưa xác nhận trực quan ở 1024/1280/1440/1920px và chưa đối chiếu mobile 390/640/768/1023px. Chưa kiểm tra thực tế các trạng thái dữ liệu, validation, tương tác filter/copy/load-more, Turbo và back/forward trên trình duyệt; cần kiểm tra khi người dùng yêu cầu. Build và rà soát tĩnh không thay thế nghiệm thu giao diện.
