# Tài khoản ẩn danh và nâng cấp CatBack

## Phạm vi đã triển khai

- Razor Pages dùng layout/theme hiện có, đủ 7 trạng thái từ package thiết kế v2.
- `/Account/Choice` là điểm vào đăng nhập; `/Account/Login` giữ form tài khoản.
- `/Account/Anonymous`, `/Account/AnonymousSuccess`, `/Account/Recovery`, `/Account/Upgrade` và `/Account/UpgradeConfirmation`.
- Hồ sơ hiện tại hiển thị nhánh anonymous. Mọi CTA rút tiền trong shell/Wallet có modal; service rút tiền kiểm tra DB độc lập với UI.
- Một `IdentityUser`/Guid xuyên suốt. Không tạo ví hoặc di chuyển link/order/cashback khi nâng cấp.
- `LoginEmail` cố định, `NormalizedLoginEmail` duy nhất; `IdentityUser.Email` tiếp tục là email liên hệ. Username/email đăng nhập được phân giải tách khỏi email liên hệ. Luồng quên mật khẩu vẫn hỗ trợ cả hai email của cùng chủ tài khoản.

## Cấu hình và phát hành

Tính năng tạo anonymous **mặc định tắt**. Khôi phục, nâng cấp, thu hồi credential và guard rút tiền vẫn hoạt động khi tắt tạo mới.

```json
{
  "Authentication": {
    "Anonymous": { "Enabled": false }
  },
  "DataProtection": {
    "CertificateThumbprint": "THUMBPRINT_RSA_CERTIFICATE",
    "PreviousCertificateThumbprints": ""
  }
}
```

1. Tạm dừng ghi tài khoản trong cửa sổ phát hành, tránh binary cũ tạo user thiếu LoginEmail sau backfill.
2. Sao lưu DB và key ring/certificate. Áp dụng migration `20260909161216_AddAnonymousAccounts` bằng quy trình DbMigrator hiện tại. Không dùng binary mới trước migration.
3. Migration backfill tất cả user cũ thành Registered. Email đăng nhập lấy từ username có dạng email, nếu không thì lấy email hiện tại. Nếu trùng email hoặc hai user có email đăng nhập/liên hệ chồng nhau, migration rollback và yêu cầu xử lý ownership; không tự merge hay xóa user.
4. Deploy backend/UI với `Enabled=false`. Cấu hình Google và SMTP hiện có; `App:SelfUrl` phải là origin HTTPS công khai đáng tin cậy để gửi link nâng cấp.
5. Production cần certificate RSA trong `CurrentUser/My` hoặc `LocalMachine/My`, tài khoản chạy web được đọc private key. Cấu hình thumbprint; app từ chối bật tạo anonymous production nếu thiếu certificate. Development có thể dùng Data Protection key ring hiện tại mà không có certificate.
6. Sau nghiệm thu được yêu cầu, đặt `Authentication:Anonymous:Enabled=true`. Cookie thiết bị dùng Secure, nên chạy HTTPS.

Recovery dùng Data Protection purpose riêng; production thêm RSA-OAEP envelope để cả các Data Protection key cũ chưa mã hóa trong DB cũng không làm lộ recovery. Không lưu private key trong DB/repo. Backup certificate/private key ngoài DB. Khi xoay certificate, giữ certificate cũ và liệt kê thumbprint trong `PreviousCertificateThumbprints` (phân cách `;`); Data Protection và recovery đều cần khóa cũ để giải mã dữ liệu cũ.

Rollback bằng tắt tạo mới. Không rollback về binary không có guard AccountType khi còn anonymous. Migration Down chủ động từ chối nếu còn user Anonymous.

## Vòng đời và API

| Endpoint | Quy tắc |
|---|---|
| POST `/api/account/anonymous` | Chấp thuận điều khoản, tạo hoặc resume theo secret cookie; khóa transaction theo hash secret và unique index chống duplicate |
| POST `/api/account/anonymous/recover` | Mã hợp lệ, tài khoản active/unlocked/Anonymous; phát device secret mới và session sau commit |
| GET `/api/account/anonymous/recovery` | Chỉ phiên anonymous hiện tại; response no-store |
| POST `/api/account/anonymous/recovery/regenerate` | Khóa theo user, thu hồi mã cũ, phát mã mới trong cùng transaction |
| PUT `/api/account/anonymous/username` | Theo policy Identity, uniqueness và kiểm tra xung đột namespace email đăng nhập |
| GET `/api/account/device/current` | Chỉ metadata credential thuộc user hiện tại |
| DELETE `/api/account/device/current` | Thu hồi credential hiện tại, xóa cookie, logout |
| POST `/api/account/upgrade` | Username/email/password/confirmPassword/acceptedTerms/returnUrl; user lấy từ phiên |
| POST `/api/account/upgrade/resend` | Phát token mới, link cũ hết hiệu lực; tài khoản vẫn Anonymous |

Các POST/PUT/DELETE dùng antiforgery. Payload JSON trả `redirectUrl`, `pending`/`message` hoặc lỗi. `CatBack:RegistrationRequired` là mã lỗi guard rút tiền. API device/recovery quản lý chỉ nhận current user, không nhận UserId tùy ý.

Recovery gồm 128 bit ngẫu nhiên (32 ký tự hex chia nhóm, prefix CB), không phải mã ngắn minh họa trong PNG. Không phân biệt hoa/thường; bỏ khoảng trắng và dấu phân nhóm. Mã dùng nhiều lần, không tự xoay khi khôi phục. Xóa browser data + mất mã có thể mất quyền truy cập; không tự dò tài khoản bằng fingerprint.

Credential cookie tồn tại 90 ngày tính từ lúc cấp, chỉ lưu hash ở DB. Logout thường giữ cookie; không tự đăng nhập khi truy cập website, phải chọn dùng lại. Quên thiết bị vô hiệu mọi phiên anonymous gắn với credential đó; không thu hồi mã recovery hoặc thiết bị khác.

Nếu cần xác minh email: lưu hash password trong yêu cầu chờ, không sửa user trước xác minh. Link có hạn 24 giờ, chỉ yêu cầu mới nhất còn hiệu lực. GET link chỉ mở màn xác nhận; POST mới hoàn tất, tránh trình quét email kích hoạt nâng cấp. SMTP lỗi không làm mất dữ liệu đã nhập; gửi lại từ màn Upgrade. Nếu không cần xác minh theo cấu hình Identity, nâng cấp ngay nhưng không gán nhầm EmailConfirmed=true.

Google upgrade dùng endpoint GIS hiện có với `upgradeUserId` ràng buộc với phiên tạo form và antiforgery. Google/email đã thuộc người khác trả lỗi, không chuyển session. OAuth callback truyền thống không được nâng cấp ngầm anonymous; dùng màn Upgrade. Khi hoàn tất, toàn bộ recovery/device anonymous và pending upgrade bị thu hồi, security stamp đổi, cache claims xóa, session mới phát sau commit. Middleware kiểm tra từng phiên anonymous để không phải chờ chu kỳ security-stamp validator mới loại phiên cũ.

## Bảo vệ và vận hành

- Bộ đếm PostgreSQL `CatBackAccountRateLimit` commit riêng, nên request thất bại vẫn tiêu quota và nhiều instance dùng chung.
- Recovery: 30/IP/15 phút và 5/device/15 phút. Create: 10/IP/giờ và 5/device/giờ. Upgrade/resend: 20/IP/giờ và 5/device/giờ. Regenerate: 30/IP/15 phút và 5/device/15 phút. Giới hạn độc lập, lỗi 429 có Retry-After.
- Thiết lập trusted reverse proxy theo hạ tầng để địa chỉ IP giới hạn không bị giả mạo.
- Không bật EF sensitive-data logging hoặc thu request body ở proxy/APM trên auth endpoints. Không thu query token của trang xác minh vào analytics.
- Các entity/DTO/action chứa secret tắt ABP audit body. Event tài khoản chỉ ghi UserId/kết quả; không chứa code/password/device secret/Google token.
- Trang account và response recovery no-store; Turbo không lưu snapshot tài khoản. Đổi trạng thái đăng nhập dùng full navigation. Copy và tạo ảnh chạy hoàn toàn trong browser; mật khẩu/recovery không được submit bằng GET khi JavaScript lỗi.

## Checklist nghiệm thu — chưa chạy

Chỉ chạy test hoặc kiểm thử trình duyệt tự động khi người dùng yêu cầu rõ ràng.

- [ ] User cũ: login username/email đăng nhập, Google link, quên mật khẩu bằng login/contact email, sửa contact email, rút tiền.
- [ ] Migration: Registered backfill; trùng email rollback; không thay UserId; AccountType=0 không bị default DB đổi thành 1.
- [ ] Guest: đúng 3 lựa chọn, URL trả về an toàn, link pending từ trang chủ giữ nguyên.
- [ ] Tạo anonymous: double-click, 2 tab, mất response/retry cùng cookie không duplicate; disabled flag chỉ chặn tạo mới.
- [ ] Recovery: copy, tải PNG, reload, bỏ qua chưa lưu, sai mã, code cũ sau regenerate, đổi thiết bị, 429.
- [ ] Session: logout thường/resume; forget/current-device; cookie refresh vẫn giữ anonymous claims; user bị khóa/inactive không được resume.
- [ ] Upgrade email: pending 24h, SMTP failure/resend, token cũ/hết hạn, uniqueness tại confirm, rollback nguyên tử, current Identity confirmation policy.
- [ ] Upgrade Google: giữ UserId, trùng email/provider không merge hoặc đổi session, đổi phiên trong lúc mở popup bị từ chối.
- [ ] Sau upgrade: so sánh UserId/link/order/cashback/balance trước-sau; mọi recovery/device/session anonymous cũ không truy cập được.
- [ ] Wallet: tất cả CTA và direct GET/POST/API anonymous bị chặn; không tạo withdrawal; Registered giữ các điều kiện ví cũ.
- [ ] UI: đủ 7 màn/state, 375/390/430px và desktop, long username/code, keyboard/modal/Escape/focus, loading/error, back/forward không lộ mã.
- [ ] Certificate/key backup, restart, nhiều instance và rotation vẫn đọc được recovery; DB dump không đủ để giải mã production code.

Kiểm tra đã thực hiện khi triển khai: build (không chạy test), kiểm tra cú pháp JavaScript và rà soát diff. Chưa áp dụng migration vào database hoặc triển khai production.
