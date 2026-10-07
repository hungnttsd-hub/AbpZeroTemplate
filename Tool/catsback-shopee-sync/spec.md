# Settlement sync specification v0.7.10

## Boundary

- Shopee cookies, CSRF token và response thô chỉ tồn tại trong MAIN world của tab Shopee.
- Extension chuyển canonical settlement rows và log chẩn đoán theo allowlist sang `127.0.0.1:32145`.
- Local Helper tạo CSV, lưu cục bộ và chỉ upload CSV cùng Bearer token CatsBack.
- Không gửi item name, bank account, Shopee cookie hoặc toàn bộ billing response đến CatsBack.

### Diagnostic log

- Schema riêng `catsback-settlement-diagnostics-v1`, không phải JSON đối soát. Collector trả log cả khi thành công và khi lỗi; giữ tối đa 60 sự kiện cuối, phiên bản tool, thời gian, trạng thái và số request/retry.
- Chỉ chọn các mã ID, số dòng/trang và trường tiền cần chẩn đoán. Tiền gốc dùng scale 100000, tiền phân bổ dùng scale 10000. Thiếu/null/trống/không hợp lệ/số không an toàn được đánh dấu riêng, không đổi thành số 0 trong log.
- Extension giữ log lần gần nhất trong Chrome storage để tải từ popup. Helper nhận `POST /api/settlements/diagnostics` tối đa 128 KiB, kiểm tra schema và allowlist, ghi đè `logs/settlement-diagnostics-latest.json` qua file tạm và ghi thông báo vào `helper.log`. Không gửi log lên CatsBack. Helper không ghi được log không chặn kết quả collector.
- Mobile chỉ tạo link tải Blob log trong tab, không gửi log đến Helper. Log có thể tải ngay khi collector trả lỗi; đóng bảng hoặc chạy lần mới sẽ thu hồi Blob cũ. Không ghi raw response, URL, header, cookie, token, thông tin ngân hàng hay nội dung lỗi tự do của API.

### Android JSON export

- Tool dấu trang hoạt động trong tab Shopee, không dùng extension API hay Local Helper.
- Bản mobile v3 chỉ lưu loader ngắn trong URL dấu trang. Loader tải `mobile.js` từ website CatsBack
  qua HTTPS, không gửi referrer và không dùng eval. Tool báo lỗi khi tải thất bại/hết 15 giây;
  vẫn phụ thuộc chính sách của Shopee cho phép tải script khác miền. Cookie Shopee và dữ liệu bảng kê
  không được truyền trong request tải mã tool. Website cài đặt tạo URL asset theo chính tên miền/đường dẫn của nó.
- Dùng cùng collector/bộ bắt `billing_list` như extension. Người dùng đổi bộ lọc sau khi bật tool
  để trang Shopee phát response; không tự request danh sách hoặc tự quét các trang Billing.
- Chỉ tạo file JSON sau khi collector hoàn tất và mọi phép đối soát hợp lệ. JSON chỉ chứa
  `schemaVersion`, `exportedAt`, `validationCount` và `rows` canonical, không chứa response thô.
- `validationCount` khớp số cặp `(source_affiliate_id, validation_id)` có dòng trong JSON.
- JSON v2 yêu cầu đủ 25 trường canonical trên mỗi dòng, tất cả là chuỗi để giữ chính xác ID và tiền.
  File sai schema, trùng tên trường, thiếu trường hoặc sai kiểu dữ liệu bị từ chối trước khi lưu.
- Admin import JSON dùng cùng parser kiểm tra tổng tiền/trùng đơn và luồng staging của CSV;
  import không tự cộng ví. Các ô và luồng import CSV/TXT hiện có vẫn giữ nguyên.

## Request pacing

- Chỉ một luồng conversion/settlement được chạy tại một thời điểm.
- Các request chi tiết Shopee chạy tuần tự và nghỉ ngẫu nhiên 1,8–3,2 giây sau khi request trước hoàn tất.
- Request đọc dữ liệu gặp `408`, `425`, `429` hoặc `5xx` được retry tối đa ba lần bằng exponential backoff; `Retry-After` được ưu tiên nếu Shopee gửi về và `429` luôn chờ tối thiểu 30 giây.

## Collection and admin approval

Mọi bill có `validation_id` hợp lệ trong response `billing_list` đều được đưa vào báo cáo. Các mã trạng thái, `payout_id` và `payment_completed_time` được giữ nguyên để admin tham khảo; quyền quyết định duyệt không bị khóa theo trạng thái Shopee.

Bill có adjustment, clawback, bonus settlement, PPP hoặc cumulative payment vẫn được lưu và hiển thị cảnh báo. Quá trình tổng hợp vẫn fail closed nếu dòng thiếu `checkout_id`, cùng cặp `(checkout_id, order_sn)` bị lặp, tổng nguồn lệch quá `max(1 VND, 0.01%)`, hoặc số dòng vượt 10.000. Mỗi order bắt buộc có `order_sn`.

`validation_detail/v2` có thể trả nhiều dòng cùng `checkout_id` nhưng chứa các đơn khác nhau; mỗi dòng có `affiliate_net_commission` riêng. Giữ nguyên những dòng này để phân bổ, không bỏ trùng theo checkout hoặc gộp rồi dùng hoa hồng của một dòng. `total_count` được đối chiếu với số dòng nhận về, không phải số checkout duy nhất. Kiểm tra trùng áp dụng cho từng đơn, kể cả khi hai dòng chỉ giao nhau một phần danh sách đơn; lỗi chỉ rõ trang/dòng của hai lần xuất hiện. Dòng không có đơn vẫn bị chặn nếu lặp cùng checkout.

Bill đã có `payout_id` được đối chiếu thêm bằng GraphQL `payoutDetail`. Tool fail closed nếu `payout_id`, affiliate, validation, tổng sau phí dịch vụ (`billCommissionAmount`), tổng thuế và tổng thực nhận do Shopee trả về không cân bằng. Không suy ra thuế từ `payable_total_commission_amount = 0` của bill Pending.

## Mapping

- `validation_id` lấy từ `billing_list`, sau đó dùng làm query của `billing_detail`.
- Thanh toán được xác định bằng `payment_completed_time` hợp lệ > 0; 0/trống là chờ xử lý. Ưu tiên detail vừa lấy, fallback list khi detail không có trường. Không dùng mã `payment_status`/`validation_payout_status` hay các cờ điều chỉnh để suy ra đã trả. Lỗi request/ngày không hợp lệ phải dừng tổng hợp.
- Danh sách đơn lấy từ `validation_detail/v2` bằng khoảng `order_completed_period_start_time/end_time` của bill và đối chiếu `affiliate_id`.
- Mã đơn ưu tiên `order_sn`, fallback `order_id`.
- Hoa hồng checkout lấy từ `affiliate_net_commission` và phân bổ cho các order theo tổng `item_commission + capped_brand_commission`.
- Tổng authoritative của bill:
  - eligible: `eligible_total_commission_amount`
  - sau phí dịch vụ: `bill_commission_amount`
  - bill đã có `payout_id`, dù đã trả hay đang xử lý: thực nhận theo kỳ lấy từ `paymentPayout.totalPaymentAmount` của `payoutDetail`. Với `accountType` 3 (localIndividual) hoặc 5 (businessIndividual), tổng thuế khấu trừ lấy từ `whtTotalAmount` (PIT) + `vatTotalAmount`, không cộng thêm `taxTotalAmount`. Các loại tài khoản 1/2/4 giữ đối soát bằng `taxTotalAmount`; không mặc định VAT của doanh nghiệp là khoản khấu trừ.
  - bill đã thanh toán chưa có `payout_id` (luồng cũ): thực trả sau thuế lấy từ `payable_total_commission_amount`
  - bill chưa thanh toán và chưa có `payout_id`: chưa có thuế kỳ thanh toán để phân bổ, nên thuế bằng 0
- Tổng thuế được phân bổ deterministic cho các validation trong cùng `payout_id` theo `eligibleTotalCommissionAmount`, rồi phân bổ xuống order theo số tiền sau phí. Phí dịch vụ và thuế đều làm tròn 4 chữ số thập phân và giữ residual để tổng cuối cùng khớp tuyệt đối với Shopee.
- Không tính lại thuế bằng tỷ lệ cố định 5%/10%, không suy ra thuế bằng chênh lệch hai tổng. Thiếu/null/trống trường thuế được chọn hoặc tổng tiền của kỳ phải dừng; không đổi thành 0. Tổng PIT/VAT vẫn được ghi vào cột `allocated_tax` hiện có. Ngày thanh toán bằng 0 vẫn là đang xử lý dù đã có số tiền sau thuế.

### Nguồn mapping PIT/VAT, kiểm tra ngày 07-10-2026

- Query `PayoutDetailQuery` của trang Shopee VN yêu cầu `accountType`, `whtTotalAmount`, `vatTotalAmount`, `whtRate`, `vatRate` trong `paymentPayout`: [bundle API của Shopee, module 28510](https://deo.shopeemobile.com/shopee/shopee-affiliate-live-vn/static/js/9341.06f3237e.js).
- Giao diện VN hiển thị WHT/PIT và VAT riêng, trừ cả hai cho localIndividual/businessIndividual; VAT có cách cộng/trừ khác với tài khoản doanh nghiệp: [bundle chi tiết thanh toán](https://deo.shopeemobile.com/shopee/shopee-affiliate-live-vn/static/js/payout_record_detail.d1881075.js).
- Giá trị enum tài khoản 3/5 được xác minh từ module 40745: [bundle app](https://deo.shopeemobile.com/shopee/shopee-affiliate-live-vn/static/js/app.335c9e26.js). Đây là bằng chứng mapping giao diện/API, không phải giả định thuế suất pháp luật.
- Ảnh người dùng của kỳ `17351990636261005`: PIT 1.148đ + VAT 1.148đ; 22.745đ − 2.296đ = 20.449đ. Log 0.7.9 cho thấy `taxTotalAmount = 0`, nên trường tổng cũ không đủ cho kỳ này.

## Canonical CSV columns

`schema_version, source_affiliate_id, validation_id, payout_id, payment_completed_at_utc, order_completed_from_utc, order_completed_to_utc, payment_status, validation_payout_status, overall_validation_status, bill_validation_status, settlement_cycle, has_adjustment, has_clawback, is_cumulative, has_bonus, has_ppp, bill_eligible_commission, bill_after_service_fee, bill_paid_commission, order_id, order_eligible_commission, allocated_service_fee, allocated_tax, actual_paid_commission`

Một file có thể chứa nhiều validation. Phí dịch vụ tự cân bằng trong từng validation; thuế của bill Pending cân bằng ở cấp `payout_id` trước rồi mới được chia xuống validation và order.
