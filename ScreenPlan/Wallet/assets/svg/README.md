# CatsBack SVG Pack

Bộ SVG này được tạo để Codex/dev có thể code giao diện CatsBack sát với thiết kế đã duyệt.

## Thành phần chính

### 1. Core icons
- `cb-home.svg`
- `cb-link.svg`
- `cb-gift.svg`
- `cb-history.svg`
- `cb-wallet-menu.svg`
- `cb-search.svg`
- `cb-search-dark.svg`
- `cb-store.svg`
- `cb-wallet.svg`
- `cb-percent.svg`
- `cb-cashback.svg`
- `cb-clock.svg`
- `cb-badge-check.svg`
- `cb-circle-x.svg`
- `cb-shield-check.svg`
- `cb-headphones.svg`
- `cb-user.svg`
- `cb-arrow-left.svg`
- `cb-chevron-right.svg`

### 2. Ready-made UI assets
- `cb-iconbubble-*.svg`
- `cb-status-pending.svg`
- `cb-status-confirmed.svg`
- `cb-status-cancelled.svg`
- `cb-chip-all-active.svg`
- `cb-chip-pending.svg`
- `cb-chip-confirmed.svg`
- `cb-chip-cancelled.svg`
- `cb-search-input.svg`
- `cb-support-button.svg`
- `cb-metric-pending-card.svg`
- `cb-metric-confirmed-card.svg`
- `cb-metric-cashback-card.svg`
- `cb-detail-summary-panel.svg`
- `cb-sidebar-promo-card.svg`
- `cb-logo-wordmark.svg`

### 3. Preview
- `cb-svg-pack-preview.svg`

## Gợi ý sử dụng với Codex

1. Ưu tiên dùng **core icons** để dựng component thật trong code.
2. Dùng **ready-made UI assets** làm reference hình học / spacing nếu cần.
3. Không cần nhúng nguyên file summary panel vào app; nên tách thành component.
4. Màu sắc nên map lại theo design tokens trong `catback-orders-design-spec.md`.

## Lưu ý
- SVG dùng font fallback: `Inter, Arial, sans-serif`.
- Text trong SVG chỉ là guideline/reference; production code vẫn nên render text bằng HTML/CSS khi có thể.
- Promo card là vector placeholder theo tone CatsBack, chưa phải mascot brand-official cuối cùng.
