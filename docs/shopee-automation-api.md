# API tự động import báo cáo Shopee

API dùng tài khoản máy riêng, không sử dụng cookie hoặc tài khoản Admin. Tool lấy Bearer token ngắn hạn bằng `ClientId` và `ClientSecret`, sau đó dùng token để upload báo cáo CSV/TXT.

## Cấu hình production

Thêm các Environment Variables trên Render:

```text
ShopeeAutomation__Enabled=true
ShopeeAutomation__ClientId=<client-id-tu-tao>
ShopeeAutomation__ClientSecret=<secret-dai-ngau-nhien>
ShopeeAutomation__TokenLifetimeMinutes=30
ShopeeAutomation__MaxReportSizeMb=10
```

Nên tạo `ClientSecret` ngẫu nhiên tối thiểu 32 byte và không lưu trong source code. Đổi secret sẽ làm các token cũ mất hiệu lực ngay.

## 1. Lấy access token

```http
POST /api/public/shopee-automation/token
Content-Type: application/json

{
  "client_id": "your-client-id",
  "client_secret": "your-client-secret"
}
```

Kết quả:

```json
{
  "access_token": "...",
  "token_type": "Bearer",
  "expires_in": 1800,
  "expires_at_utc": "2026-08-27T10:30:00+00:00"
}
```

Endpoint token giới hạn 10 request/phút/IP.

## 2. Import báo cáo

```http
POST /api/public/shopee-automation/reports/import
Authorization: Bearer <access_token>
Content-Type: multipart/form-data

report=<file CSV hoặc TXT>
```

Ví dụ PowerShell:

```powershell
$baseUrl = "https://catsback.onrender.com"
$credential = @{
  client_id = $env:CATSBACK_CLIENT_ID
  client_secret = $env:CATSBACK_CLIENT_SECRET
} | ConvertTo-Json

$token = Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/public/shopee-automation/token" `
  -ContentType "application/json" `
  -Body $credential

curl.exe -X POST "$baseUrl/api/public/shopee-automation/reports/import" `
  -H "Authorization: Bearer $($token.access_token)" `
  -F "report=@C:\Reports\AffiliateCommissionReport.csv"
```

Token chỉ dùng được cho API import Shopee, không có quyền Admin và không đăng nhập được vào giao diện quản trị.
