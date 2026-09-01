# CatsBack Shopee Sync v0.7.1

Tool gồm Chrome extension và Local Helper chạy trên Windows/Node.js 18+.

## Đối soát thanh toán

Popup có hai thao tác:

- **Tổng hợp file**: mở tab `https://affiliate.shopee.vn/payment/billing` ở nền, lấy các bảng kê đã thanh toán, tổng hợp một CSV chuẩn và lưu vào `Downloads\\CatsBackSettlements`.
- **Import đối soát**: thực hiện cùng bước tổng hợp, lưu CSV cục bộ rồi upload đến CatsBack. Dữ liệu chỉ được đưa vào hàng chờ duyệt của admin; helper không cộng ví.

Extension không mở trang chi tiết của từng `validation_id`. API Shopee được gọi trong MAIN world của đúng tab billing để dùng phiên đăng nhập hiện tại:

1. `GET /api/v3/payment/billing_list`
2. `GET /api/v3/payment/billing_detail?validation_id=...`
3. `GET /api/v3/report/validation_detail/v2?...`

Chỉ bảng kê thỏa đồng thời `payment_status=4`, `validation_payout_status=2`, có `payout_id` và `payment_completed_time` mới được lấy. Bảng kê có điều chỉnh, truy thu, bonus settlement, PPP hoặc thanh toán cộng dồn bị chặn. Tool phân bổ phí dịch vụ và thuế độc lập trong từng `validation_id`, không gộp chéo bảng kê.

CSV dùng schema `catsback-settlement-v1`. `validation_id`, `payout_id` và mã đơn luôn được giữ dưới dạng chuỗi.

## Cài Local Helper

1. Cài Node.js 18 trở lên.
2. Mở `local-helper` và chạy `start-helper.cmd`.
3. Mở `http://127.0.0.1:32145/settings` hoặc `open-settings.cmd`.
4. Nhập CatsBack API Base URL, Client ID và Client Secret mới.

Không dùng lại Client Secret từng được đóng gói trong bản cũ. Bản phát hành này không chứa `config.json`, `state.json` hay log. Lần chạy đầu helper tự tạo `config.json` từ `config.example.json` trống credentials.

Endpoint cục bộ:

- `GET /health`
- `GET|POST /api/settings`
- `POST /api/test-connection`
- `POST /api/settlements/export`
- `POST /api/settlements/import`

Endpoint CatsBack mặc định cho đối soát:

`POST /api/public/shopee-automation/settlements/import`

## Cài Chrome extension

1. Mở `chrome://extensions`.
2. Bật Developer mode.
3. Chọn Load unpacked và trỏ đến thư mục `extension`.
4. Đăng nhập `affiliate.shopee.vn`, sau đó dùng hai nút đối soát trong popup.

Luồng Conversion Report cũ vẫn được giữ trong mục mở rộng ở cuối popup.
