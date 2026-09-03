# CatsBack Shop Link — Codex Design Package

Gói triển khai cho nâng cấp **Link sản phẩm / Link shop Shopee** trên card tạo link hiện tại.

## Thứ tự Codex phải đọc
1. `spec.md`
2. `references/approved-template-product-shop.png`
3. `references/current-home-before-upgrade.png`
4. `blueprint.svg`
5. `blueprints/*.svg`
6. `assets/svg/components/*.svg`
7. `assets/svg/*.svg`

## Quy tắc quan trọng
- Không redesign toàn trang.
- Header/hero/80% cashback/wallet/bottom navigation phải reuse source hiện tại.
- Product mode là default và giữ hành vi hiện tại.
- Shop mode chỉ là option bổ sung trong cùng card.
- `blueprint.svg` là kích thước component authoritative.
- `approved-template-product-shop.png` là style/state authoritative.
- SVG component có thể được dùng trực tiếp hoặc chuyển thành Razor/CSS nếu giữ đúng hình học, spacing và màu.

## Package
- `spec.md`: technical + UX plan đầy đủ.
- `blueprint.svg`: blueprint chi tiết card tạo link.
- `blueprints/01-home-product-mode.svg`: full mobile state Product.
- `blueprints/02-home-shop-mode.svg`: full mobile state Shop trước submit.
- `blueprints/03-home-shop-created.svg`: full mobile state Shop sau tạo link.
- `assets/svg/`: icon vector.
- `assets/svg/components/`: vector hoàn chỉnh từng component.
- `references/`: ảnh UI hiện tại + thiết kế đã duyệt.
- `codex-prompt.md`: prompt có thể paste thẳng cho Codex.
