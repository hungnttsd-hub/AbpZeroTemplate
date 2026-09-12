# PROMPT_CODEX.md

Tôi đã cung cấp package thiết kế cho **trang Đơn hàng bản web của CatBack**.

## Trước khi code
Hãy đọc toàn bộ các file sau:
- `spec.md`
- `README.md`
- `design-tokens.json`
- `styles/glossy-order-page-tokens.css`
- `docs/IMPLEMENTATION_CHECKLIST.md`
- `blueprints/00_order_page_blueprint.svg`
- toàn bộ thư mục `svg/`
- toàn bộ thư mục `reference/`

## Mục tiêu
Dựng lại giao diện **Orders page** gần giống nhất với template đã chốt.

## Bắt buộc
- Giữ nguyên identity CatBack.
- Giữ logo / mascot đồng bộ với web hiện tại.
- Không dùng screenshot để thay cho UI code.
- Dùng component thật, CSS thật, layout thật.
- Tái sử dụng component `OrderCard` cho nhiều item.
- Preserve active nav state `Đơn hàng`.
- Giữ theme glossy, cyan glow, white-card sharp look.

## Thành phần phải có
1. Sidebar trái.
2. Topbar trên cùng.
3. Orders hero header.
4. 3 KPI cards.
5. Filter tabs.
6. Danh sách order cards.
7. Các state pill và CTA button đúng style.

## Khi có mâu thuẫn
Ưu tiên:
1. `reference/01_final_order_template.png`
2. `blueprints/00_order_page_blueprint.svg`
3. `svg/09_master_order_page.svg`
4. `design-tokens.json`
5. `spec.md`

## Trước khi sửa code
Hãy trả về:
1. Các file đã đọc.
2. Component hiện có nào có thể reuse.
3. File nào dự kiến sửa.
4. Mapping từng vùng giao diện với code hiện tại.
5. Kế hoạch implement theo thứ tự.

## Sau khi implement
Hãy trả report gồm:
- Files modified
- Files created
- Components added
- CSS/token added
- Responsive notes
- Những khác biệt nhỏ so với template
- Build result
