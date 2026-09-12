# DESIGN SPEC

## 1. Tổng thể
- Layout desktop 2 cột:
  - Sidebar trái: 248px.
  - Main content: phần còn lại.
- Max content width: 1500–1600px.
- Body background: #F7F9FC.
- Main padding desktop: 28–32px.
- Section gap: 20–24px.

## 2. Màu sắc
- Navy 900: #062B50
- Navy 800: #0B3F68
- Teal 600: #0897A7
- Teal 500: #10A7B4
- Teal 100: #DFF7F8
- Text primary: #0B2447
- Text secondary: #64748B
- Border: #E2E8F0
- Surface: #FFFFFF
- Background: #F7F9FC
- Success: #16A34A
- Warning: #F59E0B
- Danger: #DC2626

## 3. Typography
Khuyến nghị:
- Font: Inter / system UI / font hiện tại của dự án.
- H1: 32–36px / 700.
- H2: 22–24px / 700.
- H3: 18–20px / 700.
- Body: 14–16px / 400–500.
- Caption: 12–13px / 400.

## 4. Radius
- Button: 10–12px.
- Input: 10–12px.
- Card: 14–16px.
- Badge: pill 999px.

## 5. Shadow
Chỉ dùng nhẹ:
`0 4px 16px rgba(15, 23, 42, 0.05)`

## 6. Sidebar
- Nền: linear-gradient nhẹ hoặc navy solid.
- Menu item height: 48–52px.
- Icon 20–22px.
- Padding horizontal: 16px.
- Active state:
  - background rgba(16,167,180,.18)
  - border-left hoặc accent nhỏ.
- Không dùng glow/shadow mạnh.

## 7. Topbar
- Height: 64–72px.
- Không tạo card nền riêng cho toàn topbar.
- Search max width 600–700px.
- User area căn phải.

## 8. Hero
- Height desktop khoảng 260–300px.
- Background teal rất nhạt hoặc teal gradient nhẹ.
- Nội dung 3 vùng nhưng nhìn như 1 khối thống nhất:
  - Copy
  - Mascot
  - Cashback CTA
- CTA button navy/teal đậm.

## 9. Grid nội dung
Desktop:
- Link generator: 8/12
- Wallet: 4/12
- Orders: 8/12
- Quick guide: 4/12

Tablet:
- 12/12 hoặc 6/12 tùy block.

Mobile:
- 1 cột.

## 10. Card
Card phải:
- nền trắng
- border 1px
- không dùng gradient trừ card chiến dịch đặc biệt
- padding 20–24px

## 11. Table
- Header nền #F8FAFC.
- Row height 60–68px.
- Không zebra mạnh.
- Hover rất nhẹ.
- Status badge nhỏ.

## 12. Responsive
### >= 1440
Sidebar fixed 248px.

### 1024–1439
Sidebar 220–232px.
Main padding 24px.

### 768–1023
Sidebar collapse hoặc drawer.
Grid chuyển 1–2 cột.

### < 768
- Sidebar thành drawer.
- Table -> cards.
- Hero stack dọc.
- CTA full width nếu cần.
- Padding 16px.
