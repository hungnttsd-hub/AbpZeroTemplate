# Hide & Seek — ghi nhận kiểm tra 24/09/2026

Phạm vi: Chrome desktop với viewport override, chế độ local, hồ sơ QA tạo riêng. Master prompt yêu cầu chạy tests và visual QA; không dùng subagent. Các kết quả dưới đây không thay cho kiểm thử Android/iOS hoặc API có tài khoản trên database thật.

## Lệnh đã chạy

| Lệnh | Kết quả |
|---|---|
| `npm run test:hide-seek` tại `react/` | **54/54 đạt**: 40 luật gốc + 14 content/controller/storage/layout/audio fallback |
| `dotnet test test/WebHoanTien.Domain.Tests/WebHoanTien.Domain.Tests.csproj --filter FullyQualifiedName~HideSeekScoringTests --nologo -v:minimal` | **9/9 đạt** |
| `npm run test:balloon-dart` tại `react/` | **10/10 đạt**, hồi quy |
| `npm run test:word-builder` tại `react/` | **11/11 đạt**, hồi quy |
| `npm run build:host` tại `react/` | Thành công, đã copy production bundle và art vào ABP wwwroot |
| `dotnet build src/WebHoanTien.Web/WebHoanTien.Web.csproj --no-restore -p:SkipClientAssets=true --nologo -v:minimal` | Thành công, 0 lỗi/0 cảnh báo |
| `git -c safe.directory=E:/HungNT/WoodyWings diff --check` | Không lỗi whitespace; Git có thông báo chuẩn hóa LF/CRLF |

Vite vẫn báo chunk Phaser khoảng 1.48 MB trước gzip; engine được lazy-load riêng. Không thêm package runtime. Pipeline Golden Bell vẫn nhập **1.000 câu duy nhất**, version không đổi.

## Luật và lưu trữ đã xác minh bằng test

- Bốn nhánh target/distractor × YES/NO; đúng NO không tự hoàn thành; chỉ target YES hoàn thành.
- 0 sao vẫn chơi, không âm; double tap/replayed event; assisted NO không trừ thêm; assisted YES không tính như hiểu độc lập.
- Hidden entity chưa lộ khi đang revealing; callback cũ bị bỏ qua; world không nhận reveal mới khi quiz đang chờ.
- Nội dung sáu màn/tám entity, grammar scissors/apple/orange, chỗ ẩn không mở được/mã trùng bị từ chối.
- Shuffle nhất quán TS/C#, seed giữ nguyên; 20 lần khôi phục **controller** giữ mapping và sao. Đây không phải 20 lần restart Phaser trên thiết bị.
- Layout hitbox nằm trong vùng chơi ở 1600×900, 1280×720, 1024×768, 844×390, 667×375.
- Lỗi storage pause và retry nguyên checkpoint; completion 0 sao tính một lần; offline queue retry idempotent và không gửi sao để server tin theo.
- AudioService thực được chạy với mock trình duyệt thiếu voice/voice lỗi: có callback lỗi, không chặn câu hỏi; stop/mute hoạt động. Chưa xác minh chất lượng giọng trên thiết bị thật.
- Server tính lại stars, thứ tự/reveal/event không hợp lệ bị từ chối, cùng seed cho cùng mapping.

## Các thao tác thực hiện qua giao diện Chrome

- Lobby đủ sáu màn và lối vào từ bản đồ; tiêu đề chỉ có nhiệm vụ bằng chữ, không có ảnh đáp án mẫu.
- Kéo ngắn chưa mở, vẫn 3 sao; kéo đủ ngưỡng mở hốc cây. Chọn sai công cụ ở đá hiện hướng dẫn, không mất sao.
- Blast Berry mở đá và hiện câu hỏi; Throw Net mở bụi cây, lưới đã biến mất khi entity/quiz hiện.
- HS-01: chó + NO giữ 3 sao và cho tìm tiếp; mèo + double NO chỉ còn 2 sao; assisted YES hoàn thành, kết quả và lobby giữ đúng 2 sao và ghi có trợ giúp.
- HS-02: mở ra kéo, trả lời YES cho câu hỏi tìm chó giảm một sao, giải thích `No. These are scissors.` rồi cho tìm tiếp.
- Pause/exit/resume và reload trong câu hỏi có trợ giúp/phản hồi giữ đúng entity, sao và phase; chuyển màn tạo scene mới. Đã sửa cleanup listener ở cả shutdown/destroy sau khi phát hiện lỗi HMR.
- 667×375: YES và NO cùng **136.5×56 CSS px**, không cắt chữ. 844×390: replay/pause **50×50**, tool **110×58**. Panel, hint và kết quả không bị tràn khỏi màn.
- 390×844: hiện hướng dẫn xoay ngang và pause; quay lại ngang, bấm Chơi tiếp khôi phục đúng câu hỏi có trợ giúp với 2 sao.
- Đã quan sát panel/feedback ở 1600×900, 1280×720, 1024×768; cover/đồ vật là các lớp riêng, không có nền checkerboard.
- HS-04 ở 667×375: bật Chạm để mở lá, mở bụi, câu hỏi đúng `Are these scissors?` kể cả khi entity vừa lộ là ball. Tab từ NO quay về nút nghe lại trong dialog, không lọt xuống công cụ; console không có lỗi ở lượt kiểm tra cuối.

Ảnh thực tế trong `artifacts/hide-seek-qa/` (thư mục artifacts được Git ignore):

- `search-1600x900.jpg`
- `question-1600x900.jpg`
- `feedback-1280x720.jpg`
- `feedback-1024x768.jpg`
- `assisted-667x375.jpg`
- `portrait-paused-390x844.jpg`
- `success-844x390.jpg`
- `scissors-667x375.jpg`

## Phần cần kiểm thêm trên môi trường thật

- Android/iOS, safe area vật lý, touch đa điểm và `pointercancel` thật; reduced motion với thiết lập hệ điều hành. Code có cleanup cho cancel/blur/resize, nhưng không coi quan sát bằng chuột là kiểm thử cảm ứng.
- Mất focus/tab ẩn: reducer pause và loại thời gian pause đã có test; thử mở tab nền qua công cụ Chrome không tạo mất focus thật, nên chưa đánh dấu runtime case này đạt.
- API đăng nhập/ownership/CSRF, đồng bộ retry và concurrency với PostgreSQL thật. Domain tests kiểm tra chấm điểm, không giả định đã chạy end-to-end account sync.
- Audio en-US của trình duyệt và latency trên máy bé; pack chưa cung cấp bộ voice thu sẵn được duyệt. Art đã thay bằng sprite runtime thật, Momo vẫn dùng tween thay cho bộ animation riêng.

Không chạy migration, không deploy, không commit/push. Dữ liệu reference pack được giữ nguyên.
