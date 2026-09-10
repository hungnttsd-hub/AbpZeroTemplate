# CatsBack Shopee Sync v0.7.7

Tool gồm Chrome extension và Local Helper chạy trên Windows/Node.js 18+.

## Cập nhật trạng thái thanh toán v0.7.7

- Dùng `payment_completed_time`: ngày hợp lệ lớn hơn 0 là **Đã thanh toán**, 0 hoặc trống là **Chờ xử lý**. Ưu tiên dữ liệu `billing_detail` vừa lấy; chỉ fallback sang `billing_list` khi detail thiếu trường. Không quét thêm trang `payout_record`.
- Giữ nguyên mã trạng thái thô để tra cứu; mã 8 có thể xuất hiện ở cả kỳ đã trả và chưa trả. Adjustment/clawback và quyền duyệt admin độc lập với trạng thái thanh toán.
- Thuế vẫn đối chiếu qua `payoutDetail` khi có `payout_id`, kể cả đã thanh toán; không suy ra thuế từ số payable bằng 0 của bảng kê ngày. Lỗi API/ngày không hợp lệ dừng tổng hợp, không upload báo cáo một phần.
- Reload extension tại `chrome://extensions` và cập nhật backend/web cùng bản này. Các dòng đã lưu ngày thanh toán sẽ đổi nhãn Shopee ngay khi web được cập nhật. Với bản ghi cũ còn ở trạng thái hàng chờ, import lại CSV mới nhất để phân loại lại; import không cộng ví. File cũ thiếu ngày không được ghi đè bảng kê đã thanh toán.

## Đối soát thanh toán

Popup có hai thao tác:

- **Tổng hợp file**: mở tab `https://affiliate.shopee.vn/payment/billing` ở nền, lấy toàn bộ bảng kê trong response danh sách hiện tại, tổng hợp một CSV chuẩn và lưu vào `Downloads\\CatsBackSettlements`.
- **Import đối soát**: thực hiện cùng bước tổng hợp, lưu CSV cục bộ rồi upload đến CatsBack. Dữ liệu chỉ được đưa vào hàng chờ duyệt của admin; helper không cộng ví.

Extension không mở trang chi tiết của từng `validation_id`. Extension mở/reload trang Billing và bắt response `billing_list` do chính trang Shopee phát ra; nó không tự tạo thêm request `billing_list`. Các API chi tiết vẫn được gọi trong MAIN world của đúng tab billing để dùng phiên đăng nhập hiện tại:

1. Bắt response `GET /api/v3/payment/billing_list` của trang Billing
2. `GET /api/v3/payment/billing_detail?validation_id=...`
3. Với bảng kê đã có `payout_id`: `POST /api/v3/gql?q=payoutDetail` để lấy tổng thuế và thực nhận của kỳ thanh toán
4. `GET /api/v3/report/validation_detail/v2?...`

Các request chi tiết luôn chạy tuần tự và nghỉ ngẫu nhiên 1,8–3,2 giây giữa hai lần gọi. Tool chỉ cho phép một phiên đồng bộ chạy tại một thời điểm; khi gặp HTTP `429`, `408`, `425` hoặc `5xx`, request đọc dữ liệu được thử lại tối đa ba lần với exponential backoff và tôn trọng header `Retry-After` của Shopee. Riêng `429` luôn chờ tối thiểu 30 giây trước lần thử tiếp theo.

Mọi `validation_id` trong response `billing_list` đều được tổng hợp, không lọc theo trạng thái thanh toán. CSV giữ nguyên `payment_status`, `validation_payout_status`, trạng thái validation và các cờ điều chỉnh. Website hiển thị trạng thái Shopee để admin tự quyết định duyệt cộng ví; trạng thái nhà cung cấp không tự động ẩn nút duyệt.

Khi Shopee đã sinh `payout_id`, số thuế lấy từ `taxTotalAmount` của kỳ thanh toán kể cả lúc bảng kê còn Pending và `payable_total_commission_amount` của từng bill vẫn bằng 0. Tool phân bổ tổng thuế cho toàn bộ validation trong cùng payout trước, sau đó mới phân bổ xuống từng đơn. Vì vậy tổng `allocated_tax` và `actual_paid_commission` khớp đúng số sau thuế Shopee hiển thị, không bị lệch do làm tròn riêng từng bảng kê. Bảng kê chưa có `payout_id` chưa có số thuế authoritative nên tiếp tục để thuế bằng 0.

CSV dùng schema `catsback-settlement-v2`. `validation_id`, `payout_id` và mã đơn luôn được giữ dưới dạng chuỗi. Website vẫn đọc được file v1 cũ.

## Cài Local Helper

1. Cài Node.js 18 trở lên.
2. Mở `local-helper` và chạy `run.cmd`.
3. Mở `http://127.0.0.1:32145/settings` hoặc `open-settings.cmd`.
4. Nhập CatsBack API Base URL, Client ID và Client Secret mới.

`run.cmd` là entrypoint dùng cho cả cài đặt và cập nhật. Mỗi lần chạy, tool sẽ kiểm tra Node.js/code hiện tại, tự cài hoặc sửa Scheduled Task chạy cùng Windows, dừng helper cũ, khởi động lại bằng code trong đúng thư mục hiện tại và xác nhận endpoint `/health`. Nếu Scheduled Task cũ trỏ sang một thư mục helper khác, `config.json` và `state.json` sẽ được mang sang thư mục hiện tại khi các file này chưa tồn tại, nên không mất credentials hoặc trạng thái đã xử lý. Có thể dùng `run.cmd --no-pause` khi gọi từ terminal.

`start-helper.cmd` chỉ khởi động trực tiếp ở cửa sổ hiện tại và không cài chạy cùng Windows. `reset-local-helper.cmd` được giữ lại như alias tương thích cho `run.cmd`.

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
