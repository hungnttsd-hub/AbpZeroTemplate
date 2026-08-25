# Catback Wallet Codex Package

Gói này dành riêng cho tính năng **Quản lý ví tiền** và **Rút tiền** để Codex triển khai đúng giao diện đã duyệt.

## Bắt đầu từ đâu

1. Đọc `docs/spec.md`
2. Đọc `docs/wallet-withdraw-notes.md`
3. Mở toàn bộ ảnh trong `references/wallet/`
4. Mở các file trong `blueprints/`
5. Reuse icon / asset trong `assets/svg/`

## Nội dung chính

### docs/
- `spec.md`
  - Spec tổng thể, đã có section Wallet module
- `wallet-withdraw-notes.md`
  - Ghi chú triển khai riêng cho ví và rút tiền
- `abp-mvc-notes.md`
  - Ràng buộc triển khai theo ABP.IO Free + MVC/Razor Pages

### blueprints/
- `wallet-home-summary.svg`
  - Blueprint card ví ngắn gọn ở trang chủ
- `wallet-overview-mobile.svg`
  - Blueprint màn tổng quan ví
- `wallet-withdraw-mobile.svg`
  - Blueprint màn rút tiền

### references/wallet/
- `home-with-wallet-summary.png`
- `home-wallet-summary-card.png`
- `wallet-overview-full.png`
- `wallet-overview-balance-card.png`
- `wallet-overview-actions-and-list.png`
- `wallet-withdraw-full.png`
- `wallet-withdraw-header-and-form.png`
- `wallet-withdraw-summary-and-notes.png`

### assets/svg/
Bao gồm bộ SVG hoàn chỉnh để Codex/dev dùng lại:
- icon ví
- cashback
- trạng thái
- nút / chip / badge
- support / menu / toggle
- các icon link management và order trước đó (có thể tái sử dụng)

## Mục tiêu UI

### 1. Trang chủ
Chỉ hiển thị thông tin ví ngắn gọn:
- Số dư ví
- Đã ghi nhận
- Sắp ghi nhận
- CTA `Xem ví`

### 2. Trang ví tiền
Hiển thị:
- Số dư trong ví
- Tổng tiền đã ghi nhận trong hệ thống
- Hoa hồng sắp được ghi nhận thêm
- CTA `Rút tiền`
- CTA `Lịch sử ví`
- Danh sách biến động gần đây

### 3. Trang rút tiền
Hiển thị:
- Số dư khả dụng
- Nhập số tiền muốn rút
- Các chip chọn nhanh số tiền
- Tài khoản nhận tiền
- Phí rút tiền
- Thời gian xử lý
- Tóm tắt tiền thực nhận
- CTA `Yêu cầu rút tiền`
- Ghi chú / lưu ý
- Lịch sử rút tiền

## Ràng buộc kỹ thuật

- ABP.IO Free
- ASP.NET Core MVC / Razor Pages
- Không React
- Không TSX/JSX
- Reuse layout/theme hiện có của ABP
- Dùng DTO + Application Service
- Không hiển thị internal field / accounting nội bộ cho khách hàng

## Cách dùng với Codex

Prompt gợi ý:

> Read `docs/spec.md`, `docs/wallet-withdraw-notes.md`, all wallet reference images under `references/wallet/`, and all SVG blueprints under `blueprints/`. Implement the wallet management UI in ABP.IO Free using ASP.NET Core MVC / Razor Pages only. Do not use React. Reuse the existing ABP layout and use the SVG assets in `assets/svg/`. Match the approved UI as closely as possible for the wallet home summary, wallet overview page, and withdrawal page.
