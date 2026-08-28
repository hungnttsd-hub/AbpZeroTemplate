# CODEX INSTRUCTIONS — CATBACK GUIDE FLOW

## Read first
1. `spec.md`
2. `reference/template-guide-mobile.png`
3. `reference/detail-install-video.png`
4. `reference/detail-create-link-video.png`
5. `reference/detail-register-video.png`
6. `blueprint/*.svg`
7. `svg/*.svg`
8. `design-tokens.json`

## Non-negotiable
- One separate guide page.
- Home uses simple teal text `Xem hướng dẫn ›`.
- Home link opens `/huong-dan#tao-link-hoan-tien`.
- Auto-detect Android/iOS. No manual platform tabs by default.
- Exactly 3 compact video cards.
- Quick guide uses accordion.
- Each video card opens its own detail page.
- Detail page must contain: player hero, title/meta, steps card, related videos, bottom CTA.
- Commission-rules accordion uses the five approved rules from `spec.md`.

## Required routes
- `/huong-dan`
- `/huong-dan/video/cai-dat`
- `/huong-dan/video/tao-link-hoan-tien`
- `/huong-dan/video/dang-ky`

## Reuse project assets
If the CatBack source already has mascot/logo/icons, reuse them. The SVGs in this ZIP are layout references.

## Suggested implementation order
1. Replace Home helper description with `Xem hướng dẫn ›`.
2. Build guide route/view.
3. Build header.
4. Add platform detection.
5. Implement reusable `GuideVideoCard`.
6. Implement accordion component.
7. Implement hash scroll + subtle highlight.
8. Implement detail video route template.
9. Bind 3 detail variants.
10. Implement related video logic.
11. Responsive tests.
12. iOS/Android/unknown tests.

## Responsive validation
Check at: 360, 375, 390, 414, 430 px. No horizontal scroll.

## Final visual check
Compare implementation screenshots with files in `reference/`:
- home guide link placement;
- guide header proportions;
- compact video cards;
- accordion spacing;
- detail page video hero card;
- step list spacing;
- related-video card sizes;
- bottom CTA height and style.

## Do not do
- Do not introduce extra UI not documented in `spec.md`.
- Do not enlarge the Home helper area into a big card.
- Do not show both Android and iOS install videos at the same time.
- Do not render all accordion panels expanded by default.
- Do not show the current video again inside `Video liên quan`.
