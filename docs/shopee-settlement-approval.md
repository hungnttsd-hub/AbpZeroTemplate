# Đối soát và duyệt Shopee

## Tự ghép lại dữ liệu

`ShopeeSettlementMatcher` được gọi sau khi import/cập nhật conversion Shopee, khi import lại
bảng kê trùng, khi mở trang đối soát và ngay trước khi duyệt. Các dòng lỗi cũ được kiểm tra lại
bằng mã đơn, conversion Shopee và attribution của item. Trường hợp import bảng kê trước báo cáo
đơn hàng không còn giữ mãi trạng thái "Không tìm thấy đơn hàng".

Matcher chỉ cập nhật liên kết, trạng thái và tổng batch khi có thay đổi; không duyệt, không cộng ví.
Đơn đã duyệt/đã đối soát không bị ghép lại hoặc trừ tiền lần nữa. Trùng mã đơn hoặc thiếu conversion
sẽ xóa liên kết cũ để không thể duyệt nhầm theo liên kết đã lỗi thời.

## Quyền duyệt của admin

Admin có thể duyệt đơn chưa hoàn thành, gồm cả trạng thái chưa thanh toán, đang xử lý, hủy,
hoàn tiền và từ chối. Vẫn kiểm tra đơn/conversion hợp lệ, người nhận có attribution khớp,
tổng tiền và chống duyệt trùng. Trạng thái trước khi duyệt được lưu trong
`ExtraProperties.OrderStatusAtApproval`. Đơn đã chốt không bị import sau ghi đè trạng thái/số tiền.

Duyệt hàng loạt vẫn chỉ áp dụng cho bảng kê **Shopee đã thanh toán**. Đơn Shopee chưa thanh toán
có thể duyệt riêng với giá trị tự tính, hoặc bật "Tự nhập hoa hồng, thuế và phí" để ghi đè.

## Thuế/phí mặc định

Với bảng kê canonical có hoa hồng gốc authoritative, tính riêng từng khoản còn thiếu:

- Thuế kỳ và thuế của dòng đều bằng 0: áp 10% × hoa hồng gốc.
- Phí kỳ và phí của dòng đều bằng 0: áp 0,98% × hoa hồng gốc.
- Khoản Shopee đã trừ: giữ nguyên. Dòng có khoản phân bổ bằng 0 do làm tròn trong kỳ đã có
  khoản khấu trừ không bị tính thêm mặc định.
- Làm tròn từng khoản đến 4 chữ số, thuế không vượt phần còn lại sau phí.
- Thực nhận = hoa hồng gốc − phí − thuế; phân bổ cho người dùng bằng logic hiện có.

CSV cũ chỉ ghi số thực trả sau khấu trừ không có đủ căn cứ suy ra hoa hồng gốc, nên giữ số thực trả.
Luồng sửa số tiền thủ công dùng cùng công thức và vẫn cho phép nhập 0% rõ ràng.

Hiển thị, số tiền xem trước và duyệt dùng chung `ShopeeSettlementAmounts`. Số tiền gốc từ file
được giữ đến khi duyệt; khi duyệt lưu các trường `Original*`, tỷ lệ mặc định hoặc tỷ lệ nhập tay
vào ExtraProperties. Bản ghi đã duyệt luôn dùng số tiền đã chốt.

Không cần migration. Sau triển khai, mở trang đối soát sẽ tự kiểm tra lại các dòng lỗi cũ;
chỉ thao tác duyệt của admin mới chốt số tiền và cộng ví.

## Bộ lọc đối soát

- Trạng thái duyệt: tất cả, chưa duyệt, đã duyệt. Đã duyệt là bản ghi `Approved`;
  chưa duyệt gồm các bản ghi còn lại, kể cả bản ghi lỗi cần kiểm tra.
- Thanh toán Shopee: tất cả, đã thanh toán, chưa thanh toán; dùng cùng điều kiện
  `PaidAt > UnixEpoch` như nhãn thanh toán trên mỗi dòng.
- Affiliate ID: khớp chính xác `SourceAffiliateId` của bảng kê, bỏ khoảng trắng hai đầu.

Các bộ lọc kết hợp theo điều kiện AND và áp dụng ở cấp bản ghi trước phân trang.
Danh sách batch, chi tiết batch, số lượng và tổng tiền đều tính theo các dòng phù hợp.
Trạng thái nhãn batch vẫn mô tả trạng thái tổng thể của file nhập.
Duyệt hàng loạt chỉ xử lý các dòng Shopee đã thanh toán của batch phù hợp bộ lọc,
trên tất cả các trang. Sau duyệt, tải lại dữ liệu để đồng bộ bộ lọc và tổng tiền.
