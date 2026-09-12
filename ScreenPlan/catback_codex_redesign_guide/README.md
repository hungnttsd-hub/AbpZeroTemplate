# CatBack Web Redesign – Codex Guide

Bộ tài liệu này mô tả cách redesign giao diện CatBack theo style đơn giản, ít rối mắt, sidebar bên trái.

## Mục tiêu
- Giữ nguyên nghiệp vụ hiện tại.
- Chỉ thay đổi UI/UX và bố cục.
- Ưu tiên giao diện desktop trước, sau đó responsive tablet/mobile.
- Style: sạch, hiện đại, nhiều khoảng trắng, ít gradient, ít trang trí thừa.
- Sidebar cố định bên trái.
- Khu vực nội dung chính dùng card trắng, border nhẹ, bo góc vừa phải.
- Màu chủ đạo: navy + teal + nền trắng/xám rất nhạt.

## File trong gói
- `CODEX_INSTRUCTIONS.md`: prompt chính để đưa cho Codex.
- `DESIGN_SPEC.md`: đặc tả UI chi tiết.
- `COMPONENT_MAP.md`: cấu trúc component đề xuất.
- `IMPLEMENTATION_CHECKLIST.md`: checklist triển khai.
- `design-tokens.css`: biến màu, radius, spacing, shadow.
- `reference-homepage.png`: ảnh tham chiếu giao diện mong muốn.

## Nguyên tắc quan trọng
Không được hard-code dữ liệu giả vào logic production. Ảnh tham chiếu chỉ để mô phỏng bố cục và visual hierarchy.
