# Notification UI Implementation Notes

## Purpose
Trang thông báo giúp khách hàng theo dõi nhanh các cập nhật quan trọng liên quan đến:
- hoàn tiền
- đơn hàng
- ví tiền
- ưu đãi

## Core UX
- Mobile-first
- Header nổi bật với tiêu đề lớn `Thông báo`
- Subtitle mô tả ngắn
- Icon chuông có badge số lượng chưa đọc
- Bộ lọc dạng chip:
  - Tất cả
  - Hoàn tiền
  - Đơn hàng
  - Ví tiền
  - Ưu đãi
- Chia nhóm thông báo:
  - Hôm nay
  - Trước đó
- Hành động `Đánh dấu đã đọc tất cả`

## Notification card
Mỗi card gồm:
- icon loại thông báo
- tiêu đề
- mô tả ngắn
- thời gian
- chấm unread ở cạnh phải nếu chưa đọc

## Ví dụ loại thông báo
### Hoàn tiền
- `Hoàn tiền đã ghi nhận`
- `Hoa hồng sắp ghi nhận`

### Ví tiền
- `Tiền rút đang xử lý`
- `Tài khoản ngân hàng đã cập nhật`

### Đơn hàng
- `Đơn hàng đã đối soát`

### Ưu đãi
- `Ưu đãi mới từ Catback`

## Interaction
- Tap card => mở link chi tiết hoặc deep link sang module liên quan
- `Đánh dấu đã đọc tất cả` => mark unread -> read
- Chip filter => lọc theo loại thông báo

## Không hiển thị cho khách hàng
- internal notification id
- internal payload
- user id
- error trace
- metadata kỹ thuật
