# CatsBack — Nâng cấp tạo Link sản phẩm / Link Shop Shopee

**Repository:** `hungnttsd-hub/AbpZeroTemplate`  
**Branch baseline:** `Template/WebHoanTien`  
**Baseline commit khi lập plan:** `ff47728d76528443fcdaaccdb6c2d5dbec79367d` — `fix lỗi modal nhấp nháy`  
**Stack bắt buộc:** ABP.IO / ASP.NET Core MVC + Razor Pages, JavaScript thuần, CSS hiện có. **Không React / JSX / TSX.**

> Mục tiêu của package này: Codex có đủ business rule, source map, UI blueprint và SVG assets để triển khai đúng giao diện CatsBack đã duyệt, không tự redesign.

---

## 1. Mục tiêu nghiệp vụ

Hiện tại người dùng tạo affiliate link theo **sản phẩm**. Khi muốn mua nhiều sản phẩm trong cùng một Shop, họ phải quay lại CatsBack nhiều lần để tạo/mở từng link sản phẩm.

Nâng cấp cho phép cùng một khối `Dán link Shopee tại đây` có 2 chế độ:

1. **Link sản phẩm** — giữ nguyên flow hiện tại.
2. **Link shop** — người dùng dán URL trang Shop Shopee, CatsBack tạo affiliate link trỏ vào Shop. Người dùng chỉ cần mở Shopee một lần, sau đó duyệt Shop và thêm nhiều sản phẩm vào giỏ.

Không tạo màn hình mới cho bước nhập link. Không tách thành 2 form. Chỉ bổ sung segmented selector trong card hiện có.

---

## 2. UI đã chốt

Ảnh authoritative:
- `references/approved-template-product-shop.png`
- `references/current-home-before-upgrade.png`

SVG authoritative:
- `blueprint.svg`
- `blueprints/01-home-product-mode.svg`
- `blueprints/02-home-shop-mode.svg`
- `blueprints/03-home-shop-created.svg`
- `assets/svg/components/*`

### 2.1. Nguyên tắc bắt buộc

- **Giữ nguyên** header/hero, card hoàn tiền 80%, card ví, bottom navigation và style tổng thể hiện tại.
- Chỉ nâng cấp card đang có class `.dashboard-create-card`.
- Không chuyển card thành trang riêng.
- Không đổi tone màu app.
- Không tạo header `CatsBack Cashback` kiểu mock khác với source.
- Mobile-first. Desktop phải responsive theo layout hiện tại.
- Font dùng hệ hiện có: `Inter` cho UI; không nhúng font mới.
- Không dùng asset ngoài nếu repo đã có asset tương đương.
- Các SVG trong package này là hướng dẫn/asset mới; Codex có thể reuse icon hiện có trong `/wwwroot/catback/icons/` nếu hình và kích thước khớp.

---

## 3. Copy/UI text cố định

### Header card
`Dán link Shopee tại đây ✦`

### Segmented selector
- `Link sản phẩm`
- `Link shop`

Default: **Link sản phẩm**

### Helper line
`Chọn loại link để hệ thống tạo link hoàn tiền phù hợp.`

### Guide link
`Xem hướng dẫn ›`

### Product mode
Placeholder:
`Dán link sản phẩm Shopee tại đây...`

CTA:
`Tạo link hoàn tiền →`

### Shop mode
Placeholder:
`Dán link cửa hàng Shopee tại đây...`

CTA:
`Tạo link hoàn tiền →`

### Shop success
Badge:
`Link cửa hàng`

CTA:
`Vào Shop mua hàng →`

Hint:
`Mở shop trên Shopee để thêm nhiều sản phẩm vào giỏ hàng trong cùng một lần mua.`

Fallback shop title:
`Cửa hàng Shopee`

> Không tự suy diễn tên thương hiệu từ slug URL nếu không có metadata đáng tin cậy. Text `Apple Flagship Store` trong SVG chỉ là fixture thiết kế.

---

## 4. Interaction spec

### 4.1. Chuyển Product / Shop

Segmented selector là `<input type="radio">` hoặc control accessible tương đương.

- Default `Product`.
- Khi đổi mode:
  - không reload trang;
  - cập nhật placeholder;
  - cập nhật sr-only label;
  - reset message lỗi/success về trạng thái idle;
  - giữ nguyên URL đã paste để user có thể đổi mode nếu chọn nhầm;
  - không tự submit.
- Selected segment:
  - nền gradient teal;
  - chữ trắng;
  - icon trắng.
- Unselected segment:
  - nền rất nhạt/trắng;
  - chữ `#66758F`;
  - border/outline nhẹ.

### 4.2. Submit

Form vẫn dùng handler hiện tại `Prepare`.

Payload phải bổ sung:
`TargetType = Product | Shop`

Trong AJAX:
- disable CTA;
- text tạm: `Đang tạo link...`;
- success update UI không full reload;
- error hiện trong `#url-status`.

### 4.3. Product success

Giữ hành vi hiện tại:
- card product được upsert vào `Link của bạn`;
- CTA `Mua ngay`;
- vẫn hiển thị estimate nếu có;
- ảnh/name metadata vẫn như hiện tại.

### 4.4. Shop success

Sau khi tạo:
1. render **shop result compact card** ngay trong `.dashboard-create-card`, theo `03-home-shop-created.svg`;
2. đồng thời upsert record vào `Link của bạn`;
3. CTA chính `Vào Shop mua hàng`;
4. copy action dùng stable CatsBack URL `/go/{trackingToken}` / `RedirectUrl`;
5. không hiển thị `Hoàn lại dự kiến ...` ở shop card vì không có product offer để estimate;
6. dùng store placeholder icon nếu chưa có shop image.

Shop result compact card phải có:
- icon/thumbnail Shop;
- title/fallback;
- badge `Link cửa hàng`;
- stable link rút gọn của CatsBack;
- nút `Sao chép`;
- CTA vàng `Vào Shop mua hàng →`;
- hint bên dưới.

### 4.5. Danh sách link

Tất cả nơi render `AffiliateTrackingDto` phải phân biệt Product/Shop.

Product:
- title product;
- `Sản phẩm từ Shopee`;
- estimate;
- CTA `Mua ngay`.

Shop:
- title/fallback;
- badge `Link cửa hàng`;
- meta `Mua nhiều sản phẩm trong cùng shop`;
- không estimate product;
- CTA `Vào Shop mua hàng`.

Áp dụng ít nhất:
- Home `/` recent links.
- `/Links`.
- `/LinkResult` nếu route này còn được sử dụng.

---

## 5. Source hiện tại và file cần nâng cấp

### Web/Razor
1. `src/WebHoanTien.Web/Pages/Index.cshtml`
   - thêm segmented selector;
   - hidden/bound target type;
   - thêm shop-result template;
   - recent card render theo TargetType;
   - template AJAX render theo TargetType.

2. `src/WebHoanTien.Web/Pages/Index.cshtml.cs`
   - thêm `[BindProperty] AffiliateLinkTargetType LinkTargetType`;
   - truyền target type vào Validate/Create;
   - AJAX response trả `targetType`, `shopDisplayName` nếu có;
   - `PendingAffiliateAction` phải lưu cả URL + TargetType + Nonce.

3. `src/WebHoanTien.Web/Pages/PendingAffiliate.cshtml.cs`
   - đọc `TargetType` từ pending payload;
   - truyền vào `CreateAffiliateLinkInput`;
   - không được mặc định Product sau khi user login.

4. `src/WebHoanTien.Web/Pages/Links.cshtml`
   - card Product/Shop khác nhau.

5. `src/WebHoanTien.Web/Pages/LinkResult.cshtml`
   - condition title/estimate/CTA theo TargetType.

6. `src/WebHoanTien.Web/wwwroot/customer.js`
   - selector state;
   - placeholder/helper;
   - AJAX payload tự đi theo form;
   - `setLinkCardData()` render Product/Shop;
   - render/remove shop inline result;
   - copy URL;
   - CTA label.

7. `src/WebHoanTien.Web/wwwroot/link-page-mobile.css`
   - segmented selector;
   - helper line;
   - shop result compact card;
   - mobile responsive.
   - không phá các rule card/list hiện tại.

8. `src/WebHoanTien.Web/wwwroot/customer-dashboard.css`
   - chỉ bổ sung base style nếu component cần dùng cả desktop;
   - tránh duplicate mobile overrides với `link-page-mobile.css`.

### Contracts/Domain Shared
9. `src/WebHoanTien.Domain.Shared/Affiliates/AffiliateEnums.cs`
   - thêm:
     ```csharp
     public enum AffiliateLinkTargetType
     {
         Product = 1,
         Shop = 2
     }
     ```

10. `src/WebHoanTien.Application.Contracts/Affiliates/AffiliateDtos.cs`
    - `ValidateAffiliateUrlInput.TargetType`
    - `AffiliateUrlValidationDto.TargetType`
    - `CreateAffiliateLinkInput.TargetType`
    - `AffiliateTrackingDto.TargetType`
    - optional `ShopDisplayName`
    - optional `ShopKey` nếu classifier trả được identifier an toàn.

### Application
11. `src/WebHoanTien.Application/Affiliates/AffiliateDomainServices.cs`
    - không tiếp tục coi mọi URL `shopee.vn` là hợp lệ như nhau;
    - bổ sung classifier Product/Shop;
    - giữ product canonicalization hiện tại.

12. Khuyến nghị tạo file mới:
    `src/WebHoanTien.Application/Affiliates/ShopeeLinkTargetClassifier.cs`

    Trách nhiệm:
    - classify URL đã normalize;
    - trả `Product` nếu match product pattern hiện tại;
    - trả `Shop` nếu match shop page pattern đã allowlist;
    - reject root/search/live/video/campaign/system pages;
    - không phụ thuộc Product API.

13. `src/WebHoanTien.Application/Affiliates/SafeAffiliateUrlResolver.cs`
    - sau resolve short link phải để Create flow classify target thực tế;
    - không tin target type do client gửi.

14. `src/WebHoanTien.Application/Affiliates/AffiliateLinkAppService.cs`
    - validate requested TargetType với target thực tế;
    - không cần product metadata khi TargetType=Shop;
    - DTO map phải trả TargetType bằng classifier từ `NormalizedUrl`;
    - Product API lỗi không được biến card thành Shop.

15. `src/WebHoanTien.Application/Integrations/Shopee/ShopeeAffiliateLinkBuilder.cs`
    - **không cần đổi thuật toán** nếu URL shop đã normalize và được classifier chấp nhận;
    - builder hiện generic theo `originUrl`, nên phải reuse.

### Redirect
16. `src/WebHoanTien.Web/Controllers/AffiliateRedirectController.cs`
    - giữ `/go/{trackingToken}`;
    - giữ click logging;
    - redirect build từ `tracking.NormalizedUrl`;
    - chỉ cần bảo đảm new classifier/normalizer vẫn chấp nhận URL Shop hợp lệ.

---

## 6. Chiến lược dữ liệu — KHÔNG migration DB ở v1

### Quyết định

**Không thêm cột `TargetType` vào `AffiliateTracking` ở v1.**

Lý do:
- `NormalizedUrl` đã là nguồn xác định target;
- product URL hiện được canonical thành `/product/{shopId}/{itemId}`;
- shop URL giữ page URL;
- tránh migration cho một thuộc tính có thể derive.

### Cảnh báo quan trọng

**Không derive target bằng `ProductId != null`.**

Hiện tại `ProductId` chỉ được set sau khi `GetProductOfferAsync()` thành công. Nếu Shopee/provider lỗi, một product tracking có thể có `ProductId == null`. Derive theo field này sẽ hiển thị sai thành Shop.

### Cách đúng

`AffiliateTrackingDto.TargetType` được derive bằng **ShopeeLinkTargetClassifier trên `NormalizedUrl`**.

Kết quả:
- legacy product link vẫn classify Product;
- Product API lỗi vẫn Product;
- Shop link classify Shop;
- không migration.

Nếu sau này thêm TikTok/Lazada shop links hoặc target phức tạp hơn thì mới cân nhắc persist `TargetType`.

---

## 7. URL classification rules

### 7.1 Product

Reuse chính xác pattern hiện tại:
- `-i.{shopId}.{itemId}`
- `/product/{shopId}/{itemId}`
- `/opaanlp/{shopId}/{itemId}`

Sau normalize phải canonical:
`https://shopee.vn/product/{shopId}/{itemId}`

### 7.2 Shop

Chỉ chấp nhận page URL đã được classifier xác nhận là Shop.

V1 ưu tiên:
- direct Shopee shop page dạng path Shop thực tế được app support/test;
- short link sau resolver nếu final URL classify được Shop.

Không coi mọi URL non-product trên `shopee.vn` là Shop.

### 7.3 Bắt buộc reject

Tối thiểu reject:
- `https://shopee.vn/` root;
- search result;
- live/livestream;
- video;
- cart/checkout;
- login/account/system pages;
- campaign/landing page không phải Shop;
- URL host ngoài allowlist hiện tại;
- non-HTTPS;
- URL có user-info.

### 7.4 Selected mode mismatch

User chọn **Link sản phẩm** nhưng URL thực tế là Shop:
`Đây là link cửa hàng Shopee. Hãy chọn "Link shop" để tạo link.`

User chọn **Link shop** nhưng URL thực tế là Product:
`Đây là link sản phẩm Shopee. Hãy chọn "Link sản phẩm" để tạo link.`

Client selection chỉ là UX intent. Server phải classify lại URL sau normalization/resolution.

---

## 8. Short link

Current flow có `s.shopee.vn`, `shope.ee`, `vn.shp.ee`.

Quy tắc:
- direct URL có thể classify ngay;
- short URL chưa biết Product/Shop trước resolve;
- `CreateAsync` phải resolve bằng `ISafeAffiliateUrlResolver`;
- sau resolve mới enforce TargetType;
- không bypass SSRF/network safety hiện có.

Nếu user chưa login:
- pending payload phải giữ TargetType;
- sau login resolver + classifier chạy lại;
- mismatch trả lỗi về Home.

---

## 9. DTO/API đề xuất

```csharp
public enum AffiliateLinkTargetType
{
    Product = 1,
    Shop = 2
}

public sealed class ValidateAffiliateUrlInput
{
    public string Url { get; set; } = string.Empty;
    public AffiliateLinkTargetType TargetType { get; set; } = AffiliateLinkTargetType.Product;
}

public sealed class CreateAffiliateLinkInput
{
    public string Url { get; set; } = string.Empty;
    public AffiliateLinkTargetType TargetType { get; set; } = AffiliateLinkTargetType.Product;
}

public sealed class AffiliateUrlValidationDto
{
    // existing...
    public AffiliateLinkTargetType? TargetType { get; set; }
}

public sealed class AffiliateTrackingDto
{
    // existing...
    public AffiliateLinkTargetType TargetType { get; set; }
    public string? ShopDisplayName { get; set; }
}
```

`ShopDisplayName`:
- nullable;
- chỉ populate nếu có nguồn metadata đáng tin;
- UI fallback `Cửa hàng Shopee`.

---

## 10. PageModel change

### IndexModel

```csharp
[BindProperty]
public AffiliateLinkTargetType LinkTargetType { get; set; } = AffiliateLinkTargetType.Product;
```

Validate/Create:

```csharp
var validation = await _links.ValidateAsync(new ValidateAffiliateUrlInput
{
    Url = LinkUrl,
    TargetType = LinkTargetType
});
```

```csharp
var result = await _links.CreateAsync(new CreateAffiliateLinkInput
{
    Url = LinkUrl,
    TargetType = LinkTargetType
});
```

Pending:

```csharp
public sealed record PendingAffiliateAction(
    string Url,
    AffiliateLinkTargetType TargetType,
    string Nonce);
```

Không thay data-protector purpose `WebHoanTien.PendingAffiliate.v1` nếu không cần; nhưng phải test deserialization cookie cũ. Nếu muốn strict versioning, đổi sang `.v2` và chấp nhận cookie cũ tự hết hạn 20 phút.

---

## 11. Razor markup đề xuất

Trong `.dashboard-create-card`, sau heading và trước helper/input:

```html
<fieldset class="affiliate-target-selector" aria-label="Loại link Shopee">
    <label class="affiliate-target-option">
        <input type="radio"
               asp-for="LinkTargetType"
               value="Product"
               checked />
        <span>
            <!-- product SVG -->
            Link sản phẩm
        </span>
    </label>

    <label class="affiliate-target-option">
        <input type="radio"
               asp-for="LinkTargetType"
               value="Shop" />
        <span>
            <!-- shop SVG -->
            Link shop
        </span>
    </label>
</fieldset>

<p class="affiliate-target-help">
    <!-- info SVG -->
    Chọn loại link để hệ thống tạo link hoàn tiền phù hợp.
</p>
```

Không hardcode `checked` nếu Razor Tag Helper đã bind ModelState; chỉ dùng default từ PageModel.

---

## 12. CSS tokens / measurements

Authoritative component measurements ở `blueprint.svg`.

### Colors
- Navy text: `#08254A`
- Teal dark: `#087C89`
- Teal active: `#0B9098`
- Teal active 2: `#10A7A5`
- Border: `#DCE4EE`
- Muted: `#66758F`
- Input muted: `#9AA6B8`
- White: `#FFFFFF`
- Warning/Shop CTA yellow: `#FFBD24`
- Yellow light: `#FFD85A`
- Error: reuse existing error token/rule
- Success: reuse existing success token/rule

### Mobile create card
- horizontal page gap: existing `10px`;
- card radius: `22px`;
- card padding: `21px 14px 18px`;
- selector height: `40px`;
- selector radius: `999px`;
- input min-height: `54px`;
- input radius: `14px`;
- main CTA min-height: `50px`;
- main CTA radius: `14px`;
- component vertical gaps: 10–15px.

### Selector
- 2 equal columns;
- total width 100%;
- outer bg `#F5F7FA`;
- border `#DCE4EE`;
- active inset 2px;
- active gradient teal;
- tap target >= 40px.

---

## 13. Accessibility

- selector phải dùng radio semantics;
- visible selected state không chỉ dựa vào màu;
- input label sr-only đổi theo mode;
- CTA/loading vẫn có accessible name;
- copy result có `aria-live` hoặc reuse pattern hiện tại;
- shop placeholder `alt=""` nếu decorative;
- focus-visible ring teal;
- không đặt click handler lên `<div>` thay cho button/label.

---

## 14. Security / safety

Không nới lỏng các guard hiện tại:
- allowlisted Shopee hosts;
- HTTPS only;
- no user-info;
- SafeAffiliateUrlResolver SSRF/DNS safety;
- redirect chỉ lấy tracking active;
- `/go/{trackingToken}` tiếp tục rebuild affiliate URL server-side;
- client không được truyền arbitrary final redirect URL.

Server phải **classify actual final URL**, không tin `TargetType` từ browser.

---

## 15. Tracking/Attribution

Không đổi tracking token behavior:
- mỗi tracking vẫn có `TrackingToken`;
- click vẫn log `AffiliateClick`;
- `/go/{trackingToken}` vẫn là link CatsBack stable;
- `ShopeeAffiliateLinkBuilder` vẫn đưa `sub_id=TrackingToken`.

Shop link không cần tạo tracking mới cho từng product user thêm trong Shopee. Attribution của order vẫn phụ thuộc conversion report Shopee/sub_id hiện có.

---

## 16. Acceptance Criteria

### AC01 — Default mode
GIVEN mở Home  
THEN `Link sản phẩm` được selected mặc định.

### AC02 — Switch mode
WHEN chọn `Link shop`  
THEN placeholder đổi sang `Dán link cửa hàng Shopee tại đây...`  
AND không reload.

### AC03 — Product unchanged
GIVEN Product mode + URL product hợp lệ  
WHEN tạo link  
THEN behavior, metadata, estimate và `Mua ngay` như trước.

### AC04 — Shop direct URL
GIVEN Shop mode + URL shop hợp lệ  
WHEN tạo link  
THEN tracking được lưu  
AND stable CatsBack RedirectUrl được tạo  
AND UI hiển thị `Link cửa hàng`  
AND CTA `Vào Shop mua hàng`.

### AC05 — Shop redirect
WHEN bấm `Vào Shop mua hàng`  
THEN request đi qua `/go/{trackingToken}`  
AND click được log  
AND redirect tới Shopee affiliate URL với origin là Shop.

### AC06 — Mismatch Product
GIVEN Product mode + URL Shop  
THEN server reject bằng message yêu cầu chọn `Link shop`.

### AC07 — Mismatch Shop
GIVEN Shop mode + URL Product  
THEN server reject bằng message yêu cầu chọn `Link sản phẩm`.

### AC08 — Reject non-shop Shopee pages
GIVEN Shop mode + search/live/root/campaign URL  
THEN không tạo tracking.

### AC09 — Short Shop link
GIVEN Shop mode + Shopee short URL  
WHEN resolver trả final Shop URL hợp lệ  
THEN tạo Shop tracking.

### AC10 — Anonymous user
GIVEN chưa login + Shop mode + URL  
WHEN submit và login thành công  
THEN pending action vẫn giữ `Shop`  
AND tạo Shop link, không rơi về Product.

### AC11 — Product metadata failure
GIVEN URL product hợp lệ nhưng provider metadata fail  
THEN DTO/card vẫn là Product, không bị nhận nhầm Shop.

### AC12 — Recent Links
Shop item trong Home recent list có badge/CTA Shop; Product item giữ UI cũ.

### AC13 — `/Links`
Danh sách lịch sử phân biệt đúng 2 loại.

### AC14 — Responsive
375/390/430px không overflow ngang; desktop không phá grid.

### AC15 — No DB migration
Feature v1 chạy không cần schema migration.

---

## 17. Test matrix

| Case | Selected | URL actual | Expected |
|---|---|---|---|
| Direct product | Product | Product | Pass |
| Direct shop | Shop | Shop | Pass |
| Product mismatch | Product | Shop | Reject |
| Shop mismatch | Shop | Product | Reject |
| Short -> product | Product | Product | Pass |
| Short -> shop | Shop | Shop | Pass |
| Short mismatch | Shop | Product | Reject after resolve |
| Shopee root | Shop | Root | Reject |
| Search URL | Shop | Search | Reject |
| Live URL | Shop | Live | Reject |
| Outside Shopee | any | Other host | Reject |
| Product API error | Product | Product | Still Product |
| Existing same shop | Shop | same normalized | restore/reuse current duplicate behavior |
| Anonymous shop | Shop | Shop | Login then create Shop |
| Copy link | Shop | Shop | Copy stable CatsBack URL |
| Click link | Shop | Shop | Log click + redirect |

---

## 18. Suggested implementation order

### Phase 1 — Classification contract
1. Add `AffiliateLinkTargetType`.
2. Add TargetType to inputs/DTO.
3. Build `ShopeeLinkTargetClassifier`.
4. Unit test classifier.

### Phase 2 — Application
5. Update Validate/Create.
6. Enforce type after short-link resolution.
7. Return TargetType in DTO mapping.
8. Preserve duplicate/hidden behavior.

### Phase 3 — Pending login
9. Extend pending payload.
10. Test anonymous Shop flow.

### Phase 4 — UI
11. Add selector in `Index.cshtml`.
12. CSS to match blueprint.
13. JS switch mode.
14. AJAX Product/Shop card rendering.
15. Inline shop success card.

### Phase 5 — Other views
16. `/Links`.
17. `/LinkResult`.

### Phase 6 — Regression
18. Product direct/short.
19. `/go` tracking.
20. hide/show/copy.
21. mobile/desktop.
22. build/test.

---

## 19. Tests Codex phải bổ sung

Ưu tiên test project hiện có:

- `test/WebHoanTien.Application.Tests`
  - normalizer/classifier.
  - app service type mismatch.
  - product provider failure does not change type.
  - duplicate Shop URL.

- `test/WebHoanTien.Web.Tests`
  - PageModel pending target type.
  - redirect behavior nếu test infrastructure hỗ trợ.

Không phụ thuộc live Shopee network trong unit test. Mock resolver/provider.

---

## 20. Definition of Done

- [ ] UI đúng `approved-template-product-shop.png`.
- [ ] Product/Shop selector hoạt động bằng keyboard/touch.
- [ ] Product flow không regression.
- [ ] Shop direct + short link pass.
- [ ] Wrong mode reject server-side.
- [ ] Shop card hiển thị đúng trên Home + `/Links`.
- [ ] Anonymous Shop flow giữ mode sau login.
- [ ] `/go` log click và redirect đúng.
- [ ] không schema migration ở v1.
- [ ] unit tests mới pass.
- [ ] `dotnet build` pass.
- [ ] không thêm React/TSX/JSX.
- [ ] không redesign hero/wallet/header/navigation.

---

## 21. Prompt sẵn cho Codex

```text
Read the entire CatsBack-Shop-Link-Codex package before editing code.

Repository: hungnttsd-hub/AbpZeroTemplate
Branch: Template/WebHoanTien
Baseline reviewed by the spec: ff47728d76528443fcdaaccdb6c2d5dbec79367d

Implement the Product Link / Shop Link upgrade described in spec.md.

Hard requirements:
- ASP.NET Core MVC / Razor Pages only. No React, JSX or TSX.
- Preserve the existing CatsBack home header, 80% cashback block, wallet block and bottom navigation.
- Upgrade only the existing "Dán link Shopee tại đây" card by adding the approved Product/Shop segmented selector and Shop result state.
- Match the SVG blueprints and approved PNG reference.
- Product behavior must remain backward-compatible.
- Server must classify the real Shopee URL and must not trust the UI selection.
- Support direct and existing supported Shopee short links.
- Do not classify by ProductId nullability.
- V1 should not add a database migration; derive AffiliateLinkTargetType from normalized URL using a dedicated classifier.
- Preserve pending link TargetType through login.
- Update Home, /Links, /LinkResult, customer.js and CSS.
- Reuse /go/{trackingToken}, click logging and ShopeeAffiliateLinkBuilder.
- Add tests for classifier, mismatch, short-link result, anonymous pending flow, and product metadata failure.
- Run tests/build and report changed files and any deviation from the approved design.
```
