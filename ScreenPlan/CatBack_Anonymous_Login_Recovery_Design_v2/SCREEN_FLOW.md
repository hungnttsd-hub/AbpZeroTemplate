# Screen flow

## Flow 1: Chọn cách tiếp tục
Home / CTA `Đăng nhập`
-> `01_login_choice_v2.png`

### Options
- `Đăng nhập với Google` -> social login
- `Đăng nhập bằng tài khoản` -> form login hiện tại
- `Sử dụng tài khoản ẩn danh` -> `02_anonymous_intro.png`

## Flow 2: Anonymous account
`02_anonymous_intro.png`
-> user bấm `Tạo tài khoản ẩn danh`
-> `03_anonymous_success_recovery_code.png`
-> user sao chép / lưu mã
-> `Tiếp tục vào app`
-> app chính
-> có thể xem `05_anonymous_profile.png`

## Flow 3: Khôi phục tài khoản
Đăng nhập lại
-> `04_restore_with_recovery_code.png`
-> nhập mã khôi phục
-> xác thực thành công
-> vào app

## Flow 4: Rút tiền
User anonymous từ ví / tài khoản bấm `Rút tiền`
-> `07_withdraw_requires_registration_modal.png`
-> `Đăng ký tài khoản`
-> `06_register_upgrade.png`
-> hoàn tất đăng ký
-> cho phép rút tiền
