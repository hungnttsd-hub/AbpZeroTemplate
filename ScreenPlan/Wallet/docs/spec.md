# Catback UI — Master Implementation Spec

## 1. Technology constraints

This project uses:

- ABP.IO Free
- ASP.NET Core MVC / Razor Pages
- Server-side rendered `.cshtml`
- C#
- Existing ABP layout/theme and Bootstrap stack
- Vanilla JavaScript / jQuery only when the existing project already uses it

Do **not** introduce:
- React / JSX / TSX
- Next.js
- Vue / Angular
- a separate SPA frontend
- Tailwind unless it already exists in the project

Visual references are the source of truth. SVG blueprints are layout/geometry references.

---

# 2. Scope

This package covers:

1. Link creation / cashback landing page
2. Customer saved/generated link list
3. Hide generated products from the list
4. Per-link action menu
5. Mobile empty state
6. Order list + order detail reference assets from the previous approved design
7. Shared SVG/icon system

---

# 3. Link page — required customer behavior

## 3.1 Paste link

Customer enters a Shopee URL.

UI:
- Input label: `Dán link Shopee tại đây`
- CTA: `Tạo link hoàn tiền`
- Helper text should say the system attaches the customer's tracking marker.
- Keep benefit/trust messages compact.

Validation:
- empty => inline validation
- unsupported host => inline validation
- invalid/expired URL => friendly error
- processing => disable CTA and show local loading state
- success => newly generated item should appear at the top of `LINK CỦA BẠN`

Never expose affiliate internals in customer UI.

## 3.2 Generated link list

Customer-visible information only:
- product thumbnail
- product name
- source/platform: Shopee
- estimated cashback
- created time, desktop only when space allows
- `Mua ngay`
- overflow menu

Do not show:
- internal id
- Shopee item id
- shop id
- tracking id
- affiliate id
- commission received by Catback / seller
- raw redirect parameters

## 3.3 Hide generated products

Add a switch in `LINK CỦA BẠN` header:

`Ẩn sản phẩm đã tạo`

Behavior:
- OFF: show active + hidden items according to the current business rule.
- ON: hide items the customer has explicitly marked as hidden/created.
- Preserve the switch in query string or user preference if the project already has a preference mechanism.
- Suggested query string:
  `/Links?hideCreated=true`

Each row menu must contain:
- `Sao chép link`
- `Ẩn khỏi danh sách`
- `Xóa link`

Semantics:
- **Ẩn khỏi danh sách** = non-destructive. Do not delete DB data.
- **Xóa link** = destructive. Ask for confirmation.
- A hidden item must be recoverable when viewing hidden items or when the hide filter is disabled, depending on final business rule.

Recommended domain fields:
```csharp
public bool IsHidden { get; set; }
public DateTime? HiddenAt { get; set; }
```

Do not overload `IsDeleted` for a simple hide action.

---

# 4. ABP MVC architecture

Recommended structure:

```text
src/WebHoanTien.Web/
├── Pages/
│   ├── Links/
│   │   ├── Index.cshtml
│   │   ├── Index.cshtml.cs
│   │   ├── _CreateLinkCard.cshtml
│   │   ├── _LinkList.cshtml
│   │   ├── _LinkRowDesktop.cshtml
│   │   ├── _LinkCardMobile.cshtml
│   │   ├── _LinkActions.cshtml
│   │   └── _EmptyState.cshtml
│   └── Orders/
│       ├── Index.cshtml
│       └── Detail.cshtml
└── wwwroot/
    └── catback/
        ├── css/
        ├── js/
        └── icons/
```

Use the existing ABP layout. Do not rebuild global shell unless required.

---

# 5. Customer DTOs

```csharp
public class CustomerLinkListItemDto
{
    public Guid Id { get; set; } // routing/action only; never render as text
    public string ProductName { get; set; } = default!;
    public string? ProductImageUrl { get; set; }
    public string Platform { get; set; } = "Shopee";
    public decimal EstimatedCashback { get; set; }
    public DateTime CreatedTime { get; set; }
    public string PurchaseUrl { get; set; } = default!;
    public string CashbackUrl { get; set; } = default!;
    public bool IsHidden { get; set; }
}
```

Input:

```csharp
public class GetCustomerLinksInput : PagedAndSortedResultRequestDto
{
    public bool HideCreated { get; set; }
    public string? Keyword { get; set; }
}
```

Return:

```csharp
PagedResultDto<CustomerLinkListItemDto>
```

Never bind Entity classes directly to the Razor page.

---

# 6. PageModel flow

```text
Browser GET /Links?hideCreated=true&page=1
        ↓
Links.IndexModel.OnGetAsync()
        ↓
ICustomerLinkAppService.GetListAsync(input)
        ↓
PagedResultDto<CustomerLinkListItemDto>
        ↓
Razor render desktop/mobile from same model
```

Create:
```text
POST/handler
Paste URL
↓
Application Service validates + generates affiliate redirect
↓
returns CustomerLinkListItemDto
↓
redirect back to GET or update list through existing AJAX pattern
```

Prefer normal server-rendered GET/POST first. Add AJAX only if the existing project already uses it consistently.

---

# 7. Responsive implementation

## Desktop >= 1024px

- Existing ABP desktop shell/sidebar.
- Link creation card:
  - horizontal input + CTA
  - 3 trust-benefit points underneath
- Link list:
  - compact table/list rows
  - hide switch in section header
  - overflow menu anchored to the row
  - pagination footer

## Mobile < 768px

The same DTO powers a card list.

Mobile states:
1. link-entry
2. generated-link list
3. row action bottom sheet
4. empty state

For mobile:
- CTA full width
- action menu becomes a bottom sheet
- product info compressed to 2 lines
- cashback remains prominent
- `Mua ngay` remains visible
- minimum touch target 44x44px

---

# 8. Visual tokens

```css
:root {
  --cb-navy-950: #001B3D;
  --cb-navy-900: #00264D;
  --cb-navy-850: #00345D;
  --cb-teal-700: #087C89;
  --cb-teal-600: #0B9098;
  --cb-teal-500: #10A7A5;
  --cb-teal-100: #E8F8F7;

  --cb-text-primary: #08254A;
  --cb-text-secondary: #66758F;
  --cb-text-muted: #8B97AA;

  --cb-page: #F7F9FC;
  --cb-surface: #FFFFFF;
  --cb-border: #DCE4EE;
  --cb-divider: #E8EDF3;

  --cb-warning: #FFC83D;
  --cb-danger: #E64C5B;

  --cb-radius-sm: 10px;
  --cb-radius-md: 14px;
  --cb-radius-lg: 20px;
  --cb-shadow-card: 0 8px 24px rgba(8, 37, 74, 0.06);
}
```

Font:
```css
font-family: Inter, "Segoe UI", Roboto, Arial, sans-serif;
```

---

# 9. SVG usage rules

Use SVG for:
- navigation icons
- eye-off / visibility
- copy
- trash
- external link
- status
- cashback / wallet
- store
- support

Use raster/WebP for:
- product thumbnails
- mascot/marketing image if required

For dynamic UI, do not render whole cards as a single SVG image. Use SVG only as icons/reference; build the cards in HTML/CSS.

---

# 10. Link list actions

## Copy
- copy `CashbackUrl`
- show brief toast: `Đã sao chép link`

## Hide
- update `IsHidden = true`
- remove row from current visible list if `HideCreated=true`
- no destructive confirmation needed
- show undo if the project already has toast undo patterns

## Delete
- destructive
- confirmation:
  `Bạn có chắc muốn xóa link này?`
- use soft delete if the domain supports it
- hidden and deleted are different states

---

# 11. Empty state

When the list has no visible rows:

Title:
`Bạn chưa có link nào`

Body:
`Dán link Shopee để nhận hoàn tiền ngay!`

CTA:
`Dán link ngay`

The CTA should focus/scroll to the create-link input.

---

# 12. Accessibility

- minimum 44px touch target mobile
- keyboard accessible overflow menu
- focus state visible
- switch uses checkbox/switch semantics
- status/action cannot rely on color only
- product image has alt text
- confirmation dialog focus-trapped if modal

---

# 13. Security and ownership

Every list/action query must be scoped to the current authenticated user.

Never trust a `Guid id` from browser without verifying ownership.

Hide/Delete/Copy endpoints must not allow access to another customer's generated link.

---

# 14. Codex instructions

Before coding:
1. Read this file completely.
2. Open `references/link-page/*.png`.
3. Open `blueprints/link-page-desktop.svg` and `blueprints/link-page-mobile.svg`.
4. Reuse `assets/svg/`.
5. Inspect the existing ABP MVC conventions in the repository.

Implementation constraints:
- Razor Pages / MVC only
- no React
- no TSX/JSX
- reuse existing ABP layout
- use Application Services + DTOs
- use localization instead of hardcoded UI strings where the project has localization
- do not expose internal IDs/affiliate data
- do not use `IsDeleted` for Hide

Visual acceptance:
- desktop: compare at 1440px
- mobile: compare at 390px
- major spacing delta <= 8px
- font size delta <= 2px
- radius delta <= 4px
- navy/teal visual balance should match references

Functional acceptance:
- valid Shopee URL creates a customer link
- invalid URL shows inline error
- generated item appears at top
- copy works
- hide is non-destructive
- delete asks confirmation
- hide switch updates visible items
- mobile action menu renders as bottom sheet
- empty state is correct


---

# 15. Wallet module

This package now also includes wallet UI references.

## 15.1 Wallet information hierarchy

Customer-facing wallet information:
- current wallet balance / available balance
- total money already recorded in the system
- pending commission / money about to be recorded
- recent wallet changes
- withdraw CTA
- wallet history CTA

Suggested labels:
- `Số dư trong ví` or `Số dư khả dụng`
- `Tổng tiền đã ghi nhận`
- `Hoa hồng sắp ghi nhận`

Do not expose internal accounting data or admin reconciliation fields.

## 15.2 Home page compact wallet summary

The home page should show a short wallet summary card:
- current wallet balance as the main number
- two small supporting metrics:
  - `Đã ghi nhận`
  - `Sắp ghi nhận`
- CTA: `Xem ví`

Use the compact card reference:
`references/wallet/home-wallet-summary-card.png`

## 15.3 Wallet overview page

Main layout:
1. big balance card
2. two metric cards
3. info banner about pending commission
4. CTA buttons: `Rút tiền`, `Lịch sử ví`
5. recent wallet movements list

Recent wallet movement item:
- icon
- title
- time
- amount
- status badge (e.g. `Đã ghi nhận`, `Đang xử lý`, `Sắp ghi nhận`)

Status color guidance:
- confirmed / recorded: teal or green
- pending processing: amber
- negative/withdrawal: orange or red-orange
- avoid using color alone without text

## 15.4 Withdraw page

Main layout:
1. available balance hero card
2. amount input
3. quick amount chips:
   - `100.000đ`
   - `200.000đ`
   - `500.000đ`
   - `Toàn bộ`
4. payout bank account section
5. fee and processing-time info strip
6. summary block:
   - `Số tiền rút`
   - `Phí`
   - `Tiền thực nhận`
7. primary CTA:
   - `Yêu cầu rút tiền`
8. notes
9. withdrawal history row

Validation rules:
- entered amount must be > 0
- entered amount must not exceed available balance
- if no bank account exists, redirect/require bank account selection first
- if the amount is invalid, show inline validation under the field
- primary CTA disabled while submitting

Suggested DTO:
```csharp
public class CustomerWalletOverviewDto
{
    public decimal WalletBalance { get; set; }
    public decimal TotalRecordedAmount { get; set; }
    public decimal PendingCommissionAmount { get; set; }
}
```

```csharp
public class WithdrawRequestInput
{
    public decimal Amount { get; set; }
    public Guid BankAccountId { get; set; }
}
```

Important business semantics:
- `WalletBalance` = current withdrawable balance
- `PendingCommissionAmount` = not yet withdrawable
- `TotalRecordedAmount` = cumulative amount recorded historically; not equal to current balance

## 15.5 Suggested Razor structure

```text
Pages/
└── Wallet/
    ├── Index.cshtml
    ├── Index.cshtml.cs
    ├── Withdraw.cshtml
    ├── Withdraw.cshtml.cs
    ├── _WalletBalanceCard.cshtml
    ├── _WalletMetrics.cshtml
    ├── _WalletRecentChanges.cshtml
    ├── _WithdrawAmountForm.cshtml
    ├── _WithdrawSummary.cshtml
    └── _WalletInfoNote.cshtml
```

## 15.6 Visual references for wallet

Use:
- `references/wallet/home-with-wallet-summary.png`
- `references/wallet/home-wallet-summary-card.png`
- `references/wallet/wallet-overview-full.png`
- `references/wallet/wallet-overview-balance-card.png`
- `references/wallet/wallet-overview-actions-and-list.png`
- `references/wallet/wallet-withdraw-full.png`
- `references/wallet/wallet-withdraw-header-and-form.png`
- `references/wallet/wallet-withdraw-summary-and-notes.png`

Use blueprints:
- `blueprints/wallet-home-summary.svg`
- `blueprints/wallet-overview-mobile.svg`
- `blueprints/wallet-withdraw-mobile.svg`
