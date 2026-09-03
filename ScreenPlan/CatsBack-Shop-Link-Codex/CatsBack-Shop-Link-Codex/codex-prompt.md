# Codex Prompt

Read `spec.md` completely, then inspect all SVG files and both reference PNGs before making any code change.

Implement the feature on `Template/WebHoanTien` as specified. Do not redesign unrelated parts of the page.

Important: the checked design is the same existing CatsBack home page, with one enhancement inside `.dashboard-create-card`: a Product/Shop segmented selector plus the Shop success state.

Use MVC/Razor Pages + existing JavaScript/CSS only. Do not introduce React/JSX/TSX. Keep Product behavior backward-compatible. Do not add a DB migration in v1. Server-side classify normalized/resolved Shopee URLs and preserve target type through pending-login flow.

Before finishing:
- run unit tests,
- run `dotnet build`,
- verify 375, 390, 430 mobile widths,
- verify desktop,
- list every changed file,
- call out any design deviation.
