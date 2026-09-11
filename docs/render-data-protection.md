# Chứng chỉ bảo vệ dữ liệu trên Render

Ứng dụng hỗ trợ RSA PFX có private key qua:

- DataProtection:CertificatePath: đường dẫn PFX nhị phân.
- DataProtection:CertificateBase64Path: đường dẫn file chứa PFX mã hóa Base64, phù hợp Secret Files của Render.
- DataProtection:CertificatePassword: mật khẩu PFX.
- DataProtection:CertificateThumbprint: tùy chọn kiểm tra khớp file; nếu không cấu hình file, tiếp tục tìm trong certificate store như trước.

Chỉ dùng một loại đường dẫn cho mỗi chứng chỉ. App kiểm tra private key RSA khi khởi động, giữ khóa trong bộ nhớ và không ghi private key ra filesystem.

## Chuẩn bị một lần trên máy Windows

Chạy PowerShell dưới tài khoản của bạn (không cần Administrator với CurrentUser). Các lệnh dưới đây tạo chứng chỉ mới; không chạy lại mỗi lần deploy:

```powershell
$catbackCert = New-SelfSignedCertificate -Subject "CN=CatBack Data Protection" -CertStoreLocation "Cert:\CurrentUser\My" -KeyAlgorithm RSA -KeyLength 3072 -KeyUsage KeyEncipherment,DataEncipherment -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(5)
$catbackPassword = Read-Host "Mật khẩu PFX (lưu trong trình quản lý mật khẩu)" -AsSecureString
$catbackPfx = $catbackCert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $catbackPassword)
[Convert]::ToBase64String($catbackPfx) | Set-Clipboard
[Array]::Clear($catbackPfx, 0, $catbackPfx.Length)
```

Clipboard chứa Base64 của PFX đã mã hóa bằng mật khẩu. Lưu bản sao an toàn ngoài repository; xóa clipboard sau khi dán vào Secret Files. Không gửi PFX hoặc mật khẩu qua chat/log.

## Cấu hình Render

1. Service → Environment → Secret Files → Add Secret File.
2. Filename: catback-dataprotection.pfx.base64. Contents: dán nội dung clipboard.
3. Trong Secret File `appsettings.secrets.json`, gộp cấu hình sau vào nội dung hiện có (giữ nguyên database, Google và các secrets khác):

```json
{
  "DataProtection": {
    "CertificatePath": "",
    "CertificateBase64Path": "/etc/secrets/catback-dataprotection.pfx.base64",
    "CertificatePassword": "MAT_KHAU_PFX_CUA_BAN",
    "CertificateThumbprint": "",
    "PreviousCertificateThumbprints": "",
    "PreviousCertificates": []
  },
  "Authentication": {
    "Anonymous": {
      "Enabled": true
    }
  }
}
```

Các thiết lập trên đều đọc từ `appsettings.secrets.json`; không cần thêm biến môi trường. File `/etc/secrets/appsettings.secrets.json` có ưu tiên cao nhất, sau đó đến file secrets trong thư mục ứng dụng, rồi đến biến môi trường `CATBACK_`. Không cần đặt thumbprint khi dùng file; xóa thumbprint mẫu hoặc sửa cho khớp chứng chỉ nếu trước đó đã cấu hình. Chỉ bật Anonymous sau khi điền đúng đường dẫn và mật khẩu chứng chỉ.

Deploy phiên bản code có hỗ trợ này. Secret Files có tại /etc/secrets/<filename> khi chạy, không cần đưa vào Docker image. Đảm bảo migration tài khoản ẩn danh đã áp dụng. Không bật Development trên production.

Nguồn: https://render.com/docs/configure-environment-variables#secret-files

## Dữ liệu cũ và đổi chứng chỉ

Giữ nguyên database chứa Data Protection key ring và ApplicationName (mặc định CatsBack). Dữ liệu cũ chưa có envelope RSA vẫn giải mã qua key ring hiện có.

Khi thay chứng chỉ chính, giữ PFX cũ trong Secret Files và cấu hình:

Trong `DataProtection` của cùng file `appsettings.secrets.json`, đặt:

```json
"PreviousCertificates": [
  {
    "CertificateBase64Path": "/etc/secrets/catback-old.pfx.base64",
    "CertificatePassword": "MAT_KHAU_PFX_CU"
  }
]
```

Thêm phần tử vào mảng nếu có nhiều chứng chỉ cũ. PreviousCertificateThumbprints vẫn dùng được với chứng chỉ cũ trong certificate store, nhưng Render nên dùng PreviousCertificates với file. Cả Data Protection key ring và mã khôi phục đều sử dụng các chứng chỉ này để giải mã.

Không xóa khóa cũ khi còn dữ liệu cần giải mã. Mất private key có thể khiến mã khôi phục và key ring đã mã hóa không đọc được. Thay Secret Files hoặc biến môi trường cần redeploy; chứng chỉ được nạp khi khởi động.
