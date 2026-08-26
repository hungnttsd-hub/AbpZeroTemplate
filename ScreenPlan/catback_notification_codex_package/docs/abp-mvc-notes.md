# ABP MVC Implementation Notes

This package is intentionally MVC/Razor-first.

Recommended principle:
- UI state that belongs in URL: GET query string.
- User action that changes data: POST/Application Service.
- Render desktop and mobile variants from the same DTO.
- Prefer partials for presentational reuse.
- Prefer ViewComponent only when the component has its own server-side data acquisition.
- Keep SVG as individual icon assets under `wwwroot/catback/icons`.
- Use ABP localization and authorization conventions already present in the solution.
