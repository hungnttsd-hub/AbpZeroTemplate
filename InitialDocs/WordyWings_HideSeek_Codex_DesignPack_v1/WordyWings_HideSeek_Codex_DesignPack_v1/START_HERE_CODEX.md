# MASTER PROMPT — Thêm màn Hide & Seek cho Wordy Wings

> Dán toàn bộ nội dung dưới đây cho Codex sau khi đưa thư mục thiết kế vào workspace của dự án.

Bạn đang làm việc trong repository Wordy Wings. Hãy triển khai thêm mechanic **Hide & Seek — Trốn tìm tiếng Anh**, tích hợp vào game hiện có. Backend dự kiến ABP.io / ASP.NET Core, app shell React + TypeScript, game engine Phaser; mobile sau này dùng Capacitor. **Phải kiểm tra repository thực tế trước, không suy đoán tên module, phiên bản, API hoặc cấu trúc hiện có.**

Tài liệu và ảnh đính kèm là bộ bàn giao thiết kế, không phải source app. Đừng trả lời chỉ bằng kế hoạch. Sau khi kiểm tra repo, nêu ngắn gọn kế hoạch và triển khai theo từng bước, chạy những test khả dụng, rồi báo đúng những gì đã làm và chưa làm.

## 1. Thứ tự đọc

1. `README.md`.
2. `design/01_VISUAL_SPEC.md` và `design/02_GAMEPLAY_RULES.md`.
3. `images/annotated/A00_measured_overlay.png`, A01, A04, A08–A11.
4. Các ảnh chi tiết D01–D36; xem trực tiếp ảnh, không chỉ suy luận từ tên file.
5. `design/03_MOTION_AUDIO.md`, `design/04_RESPONSIVE_ACCESSIBILITY.md`.
6. `engineering/IMPLEMENTATION_PLAN.md`, `engineering/INTEGRATION_NOTES.md`.
7. `engineering/contracts.ts`, `engineering/rules.ts`, JSON data và schema.
8. `qa/ACCEPTANCE_CRITERIA.md`.

Nếu ảnh concept mâu thuẫn tài liệu quy tắc, **quy tắc và tiêu chí nghiệm thu có ưu tiên cao hơn**. Không sao chép lỗi chữ trong ảnh.

## 2. Trải nghiệm cần tạo

Một khu rừng hoạt hình 2D/2.5D sáng sủa, vui nhộn, cùng visual language Wordy Wings. Momo rồng con màu xanh đứng bên cạnh hướng dẫn. Giữa màn có các chỗ ẩn nấp: hốc cây, bụi lá, tảng đá, cỏ cao hoặc thân gỗ rỗng. Dưới cùng có ba công cụ rõ ràng.

Bắt đầu với nhiệm vụ **Find a cat.**, kèm audio và nút nghe lại. Không hiển thị ảnh mèo, ảnh ví dụ, đuôi, tai, mắt, bóng mèo, silhouette hay tiếng meo meo trước khi bé tự mở đúng chỗ. Không để Momo chỉ vào nơi có mèo. Bụi cây đúng không sáng hơn hoặc rung khác những bụi còn lại.

Tất cả vật thể ẩn phải được dựng thành entity riêng bên dưới cover riêng; mặc định không render và không nhận input. Không dùng toàn bộ ảnh concept chứa sẵn động vật làm background rồi phủ hitbox vô hình lên trên.

## 3. Ba tương tác tìm kiếm

### Swipe Leaves

Bé chọn công cụ bàn tay rồi kéo lá sang một bên. Lớp lá đi theo cử chỉ, tự hoàn thành khi vượt ngưỡng. Thả sớm thì lá nhẹ nhàng trở về, không mất sao. Có phương án tap để mở dành cho trẻ khó kéo chính xác. Không tính tổng chuyển động lắc qua lại thành thao tác thành công; dùng độ dịch chuyển có hướng từ điểm bắt đầu.

### Throw Net

Bé chọn lưới rồi chạm vào bụi cây phù hợp. Lưới bay theo đường cong ngắn tới bụi, kéo tán lá lên rồi biến mất; **sau đó** vật thể mới hiện. Lưới không quấn hoặc siết con vật. Không cần mô phỏng lưới bằng hàng chục body vật lý.

### Blast Berry

Bé chọn quả mọng phép thuật rồi chạm vào tảng đá. Quả mọng chạm lớp đá che, tạo puff hoạt hình mềm, lớp đá tách ra và các mảnh tan đi. Sau đó đối tượng hiện rõ. Không tạo bom thực, sát thương, vết thương, mảnh sắc hoặc bắn vào động vật đã hiện.

Cả ba tương tác có thể làm bằng tween và timeline. Chỉ dùng physics module đã có khi thật sự cần; không cài thêm engine khác vì hiệu ứng trang trí. Tác động vào chỗ không hỗ trợ công cụ chỉ bounce/hint công cụ, không trừ sao.

## 4. Luồng kiểm tra Yes/No

Mở được một chỗ → chờ hiệu ứng hoàn tất → cho thấy rõ một đối tượng → mở panel xác nhận.

Với nhiệm vụ tìm mèo, câu hỏi chính xác là **Is this a cat?**. Panel chỉ được dùng ảnh đối tượng vừa hé lộ, không có thêm ảnh đáp án mèo mẫu. Không đặt tên đối tượng hoặc phát âm tên chó/hổ trước khi bé chọn. YES và NO cùng kích thước, cùng độ nổi bật. Khóa input khu rừng và thanh công cụ khi panel đang mở. Không có nút X để bỏ qua câu đang chờ; vẫn cho phép pause và quay lại sau bằng flow của game.

Bảng quy tắc bắt buộc:

| Đối tượng vừa thấy | Bé chọn | Kết quả | Bước tiếp |
|---|---|---|---|
| Mèo | YES | Đúng, không trừ sao | Hoàn thành màn |
| Mèo | NO | Sai, trừ 1 sao một lần | Giải thích, cho chọn YES có trợ giúp |
| Chó/hổ/kéo | NO | Đúng, không trừ sao | Đánh dấu đã tìm, tiếp tục tìm mèo |
| Chó/hổ/kéo | YES | Sai, trừ 1 sao một lần | Giải thích đúng tên, tiếp tục tìm |

Một câu NO đúng trên chó **không được** hoàn thành màn. Mở hết các chỗ cũng **không tự động** hoàn thành màn. Chỉ xác nhận YES trên đúng mục tiêu mới qua màn.

## 5. Sao và chống xử lý lặp

Bắt đầu với 3 sao. Chỉ câu trả lời nhận diện sai mới trừ sao; motor miss, đổi công cụ, mở chỗ có đối tượng nhiễu, nghe lại, thời gian chơi đều không bị phạt.

Mỗi lần reveal có `revealId`, mỗi lần trả lời có `answerEventId`. Trừ tối đa một sao trong cùng reveal. Disable lựa chọn ngay sau click đầu tiên, chuyển sang feedback atomically. Double-tap, hai event listener, replay event và retry mạng không được trừ thêm.

Ở 0 sao vẫn cho chơi với trợ giúp. Không sao âm, không buộc xem quảng cáo hoặc thanh toán. Nếu mục tiêu bị trả lời NO, giải thích rồi mở assisted retry; YES sau gợi ý hoàn thành nhưng không được ghi nhận như một lần hiểu độc lập. Tiếp tục chọn sai ở assisted retry không trừ tiếp sao của reveal đó.

Kết quả lưu `starsRemaining` thật, không luôn hiển thị ba sao. Không tự chỉnh lại cơ chế sao toàn game; tích hợp qua adapter hiện có, thêm trường/enum có version khi cần.

## 6. Cấu trúc triển khai

Dùng React cho route, loading shell, tài khoản, trang phụ huynh và integration. Dùng Phaser cho khu rừng, cover, object, công cụ, chuyển động và feedback trong game. Nếu dự án đã có lớp accessible UI cho Phaser thì tái sử dụng, không dựng hai nguồn state độc lập.

Tách thành các trách nhiệm tương đương:

- `HideSeekController`: điều khiển state machine và phiên chơi.
- `HidingSpotView`: closed / revealing / revealed / resolved.
- `RevealInteraction`: implementations cho leaves, net, berry.
- `QuizPresenter`: YES/NO, khóa input, feedback.
- `HideSeekRules`: pure functions; có thể tái sử dụng `engineering/rules.ts`.
- `HideSeekContentAdapter`: nạp, validate và normalize JSON.
- `HideSeekProgressAdapter`: kết nối progress hiện có, offline queue và idempotency.
- `HideSeekAudioAdapter`: dùng AudioManager hiện tại, không phát nhiều voice đè nhau.

Đây là tên đề xuất, không bắt buộc đổi tên module đang chạy tốt. Không cần một scene riêng cho từng từ. Không viết `if targetWord === 'cat'` cho logic đúng/sai; so sánh entity ID trong dữ liệu.

## 7. Nội dung và grammar

Import `data/hide-seek-levels.json` qua content pipeline hiện có. Có 6 màn demo, 8 entity và schema riêng. Mỗi màn có đúng một vị trí chứa mục tiêu và mọi vị trí đều mở được bằng ít nhất một công cụ đang có.

Các câu tiếng Anh đã được viết đầy đủ trong entity data. Không tự nối `a` với mọi danh từ: `an apple`, `an orange`, `Are these scissors?` cần đúng grammar.

Cho phép random vị trí entity bằng seeded shuffle trong tập spot tương thích. Để tái hiện bug hoặc resume, phải giữ lại seed/mapping của cùng phiên. Dữ liệu gốc là bố trí mẫu, không phải vị trí cố định bé luôn ghi nhớ. Không shuffle lại sau mỗi lần chọn sai hoặc resize.

`runtime-asset-checklist.json` ghi asset chưa có. Phải phân biệt reference crop và runtime texture: crop có nền không phải sprite trong suốt. Tận dụng asset repo trước; nếu thiếu, dựng fallback trung tính và ghi rõ pending art. Không báo đạt visual fidelity production khi vẫn dùng placeholder.

## 8. Responsive, input và audio

Desktop tham chiếu 1600×900. Mobile landscape phải reflow HUD, không thu nhỏ nút 80 design px thành nút 30 CSS px. Các control chủ yếu tối thiểu 48 CSS px sau mọi transform. Hỗ trợ safe area và resize. Portrait: pause bảo toàn state và gợi ý xoay ngang; không mất sao. Không phụ thuộc API bắt buộc khóa orientation của trình duyệt.

Cử chỉ dùng pointer abstraction hỗ trợ mouse/touch. Chỉ một cử chỉ active; pointercancel, mất focus hoặc đổi kích thước phải trả state về chỗ an toàn, không lặp reveal.

Audio unlock sau user gesture. Nếu không phát được, vẫn có chữ và công cụ hoạt động; không hiện hình mục tiêu để thay cho audio. Tên đối tượng nhiễu chỉ phát sau khi trả lời. Nếu bị pause, khóa timeline và voice theo AudioManager hiện có. Khi bật reduced motion, thay chuyển động mạnh bằng fade nhẹ.

## 9. Backend và triển khai

Giữ nguyên backend ABP, database, auth và hosting đang dùng. Không cần thêm hạ tầng chỉ để có mechanic này. Kiểm tra endpoints/service hiện tại và nối qua adapter; API trong tài liệu là đề xuất, không mặc định đã tồn tại.

Không gửi request cho mỗi frame vạch lá. Lưu local checkpoint khi nhận câu trả lời và đồng bộ theo cơ chế hiện có. Completion/progress phải idempotent; server kiểm tra quyền với child profile và tính lại kết quả từ version nội dung nếu server là nguồn tin cậy. Không tin một trường `stars: 3` tự gửi từ client. Không log thông tin định danh trẻ vào console/analytics không cần thiết.

## 10. Trình tự làm và nghiệm thu

A. Audit repo, phiên bản thực tế, scene registry, routes, AudioManager, AssetManager, progress adapter; báo file sẽ reuse và file mới.

B. Chạy test pure rules từ pack; tích hợp loader và dữ liệu. Làm một màn HS-01 chạy xuyên suốt với chỗ ẩn kín, ba công cụ, NO tiếp tục, YES hoàn thành.

C. Thêm motion, Momo, quiz UI, sao và responsive bám ảnh. Không thêm feature mới làm rối màn.

D. Nối progress, pause/resume/offline, sáu demo levels, test và visual QA.

Bắt buộc test: mèo/YES; mèo/NO; chó/NO; chó/YES; 0 sao; double tap; assisted retry; scene restart; mất focus; audio lỗi; resize lúc mở panel; pointercancel; title không có ảnh đáp án; không có target leak dưới cover; spelling/grammar scissors.

Kết thúc cung cấp danh sách file thay đổi, lệnh build/test thực sự đã chạy, screenshot nếu có thể capture, cách thêm màn mới bằng JSON, và các phần chưa làm/asset còn thiếu. Không khẳng định đã test trên Android/iOS khi chưa chạy thật. Không tự commit, push, deploy hoặc chạy migration production ngoài phạm vi được yêu cầu.
