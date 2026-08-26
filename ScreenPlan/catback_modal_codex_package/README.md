# Catback Modal Codex Package

Gói này dành riêng cho **template modal dùng chung** của Catback để Codex/dev có thể triển khai đúng giao diện đã duyệt.

## Bắt đầu
1. Đọc `docs/spec.md`
2. Đọc `docs/modal-system-notes.md`
3. Đọc `docs/abp-mvc-notes.md`
4. Nếu muốn prompt sẵn cho Codex, dùng `docs/codex-prompt.md`
5. Mở toàn bộ ảnh trong `references/modal/`
6. Mở toàn bộ blueprint SVG trong `blueprints/`

## Cấu trúc

### docs/
- `spec.md`
- `modal-system-notes.md`
- `abp-mvc-notes.md`
- `codex-prompt.md`

### references/modal/
- `modal-guideline-full.png`
- `modal-main-example.png`
- `modal-component-notes.png`
- `modal-variants-row.png`
- `modal-usage-guidelines.png`
- `modal-header-title.png`

### blueprints/
- `modal-confirmation.svg`
- `modal-variants.svg`

### assets/svg/
Bộ SVG hoàn chỉnh, bao gồm:
- full SVG pack trước đó
- close icon
- warning / success / info icons
- button primitives
- reusable Catback SVG assets khác

## Modal variants cần triển khai
- Cảnh báo / xác nhận xóa
- Thành công / hoàn tất
- Thông tin / thông báo

## Stack
- ABP.IO Free
- ASP.NET Core MVC / Razor Pages
- Không React
- Không TSX/JSX

## Mục tiêu
Thay toàn bộ popup mặc định của trình duyệt bằng hệ modal đồng nhất, hiện đại và tái sử dụng được cho toàn project.
