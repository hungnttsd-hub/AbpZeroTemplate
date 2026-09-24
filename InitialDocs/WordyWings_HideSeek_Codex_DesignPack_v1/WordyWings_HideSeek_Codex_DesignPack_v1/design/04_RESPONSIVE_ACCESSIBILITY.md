# 04 — Responsive và hỗ trợ thao tác

## 1. Desktop và tablet ngang

Thiết kế cảnh theo logical viewport 1600×900. Background có thể cover/decorative crop nhưng vùng hitbox và HUD phải nằm trong safe rectangle. Không kéo giãn ảnh theo hai trục khác nhau. Ưu tiên reflow object anchors thay vì làm chúng nhỏ tới mức không nhận diện được.

Tách layer camera/world khỏi HUD. Tính lại layout khi resize. Tọa độ pointer đổi sang world bằng cơ chế đúng với engine/version đang dùng; không dùng raw pageX trừ offset đoán trước.

## 2. Điện thoại ngang

Reference CSS viewport 844×390 là điểm kiểm thử, không ràng buộc chỉ một thiết bị. Trừ safe-area trước khi layout. Header gọn một dòng, tool tray nhỏ hơn nhưng mỗi tool có hit target ít nhất 48 CSS px; label có thể giảm hoặc chỉ hiện tool đang chọn, giữ icon và accessible name.

Panel quiz chuyển vào giữa safe area, khoảng 470×242 CSS px tại reference này. Hai nút YES/NO mỗi nút nên tối thiểu 120×56 CSS px. Portrait vừa thấy tối thiểu khoảng 110×110 CSS px. Nếu không đủ chỗ, giảm phần decor và Momo, không thu nhỏ nút dưới ngưỡng.

Không áp dụng đơn thuần `scale = width / 1600` cho toàn UI: nút 80 design px ở width 844 chỉ còn khoảng 42 CSS px. Với CSS viewport nhỏ hơn nữa, phải reflow.

## 3. Portrait

Bản đầu tập trung landscape. Khi portrait, pause và hiện hướng dẫn xoay ngang, giữ nguyên reveal/quiz/star. Có nút quay lại an toàn. Không gọi forced orientation lock như một điều kiện để game chạy. Resume không xáo vị trí hoặc phát lại penalty.

## 4. Input

Mouse: chọn công cụ rồi click hoặc drag. Touch: chạm công cụ rồi chạm/drag cover. Tap-to-open là lựa chọn tương đương về tiến độ, không phải chế độ bị trừ điểm. Cử chỉ chỉ chiếm pointer trong vùng game; không chặn scroll của toàn website khi người dùng ở parent dashboard.

Giữ một active pointer cho reveal. Ngón thứ hai không tạo projectile thứ hai. `pointercancel`, mất focus hoặc teardown scene hủy an toàn cử chỉ chưa hoàn tất. Một điểm đã revealed/resolved không mở quiz mới vì click xuyên panel.

Keyboard trên web: focus các tool và spot bằng lớp accessibility đã có; Enter/Space để kích hoạt. YES/NO có focus rõ. Không bắt mọi trẻ dùng phím tắt chữ Y/N; có thể bổ sung nhưng vẫn cần nút.

## 5. Không lộ đáp án qua accessibility/debug

Accessible label ban đầu là “Bụi cây 1”, “Tảng đá”, “Hốc cây”, không phải “Cat behind bush”. Tooltip, title, nhãn debug và DOM text phục vụ screen reader cũng không được hiện target location trong mode nhận diện.

Một trò dựa vào nhìn hình không tự nhiên trở thành phù hợp hoàn toàn cho người không nhìn được ảnh. Không tuyên bố hỗ trợ đầy đủ chỉ vì thêm ARIA. Khi làm mode thay thế bằng mô tả hoặc audio, cần ghi nhận đó là cách chơi có hỗ trợ và không đánh đồng chỉ số nhận diện hình với mode chuẩn.

## 6. Reduced motion và âm thanh

Tôn trọng preference hệ thống và setting trong game. Chuyển moving/net flight thành fade hoặc chuyển động ngắn; giữ feedback đúng/sai bằng chữ, icon và sao. Không dựa riêng vào màu hoặc âm thanh. Mute không làm hết khả năng chơi vì quest và câu hỏi vẫn có text.

## 7. Ma trận kiểm thử đề xuất

| Viewport CSS | Input | Kiểm tra |
|---|---|---|
| 1600×900 | mouse | HUD, 6 spot, quiz bên phải |
| 1280×720 | mouse | label không cắt, không overlap |
| 1024×768 | touch | tablet, tool/quiz hit area |
| 844×390 | touch | safe area, panel giữa, tool min-size |
| 667×375 | touch | viewport ngang nhỏ, decor giảm |
| 390×844 | touch | pause portrait, quay lại không reset |

Đây là ma trận cần chạy khi tích hợp; việc crop ảnh trong pack không chứng minh đã vượt qua các bài test thiết bị này.
