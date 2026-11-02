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

Nút **Duyệt hàng loạt theo bộ lọc** áp dụng cho cả đơn Shopee đã thanh toán và chưa thanh toán
phù hợp bộ lọc. Dùng giá trị tự tính đang hiển thị, cùng công thức với duyệt riêng;
không ghi đè số tiền thủ công hàng loạt. Đơn Shopee chưa thanh toán vẫn có thể duyệt riêng
và bật "Tự nhập hoa hồng, thuế và phí" để ghi đè.

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
Form duyệt thủ công đọc số bằng `InvariantCulture`, theo định dạng dấu chấm thập phân
của HTML `input type="number"`, không chấp nhận dấu phân tách hàng nghìn. Không dùng
model binding theo ngôn ngữ giao diện cho ba trường tiền/tỷ lệ, tránh `0.98` bị đọc
thành `98` khi giao diện tiếng Việt. Kiểm tra giới hạn tiền, tỷ lệ và tổng tỷ lệ
vẫn được thực hiện trong dịch vụ duyệt trước khi chốt tiền.

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
- Từ ngày đặt hàng / đến hết ngày: lọc `PurchaseTime` của conversion Shopee đã ghép,
  theo ngày Việt Nam (UTC+7). Ngày bắt đầu được tính từ 00:00; ngày kết thúc bao gồm
  cả ngày, bằng điều kiện nhỏ hơn 00:00 ngày kế tiếp. Cho phép chỉ nhập một mốc ngày;
  để trống cả hai sẽ không giới hạn ngày. Khi có lọc ngày, bản ghi chưa ghép được
  conversion có ngày đặt hàng sẽ bị loại khỏi kết quả. Không dùng ngày import hoặc ngày Shopee trả tiền.
  Ngày phải từ năm 1900 đến 9998 và từ ngày không được sau đến ngày; bộ lọc không hợp lệ
  hiển thị lỗi và không được dùng để duyệt hàng loạt.

Các bộ lọc kết hợp theo điều kiện AND và áp dụng ở cấp bản ghi trước phân trang.
Danh sách batch, chi tiết batch, số lượng và tổng tiền đều tính theo các dòng phù hợp.
Khoảng ngày được giữ khi chuyển batch, phân trang và gửi yêu cầu duyệt hàng loạt;
hộp xác nhận duyệt hiển thị lại khoảng ngày. Backend áp dụng cùng điều kiện ngày
cho danh sách, thống kê và chọn các bản ghi để duyệt trên mọi trang. Không cần migration.
Trạng thái nhãn batch vẫn mô tả trạng thái tổng thể của file nhập.
Duyệt hàng loạt xử lý các dòng phù hợp bộ lọc, trên tất cả các trang. Trong từng batch,
nút duyệt chỉ xử lý batch đó. Ở mục **Tất cả đơn đối soát**, nút **Duyệt hàng loạt theo
bộ lọc** xử lý mọi batch khớp bộ lọc; không giới hạn ở 20 batch hoặc 50 đơn đang hiển thị.
Chọn **Shopee đã thanh toán** để chỉ duyệt đơn đã thanh toán; chọn **Shopee chưa thanh toán**
để chỉ duyệt đơn chưa thanh toán. Xóa bộ lọc sẽ bao gồm cả hai nhóm.

Trang hiển thị số bản ghi chưa duyệt cần kiểm tra trên toàn bộ phạm vi đang lọc,
không tính `Approved`/`AlreadySettled`; nút bị vô hiệu hóa khi số này bằng 0.
Hộp xác nhận nêu rõ số bản ghi, phạm vi mọi trang, khả năng bao gồm đơn chưa thanh toán
và thao tác sẽ cộng ví. Backend chọn lại dữ liệu theo bộ lọc, bỏ bản ghi
`Approved`/`AlreadySettled`, ghép lại dữ liệu và dùng chung kiểm tra/phân bổ/chốt tiền
với duyệt theo batch. Đơn chưa thanh toán dùng thời gian duyệt làm thời gian chốt,
kể cả khi nguồn ghi `PaidAt` bằng UnixEpoch.
Các bản ghi trùng đơn giữa các batch trong lượt duyệt được phát hiện cùng nhau và
bỏ qua để kiểm tra; đơn thiếu dữ liệu nhận tiền cũng được bỏ qua.
Các phương thức cũ `ApproveAllAsync`/`ApproveAllPaidAsync` vẫn chỉ duyệt đơn đã thanh toán;
giao diện mới dùng `ApproveFilteredAsync`. Kết quả báo số đã duyệt/số bỏ qua; sau duyệt,
tải lại dữ liệu để đồng bộ bộ lọc và tổng tiền. Quyền `Affiliate.Admin.Orders` và
antiforgery token vẫn được kiểm tra. Không cần migration.
