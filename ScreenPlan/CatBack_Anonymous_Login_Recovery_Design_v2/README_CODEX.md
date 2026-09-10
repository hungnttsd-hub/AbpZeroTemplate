# CatBack – Bộ thiết kế chi tiết cho luồng Đăng nhập / Tài khoản ẩn danh / Mã khôi phục

## 1. Mục tiêu
Bổ sung cho CatBack một luồng đăng nhập thân thiện hơn, cho phép người dùng chọn 1 trong 3 cách tiếp tục:
1. **Đăng nhập với Google**
2. **Đăng nhập bằng tài khoản**
3. **Sử dụng tài khoản ẩn danh**

Luồng này phải bám sát giao diện hiện tại của app CatBack và giữ tính đồng bộ về màu sắc, header, button và card style.

---

## 2. Quy tắc nghiệp vụ
### 2.1 Các lựa chọn khi bấm “Đăng nhập”
Khi người dùng bấm nút **Đăng nhập** từ trang hiện tại của app, không vào thẳng form cũ nữa, mà hiển thị màn hình lựa chọn với 3 option:
- **Đăng nhập với Google** → đi theo luồng Google sign-in.
- **Đăng nhập bằng tài khoản** → mở form đăng nhập bằng username/email + password.
- **Sử dụng tài khoản ẩn danh** → tạo / dùng tài khoản ẩn danh gắn với thiết bị và có mã khôi phục.

### 2.2 Tài khoản ẩn danh
- Hệ thống sinh tự động username random, ví dụ: `cat_X7K29F`
- Có **mã khôi phục** dạng ví dụ: `CB-7K29-FX83-MP5Q`
- Người dùng có thể tạo link, xem link, xem đơn hàng, tích lũy cashback.
- Khi cần **rút tiền**, người dùng phải **nâng cấp thành tài khoản chính thức**.
- Khi nâng cấp, **giữ nguyên UserId / số dư / link / lịch sử đơn hàng / cashback**.

### 2.3 Mã khôi phục
- Hiển thị ngay sau khi tạo tài khoản ẩn danh thành công.
- Có nút **Sao chép** và **Lưu ảnh**.
- Mục đích: cho phép đăng nhập lại nếu bị logout, đổi máy hoặc mất phiên.

---

## 3. Bộ màn hình
### `screens/01_login_choice_v2.png`
Màn hình chọn cách tiếp tục với 3 lựa chọn đúng như yêu cầu mới:
- Đăng nhập với Google
- Đăng nhập bằng tài khoản
- Sử dụng tài khoản ẩn danh

### `screens/02_anonymous_intro.png`
Màn hình giới thiệu ngắn về tài khoản ẩn danh.

### `screens/03_anonymous_success_recovery_code.png`
Màn hình tạo tài khoản ẩn danh thành công, hiển thị username tạm thời và mã khôi phục.

### `screens/04_restore_with_recovery_code.png`
Màn hình nhập mã khôi phục để dùng lại tài khoản ẩn danh.

### `screens/05_anonymous_profile.png`
Màn hình hồ sơ của user anonymous, có CTA nâng cấp tài khoản.

### `screens/06_register_upgrade.png`
Màn hình đăng ký / nâng cấp tài khoản chính thức.

### `screens/07_withdraw_requires_registration_modal.png`
Bottom sheet / modal khi user anonymous bấm rút tiền.

---

## 4. Luồng người dùng
### Flow A – Đăng nhập từ app
Home / Hero / CTA **Đăng nhập**
→ Mở `01_login_choice_v2.png`

### Flow B – Đăng nhập với Google
`01_login_choice_v2.png`
→ Bấm **Đăng nhập với Google**
→ Đi theo flow social login

### Flow C – Đăng nhập bằng tài khoản
`01_login_choice_v2.png`
→ Bấm **Đăng nhập bằng tài khoản**
→ Mở form đăng nhập bằng username/email + password

> Lưu ý: Form đăng nhập bằng tài khoản là form đăng nhập hiện tại của app, không phải form đăng ký.

### Flow D – Dùng tài khoản ẩn danh
`01_login_choice_v2.png`
→ Bấm **Sử dụng tài khoản ẩn danh**
→ `02_anonymous_intro.png`
→ Bấm **Tạo tài khoản ẩn danh**
→ `03_anonymous_success_recovery_code.png`
→ Bấm **Tiếp tục vào app**

### Flow E – Khôi phục tài khoản ẩn danh
Người dùng bị logout / đổi máy / mất phiên
→ Mở `04_restore_with_recovery_code.png`
→ Nhập mã khôi phục
→ Đăng nhập lại tài khoản ẩn danh

### Flow F – Rút tiền
User anonymous bấm **Rút tiền**
→ Hiện `07_withdraw_requires_registration_modal.png`
→ Bấm **Đăng ký tài khoản**
→ `06_register_upgrade.png`
→ Hoàn tất đăng ký / nâng cấp tài khoản
→ Cho phép rút tiền

---

## 5. Hướng dẫn UI cho Codex
### 5.1 Brand style
- Header nền **navy đậm**
- Mascot CatBack bên trái
- Nút small action ở phải: **Quay lại / Đăng nhập / Bỏ qua** tùy màn hình
- Background sáng, card trắng, border xanh nhạt, bóng đổ rất nhẹ
- Text heading màu navy
- CTA chính dùng gradient teal
- CTA nổi bật phụ có thể dùng vàng

### 5.2 Tone màu gợi ý
```css
:root {
  --cb-navy: #082b63;
  --cb-navy-2: #0d3b78;
  --cb-teal: #11b3b8;
  --cb-teal-2: #109fa7;
  --cb-gold: #f3c94d;
  --cb-bg: #eef6f9;
  --cb-card: #ffffff;
  --cb-border: #d8e7ef;
  --cb-text: #082b63;
  --cb-muted: #6d7b97;
  --cb-link: #0d9ea8;
  --cb-warning-bg: #fff7de;
  --cb-info-bg: #f4fbfd;
  --cb-shadow: 0 10px 28px rgba(8, 43, 99, 0.08);
  --cb-radius-lg: 28px;
  --cb-radius-md: 20px;
}
```

### 5.3 Component nên tách
- `AppHeader`
- `OptionCard`
- `PrimaryButton`
- `SecondaryButton`
- `RoundedInput`
- `InfoBanner`
- `RecoveryCodeCard`
- `ProfileListItem`
- `BottomSheetModal`
- `AnonymousBadge`

---

## 6. Hành vi cần có
- Bấm **Đăng nhập** từ app → mở màn hình lựa chọn 3 option.
- Chọn **Đăng nhập bằng tài khoản** → mở form login truyền thống.
- Chọn **Sử dụng tài khoản ẩn danh** → mở intro anonymous.
- Tạo anonymous → sinh username + recovery code.
- Có thể sao chép / lưu mã khôi phục.
- Hồ sơ anonymous phải có CTA **Đăng ký để rút tiền**.
- Bấm **Rút tiền** khi anonymous → không cho đi tiếp trực tiếp, phải hiện modal yêu cầu đăng ký.

---

## 7. Acceptance criteria
- Có đúng 3 option: **Đăng nhập với Google / Đăng nhập bằng tài khoản / Sử dụng tài khoản ẩn danh**.
- Tài khoản ẩn danh có mã khôi phục.
- Flow khôi phục tài khoản có màn hình riêng.
- Flow rút tiền của tài khoản ẩn danh bị chặn bằng modal đăng ký.
- UI đồng bộ với CatBack hiện tại.
- Mobile-first, dễ nhìn, dễ bấm.
