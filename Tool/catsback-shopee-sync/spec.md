# Settlement sync specification v0.7.1

## Boundary

- Shopee cookies, CSRF token và response thô chỉ tồn tại trong MAIN world của tab Shopee.
- Extension chỉ chuyển canonical settlement rows sang `127.0.0.1:32145`.
- Local Helper tạo CSV, lưu cục bộ và chỉ upload CSV cùng Bearer token CatsBack.
- Không gửi item name, bank account, Shopee cookie hoặc toàn bộ billing response đến CatsBack.

## Paid gate

Một bill được đưa vào báo cáo khi:

- `payment_status === 4`
- `validation_payout_status === 2`
- `payout_id` khác rỗng
- `payment_completed_time > 0`

Fail closed nếu bill có adjustment, clawback, bonus settlement, PPP (`bill_ppp_amount` hoặc `ppp_settlement_list`), cumulative payment, trang conversion thiếu/trùng `checkout_id`, tổng nguồn lệch quá `max(1 VND, 0.01%)`, hoặc số checkout vượt 10.000. Mỗi order bắt buộc có `order_sn`.

## Mapping

- `validation_id` lấy từ `billing_list`, sau đó dùng làm query của `billing_detail`.
- Danh sách đơn lấy từ `validation_detail/v2` bằng khoảng `order_completed_period_start_time/end_time` của bill và đối chiếu `affiliate_id`.
- Mã đơn ưu tiên `order_sn`, fallback `order_id`.
- Hoa hồng checkout lấy từ `affiliate_net_commission` và phân bổ cho các order theo tổng `item_commission + capped_brand_commission`.
- Tổng authoritative của bill:
  - eligible: `eligible_total_commission_amount`
  - sau phí dịch vụ: `bill_commission_amount`
  - thực trả sau thuế: `payable_total_commission_amount`
- Phí dịch vụ và thuế được phân bổ riêng, theo tỷ trọng đơn và làm tròn 4 chữ số thập phân với residual deterministic.

## Canonical CSV columns

`schema_version, source_affiliate_id, validation_id, payout_id, payment_completed_at_utc, order_completed_from_utc, order_completed_to_utc, payment_status, validation_payout_status, has_adjustment, has_clawback, is_cumulative, bill_eligible_commission, bill_after_service_fee, bill_paid_commission, order_id, order_eligible_commission, allocated_service_fee, allocated_tax, actual_paid_commission`

Một file có thể chứa nhiều validation. Mỗi validation tự cân bằng các tổng của nó; không dùng tổng chung làm denominator.
