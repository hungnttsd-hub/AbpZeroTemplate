# QR chuyển khoản cho admin

Trang `/Admin/Payouts` thêm QR vào thẻ yêu cầu Pending đủ bảo chứng, cùng điều kiện hiển thị nội dung chuyển khoản trước đây. Quyền truy cập vẫn là quyền Admin.Payouts của trang.

`Web/Payments/VietQrService` chỉ tạo URL ảnh từ snapshot ngân hàng/số tài khoản trên yêu cầu, `NetAmount` và nội dung `CB {RequestCode}`. Không ghi DB, không gọi MarkPaid, không tự chuyển tiền. Mô hình ngân hàng, số dư, reserve, chứng từ và quy trình thanh toán không thay đổi. Nội dung trên block QR và nút sao chép khớp nhau; helper nội dung thông báo hiện có không bị sửa.

Ảnh tải trực tiếp từ VietQR.io, vì vậy nhà cung cấp nhận BIN ngân hàng, số tài khoản, số tiền và nội dung khi admin mở trang. Không gửi tên người nhận, token hoặc cookie ứng dụng; ảnh đặt `referrerpolicy=no-referrer`. Service không gọi mạng phía server, JS không retry liên tục; URL ổn định khi dữ liệu không đổi. Không lưu ảnh vào DB. Lỗi ảnh hoặc timeout 20 giây chỉ ảnh hưởng block QR.

Mapping 20 ngân hàng hiện tại đã đối chiếu https://api.vietqr.io/v2/banks ngày 10/09/2026; mã nội bộ CTG tương ứng ICB, AGR tương ứng VBA. Không tự suy đoán BIN khi ngân hàng không nằm trong mapping.

Quick Link: https://vietqr.io/danh-sach-api/link-tao-ma-nhanh/

Mặc định template `qr_only`. VietQR.io hướng dẫn tạo template riêng khi dùng cho dự án chính thức; cấu hình tên template được cấp qua `VietQr:Template` (biến môi trường `VietQr__Template`). Nếu reverse proxy có CSP, cần cho phép ảnh từ `https://img.vietqr.io`.

Kiểm tra đầu vào: số tài khoản 1–19 ký tự chữ/số theo Quick Link, số tiền nguyên dương tối đa 13 chữ số, nội dung tối đa 50 chữ/số/khoảng trắng. Không làm tròn hoặc cắt nội dung để ép tạo QR. Ảnh rộng tối đa 260px và co theo màn hình.

Chưa kiểm thử trình duyệt hoặc quét bằng ứng dụng ngân hàng. Khi nghiệm thu, đối chiếu ngân hàng/STK/số tiền/nội dung trên app ngân hàng trước khi chuyển; kiểm tra tải ảnh lỗi, thiếu mapping, mobile và việc ẩn block sau Paid/Rejected.
