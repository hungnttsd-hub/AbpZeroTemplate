# Word Shot — bắn mục tiêu có chiến thuật

Áp dụng cho các màn `word_shot` và hai vòng bắn trong boss. Dùng nhân vật Pip, đồ họa vector và tên đạn riêng của Wordy Wings. Không sử dụng asset/âm thanh của Angry Birds.

## Cách chơi

1. Chọn đạn ở thanh dưới; số bên cạnh là lượng còn lại của loại đó.
2. Di chuyển Pip bằng hai nút mũi tên gần mặt đất để đổi vị trí bắn.
3. Nhấn hạt trên ná, kéo xuống/trái để ngắm lên/phải. Độ dài kéo quyết định lực, hướng kéo quyết định góc. Vệt chấm xem trước dùng cùng thuật toán vật lý với phát bắn thực.
4. Thả để bắn. Chạm riêng mục tiêu không còn tự bắn trúng. Chạm rồi thả ná mà chưa kéo không tiêu tốn đạn.
5. Phá vỡ toàn bộ vỏ giữ Word Stars để hoàn thành màn. Chấm vàng trên mục tiêu biểu thị số điểm độ bền còn lại. Mục tiêu dùng từ đang học và phát lại từ khi bị trúng.

Điều khiển bàn phím: **1–6** chọn đạn; **A/D** di chuyển; **←/→** tăng/giảm góc; **↑/↓** tăng/giảm lực; **Space** bắn; **R** thử lại sau khi hết đạn. Nút tạm dừng vẫn có Resume / Restart / Exit.

## Đạn và vật cản

| Loại | Tác dụng |
|---|---|
| Hạt sao | 2 sát thương, dừng ở lần va chạm đầu tiên |
| Hạt nổ | 3 sát thương trong bán kính 175; phá gỗ trong vùng nổ; đá chắn được vụ nổ |
| Mũi xuyên | 2 sát thương mỗi mục tiêu, xuyên tối đa 3 lớp rồi dừng ở va chạm tiếp theo; giảm tốc khi xuyên |
| Hạt nảy | 2 sát thương, nảy tối đa 4 lần trên đá/mặt đất, giữ 86% vận tốc sau mỗi lần nảy |
| Hạt băng | 1 sát thương trong vùng 190; giữ mục tiêu đứng yên 5 giây; phát trúng tiếp theo bằng loại đạn khác được thêm 1 sát thương khi băng còn hiệu lực; đá chặn băng lan |
| Thiên thạch | 6 sát thương; phá đá hoặc gỗ và tiếp tục bay sau vật cản đầu tiên đã vỡ; vận tốc đầu ×0.88, trọng lực ×1.25 nên cần lực lớn hơn |

Gỗ có 3 điểm độ bền, bị đạn phá được. Đá có 6 điểm độ bền và chỉ thiên thạch làm hỏng đá; các loại đạn khác cần bắn vòng, xuyên hoặc bật nảy. Mặt đất không bị phá hủy. Tán lá là trang trí, thân/cành là vật cản thực. Không được đổi đạn hoặc di chuyển Pip khi đạn đang bay.

## Bố cục và độ khó

Mỗi lần vào màn sinh vị trí có xê dịch trong các khu vực định sẵn, cách nhau đủ để không chồng lên nhau: cành cây, sau thân cây, trên mỏm đá, hốc đá mở bên trái, sau tấm chắn gỗ. Thử lại khi hết đạn giữ nguyên bố cục và gió, hồi mục tiêu và lượng đạn để thử chiến thuật khác. Không lưu sao hay mở màn khi chưa giải cứu hết mục tiêu. Các vòng đã qua của boss được giữ nguyên.

Mũi xuyên bay nhanh hơn 12%, chịu 88% trọng lực; hạt băng chịu 82% trọng lực. Preview áp dụng đúng hệ số của từng loại đạn. Tổng số đạn mỗi nhóm độ khó được giữ như trước (10 / 10 / 11 / 13 / 12) và phân bổ lại cho sáu loại; cả băng và thiên thạch có từ màn đầu.

Luật nằm trong `react/src/game/word-shot/rules.json`. Độ khó tăng theo thứ tự world và level:

- Đầu hành trình: 2 mục tiêu, 2 độ bền, không gió, không di chuyển.
- Sau đó: 3 mục tiêu và các vị trí có che chắn.
- Từ cuối world đầu: 4 mục tiêu, 1 mục tiêu chuyển động, gió nhẹ.
- Cuối world hai: 5 mục tiêu, 3 độ bền, 2 mục tiêu chuyển động, gió mạnh hơn.
- Cuối world ba: 5 mục tiêu, 3 mục tiêu chuyển động, trọng lực/gió lớn hơn, ít đạn thường hơn.

Boss dùng 2 mục tiêu trong mỗi vòng bắn để tránh kéo dài cả chuỗi quá nhiều. Các mục tiêu chuyển động lên/xuống phía trên vị trí gốc, không chìm vào bệ. Vị trí sinh theo khu vực giúp tránh chồng lấn, nhưng cân bằng độ khó/khả năng hoàn thành với số đạn cần được chơi thử trên thiết bị thật.

## Vật lý và lưu tiến trình

`physics.ts` dùng bước thời gian cố định 1/120 giây, trọng lực và gia tốc gió, kiểm tra đoạn đường đạn đi qua với hình tròn mục tiêu / đa giác vật cản (hình chữ nhật cho mặt đất). Vì vậy đạn tốc độ cao không bỏ qua một tấm gỗ mỏng giữa hai frame. Va chạm gần nhất được xử lý trước. Preview mô phỏng trên bản sao trạng thái, không gây sát thương thật hay giảm đạn.

Lượt bắn không gây tác dụng được ghi là lần thử sai của từ đang học; phá gỗ/đá mở đường hoặc gây sát thương bằng băng không bị tính là sai. Tạm dừng cũng dừng thời gian đóng băng; thử lại xóa hiệu ứng và trạng thái băng. Chỉ sau khi hoàn thành toàn bộ mục tiêu mới dùng luồng `AttemptTracker` / IndexedDB / API hiện có. Không cần migration database mới cho cơ chế này.

Chưa chạy test hoặc kiểm thử trình duyệt tự động theo chỉ dẫn dự án. Build TypeScript/production được dùng để xác nhận code biên dịch; không thay thế kiểm tra cân bằng vật lý khi chơi thực tế.

## Đồ họa và hiệu ứng — 22/09/2026

- Sáu biểu tượng SVG riêng: hạt sao lá nhỏ, hạt nổ có đai, mũi xuyên pha lê, bóng nảy vòng vàng, tinh thể băng, đá thiên thạch. Dùng cùng hình trên ná, khi bay và ở thanh chọn đạn.
- Vỏ mục tiêu có độ bóng, vành, thẻ tên và chấm độ bền. Vỏ nứt khi bị thương; viền băng khi đông; Word Star bay về thanh tiến trình khi được cứu.
- Gỗ có vân, đai kim loại, đinh; thân cây có lá. Đá có mặt sáng/tối, lớp địa chất và rêu. Bản địa hình mới bên dưới thay các cột chữ nhật bằng đa giác có góc vát; lá/rêu chỉ trang trí.
- Mỗi đạn có vệt bay mang màu riêng; xuyên có lõi sáng, băng có bông tuyết, thiên thạch có bụi. Va chạm phát mảnh gỗ/đá theo vật liệu; nổ có vòng lan, tia sao và bụi; băng có sóng lạnh; nảy có vòng đàn hồi.
- Thanh đạn dùng đủ chiều ngang, phím 1–6, lượng còn lại tách khỏi mô tả. Ná gỗ có dây, đai và điểm sáng. Gió hiển thị bằng mô tả dễ hiểu.
- `visuals.ts` chứa vector và vật thể; `effects.ts` quản lý tối đa 96 hạt tái sử dụng. Với `prefers-reduced-motion`, dùng tối đa 12 hạt, giảm chuyển động/vòng nổ và không cho sao bay qua màn hình. Các hiệu ứng được dọn khi thử lại, thoát hoặc chuyển vòng boss.
- Đồ họa SVG/Phaser được vẽ trong mã nguồn, không tải asset bên ngoài. Hiệu ứng chỉ minh họa; sát thương luôn do `physics.ts` quyết định.

Đã build TypeScript/Vite và cập nhật web host. Chưa chạy test hoặc trình duyệt tự động theo AGENTS.md; độ khó và hiệu năng thực tế cần được xác nhận bằng lượt chơi trên thiết bị.
## Nhãn chữ và bộ địa hình mới

- Tách tên từ và chấm độ bền khỏi vỏ mục tiêu: nhãn có nền kem đặc, nằm trên lớp địa hình. Vỏ/hình bên trong vẫn bị đá và gỗ che như cơ chế bắn hiện tại.
- Nhãn tự tìm khoảng trống phía trên mái hang, tán lá, thân vật cản, vỏ mục tiêu khác và các nhãn đã đặt. Đường nối giúp nhận ra mục tiêu tương ứng; đoạn đi qua vật cản nằm phía sau vật cản. Dành trước khoảng chuyển động của mục tiêu để nhãn không nhảy lên/xuống theo từng frame. Nhãn được ẩn/dọn cùng mục tiêu khi giải cứu, thử lại và thoát màn.
- Mười mẫu SVG riêng trong `terrainArt.ts`: trụ ba tảng đá xếp lệch, mỏm đá đứng, mái hang phủ cỏ/hoa, vách hang có dây leo, bệ đá có tinh thể, thân cây, cột buộc dây, dầm gỗ, hàng rào ba ván và thùng xếp chồng.
- Đá dùng mặt vát, màu sáng/tối, hạt khoáng, rêu và hoa nhỏ; gỗ có vân, mắt gỗ, vòng gỗ ở đầu cắt, dây thừng và đai có đinh tán. Hình được rasterize từ SVG ở độ phân giải ×2 khi preload, không dựng lại tranh mỗi frame.
- Các màn có ít nhất một vị trí đá; mỏm đá thay đổi giữa trụ đá xếp và đá đứng, tấm chắn thay đổi giữa hàng rào và hai thùng gỗ. Hốc đá được mở rộng cho cả vỏ mục tiêu khi chuyển động. Thử lại vẫn giữ nguyên địa hình đã sinh.
- `terrainShapes.ts` là đường bao dùng chung cho SVG và vật lý. Va chạm dùng swept circle với cạnh/đỉnh của đa giác, kiểm tra hình chữ nhật bao ngoài trước; đạn có thể đi qua góc trống, hạt nảy phản xạ theo mặt nghiêng. Nổ và băng cũng dùng đa giác để xác định đá chắn. Rêu, lá, dây thừng, hoa, bóng đổ và tinh thể nhỏ là trang trí.
- Bộ địa hình được vẽ trực tiếp bằng SVG trong mã nguồn. Các bản thử ảnh sinh bằng image_gen không đạt nền trong suốt nên không đưa vào game; game không cần tải thêm PNG mới cho đợt này.

Đã xem bản render tĩnh của mười mẫu SVG và build TypeScript/Vite thành công; web host đã được cập nhật. Chưa chạy test, tự động hóa trình duyệt hoặc xác nhận gameplay trực tiếp, theo chỉ dẫn dự án.
