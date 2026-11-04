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
$baseUrl = "https://catback.id.vn"
$credential = @{
  client_id = $env:CATBACK_CLIENT_ID
  client_secret = $env:CATBACK_CLIENT_SECRET
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

## Xử lý báo cáo lớn

API và màn hình quản trị dùng chung luồng import theo lô 100 đơn. Mỗi lô tính lại tỷ lệ
hoàn tiền và ghép đối soát một lần, sau đó commit cùng giao dịch; không lưu lô khi hai
bước này chưa hoàn tất. Nếu lô lỗi, hệ thống rollback và thử từng đơn trong giao dịch
riêng để vẫn nhập được các đơn hợp lệ.

Đơn đã duyệt (`Status = Settled` hoặc có `SettledAt`) được lọc trước khi upsert và
bỏ qua hoàn toàn: không ghi lại conversion, item, attribution, số tiền hay trạng thái
conflict. Kết quả trả thêm `skippedSettledCount`, tính theo số đơn đã duyệt bị bỏ qua;
không cộng các đơn này vào `updatedCount`, `unmatchedCount` hoặc `errorCount`.
`importedRowCount` vẫn là tổng số dòng hợp lệ đọc từ file, `conversionCount` vẫn là
tổng số đơn phân nhóm trong file, gồm cả các đơn bỏ qua.

Log hoàn tất import ghi số dòng, thêm/cập nhật/bỏ qua/lỗi và tổng thời gian xử lý
để theo dõi hiệu năng trên báo cáo thực tế.
