# Prompt dùng trực tiếp cho Codex

Tôi đã cung cấp file ZIP chứa bộ UI handoff đã được chốt cho trang Dashboard CatBack.

Mục tiêu: **redesign giao diện dashboard hiện tại của CatBack theo đúng template glossy đã duyệt, nhưng KHÔNG thay đổi nghiệp vụ/backend hiện tại.**

## 1. Trước khi code

Giải nén ZIP và đọc/xem TOÀN BỘ theo thứ tự:

1. `spec.md`
2. `reference/01_final_template.png`
3. `blueprints/00_page_layout_blueprint.svg`
4. toàn bộ `reference/crops/*.png`
5. toàn bộ `svg/*.svg`
6. `css/catback-glossy-tokens.css`
7. `design-tokens.json`

Sau đó inspect source project hiện tại và xác định:

- View/page nào đang render dashboard/home.
- Layout/sidebar/topbar hiện tại nằm ở đâu.
- CSS/SCSS hiện tại nằm ở đâu.
- Logo CatBack hiện tại là asset nào.
- Mascot CatBack hiện tại là asset nào.
- Icon library đang sử dụng.
- Components/partials có thể reuse.
- Data/model hiện tại đang bind vào hero, ví, đơn hàng và hướng dẫn.

**Không tạo logo mới. Không tạo mascot mới. Reuse đúng asset hiện có trong project.**

## 2. Source of truth

Thứ tự ưu tiên:

1. `reference/01_final_template.png` – visual tổng thể.
2. `spec.md` – kích thước, token, behavior, constraints.
3. `blueprints/00_page_layout_blueprint.svg` – tỷ lệ layout.
4. `svg/*_exact.svg` – reference từng component.
5. `svg/09_gloss_shadow_system.svg` – glossy/shadow recipe.

Các `*_exact.svg` là visual reference có embedded raster. **Không được dùng chúng làm UI production.** Phải rebuild bằng HTML/Razor + CSS + asset thật của project.

## 3. Scope

Chỉ redesign UI của dashboard.

Không thay:

- API
- database
- flow tạo link
- flow đơn hàng
- ví
- rút tiền
- authentication
- quyền
- business rules

Nếu thấy cần thay backend để render UI thì dừng và báo lại, không tự mở rộng scope.

## 4. Layout phải bám blueprint

Desktop reference 1672 × 941:

- Sidebar: 244px
- Topbar: 70px
- Hero: x=270, y=70, w=1365, h=278
- Link generator: 892 × 158
- Wallet: 465 × 181
- Feature tiles: 892 × 66
- Recent orders: 895 × 294
- Commission guide: 465 × 352

Không cần hardcode canvas 1672px; hãy dùng CSS grid/flex responsive nhưng ở viewport ~1672px phải ra rất gần reference.

## 5. Glossy style bắt buộc

Độ bóng phải giống template đã chốt:

- Wallet: bóng rõ nhất.
- Active sidebar: cyan glow nhẹ.
- Hero: gradient + translucent highlight/wave.
- White card: shadow nhẹ, không glow mạnh.
- Text/icon luôn sharp, không blur.

Dùng token trong:

`css/catback-glossy-tokens.css`

Có thể tích hợp token vào hệ CSS hiện tại, không cần giữ nguyên tên class nếu project đã có convention tốt hơn.

## 6. Logo và mascot

Đặc biệt quan trọng:

- Giữ NGUYÊN logo CatBack của web hiện tại.
- Reuse mascot CatBack hiện có.
- Không dùng logo/mascot do AI tạo lại từ reference.
- Không crop logo từ screenshot để dùng production.

Nếu project có nhiều logo/mascot, chọn asset đang được dùng trên trang hiện tại.

## 7. Component mapping

Cần implement/reuse các vùng sau:

### Sidebar
Reference: `svg/01_sidebar_exact.svg`

### Topbar
Reference: `svg/02_topbar_exact.svg`

### Hero
Reference: `svg/03_hero_banner_exact.svg`

### Link generator
Reference: `svg/04_link_generator_exact.svg`

### Wallet glossy card
Reference: `svg/05_wallet_card_exact.svg`

### Feature tiles
Reference: `svg/06_feature_tiles_exact.svg`

### Recent orders
Reference: `svg/07_recent_orders_exact.svg`

### Commission guide
Reference: `svg/08_commission_guide_exact.svg`

### Gloss/shadow
Reference: `svg/09_gloss_shadow_system.svg`

## 8. Các quy tắc visual

- Background page: gần `#F6FBFE`.
- Navy text: gần `#082D5B`.
- Card border: gần `#DDEAF0`.
- Card radius: 18px chủ đạo.
- Shadow card: mềm, xanh xám, không đen nặng.
- CTA chính: navy/blue gradient.
- Cyan/teal chỉ dùng làm accent.
- Wallet: gradient deep blue → teal → mint.
- Các status pill giữ pastel và readable.

## 9. Responsive

Desktop là source of truth.

Ở >=1440px: bám sát template.

Ở 1200–1439px: co kích thước theo grid nhưng giữ 2 cột nếu đủ chỗ.

Ở 992–1199px: cho phép right column xuống dưới.

Ở mobile: reuse responsive behavior hiện tại; không scale toàn dashboard desktop xuống mobile.

## 10. Cách làm mong muốn

Trước khi sửa code, trả về ngắn gọn:

1. Dashboard hiện tại nằm ở file nào.
2. Sidebar/topbar nằm ở file nào.
3. CSS chính liên quan.
4. Logo/mascot asset tìm thấy.
5. Component nào reuse được.
6. File dự kiến sửa.
7. Điểm nào chưa rõ.

Sau đó bắt đầu implementation.

## 11. Visual QA sau khi code

Sau khi implement:

1. Build project.
2. Mở dashboard ở viewport gần 1672 × 941.
3. So sánh trực tiếp với `reference/01_final_template.png`.
4. Kiểm tra:
   - width sidebar
   - height topbar
   - height hero
   - alignment 2 columns
   - wallet gloss
   - active nav glow
   - card shadows
   - typography hierarchy
   - mascot/logo không bị stretch
   - không horizontal scrollbar
5. Chỉnh CSS cho tới khi khoảng cách/hierarchy gần reference.

## 12. Không được làm

- Không dùng `reference/01_final_template.png` làm background toàn trang.
- Không render `*_exact.svg` trực tiếp thay UI.
- Không hardcode dữ liệu demo trong ảnh.
- Không thay logo CatBack.
- Không tự thêm feature mới.
- Không thay business flow.
- Không dùng blur mạnh làm text/icon mờ.

## 13. Kết quả cuối

Trả report:

- Files modified
- Files created
- Components reused
- CSS/token changes
- Responsive changes
- Build result
- Visual differences còn lại (nếu có)

Ưu tiên kết quả **giống template, sắc nét, bóng mượt nhưng vẫn là UI thật bằng HTML/CSS**, không phải ảnh giả giao diện.
