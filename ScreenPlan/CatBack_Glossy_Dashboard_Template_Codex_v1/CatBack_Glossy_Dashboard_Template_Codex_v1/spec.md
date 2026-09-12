# CatBack Dashboard – Glossy Desktop UI Spec

**Status:** READY FOR DEV  
**Reference URL:** https://catback.id.vn/  
**Approved visual:** `reference/01_final_template.png`  
**Primary canvas:** 1672 × 941 px

---

## 1. Tóm tắt cách hiểu

Thiết kế đã chốt là bản dashboard desktop CatBack với bố cục hiện tại được giữ nguyên về nghiệp vụ nhưng nâng cấp mạnh về chất lượng thị giác:

- Sidebar xanh navy cố định bên trái.
- Topbar trắng, sạch, ít chi tiết.
- Hero banner xanh mint/cyan có mascot CatBack và khối “Hoàn tiền lên tới 80% hoa hồng”.
- Card tạo link là thao tác chính của trang.
- Card ví sử dụng hiệu ứng **glossy/premium** rõ nhất: gradient xanh đậm → teal → mint, viền cyan sáng, highlight bóng, icon ví 3D/asset hiện có.
- Các card nội dung còn lại giữ nền trắng, bóng nhẹ, sắc nét, không lạm dụng glow.
- Typography navy đậm, icon và CTA teal/cyan.
- Giao diện phải sáng, mượt, có chiều sâu nhưng nội dung vẫn rõ, nhanh và phù hợp web quản trị/hoàn tiền.

Mục tiêu triển khai: **giữ nguyên nghiệp vụ và dữ liệu hiện tại, chỉ thay layout/style theo template đã duyệt.**

---

## 2. Source of truth

Khi các tài liệu có khác biệt, dùng thứ tự ưu tiên sau:

1. `reference/01_final_template.png` – source of truth về visual tổng thể.
2. `spec.md` – source of truth về kích thước, token, hành vi và quy tắc triển khai.
3. `blueprints/00_page_layout_blueprint.svg` – source of truth về vùng layout và tỷ lệ.
4. `svg/*_exact.svg` – reference từng component ở đúng visual đã chốt.
5. `svg/09_gloss_shadow_system.svg` – cách dựng độ bóng/glow.
6. `PROMPT_CODEX.md` – chỉ dẫn thao tác cho Codex.

**Không lấy chữ bị sai/biến dạng trong ảnh làm dữ liệu nghiệp vụ. Text thật phải lấy từ project hiện tại.**

---

## 3. Không được thay đổi

- Không thay logo CatBack hiện tại.
- Không tạo lại mascot nếu project đã có asset mascot.
- Không đổi tên menu/nghiệp vụ nếu không có yêu cầu riêng.
- Không đổi flow tạo link, đơn hàng, ví, rút tiền.
- Không thay backend API chỉ để phục vụ redesign.
- Không đổi database.
- Không đổi icon library nếu project đã có bộ icon phù hợp.
- Không đưa ảnh chụp toàn màn hình vào production thay cho HTML/CSS.

### Brand assets
Codex phải tìm và **reuse logo + mascot hiện tại trong project**. Ảnh reference chỉ dùng để canh layout/style.

---

## 4. Layout desktop chuẩn

Canvas tham chiếu: **1672 × 941**.

| Vùng | X | Y | W | H | Ghi chú |
|---|---:|---:|---:|---:|---|
| Sidebar | 0 | 0 | 244 | 941 | Fixed/Sticky left |
| Topbar | 244 | 0 | 1428 | 70 | Sticky nếu project hiện hỗ trợ |
| Main left padding | 270 | 70 | - | - | 26px sau sidebar |
| Hero | 270 | 70 | 1365 | 278 | Full main width |
| Link generator | 270 | 360 | 892 | 158 | Main column |
| Wallet | 1170 | 360 | 465 | 181 | Right column |
| Feature tiles | 270 | 532 | 892 | 66 | 4 tiles |
| Recent orders | 270 | 615 | 895 | 294 | Main column |
| Commission guide | 1170 | 557 | 465 | 352 | Right column |

### Grid logic
- Sidebar: 244px.
- Main content start: 270px.
- Main content right edge: khoảng 1635px.
- Content width: khoảng 1365px.
- Main column: ~892px.
- Right column: ~465px.
- Gutter giữa 2 cột: ~8–12px theo component.
- Vertical gap chính: 12–18px.

Không cần tuyệt đối từng px khi viewport khác 1672, nhưng tỷ lệ và hierarchy phải giữ giống template.

---

## 5. Design tokens

### Colors
```css
--cb-navy-950: #062D5B;
--cb-navy-900: #073964;
--cb-navy-800: #064B70;
--cb-blue-700: #075C8B;
--cb-blue-600: #0A6B8A;
--cb-teal-600: #089AA2;
--cb-teal-500: #08A6B0;
--cb-cyan-400: #20D5C9;
--cb-cyan-300: #4CEBE1;
--cb-mint-100: #D8FAFB;
--cb-mint-050: #EFFDFD;
--cb-page-bg: #F6FBFE;
--cb-card: #FFFFFF;
--cb-border: #DDEAF0;
--cb-text: #082D5B;
--cb-muted: #6A7A91;
--cb-success: #00A547;
--cb-warning-bg: #FFF1C8;
--cb-info-bg: #E2F2FF;
--cb-success-bg: #DDF8E7;
```

### Radius
```css
--radius-xs: 8px;
--radius-sm: 10px;
--radius-md: 14px;
--radius-lg: 18px;
--radius-xl: 22px;
```

### Shadows
```css
--shadow-card: 0 10px 28px rgba(10, 76, 115, 0.10);
--shadow-soft: 0 5px 16px rgba(10, 76, 115, 0.08);
--shadow-glow: 0 10px 30px rgba(0, 231, 224, 0.24);
```

---

## 6. Công thức “bóng mượt” đã chốt

Hiệu ứng bóng phải tập trung ở **surface**, không làm blur chữ/icon.

### 6.1 Card thường
- `background: #fff`
- `border: 1px solid #DDEAF0`
- `border-radius: 18px`
- `box-shadow: 0 10px 28px rgba(10,76,115,.10)`
- Hover optional: translateY(-1px), shadow tăng nhẹ.

### 6.2 Card ví / premium surface
Base:
```css
background: linear-gradient(135deg, #075C8B 0%, #08A4A7 55%, #20D5C9 100%);
border: 1px solid rgba(76,235,225,.9);
box-shadow:
  0 10px 30px rgba(0,231,224,.24),
  0 14px 34px rgba(6,45,91,.14);
```

Gloss overlay:
```css
::before {
  content: "";
  position: absolute;
  inset: 0;
  border-radius: inherit;
  pointer-events: none;
  background:
    radial-gradient(circle at 76% 18%, rgba(255,255,255,.30), transparent 30%),
    linear-gradient(180deg, rgba(255,255,255,.32), rgba(255,255,255,0) 45%);
  opacity: .55;
}
```

Edge highlight:
```css
box-shadow: inset 0 1px 0 rgba(255,255,255,.34);
```

### 6.3 Active sidebar
```css
background: linear-gradient(90deg,#0C6B8A,#0A8FA6);
border: 1px solid rgba(32,240,229,.85);
box-shadow: 0 8px 22px rgba(0,231,224,.20);
```

### 6.4 Hero
- Mint/cyan gradient nhẹ hơn card ví.
- Có highlight / wave translucent lớn ở background.
- Không làm nền quá neon.
- Mascot và CTA/stat card phải nổi rõ nhưng text bên trái vẫn là focus.

---

## 7. Typography

Ưu tiên dùng font hiện tại của project. Nếu chưa có hệ thống typography rõ ràng, dùng:

```css
font-family: Inter, "Segoe UI", Arial, sans-serif;
```

### Scale tham chiếu
- Hero H1: 40–46px, weight 800/900, line-height 0.98–1.05.
- Section title: 20–24px, weight 800.
- Card title: 16–20px, weight 700/800.
- Body: 13–15px, weight 400–550.
- Caption: 11–12px.
- Table: 11–13px.

Text luôn sharp, `text-shadow` không dùng trừ trường hợp cực nhỏ trên nền tối và phải rất nhẹ.

---

## 8. Component specs

### 8.1 Sidebar
Reference: `svg/01_sidebar_exact.svg`

- Width desktop: 244px.
- Gradient navy dọc.
- Logo CatBack hiện tại trên cùng, không thay.
- Active menu dùng glossy cyan border/glow.
- Icon 20–22px, label ~15px.
- Support card là card tối hơn, viền cyan nhẹ.
- Footer text nhỏ, muted blue-white.

### 8.2 Topbar
Reference: `svg/02_topbar_exact.svg`

- Height 70px.
- White / translucent white.
- Search field ở trái, width khoảng 620px.
- User actions bên phải.
- Không dùng shadow nặng.

### 8.3 Hero banner
Reference: `svg/03_hero_banner_exact.svg`

- Height 278px.
- Rounded 18–20px.
- Hero left: tag → H1 → subtitle → 3 trust items.
- Mascot ở center-right.
- 80% commission card ở far right.
- Giữ nhiều khoảng thở; không nhồi thêm thông tin.

### 8.4 Link generator
Reference: `svg/04_link_generator_exact.svg`

- Đây là action card chính.
- Title + helper line + “Xem hướng dẫn”.
- Input dài, CTA nằm cùng hàng desktop.
- Icon tile teal 48px.
- Primary button navy → blue gradient.

### 8.5 Wallet card
Reference: `svg/05_wallet_card_exact.svg`

- Đây là surface bóng nhất trong dashboard.
- `0đ` rất lớn, trắng, bold.
- Divider cyan mảnh.
- Hai metric “Đã ghi nhận”, “Sắp ghi nhận”.
- Illustration ví nằm bên phải.
- Dùng asset illustration hiện có nếu project có; nếu chưa có, dùng icon/vector tương đương, **không lấy ảnh screenshot làm asset production**.

### 8.6 Feature tiles
Reference: `svg/06_feature_tiles_exact.svg`

- 4 tile bằng nhau.
- Background trắng.
- Icon tile soft pastel.
- Text ngắn 2 dòng.
- Shadow rất nhẹ.

### 8.7 Recent orders
Reference: `svg/07_recent_orders_exact.svg`

- Table/card lớn.
- Header section rõ.
- Header table #F4F8FB.
- Status dùng pill pastel.
- Cashback positive dùng green.
- Không dùng grid line đậm.

### 8.8 Commission guide
Reference: `svg/08_commission_guide_exact.svg`

- Card trắng.
- 4 step đánh số tròn pastel.
- Dòng quan trọng dùng weight 700.
- Bottom tip dùng mint background.

---

## 9. Iconography

- Reuse icon set của project hiện tại.
- Stroke/weight nhất quán.
- Màu icon chủ đạo: white trong sidebar, teal/navy trong content.
- Không trộn emoji vào production UI nếu project hiện dùng icon SVG/font icon.

Các ký hiệu trong file SVG reference chỉ là placeholder thị giác.

---

## 10. Interaction states

### Buttons
- Hover: tăng sáng 4–6%, shadow +10–15%.
- Active: translateY(1px), shadow giảm.
- Focus: `outline: 2px solid rgba(32,213,201,.35)`.
- Disabled: opacity .55, no glow.

### Cards
- Không phải card nào cũng hover.
- Chỉ card clickable mới có hover/focus.

### Input
- Border normal: `#DCE8F0`.
- Focus: teal border + subtle glow.
- Không đổi chiều cao khi focus.

---

## 11. Responsive

Bản chốt là desktop. Không redesign mobile từ template này.

### >= 1440px
- Bám gần đúng blueprint.
- Sidebar 244px.
- Hai cột dưới hero.

### 1200–1439px
- Sidebar có thể 220–232px nếu current layout yêu cầu.
- Main + right column co theo tỷ lệ.
- Wallet vẫn nằm cạnh link generator nếu đủ chỗ.

### 992–1199px
- Có thể stack right column xuống dưới main column.
- Hero giảm font và mascot scale.

### < 992px
- Reuse responsive/mobile behavior hiện tại của project.
- Không ép desktop template xuống mobile bằng scale transform.

---

## 12. Implementation constraints cho Codex

- Project hiện tại là source of truth về routing/data/API.
- Chỉ thay view/layout/CSS/component cần thiết.
- Reuse partial/view component hiện có nếu phù hợp.
- Không duplicate CSS component.
- Không hardcode dữ liệu demo từ PNG vào production.
- Không dùng screenshot làm background của trang.
- Không nhúng SVG exact wrapper vào production như một ảnh toàn màn hình.
- SVG exact trong ZIP chỉ để Codex nhìn đúng visual.

---

## 13. Performance

- Không thêm blur backdrop quá lớn trên nhiều card.
- Hạn chế `filter: blur()` runtime.
- Glow chỉ ở sidebar active, wallet, một số CTA/premium surface.
- Ưu tiên CSS gradients thay ảnh nền.
- Mascot/logo dùng asset optimized hiện có.

---

## 14. Accessibility

- Contrast text navy trên nền sáng đạt mức đọc rõ.
- White text trên glossy wallet phải luôn đủ contrast.
- Focus visible cho button/link/input.
- Không dùng màu đơn thuần để truyền status; giữ text status.
- Table phải giữ semantic table nếu hiện tại đã dùng table.

---

## 15. Acceptance Criteria

### AC01 – Overall
Trang desktop nhìn gần template đã duyệt, cùng hierarchy, spacing, color, depth và card proportions.

### AC02 – Logo
Logo CatBack hiện tại được giữ nguyên; không dùng logo do AI tạo lại.

### AC03 – Gloss
Card ví, active sidebar và hero có hiệu ứng bóng/gloss đúng mức; white content card vẫn sạch, sharp.

### AC04 – Sharpness
Typography, icon và border không bị blur. Không dùng ảnh screenshot để giả UI.

### AC05 – Hero
Hero có headline, mascot, 80% card và trust items đúng bố cục.

### AC06 – Link generator
Input + CTA + helper text đúng thứ bậc như template.

### AC07 – Wallet
Wallet card hiển thị balance, 2 metrics và CTA/illustration ở phải, đúng gradient/gloss.

### AC08 – Orders
Recent Orders table rõ, status pill pastel, cashback green khi >0.

### AC09 – Responsive
Không vỡ layout ở 1440/1366/1280; tablet/mobile fallback hợp lý theo current app.

### AC10 – No business regression
Redesign không thay flow/data/API hiện tại.

---

## 16. Visual QA checklist

So sánh implementation với `reference/01_final_template.png` ở cùng viewport:

- [ ] Sidebar width và active item giống tỷ lệ.
- [ ] Topbar height 70px ±2px.
- [ ] Hero height ~278px.
- [ ] H1 không nhỏ hơn template đáng kể.
- [ ] Mascot không bị stretch.
- [ ] Card 80% không sát mép.
- [ ] Link generator + wallet ngang hàng desktop.
- [ ] Wallet có glossy highlight nhưng text vẫn rõ.
- [ ] Feature tiles cùng chiều cao.
- [ ] Orders + guide align đáy gần nhau.
- [ ] Shadows không bị đen/xám nặng.
- [ ] Không có horizontal scrollbar ở desktop chuẩn.

---

## 17. Files trong gói handoff

- `reference/01_final_template.png` – ảnh chốt.
- `reference/crops/*` – crop từng vùng.
- `blueprints/00_page_layout_blueprint.svg` – blueprint kích thước.
- `svg/00_dashboard_master_exact.svg` – full exact SVG wrapper reference.
- `svg/01_sidebar_exact.svg`
- `svg/02_topbar_exact.svg`
- `svg/03_hero_banner_exact.svg`
- `svg/04_link_generator_exact.svg`
- `svg/05_wallet_card_exact.svg`
- `svg/06_feature_tiles_exact.svg`
- `svg/07_recent_orders_exact.svg`
- `svg/08_commission_guide_exact.svg`
- `svg/09_gloss_shadow_system.svg` – pure vector gloss system.
- `css/catback-glossy-tokens.css`
- `design-tokens.json`
- `PROMPT_CODEX.md`
- `README.md`

