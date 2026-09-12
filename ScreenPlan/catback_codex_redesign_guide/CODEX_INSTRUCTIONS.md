# CODEX INSTRUCTIONS

Bạn đang redesign giao diện web CatBack.

## Nhiệm vụ
Refactor giao diện hiện tại sang layout mới theo ảnh `reference-homepage.png`.

### Yêu cầu bắt buộc
1. Sidebar nằm cố định bên trái.
2. Header/topbar đơn giản, chỉ giữ search, notification, user.
3. Khu vực nội dung không dùng quá nhiều banner/card.
4. Tập trung vào 4 nhóm chức năng chính:
   - Tạo link hoàn tiền Shopee.
   - Ví hoàn tiền.
   - Đơn hàng gần đây.
   - Hướng dẫn nhanh.
5. Hero banner chỉ là 1 khối lớn, không chia quá nhiều box con.
6. Không dùng quá nhiều gradient; chỉ dùng ở hero hoặc CTA chính.
7. Card dùng nền trắng, border #E5E7EB, shadow rất nhẹ.
8. Sidebar dùng nền navy đậm.
9. Active menu dùng teal/navy sáng vừa phải, không phát sáng mạnh.
10. Responsive tốt từ 1440px xuống tablet/mobile.
11. Không thay đổi API, model, business logic, authorization hay routing hiện tại nếu không cần thiết.
12. Không hard-code dữ liệu demo nếu dữ liệu thật đã có binding sẵn.
13. Tái sử dụng component/common styles hiện có nếu hợp lý; chỉ refactor phần cần thiết.
14. Giữ nguyên text nghiệp vụ tiếng Việt hiện có, chỉ chỉnh wording nếu cần cho rõ hơn.
15. Không thêm animation gây nhiễu; transition 150–200ms là đủ.

## Ưu tiên visual
- Giao diện phải gọn, thoáng, ít rối mắt.
- Khoảng trắng rõ ràng giữa các section.
- Typography mạnh ở title, nhẹ ở secondary text.
- CTA chính rõ ràng nhưng không lấn át toàn trang.
- Các trạng thái đơn hàng dùng badge nhỏ, không dùng block màu lớn.

## Hành vi mong muốn
### Sidebar
- Width desktop: 240–260px.
- Logo ở trên cùng.
- Menu:
  - Trang chủ
  - Đơn hàng
  - Link của bạn
  - Ví hoàn tiền
  - Ưu đãi
  - Hướng dẫn
  - Lịch sử hoàn tiền
  - Cài đặt
- Support box nằm cuối sidebar hoặc gần cuối.
- Mobile: sidebar chuyển thành drawer.

### Main content
Thứ tự:
1. Topbar
2. Hero banner
3. Link generator + Wallet summary
4. Benefit cards / quick info
5. Recent orders + Quick guide

### Hero
- 1 banner lớn.
- Bên trái: headline + mô tả.
- Giữa/phải: mascot.
- Góc phải: % cashback + CTA.
- Không chia thành 2–3 banner độc lập.

### Link generator
- Input lớn.
- Button “Tạo link hoàn tiền”.
- Có helper text nhỏ dưới input.
- Validation error hiển thị ngay dưới field.

### Wallet
- Hiển thị:
  - Số dư ví.
  - Đã ghi nhận.
  - Sắp ghi nhận.
- Không cần minh hoạ lớn trong card.

### Đơn hàng gần đây
- Desktop dùng table.
- Mobile dùng stacked card.
- Cột ưu tiên:
  - Ngày
  - Sản phẩm
  - Giá trị đơn
  - Hoa hồng
  - Trạng thái
  - Thao tác

### Hướng dẫn nhanh
- 4 bước ngắn.
- Không cần icon quá lớn.
- Có “Xem tất cả”.

## Chất lượng code
- Ưu tiên semantic HTML.
- Hạn chế inline style.
- Tạo CSS variables/tokens.
- Component có trách nhiệm rõ ràng.
- Không lặp markup nếu có thể tạo partial/component.
- Không phá layout hiện tại ở các trang khác.
- Nếu có Bootstrap hiện tại, tận dụng grid/utilities hợp lý thay vì thêm framework mới.

## Kết quả cần trả về từ Codex
1. Danh sách file đã sửa.
2. Tóm tắt thay đổi.
3. Screenshot/preview sau khi build nếu môi trường hỗ trợ.
4. Các điểm chưa chắc chắn hoặc cần BA xác nhận.
5. Không tự ý thay đổi nghiệp vụ.
