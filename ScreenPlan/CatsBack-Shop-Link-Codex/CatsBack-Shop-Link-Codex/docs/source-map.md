# Source map reviewed

Baseline: `Template/WebHoanTien` @ `ff47728d76528443fcdaaccdb6c2d5dbec79367d`

- `src/WebHoanTien.Web/Pages/Index.cshtml` — current create form + recent link cards/templates.
- `src/WebHoanTien.Web/Pages/Index.cshtml.cs` — Prepare handler, anonymous pending cookie, AJAX payload.
- `src/WebHoanTien.Web/Pages/PendingAffiliate.cshtml.cs` — creates pending link after login.
- `src/WebHoanTien.Web/Pages/Links.cshtml` — full link history cards.
- `src/WebHoanTien.Web/Pages/LinkResult.cshtml` — result view.
- `src/WebHoanTien.Web/wwwroot/customer.js` — AJAX create/upsert/copy/hide behavior.
- `src/WebHoanTien.Web/wwwroot/customer-dashboard.css` — dashboard base.
- `src/WebHoanTien.Web/wwwroot/link-page-mobile.css` — mobile link/create/card styling.
- `src/WebHoanTien.Application.Contracts/Affiliates/AffiliateDtos.cs` — link contracts/DTO.
- `src/WebHoanTien.Domain.Shared/Affiliates/AffiliateEnums.cs` — shared enums.
- `src/WebHoanTien.Application/Affiliates/AffiliateDomainServices.cs` — Shopee normalizer and product URL recognition.
- `src/WebHoanTien.Application/Affiliates/SafeAffiliateUrlResolver.cs` — short link safe resolution.
- `src/WebHoanTien.Application/Affiliates/AffiliateLinkAppService.cs` — create/duplicate/product metadata/DTO mapping.
- `src/WebHoanTien.Application/Integrations/Shopee/ShopeeAffiliateLinkBuilder.cs` — generic Shopee affiliate redirect builder.
- `src/WebHoanTien.Web/Controllers/AffiliateRedirectController.cs` — `/go/{trackingToken}`, click logging, redirect.

Core finding: the current Shopee builder is generic by `originUrl`, so Shop origin can reuse it. The risky part is URL classification: the current normalizer accepts any HTTPS URL on allowed Shopee hosts and only extracts product IDs for product patterns.
