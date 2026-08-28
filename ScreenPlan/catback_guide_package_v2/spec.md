# CATBACK — SPEC TRANG HƯỚNG DẪN MOBILE + MÀN CHI TIẾT VIDEO

## 0. Mục tiêu

Tài liệu này là nguồn tham chiếu để Codex/Developer dựng đúng **cụm tính năng Hướng dẫn mobile CatBack**, bao gồm:

1. **Home** có link `Xem hướng dẫn ›` trong card `Dán link Shopee tại đây ✨`.
2. **Trang Hướng dẫn** riêng, hiển thị 3 video card nhỏ + phần accordion.
3. **Trang Chi tiết video** khi người dùng bấm xem một video hướng dẫn.

Phạm vi:
- Mobile first.
- Một trang Hướng dẫn riêng.
- Ba màn chi tiết video dùng chung một layout template.
- Tự nhận Android/iOS cho nội dung cài đặt/thêm lối tắt.

> Không tự ý đổi cấu trúc, không biến link hướng dẫn thành card/banner lớn ở Home, không làm video card quá to, không bỏ accordion.

---

# 1. Tài liệu tham chiếu trong ZIP

## Tài liệu chính
- `spec.md`: tài liệu nguồn ưu tiên cao nhất.
- `CODEX_INSTRUCTIONS.md`: checklist triển khai.
- `design-tokens.json`: màu, spacing, radius, typography.

## Visual reference
- `reference/home-current.png`: Home hiện tại để giữ đồng bộ.
- `reference/template-guide-mobile.png`: mockup trang Hướng dẫn.
- `reference/detail-install-video.png`: mockup màn chi tiết video cài đặt.
- `reference/detail-create-link-video.png`: mockup màn chi tiết video tạo link.
- `reference/detail-register-video.png`: mockup màn chi tiết video đăng ký tài khoản.
- `reference/commission-rules-source.png`: nội dung text gốc của phần lưu ý hoa hồng.

## Blueprint / SVG
- `blueprint/guide-mobile-blueprint.svg`
- `blueprint/detail-video-install-blueprint.svg`
- `blueprint/detail-video-create-link-blueprint.svg`
- `blueprint/detail-video-register-blueprint.svg`
- `svg/guide-mobile-full.svg`
- `svg/detail-video-install-full.svg`
- `svg/detail-video-create-link-full.svg`
- `svg/detail-video-register-full.svg`
- `svg/*.svg`: bộ component SVG rời.

---

# 2. Route và điều hướng

## 2.1. Home
Trong card `Dán link Shopee tại đây ✨`:
- Bỏ mô tả cũ.
- Chỉ giữ link text `Xem hướng dẫn ›`.
- Màu teal, underline nhẹ.
- Không background, không card phụ.
- Không làm card Dán link cao thêm đáng kể.

```html
<a class="guide-link" href="/huong-dan#tao-link-hoan-tien">
  Xem hướng dẫn <span aria-hidden="true">›</span>
</a>
```

## 2.2. Trang Hướng dẫn
Route đề xuất: `/huong-dan`

Anchor:
- `#cai-dat`
- `#tao-link-hoan-tien`
- `#dang-ky`
- `#luu-y-hoa-hong`

Khi đi từ Home:
- mở `/huong-dan#tao-link-hoan-tien`;
- chờ layout render rồi scroll card `Tạo link hoàn tiền` vào vùng nhìn;
- offset theo header sticky nếu có;
- highlight border/shadow teal rất nhẹ 800–1200 ms;
- tôn trọng `prefers-reduced-motion`.

## 2.3. Trang Chi tiết video
Route đề xuất:
- `/huong-dan/video/cai-dat`
- `/huong-dan/video/tao-link-hoan-tien`
- `/huong-dan/video/dang-ky`

Hành vi:
- Bấm video card ở trang Hướng dẫn → mở đúng route chi tiết.
- Nút Back quay lại `/huong-dan`, ưu tiên giữ lại anchor gần nhất.
- Nếu đang ở chi tiết `Tạo link hoàn tiền`, back về `/huong-dan#tao-link-hoan-tien`.
- Nếu đang ở chi tiết `Cài đặt`, back về `/huong-dan#cai-dat`.
- Nếu đang ở chi tiết `Đăng ký`, back về `/huong-dan#dang-ky`.

---

# 3. Khung mobile

- Design width: **390 px**
- Target: **360–430 px**
- Page scroll dọc.
- Safe area top/bottom theo `env(safe-area-inset-*)`.
- Body background: `#F5F9FC`
- Card: `#FFFFFF`
- Border: `#DDE7EE`

Desktop/tablet:
```css
.guide-page,
.guide-video-page {
  width: min(100%, 430px);
  margin: 0 auto;
}
```

---

# 4. Header

## 4.1. Header trang Hướng dẫn
- Navy/blue gradient.
- Cao khoảng 156 px.
- Bo đáy 24 px.
- Back tròn trắng bên trái.
- Title giữa: `Hướng dẫn`
- Subtitle: `Video và lưu ý để tạo link hoàn tiền đúng cách`
- Mascot CatBack nhỏ bên phải.
- Nếu source project đã có mascot thật: **reuse asset hiện có**, không vẽ lại.

## 4.2. Header trang Chi tiết video
- Cùng style gradient/navy với trang Hướng dẫn để giữ hệ thống.
- Title giữa: `Chi tiết hướng dẫn`
- Không cần subtitle dài.
- Back tròn trắng bên trái.
- Mascot nhỏ bên phải.

Kích thước dùng chung:
- page padding: 16 px
- back: 44×44 px
- title: 24/30, 700
- subtitle: 13–14 px
- mascot: 72–88 px

---

# 5. Card nhận diện thiết bị

Ngay dưới header của trang Hướng dẫn.

Chip: `Tự động theo thiết bị`

Android:
`Đang hiển thị hướng dẫn cho Android`

iOS:
`Đang hiển thị hướng dẫn cho iPhone`

Không có tab chọn tay.

```js
function detectPlatform() {
  const ua = navigator.userAgent || navigator.vendor || window.opera || "";
  const isAndroid = /android/i.test(ua);
  const isIOS =
    /iPad|iPhone|iPod/.test(ua) ||
    (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);

  if (isIOS) return "ios";
  if (isAndroid) return "android";
  return "unknown";
}
```

Unknown:
`Hướng dẫn cài đặt cho thiết bị của bạn`

Visual:
- white card
- radius 18
- border `#DDE7EE`
- padding 16
- device icon trong nền teal nhạt
- chip `#E7F7F6`
- tên Android/iPhone màu teal

---

# 6. Video hướng dẫn trên trang Hướng dẫn

Heading: `Video hướng dẫn`

Có đúng 3 card nhỏ.

## 6.1. Cài đặt / thêm lối tắt
ID: `cai-dat`

Android:
- Title: `Cài đặt ứng dụng / thêm lối tắt`
- Subtitle: `Dành cho thiết bị bạn đang sử dụng`
- Duration demo trên mockup: `01:20`

iOS:
- Title có thể dùng lại `Cài đặt ứng dụng / thêm lối tắt`
- Subtitle đổi theo ngữ cảnh iPhone.

Video Android/iOS là 2 source khác nhau; frontend chọn tự động.

## 6.2. Tạo link hoàn tiền
ID: `tao-link-hoan-tien`

- Title: `Tạo link hoàn tiền`
- Subtitle: `Cách dán link và tạo link nhanh`
- Duration demo: `00:45`

Đây là card focus khi vào từ Home.

## 6.3. Đăng ký tài khoản
ID: `dang-ky`

- Title: `Đăng ký tài khoản`
- Subtitle: `Tạo tài khoản CatBack trong vài bước`
- Duration demo: `01:10`

## 6.4. Cấu trúc card

- Card toàn bộ có thể bấm.
- Thumbnail trái.
- Play icon overlay trên thumbnail.
- Title/subtitle/duration giữa.
- Chevron phải.

Base 390:
- width: calc(100% - 32px)
- min-height: 92 px
- thumb: 106×68
- radius card: 16
- radius thumb: 12
- gap: 12
- padding: 10 12

Typography:
- title: 15–16 px / 700
- subtitle: 12.5–13.5 px
- duration pill: 11–12 px

Player:
- ưu tiên modal/bottom sheet hoặc route riêng; ở thiết kế này **route riêng được chốt**;
- ratio 16:9;
- lazy-load player;
- không tải 3 iframe khi page vừa mở.

---

# 7. Hướng dẫn nhanh — Accordion

Heading: `Hướng dẫn nhanh`

Các mục:
1. `Cách dán link Shopee` — collapsed
2. `Khi nào đơn hàng được ghi nhận?` — collapsed
3. `Để chắc chắn được tính hoa hồng` — expanded trong mockup
4. `Vì sao chưa thấy hoa hồng?` — collapsed

Behavior:
- chỉ mở 1 mục tại một thời điểm;
- tap header để mở/đóng;
- chevron xoay 180°;
- animation 180–220 ms;
- expanded body tự tăng chiều cao, không clamp.

---

# 8. Nội dung accordion: Để chắc chắn được tính hoa hồng

1. **Bấm Mua ngay** để sang sàn rồi đặt hàng.

2. **Mỗi đơn một lần bấm.** Mua thêm đơn nữa thì quay lại đây bấm Mua ngay lần nữa, không bấm lại thì đơn sau không được tính.

3. Đặt hàng **trong vòng 7 ngày** kể từ lúc bấm Mua ngay.

4. **Tuyệt đối đừng xem video hay livestream** của sản phẩm đó trên sàn. Xem livestream sẽ kích hoạt hoa hồng cho bên livestream: CatBack không nhận được gì, nên cũng không có gì để chia lại cho bạn.

5. Sau khi bấm Mua ngay và truy cập sản phẩm trên sàn, **đừng vội mua hàng ngay lập tức.** Hãy kéo xuống đọc thông tin về sản phẩm vài giây rồi hãy chốt đơn nhé.

---

# 9. Trang Chi tiết video

Trang chi tiết video dùng chung một template layout cho cả 3 video.

## 9.1. Cấu trúc màn hình
Từ trên xuống:
1. Header gradient + back + title `Chi tiết hướng dẫn` + mascot.
2. Card player/video hero.
3. Title video + subtitle mô tả.
4. Meta pills (duration, platform/category).
5. Card `Các bước chính`.
6. Section `Video liên quan` gồm 2 item còn lại.
7. CTA cuối trang.

## 9.2. Card player/video hero
- Card nền trắng.
- Radius 18.
- Border `#DDE7EE`.
- Player preview theo tỷ lệ gần 16:9.
- Có overlay play ở giữa.
- Thanh progress minh hoạ phía dưới thumbnail.
- Góc dưới hiển thị time start `00:00` và tổng duration.
- Có icon fullscreen/miniplayer ở phải.

Lưu ý:
- Mockup là visual reference, production có thể dùng video player thực tế.
- Nếu dùng embed, chỉ load player khi người dùng bấm play hoặc mở màn hình chi tiết.

## 9.3. Các bước chính
- Card riêng nền trắng.
- Title: `Các bước chính`.
- Danh sách 4 bước đánh số trong vòng tròn teal nhạt.
- Body text 15–16 px, line-height 1.45–1.6.

## 9.4. Video liên quan
- Title: `Video liên quan`.
- Gồm 2 item card nhỏ, cùng pattern với card ở trang Hướng dẫn nhưng có thể gọn hơn.
- Không lặp chính video hiện tại.
- Bấm item → sang video detail tương ứng.

## 9.5. CTA cuối trang
- Nút full width, gradient teal.
- Radius pill/18–20.
- Cao 52–56 px.
- CTA theo từng màn:
  - Chi tiết `Cài đặt`: `Xem video tiếp theo`
  - Chi tiết `Tạo link hoàn tiền`: `Xem video tiếp theo`
  - Chi tiết `Đăng ký tài khoản`: `Bắt đầu ngay`

## 9.6. Nội dung từng màn chi tiết

### A. Chi tiết — Cài đặt / thêm lối tắt
Route: `/huong-dan/video/cai-dat`

Title: `Cài đặt ứng dụng / thêm lối tắt`
Subtitle:
- Android: `Dành cho thiết bị Android đang sử dụng`
- iOS: `Dành cho thiết bị iPhone đang sử dụng`

Meta pills:
- duration `01:20`
- platform pill: `Android` hoặc `iPhone`

Các bước chính (bản Android demo):
1. Mở trình duyệt bạn đang dùng (Chrome, Samsung Internet, v.v.).
2. Bấm vào nút Chia sẻ (`⋮` hoặc menu ở góc trên).
3. Chọn `Thêm vào màn hình chính`.
4. Xác nhận để thêm lối tắt CatBack ra màn hình chính.

Nếu là iOS có thể đổi text bước 2–4 cho phù hợp Safari.

Video liên quan:
- `Tạo link hoàn tiền`
- `Đăng ký tài khoản`

CTA: `Xem video tiếp theo`

### B. Chi tiết — Tạo link hoàn tiền
Route: `/huong-dan/video/tao-link-hoan-tien`

Title: `Tạo link hoàn tiền`
Subtitle: `Cách dán link và tạo link nhanh`

Meta pills:
- duration `00:45`
- category/platform pill: `Shopee`

Các bước chính:
1. Sao chép link sản phẩm Shopee bạn muốn chia sẻ.
2. Dán link vào ô tạo link hoàn tiền trên CatBack.
3. Bấm `Tạo link hoàn tiền` để hệ thống xử lý.
4. Chia sẻ link mới tạo và theo dõi đơn hàng trong mục Đơn hàng.

Video liên quan:
- `Cài đặt ứng dụng / thêm lối tắt`
- `Đăng ký tài khoản`

CTA: `Xem video tiếp theo`

### C. Chi tiết — Đăng ký tài khoản
Route: `/huong-dan/video/dang-ky`

Title: `Đăng ký tài khoản`
Subtitle: `Tạo tài khoản CatBack trong vài bước`

Meta pills:
- duration `01:10`
- category pill: `Tài khoản`

Các bước chính:
1. Chọn Đăng ký trên màn hình tài khoản hoặc trang chào mừng.
2. Nhập số điện thoại hoặc thông tin cần thiết để tạo tài khoản.
3. Xác thực và hoàn tất hồ sơ cơ bản theo hướng dẫn.
4. Đăng nhập để bắt đầu tạo link và nhận hoàn tiền.

Video liên quan:
- `Cài đặt ứng dụng / thêm lối tắt`
- `Tạo link hoàn tiền`

CTA: `Bắt đầu ngay`

---

# 10. Design tokens chính

Brand:
- Navy 900 `#063B68`
- Navy 800 `#07527E`
- Teal 700 `#078D92`
- Teal 600 `#0BA5A4`
- Teal 500 `#12B8B2`
- Teal 100 `#E2F7F5`
- Yellow 500 `#FFC438`
- Yellow 400 `#FFD75E`

Neutral:
- Text primary `#0B2747`
- Text secondary `#5B6C82`
- Text tertiary `#8C9AAD`
- Border `#DDE7EE`
- Page `#F5F9FC`
- White `#FFFFFF`

Typography:
```css
font-family:
  Inter,
  ui-sans-serif,
  system-ui,
  -apple-system,
  BlinkMacSystemFont,
  "Segoe UI",
  sans-serif;
```

- page title 24/30 700
- section title 20/26 700
- card title 15.5/21 700
- body 13.5/20 400
- small 12/17 400
- chip 12.5/18 600

Spacing:
`4, 8, 10, 12, 16, 20, 24, 32`

Radius:
- large 18
- medium 16
- thumbnail 12
- pill 999

Shadow:
```css
box-shadow: 0 4px 16px rgba(6,59,104,.06);
```

---

# 11. Home — vị trí link

Cấu trúc đã chốt:

```text
[Dán link Shopee tại đây ✨]
Xem hướng dẫn ›

[Input Dán link Shopee tại đây...]

[Tạo link hoàn tiền →]
```

- chỉ là text teal;
- không mô tả;
- không card phụ;
- title → link: 6 px;
- link → input: 16 px;
- font 13–14 semibold;
- underline nhẹ.

Tham chiếu `svg/home-guide-link.svg`.

---

# 12. Responsive

360:
- thumbnail 94–100 px;
- title tối đa 2 dòng;
- không overflow.

390:
- theo SVG.

430:
- giữ padding 16;
- không phóng typography quá mức.

Không dùng fixed height cho expanded accordion.
Không dùng fixed height cứng cho card `Các bước chính`; cho phép tăng theo text.

---

# 13. Edge cases

- iOS/iPadOS → video iOS.
- Android → video Android.
- unknown → neutral fallback, không gán sai Android.
- URL video rỗng → không cho click vào link rỗng.
- hash sai → load top page.
- `#tao-link-hoan-tien` → scroll sau khi layout render.
- text zoom 125–150% không vỡ layout.
- route video slug sai → điều hướng về `/huong-dan` hoặc 404 mềm.
- `Video liên quan` không được chứa video hiện tại.

---

# 14. Accessibility

- contrast tối thiểu WCAG AA;
- Back có `aria-label="Quay lại"`;
- Accordion dùng `button`, `aria-expanded`, `aria-controls`;
- video card có accessible label;
- focus visible 2 px teal;
- touch target >= 44 px;
- nếu CTA dùng nút thật thì có text label đúng ngữ nghĩa.

---

# 15. Acceptance Criteria

**AC01 — Home**  
`Xem hướng dẫn ›` nằm dưới `Dán link Shopee tại đây ✨`; không còn mô tả cũ và không làm card cao đáng kể.

**AC02 — Navigation to guide**  
Bấm link mở trang Hướng dẫn riêng và focus `Tạo link hoàn tiền`.

**AC03 — Platform**  
Tự nhận Android/iOS và hiện video cài đặt đúng thiết bị, không tab tay.

**AC04 — Guide video list**  
Đúng 3 video card nhỏ: cài đặt, tạo link, đăng ký.

**AC05 — Accordion**  
Có `Hướng dẫn nhanh`, hiển thị dạng accordion.

**AC06 — Commission rules**  
Mục `Để chắc chắn được tính hoa hồng` có đúng 5 lưu ý ở section 8.

**AC07 — Mobile**  
Không overflow ngang 360–430 px.

**AC08 — Detail pages**  
Mỗi video mở ra một màn chi tiết riêng với player card, `Các bước chính`, `Video liên quan`, CTA cuối trang.

**AC09 — Related videos**  
`Video liên quan` chỉ gồm 2 video còn lại, không lặp video hiện tại.

**AC10 — Detail CTA**  
CTA cuối trang đúng theo từng màn: `Xem video tiếp theo` hoặc `Bắt đầu ngay`.

**AC11 — Visual**  
Bám `reference/template-guide-mobile.png` và 3 ảnh detail trong thư mục `reference/` cùng blueprint/SVG tương ứng.

**AC12 — Performance**  
Video lazy-load; không tải 3 iframe ngay khi mở trang Hướng dẫn.

**AC13 — Accessibility**  
Back, video, accordion thao tác được bằng keyboard/screen reader.

---

# 16. Những điều Codex KHÔNG được tự thay đổi

- Không biến `Xem hướng dẫn` thành banner/card.
- Không thêm tab Android/iOS mặc định.
- Không thêm video ngoài 3 nhóm đã chốt.
- Không đổi accordion thành list luôn mở.
- Không bỏ `Video liên quan` trong màn chi tiết.
- Không thay CTA cuối trang thành text link nhỏ.
- Không thêm bottom nav vào màn con nếu app hiện tại không dùng.
- Không thay mascot nếu project có asset thật.
- Không làm video/card quá lớn.
- Không copy lỗi chữ trong ảnh AI nếu khác nội dung chuẩn trong spec.

---

# 17. Thứ tự ưu tiên khi có xung đột

1. `spec.md`
2. `reference/template-guide-mobile.png`
3. `reference/detail-install-video.png`
4. `reference/detail-create-link-video.png`
5. `reference/detail-register-video.png`
6. `blueprint/*.svg`
7. `svg/*.svg`
8. suy luận khác
