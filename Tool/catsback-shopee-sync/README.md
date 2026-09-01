# CatsBack Shopee Sync v0.7.4

Tool gồm Chrome extension và Local Helper chạy trên Windows/Node.js 18+.

## Đối soát thanh toán

Popup có hai thao tác:

- **Tổng hợp file**: mở tab `https://affiliate.shopee.vn/payment/billing` ở nền, lấy toàn bộ bảng kê trong response danh sách hiện tại, tổng hợp một CSV chuẩn và lưu vào `Downloads\\CatsBackSettlements`.
- **Import đối soát**: thực hiện cùng bước tổng hợp, lưu CSV cục bộ rồi upload đến CatsBack. Dữ liệu chỉ được đưa vào hàng chờ duyệt của admin; helper không cộng ví.

Extension không mở trang chi tiết của từng `validation_id`. Extension mở/reload trang Billing và bắt response `billing_list` do chính trang Shopee phát ra; nó không tự tạo thêm request `billing_list`. Các API chi tiết vẫn được gọi trong MAIN world của đúng tab billing để dùng phiên đăng nhập hiện tại:

1. Bắt response `GET /api/v3/payment/billing_list` của trang Billing
2. `GET /api/v3/payment/billing_detail?validation_id=...`
3. `GET /api/v3/report/validation_detail/v2?...`

Các request chi tiết luôn chạy tuần tự và nghỉ ngẫu nhiên 1,8–3,2 giây giữa hai lần gọi. Tool chỉ cho phép một phiên đồng bộ chạy tại một thời điểm; khi gặp HTTP `429`, `408`, `425` hoặc `5xx`, request GET được thử lại tối đa ba lần với exponential backoff và tôn trọng header `Retry-After` của Shopee. Riêng `429` luôn chờ tối thiểu 30 giây trước lần thử tiếp theo.

Mọi `validation_id` trong response `billing_list` đều được tổng hợp, không lọc theo trạng thái thanh toán. CSV giữ nguyên `payment_status`, `validation_payout_status`, trạng thái validation và các cờ điều chỉnh. Website hiển thị trạng thái Shopee để admin tự quyết định duyệt cộng ví; trạng thái nhà cung cấp không tự động ẩn nút duyệt.

CSV dùng schema `catsback-settlement-v2`. `validation_id`, `payout_id` và mã đơn luôn được giữ dưới dạng chuỗi. Website vẫn đọc được file v1 cũ.

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
