# 01 — Đặc tả hình ảnh Hide & Seek

## 1. Ý đồ chính

Màn chơi là khu rừng khám phá chứ không phải bốn thẻ trắc nghiệm đặt cạnh nhau. Từ mục tiêu chỉ xuất hiện bằng chữ/giọng đọc. Bé phải tự tìm hiểu thứ đang nấp, rồi quyết định đối tượng vừa thấy có đúng với tên được yêu cầu không.

Giữ phong cách ảnh đã chốt: cây thân lớn, lá xanh, ánh sáng ban ngày, bảng gỗ kem, nét bo tròn, Momo rồng xanh thân thiện. Tránh tối, gây sợ hoặc quá nhiều chữ. Các ảnh trong `images/details` là reference cụ thể cho từng phần, không phải danh mục ảnh stock có thể thay tùy ý.

## 2. Mục lục ảnh cần xem

| Nhóm | File | Dùng để làm gì |
|---|---|---|
| HUD | D01–D04 | Tiêu đề, logo, sao, pause |
| Nhân vật | D05, D35 | Momo hướng dẫn và ăn mừng |
| Chỗ ẩn | D06–D10 | Hốc cây, bụi lá, đá, cỏ, thân gỗ |
| Công cụ | D11–D14 | Thanh công cụ và ba icon |
| Quiz | D15–D18, D36 | Khung panel, portrait vừa lộ, YES/NO, sửa câu hỏi |
| Màn bắt đầu | D20 | Reference không lộ đối tượng; nhớ sửa số sao thành 3 |
| Vạch lá | D21–D22 | Trước/sau mở lá |
| Tung lưới | D23–D24 | Bay lưới, nâng lá |
| Mở đá | D25–D26 | Quả mọng phép thuật, đá tách |
| Feedback | D27–D30 | Sai/đúng/hoàn thành/luồng chơi — phải áp dụng sửa lỗi spec |
| Đối tượng | D31–D34 | Chó, kéo, mèo, hổ sau khi đã hé lộ |

A00 đánh dấu vị trí trên ảnh tổng thể. A01–A15 có quy tắc ở ngay bên cạnh mỗi vùng hình. `image-map.json` lưu chính xác ảnh nguồn, tọa độ crop và kích thước gốc.

## 3. Bố cục desktop

Canvas logic: **1600×900**. Giá trị dưới đây là layout đề xuất dùng khi implement, không phải tuyên bố ảnh AI đã có chính xác các kích thước này.

| Thành phần | X | Y | W | H |
|---|---:|---:|---:|---:|
| Banner nhiệm vụ | 470 | 14 | 660 | 118 |
| Bộ đếm sao | 1210 | 24 | 260 | 98 |
| Pause | 1494 | 22 | 80 | 80 |
| Momo | 28 | 260 | 325 | 330 |
| Thanh công cụ | 486 | 722 | 640 | 156 |
| Panel xác nhận | 1060 | 160 | 510 | 316 |

Vùng còn lại dành cho bốn đến sáu chỗ ẩn nấp. Các hitbox không chồng HUD; không yêu cầu bé chạm một chi tiết lá nhỏ. Quy tắc tỉ lệ, tọa độ demo và layout mobile nằm trong `layout.json` và level JSON.

Khi mở quiz, có thể phủ dim nhẹ khu rừng và giữ đối tượng vừa hé lộ sáng rõ trong portrait. Việc đối tượng đang thấy là chó không được truyền bằng chữ cho tới sau lần trả lời đầu tiên. Không để popup che cả nhiệm vụ Find a cat.

## 4. Trạng thái ảnh quan trọng

### SEARCH — lúc bắt đầu

Tất cả chỗ ẩn đóng. Không hiện ảnh mèo ở header. Không hiện mặt chó, tai hổ, kéo, silhouette hay mắt ló ra. Không có màu/hướng nhìn Momo gợi ý vị trí đúng. Mỗi chỗ có thể lắc rất nhẹ theo cùng một logic, độc lập với entity bên trong.

Ảnh gốc chính cố tình cho thấy nhiều đối tượng để giới thiệu concept. Nó không đáp ứng trạng thái SEARCH và không được đặt trực tiếp làm backdrop runtime. D20 gần đúng hơn cho bố cục đóng; các sao ở D20 vẫn cần sửa.

### REVEALING — đang thao tác

Chỉ một chỗ tương tác ở một thời điểm. Cover chia thành layer hoặc frame trước/sau. Không để object lộ qua một lỗ mask nhỏ quá sớm. Sau thời điểm kết thúc reveal, object hiện đầy đủ trong vùng an toàn; tool VFX đã tan hoặc rời đi.

### ASKING — vừa thấy đối tượng

Mỗi object có portrait cùng kích thước và cùng độ nổi bật; không thưởng sparkle riêng vì đó là mèo. Header panel là `Is this a cat?`. Có YES và NO cân bằng. Gỡ X khỏi panel đang chờ trả lời để không bypass bài kiểm tra.

### FEEDBACK

Nếu đúng NO trên chó: hiện `No. It is a dog.`; Momo khích lệ nhẹ, không animation qua màn. Nếu đúng YES trên mèo: hiện `Yes! It is a cat.` và chuyển success sau feedback. Nếu sai: một sao chuyển trạng thái, không hiện chữ WRONG lớn hoặc nhấp nháy mạnh.

### COMPLETED

Momo và mèo xuất hiện cạnh nhau trong vùng chính. Hiện số sao còn lại thật. Nút tiếp tục và chơi lại nằm trong safe area. Không tự chuyển màn ngay trước khi bé nhìn thấy lời kết.

## 5. Màu, chữ và button

Dùng `design-tokens.json` làm điểm khởi đầu: chữ xanh đậm, nền kem, viền xanh trời, vàng sao, gỗ nâu. Tái sử dụng font bo tròn dự án đang có quyền dùng. Không yêu cầu tải file font trong gói này.

Quest 42 design px desktop; câu hỏi 34; YES/NO 32; nhãn tool 22. Khi qua mobile, reflow và đo lại CSS size; không áp dụng scale một cách máy móc.

Không biến màu thành đáp án: nút NO có thể đỏ theo concept nhưng không làm nó mờ, nhỏ hoặc mang ý nghĩa “sai”. Hai nút đều bấm được và có label rõ. Tại level tìm mèo, NO thường là lựa chọn đúng với vật nhiễu.

## 6. Lớp render

| Depth đề xuất | Lớp |
|---:|---|
| 0 | Background không có entity ẩn |
| 20 | Object/animal — invisible khi closed |
| 30 | Cover kín |
| 40 | Lá/lưới/đá/VFX ngắn |
| 50 | Momo và reaction |
| 100 | HUD và tool tray |
| 200/210 | Quiz scrim và quiz panel |
| 300/310 | Pause scrim và pause panel |

Depth không thay input lock. Dù overlay nằm trên, phải chủ động tắt world input. Một mask hiển thị không đồng nghĩa entity không còn hitbox; quản lý visibility và interactive state riêng.

## 7. Xuất asset thật

Mỗi chỗ ẩn cần cover closed/open độc lập; nhân vật và object cần nền trong, không chữ. Background clean không chứa chó, hổ, mèo hay kéo. Các label tạo bằng Text/UI runtime để đổi ngôn ngữ và giữ nét.

Có **32 nhóm asset runtime** trong checklist; một nhóm có thể cần nhiều frame. Cột `runtimeFileIncluded: false` là chủ ý: các file hiện có là ảnh tham chiếu, không giả làm alpha sprite. Các object apple/orange/ball/rabbit ở demo mở rộng chưa có minh họa riêng trong hai ảnh nguồn; Codex ưu tiên reuse thư viện từ vựng của game.

Kiểm tra alpha bằng nền sáng và nền tối. Chừa padding an toàn cho tai Momo, lưới và foliage; chọn pivot hợp lý rồi khai báo atlas. Không crop tùy tiện xuyên chân nhân vật để tạo frame chạy.
