# Chẩn đoán phản hồi OAuth TikTok

Sau khi triển khai bản này, log được ghi ở mức Information vào console và `Logs/logs.txt` theo cấu hình hiện có trong `Program.cs`. Không có chế độ ghi token thô.

## Các sự kiện

- `TikTok callback diagnostic` (41002): số lượng và độ dài state, có cookie hay không, độ dài cookie, state có khớp không và có code hay không. Không ghi giá trị state, cookie hoặc code. Sự kiện này nằm sau kiểm tra đăng nhập/quyền CatBack, trước kiểm tra state.
- `TikTok token diagnostic` (41001): giai đoạn `token_get` hoặc `token_refresh`, HTTP status, mã kết quả TikTok, request_id và JSON tóm tắt an toàn. Ghi ngay sau khi đọc phản hồi JSON, trước khi từ chối HTTP/platform error hoặc kiểm tra `user_type`/scope.

Tóm tắt gồm `user_type`, thời hạn token, token có xuất hiện không và độ dài, các tên scope Creator/Seller hợp lệ trong `granted_scopes`, cùng `response_shape` mô tả các trường và kiểu dữ liệu lồng nhau. Giới hạn độ sâu, số trường, số phần tử và tổng số nút để giữ log hữu hạn. Những scope không thuộc dạng Creator/Seller được che; khi thiếu dữ liệu hợp lệ, số là null và danh sách scope có thể rỗng. Đây là thông tin chẩn đoán, không phải kết luận cấp quyền thành công.

Giá trị access/refresh token được thay bằng `[REDACTED]`; mọi giá trị không thuộc danh sách metadata cho phép, bao gồm thông tin tài khoản và platform message, chỉ được biểu diễn bằng kiểu dữ liệu. Không ghi app secret, URL request, header, mã ủy quyền hoặc phản hồi nguyên bản. Cấu trúc lồng nhau có thể giúp phát hiện trường token bổ sung mà không lộ giá trị.

## Đọc kết quả

1. Bắt đầu từ nút kết nối thật trên CatBack trong cùng trình duyệt.
2. Tìm `TikTok callback diagnostic`. Nếu `state_matches=false`, callback bị từ chối và **chưa có request đổi token**; vì thế không có token response để nghiên cứu.
3. Nếu có `TikTok token diagnostic`, đối chiếu HTTP/code/request_id và `summary`. `user_type=1` là điều kiện Creator mà client hiện tại yêu cầu. Token khác loại vẫn được ghi metadata trước khi từ chối; không được dùng thay Creator token.
4. Việc có code hoặc có token response chưa chứng minh kết nối thành công: còn kiểm tra scope, thời hạn và hồ sơ nhà sáng tạo.

Bộ lọc callback vẫn loại bỏ log request thông thường. Chỉ hai sự kiện có đúng SourceContext và EventId/Name được cho qua; context đi kèm như đường dẫn callback, query, người dùng và ngoại lệ không được lưu trong các sự kiện này. Kiểm tra state và token type được giữ nguyên.

Chưa triển khai server, chưa gọi TikTok API và chưa chạy test trong lần bổ sung logging này.
