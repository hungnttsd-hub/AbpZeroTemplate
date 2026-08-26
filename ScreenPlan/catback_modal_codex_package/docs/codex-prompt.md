# Codex Prompt — Catback Modal System

Read these files first:
1. `docs/spec.md`
2. `docs/modal-system-notes.md`
3. `docs/abp-mvc-notes.md`

Then inspect:
- all images in `references/modal/`
- all SVG blueprints in `blueprints/`
- reusable icons in `assets/svg/`

Implement a reusable Catback modal component system using:
- ABP.IO Free
- ASP.NET Core MVC / Razor Pages
- existing ABP layout/theme
- server-side rendering

Constraints:
- no React
- no TSX/JSX
- replace browser default popup usage with the Catback modal system
- keep Vietnamese UI text
- match the approved visual style closely

Required variants:
- destructive confirmation modal
- success modal
- info modal

Suggested output:
- reusable shared partial/component
- sample usage page
- JS helper methods for open/close/confirm
