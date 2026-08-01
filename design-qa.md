# Design QA — IZONE homepage

## Comparison target

- Reference content and navigation: https://www.izone.edu.vn/ (captured desktop and 390 × 844 mobile on 2026-08-01).
- Visual asset source: the supplied `Assets/App/images` set and the supplied `trang-chu.html` layout reference.
- Implementation: local ASP.NET Core homepage at `/`.

## Checked

- Desktop: brand header, course-oriented hero, CTA hierarchy, course cards, teaching-method section, teacher cards, resources, registration form, and footer all render from local assets.
- Mobile (390 × 844): header collapses to a menu control, hero stacks, no horizontal overflow, and text remains readable.
- Interactions: mobile navigation expands/collapses; anchor CTAs navigate to their sections; registration shows its confirmation without making an external request; back-to-top is available after scrolling.
- Asset integrity: all visible image assets are served locally from `wwwroot/izone-assets`; no source-site assets are hotlinked.

## Result

The implementation intentionally follows the supplied local IZONE design package while retaining the current public site's core information architecture and content themes. No P0, P1, or P2 visual or interaction defects were found in the checked desktop/mobile states.

final result: passed
