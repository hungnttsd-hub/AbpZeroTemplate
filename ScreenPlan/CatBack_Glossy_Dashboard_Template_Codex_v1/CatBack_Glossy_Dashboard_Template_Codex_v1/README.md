# CatBack Glossy Dashboard – Codex Handoff Package

Đây là gói thiết kế đã chốt cho dashboard desktop CatBack.

## Dùng nhanh

1. Đưa file ZIP vào project / workspace Codex.
2. Giải nén.
3. Gửi cho Codex nội dung trong `PROMPT_CODEX.md`.
4. Yêu cầu Codex đọc `spec.md` và toàn bộ reference trước khi code.

## File quan trọng

- `spec.md` – đặc tả UI/dev handoff.
- `PROMPT_CODEX.md` – prompt dùng trực tiếp.
- `reference/01_final_template.png` – ảnh thiết kế chốt.
- `blueprints/00_page_layout_blueprint.svg` – blueprint kích thước.
- `svg/00_dashboard_master_exact.svg` – toàn dashboard ở dạng SVG reference.
- `svg/01...08_*_exact.svg` – từng vùng giao diện.
- `svg/09_gloss_shadow_system.svg` – vector guideline về bóng/gloss.
- `css/catback-glossy-tokens.css` – token CSS tham khảo.
- `design-tokens.json` – token machine-readable.

## Lưu ý về SVG exact

Các file `*_exact.svg` nhúng trực tiếp crop của template để Codex/Dev có thể mở SVG và nhìn đúng visual của từng vùng. Chúng là **design reference**, không phải asset production.

Khi implement cần rebuild bằng Razor/HTML/CSS và reuse logo/mascot hiện có trong source project.

## Brand rule

Logo CatBack và mascot phải lấy từ web/project hiện tại. Không tạo lại bằng AI, không crop từ screenshot để đưa vào production.
