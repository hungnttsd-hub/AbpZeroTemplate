# Modal System Implementation Notes

## Purpose
Đây là template modal dùng chung cho toàn bộ project Catback, thay thế popup mặc định của trình duyệt.

## Core goals
- Giao diện hiện đại, đồng nhất
- Dễ tái sử dụng cho toàn bộ dự án
- Có các biến thể rõ ràng: cảnh báo/xác nhận, thành công, thông tin
- Tạo cảm giác tin cậy hơn `alert()` / `confirm()` mặc định

## Core structure
Một modal tiêu chuẩn nên gồm:
1. overlay nền
2. dialog container
3. close button (tuỳ trường hợp)
4. icon trạng thái
5. title
6. description
7. action buttons

## Variants
### 1. Warning / destructive confirm
Dùng cho:
- xóa thông báo
- xóa dữ liệu
- hủy thao tác không thể hoàn tác

UI:
- icon đỏ / soft red background
- title rõ ràng
- secondary button: `Hủy`
- primary destructive button: `Xóa`

### 2. Success
Dùng cho:
- thao tác hoàn tất
- cập nhật thành công
- lưu thành công

UI:
- icon xanh lá hoặc teal
- CTA duy nhất: `Đóng` hoặc `Tiếp tục`

### 3. Info / notice
Dùng cho:
- thông báo hệ thống
- hướng dẫn ngắn
- nội dung cần người dùng xác nhận đã hiểu

UI:
- icon xanh dương
- CTA duy nhất: `Đã hiểu`

## UX rules
- Không dùng text quá dài trong title.
- Description nên ngắn, 1–3 dòng là lý tưởng.
- Với nội dung dài, mở sang trang chi tiết hoặc modal có scroll riêng.
- Destructive action phải có màu nổi bật, dễ phân biệt.
- Không nên có quá 2 nút chính ở footer.
- Overlay click-to-close chỉ nên bật với modal thông tin; với cảnh báo quan trọng có thể tắt.

## Technical recommendation for ABP MVC
Có thể tạo component partial dùng chung:
- `_AppModal.cshtml`
- `_AppModalFooter.cshtml`
- `_AppConfirmModal.cshtml`
- `_AppSuccessModal.cshtml`
- `_AppInfoModal.cshtml`

JS helper gợi ý:
- `showConfirmModal(options)`
- `showSuccessModal(options)`
- `showInfoModal(options)`

## Customer-facing text examples
- `Bạn có chắc chắn muốn xóa thông báo này?`
- `Hành động này không thể hoàn tác.`
- `Đã xóa thành công!`
- `Đây là nội dung thông tin hoặc hướng dẫn đến người dùng.`
