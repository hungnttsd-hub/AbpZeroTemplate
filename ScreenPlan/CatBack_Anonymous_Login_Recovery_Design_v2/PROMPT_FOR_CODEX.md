# Prompt cho Codex

Hãy triển khai bộ giao diện mobile-first cho app CatBack dựa trên các file thiết kế trong thư mục `screens/`.

## Bối cảnh
- Đây là app cashback CatBack.
- Hiện tại user có thể tạo link Shopee, theo dõi đơn hàng và cashback.
- Cần bổ sung luồng **Đăng nhập / Tài khoản ẩn danh / Mã khôi phục**.
- Stack ưu tiên: **ASP.NET Core MVC / ABP.IO MVC**.

## Mục tiêu chính
Khi người dùng bấm nút **Đăng nhập** trên app, không mở thẳng form login cũ. Thay vào đó, mở một màn hình lựa chọn với 3 option:
1. **Đăng nhập với Google**
2. **Đăng nhập bằng tài khoản**
3. **Sử dụng tài khoản ẩn danh**

## Quy tắc bắt buộc
- Nếu chọn **Đăng nhập với Google** → đi theo social login.
- Nếu chọn **Đăng nhập bằng tài khoản** → mở form đăng nhập username/email + password.
- Nếu chọn **Sử dụng tài khoản ẩn danh** → mở flow anonymous account.
- Sau khi tạo anonymous account, hệ thống phải hiển thị username random và recovery code.
- Người dùng anonymous vẫn được tạo link, xem đơn hàng, tích lũy cashback.
- Khi user anonymous bấm **Rút tiền**, phải hiện modal yêu cầu nâng cấp lên tài khoản chính thức.
- Khi nâng cấp, phải giữ nguyên dữ liệu và số dư.

## Tài nguyên thiết kế cần bám theo
- `screens/01_login_choice_v2.png`
- `screens/02_anonymous_intro.png`
- `screens/03_anonymous_success_recovery_code.png`
- `screens/04_restore_with_recovery_code.png`
- `screens/05_anonymous_profile.png`
- `screens/06_register_upgrade.png`
- `screens/07_withdraw_requires_registration_modal.png`

## Gợi ý route
- `/auth/choice`
- `/auth/login`
- `/auth/google`
- `/auth/anonymous`
- `/auth/anonymous/success`
- `/auth/recovery-code`
- `/account/profile`

## Gợi ý model
```csharp
public enum AccountType
{
    Anonymous = 0,
    Registered = 1
}

public class AnonymousAccountViewModel
{
    public string Username { get; set; }
    public string RecoveryCode { get; set; }
    public bool IsCurrentDeviceRemembered { get; set; }
}
```

## Hành vi chi tiết
1. Home → bấm `Đăng nhập` → mở `/auth/choice`.
2. Tại `/auth/choice` hiển thị 3 option như trong `01_login_choice_v2.png`.
3. Bấm `Đăng nhập với Google` → flow social login.
4. Bấm `Đăng nhập bằng tài khoản` → mở `/auth/login` với form login hiện tại.
5. Bấm `Sử dụng tài khoản ẩn danh` → mở `/auth/anonymous`.
6. Tại `/auth/anonymous`, bấm `Tạo tài khoản ẩn danh` → tạo account + chuyển `/auth/anonymous/success`.
7. Màn thành công phải hiển thị recovery code rõ ràng, có nút `Sao chép` và `Lưu ảnh`.
8. `/auth/recovery-code` cho phép user nhập mã để đăng nhập lại.
9. Hồ sơ user anonymous phải có badge anonymous + CTA đăng ký tài khoản.
10. Khi bấm `Rút tiền` mà `AccountType = Anonymous` → mở bottom sheet theo thiết kế `07_withdraw_requires_registration_modal.png`.

## Yêu cầu UI
- Giữ header đồng bộ với app CatBack hiện tại: nền navy đậm, mascot bên trái, nút nhỏ bên phải.
- Dùng card trắng bo góc lớn, nền sáng, text navy, button teal gradient.
- Mobile-first, màn hình khoảng 375–430px width.
- Cần dễ maintain, dễ tách component.

## Component nên tách
- `AppHeader`
- `OptionCard`
- `PrimaryButton`
- `SecondaryButton`
- `RoundedInput`
- `InfoBanner`
- `RecoveryCodeCard`
- `ProfileMenuList`
- `BottomSheetModal`

## Kết quả mong muốn
- UI ra gần giống thiết kế nhất có thể.
- Có đủ các màn hình và modal.
- Dễ tích hợp tiếp vào backend nghiệp vụ CatBack.
