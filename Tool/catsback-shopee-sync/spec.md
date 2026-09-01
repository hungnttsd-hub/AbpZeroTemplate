# Settlement sync specification v0.7.3

## Boundary

- Shopee cookies, CSRF token và response thô chỉ tồn tại trong MAIN world của tab Shopee.
- Extension chỉ chuyển canonical settlement rows sang `127.0.0.1:32145`.
- Local Helper tạo CSV, lưu cục bộ và chỉ upload CSV cùng Bearer token CatsBack.
- Không gửi item name, bank account, Shopee cookie hoặc toàn bộ billing response đến CatsBack.

## Collection and admin approval

Mọi bill có `validation_id` hợp lệ trong response `billing_list` đều được đưa vào báo cáo. Các mã trạng thái, `payout_id` và `payment_completed_time` được giữ nguyên để admin tham khảo; quyền quyết định duyệt không bị khóa theo trạng thái Shopee.

Bill có adjustment, clawback, bonus settlement, PPP hoặc cumulative payment vẫn được lưu và hiển thị cảnh báo. Quá trình tổng hợp vẫn fail closed nếu trang conversion thiếu/trùng `checkout_id`, tổng nguồn lệch quá `max(1 VND, 0.01%)`, hoặc số checkout vượt 10.000. Mỗi order bắt buộc có `order_sn`.

## Mapping

- `validation_id` lấy từ `billing_list`, sau đó dùng làm query của `billing_detail`.
- Danh sách đơn lấy từ `validation_detail/v2` bằng khoảng `order_completed_period_start_time/end_time` của bill và đối chiếu `affiliate_id`.
- Mã đơn ưu tiên `order_sn`, fallback `order_id`.
- Hoa hồng checkout lấy từ `affiliate_net_commission` và phân bổ cho các order theo tổng `item_commission + capped_brand_commission`.
- Tổng authoritative của bill:
  - eligible: `eligible_total_commission_amount`
  - sau phí dịch vụ: `bill_commission_amount`
  - thực trả sau thuế: `payable_total_commission_amount` khi Shopee đã hoàn tất thanh toán; với bill Pending dùng `bill_commission_amount` làm giá trị đối soát và thuế bằng 0
- Phí dịch vụ và thuế được phân bổ riêng, theo tỷ trọng đơn và làm tròn 4 chữ số thập phân với residual deterministic.

## Canonical CSV columns

`schema_version, source_affiliate_id, validation_id, payout_id, payment_completed_at_utc, order_completed_from_utc, order_completed_to_utc, payment_status, validation_payout_status, overall_validation_status, bill_validation_status, settlement_cycle, has_adjustment, has_clawback, is_cumulative, has_bonus, has_ppp, bill_eligible_commission, bill_after_service_fee, bill_paid_commission, order_id, order_eligible_commission, allocated_service_fee, allocated_tax, actual_paid_commission`

Một file có thể chứa nhiều validation. Mỗi validation tự cân bằng các tổng của nó; không dùng tổng chung làm denominator.
