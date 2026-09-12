# IMPLEMENTATION_CHECKLIST.md

## A. Setup
- [ ] Đọc toàn bộ package.
- [ ] Kiểm tra component/layout orders page hiện tại.
- [ ] Xác định asset logo / mascot CatBack đang dùng.

## B. Layout
- [ ] Tạo / cập nhật sidebar theo template.
- [ ] Tạo / cập nhật topbar.
- [ ] Tạo hero header cho page Đơn hàng.
- [ ] Tạo 3 KPI cards.
- [ ] Tạo filter tab row.
- [ ] Tạo order card reusable component.

## C. Visual style
- [ ] Áp design tokens.
- [ ] Active item có cyan glow.
- [ ] Card trắng có border + shadow nhẹ.
- [ ] CTA outline / pill đúng style.
- [ ] Background xanh nhạt, sạch, không bị đục.

## D. Order card
- [ ] Thumbnail trái.
- [ ] Tên sản phẩm rõ ràng, 2 dòng max.
- [ ] Nguồn Shopee + thời gian.
- [ ] Status pill vàng nhạt.
- [ ] Số tiền đơn hàng.
- [ ] Hoàn tiền dự kiến.
- [ ] Mã đơn hàng + icon copy.
- [ ] Nút `Xem chi tiết`.
- [ ] Hàng recipient / token warning.

## E. QA visual
- [ ] So pixel-compare thủ công với `01_final_order_template.png`.
- [ ] Check khoảng cách giữa các block.
- [ ] Check độ tương phản text.
- [ ] Check bo góc, shadow, gloss overlay.
- [ ] Check web ở wide desktop.
- [ ] Check responsive tablet.

## F. Không được làm
- [ ] Không thay nhận diện CatBack.
- [ ] Không biến page thành table khô cứng.
- [ ] Không dùng màu tối quá ở content area.
- [ ] Không bỏ hiệu ứng gloss / glow chủ đạo.
