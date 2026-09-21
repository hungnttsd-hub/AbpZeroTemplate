# Word Shot — bắn mục tiêu có chiến thuật

Áp dụng cho các màn `word_shot` và hai vòng bắn trong boss. Dùng nhân vật Pip, đồ họa vector và tên đạn riêng của Wordy Wings. Không sử dụng asset/âm thanh của Angry Birds.

## Cách chơi

1. Chọn đạn ở thanh dưới; số bên cạnh là lượng còn lại của loại đó.
2. Di chuyển Pip bằng hai nút mũi tên gần mặt đất để đổi vị trí bắn.
3. Nhấn hạt trên ná, kéo xuống/trái để ngắm lên/phải. Độ dài kéo quyết định lực, hướng kéo quyết định góc. Vệt chấm xem trước dùng cùng thuật toán vật lý với phát bắn thực.
4. Thả để bắn. Chạm riêng mục tiêu không còn tự bắn trúng. Chạm rồi thả ná mà chưa kéo không tiêu tốn đạn.
5. Phá vỡ toàn bộ vỏ giữ Word Stars để hoàn thành màn. Chấm vàng trên mục tiêu biểu thị số điểm độ bền còn lại. Mục tiêu dùng từ đang học và phát lại từ khi bị trúng.

Điều khiển bàn phím: **1–4** chọn đạn; **A/D** di chuyển; **←/→** tăng/giảm góc; **↑/↓** tăng/giảm lực; **Space** bắn; **R** thử lại sau khi hết đạn. Nút tạm dừng vẫn có Resume / Restart / Exit.

## Đạn và vật cản

| Loại | Tác dụng |
|---|---|
| Hạt sao | 2 sát thương, dừng ở lần va chạm đầu tiên |
| Hạt nổ | 3 sát thương trong bán kính 175; phá gỗ trong vùng nổ; đá chắn được vụ nổ |
| Mũi xuyên | 2 sát thương mỗi mục tiêu, xuyên tối đa 3 lớp rồi dừng ở va chạm tiếp theo; giảm tốc khi xuyên |
| Hạt nảy | 2 sát thương, nảy tối đa 4 lần trên đá/mặt đất, giữ 86% vận tốc sau mỗi lần nảy |

Gỗ có 3 điểm độ bền, bị đạn phá được. Đá giữ nguyên, cần bắn vòng, xuyên hoặc bật nảy. Tán lá là trang trí, thân/cành là vật cản thực. Không được đổi đạn hoặc di chuyển Pip khi đạn đang bay.

## Bố cục và độ khó

Mỗi lần vào màn sinh vị trí có xê dịch trong các khu vực định sẵn, cách nhau đủ để không chồng lên nhau: cành cây, sau thân cây, trên mỏm đá, hốc đá mở bên trái, sau tấm chắn gỗ. Thử lại khi hết đạn giữ nguyên bố cục và gió, hồi mục tiêu và lượng đạn để thử chiến thuật khác. Không lưu sao hay mở màn khi chưa giải cứu hết mục tiêu. Các vòng đã qua của boss được giữ nguyên.

Luật nằm trong `react/src/game/word-shot/rules.json`. Độ khó tăng theo thứ tự world và level:

- Đầu hành trình: 2 mục tiêu, 2 độ bền, không gió, không di chuyển.
- Sau đó: 3 mục tiêu và các vị trí có che chắn.
- Từ cuối world đầu: 4 mục tiêu, 1 mục tiêu chuyển động, gió nhẹ.
- Cuối world hai: 5 mục tiêu, 3 độ bền, 2 mục tiêu chuyển động, gió mạnh hơn.
- Cuối world ba: 5 mục tiêu, 3 mục tiêu chuyển động, trọng lực/gió lớn hơn, ít đạn thường hơn.

Boss dùng 2 mục tiêu trong mỗi vòng bắn để tránh kéo dài cả chuỗi quá nhiều. Các mục tiêu chuyển động lên/xuống phía trên vị trí gốc, không chìm vào bệ. Vị trí sinh theo khu vực giúp tránh chồng lấn, nhưng cân bằng độ khó/khả năng hoàn thành với số đạn cần được chơi thử trên thiết bị thật.

## Vật lý và lưu tiến trình

`physics.ts` dùng bước thời gian cố định 1/120 giây, trọng lực và gia tốc gió, kiểm tra đoạn đường đạn đi qua với hình tròn mục tiêu / hình chữ nhật vật cản. Vì vậy đạn tốc độ cao không bỏ qua một tấm gỗ mỏng giữa hai frame. Va chạm gần nhất được xử lý trước. Preview mô phỏng trên bản sao trạng thái, không gây sát thương thật hay giảm đạn.

Lượt bắn không gây tác dụng được ghi là lần thử sai của từ đang học; phá gỗ mở đường không bị tính là sai. Chỉ sau khi hoàn thành toàn bộ mục tiêu mới dùng luồng `AttemptTracker` / IndexedDB / API hiện có. Không cần migration database mới cho cơ chế này.

Chưa chạy test hoặc kiểm thử trình duyệt tự động theo chỉ dẫn dự án. Build TypeScript/production được dùng để xác nhận code biên dịch; không thay thế kiểm tra cân bằng vật lý khi chơi thực tế.
