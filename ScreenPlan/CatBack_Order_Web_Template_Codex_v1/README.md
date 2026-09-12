# CatBack Order Page Web Template – Codex Package v1

Đây là gói handoff hoàn chỉnh cho **trang Đơn hàng bản web** của CatBack, theo đúng template đã chốt.

## Mục tiêu
Giúp Codex dựng lại chính xác giao diện order page với style:
- glossy / bóng mượt
- card trắng sắc nét
- active state cyan phát sáng
- giữ nguyên nhận diện CatBack hiện tại

## Thành phần chính trong gói
- `spec.md`: mô tả BA/dev handoff chi tiết.
- `PROMPT_CODEX.md`: prompt chuẩn để đưa trực tiếp cho Codex.
- `blueprints/00_order_page_blueprint.svg`: blueprint tổng thể trang.
- `svg/`: bộ SVG thành phần + master SVG.
- `reference/01_final_order_template.png`: ảnh template đã chốt.
- `design-tokens.json`: token màu, radius, shadow, layout.
- `styles/glossy-order-page-tokens.css`: CSS token sẵn dùng.
- `docs/IMPLEMENTATION_CHECKLIST.md`: checklist triển khai.

## Rule quan trọng
1. Giữ logo và mascot CatBack đồng bộ với web hiện tại.
2. Không thay đổi cấu trúc điều hướng chính của sidebar.
3. Dùng lại component order card lặp lại theo 1 hệ thống thống nhất.
4. Đây là handoff thiết kế UI cho page order, không bao gồm business logic backend.
