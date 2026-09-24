# 03 — Chuyển động, VFX và audio

Các số dưới đây là thông số thiết kế khởi đầu, cần điều chỉnh sau khi thử trên thiết bị mục tiêu. Không phải kết quả đo FPS của game đã dựng.

## 1. Ambient motion

Mây có thể trôi rất chậm, cây lay 1–2 độ, lá dao động 2–4 design px. Chỗ có mục tiêu và chỗ có nhiễu dùng cùng phân phối motion. Không liên kết random seed ambient với đáp án đúng theo cách tạo cue nhận biết.

Momo idle chớp mắt, thở, nghiêng đầu. Không lặp voice thúc giục. Khi bé chọn công cụ, Momo nhìn công cụ, không nhìn vị trí target. Sau một NO đúng, vẫy tay hoặc gật đầu ngắn; chỉ success mới có celebration dài hơn.

## 2. Swipe Leaves

| Mốc | Hoạt động |
|---|---|
| pointerdown | Ghi activePointerId và dragOrigin; chụp trạng thái cover |
| drag | Cover trái/phải đi theo signed displacement, clamp trong biên |
| thả dưới ngưỡng | Tween trở về trong 180 ms; không thay state khám phá |
| đủ ngưỡng | Khóa cử chỉ, hoàn tất mở lá trong khoảng 340 ms |
| 520–820 ms | Lá ổn định; object visible; neutral reveal cue; mở quiz |

Ngưỡng đề xuất `min(0.55 * coverWidth, 96)` design px, nhưng hitbox phải đạt yêu cầu CSS px. Không cộng total path length vì lắc ngón tay tại chỗ không phải vạch lá. Tap alternative dùng cùng kết quả và scoring; không âm thầm hạ sao vì dùng trợ giúp thao tác.

## 3. Throw Net

Select → tap spot. Từ vị trí công cụ hoặc tay Momo, animate lưới theo đường cong 360 ms. Kéo lớp lá lên 260 ms, cho lưới dissolve khoảng 280 ms. Khi net đã không quấn quanh vùng occupant, mới show object rồi quiz.

Không cần chất liệu lưới deform phức tạp hoặc vật lý va chạm giữa từng nút. Có thể dùng 2–4 frame và scale/rotation/tween. Trên reduced motion: icon tool highlight, cover fade, object hiện; không bay qua nửa màn.

## 4. Blast Berry

Quả mọng mềm bay khoảng 280 ms. Chạm tảng đá đóng → puff tròn nhẹ, đá tách tối đa 3–4 mảnh ở hướng tránh vị trí occupant. Mảnh fade hết vào khoảng 850–1000 ms rồi cho thấy object.

Không camera shake bắt buộc; default chỉ scale bounce rất nhỏ trên rock. Không ảnh lửa thật, mảnh sắc, âm nổ lớn hoặc hit animation trên con vật. Công cụ không còn dùng được trên spot đã revealed/resolved.

## 5. Quiz

Panel vào bằng scale 0.97→1 hoặc fade 180–220 ms. Không bật quiz ngay khi object còn đang bị che bởi VFX. Nút pressed scale 0.97 trong 80 ms; chấp nhận lựa chọn ngay một lần, không đợi animation mới lock.

Sai: một sao ở HUD chuyển từ vàng sang outline trong khoảng 300 ms, text giải thích giữ ổn định. Không nhấp nháy đỏ toàn màn. Đúng NO: pulse nhẹ, không chuông thắng. Đúng YES trên target: dấu hiệu xác nhận rồi Momo đi tới chào object.

Feedback không tự biến mất quá nhanh trước khi voice đọc xong. Cho nút Continue sau khi nội dung đã xuất hiện; nếu voice lỗi, kết thúc theo fallback text dwell khoảng 1200 ms. Dùng một state transition guard chung cho nút và callback.

## 6. Success

Timeline đề xuất: xác nhận 0–400 ms → object và Momo xuất hiện cạnh nhau 400–1400 ms → sao còn lại và nút 1400–2400 ms. Hiệu ứng sparkle chỉ trang trí; mất callback hạt không được chặn flow hoàn thành.

Nút Next phải ổn định sau animation. Không đòi trẻ kéo/tap thêm một hành động tinh để được lưu thành tích. Lưu completion idempotent khi state completed được chấp nhận, không phụ thuộc phát xong mọi audio.

## 7. Audio policy

`data/audio-manifest.json` chứa 37 script cho tám entity và phản hồi chung. **Không có MP3 trong ZIP.** Cần dùng bản thu/TTS đã được người lớn kiểm tra hoặc thư viện voice sẵn có trong game. Đường dẫn manifest là đường dẫn dự kiến, không phải file đang tồn tại.

Một voice channel tại một thời điểm. Replay quest không chồng lên quiz question; ưu tiên câu hỏi hiện hành. Duck nhạc nền khi đọc, giữ âm lượng tương đối dễ nghe. Cử chỉ user phải unlock playback theo AudioManager hiện có. Failed playback không làm thất bại nhận diện.

Trước câu trả lời đầu tiên: không audio dog/tiger/scissors, không tiếng con vật hé lộ ý nghĩa. Sau trả lời mới dùng `negativeIdentification` hoặc `affirmative`. Không đọc mọi tool name mỗi lần chuyển tool nếu gây ồn.

## 8. Lifecycle

Dùng scene clock và cancellation token/scene lifecycle để dừng tween, timer và audio khi pause/shutdown. Khi pause trong reveal, ghi progress timeline hoặc pause tween thật; resume đúng nơi, không reset sao. Cleanup event handler đúng reference. Không gắn event bus mới mỗi lần mở panel mà quên tháo khi đóng.
