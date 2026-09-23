# Đuổi hình bắt chữ · Chuông Sao

Màn chơi riêng trên bản đồ Wordy Wings, triển khai từ `InitialDocs/WordyWings_GoldenBell_1000Q_CodexPack_v1/wordy_golden_bell_pack/`.

## Mở màn chơi

```powershell
cd E:\HungNT\WoodyWings\react
npm run dev
```

Mở `http://127.0.0.1:5173/wordy-wings/`, chọn hồ sơ bé → bản đồ → **Khám phá 1.000 câu hỏi**. Chọn **Khám phá tự do** để chơi 12 câu tăng dần độ khó, **Ôn từ đã học** để giới hạn theo vốn từ trong các màn đã hoàn thành, hoặc chọn một mã câu trong khu vườn để luyện cùng mức khó. Nếu đang chạy Vite, tải lại trang sau khi cập nhật code.

Chơi trên thiết bị không cần API, PostgreSQL hay migration. Cần API khi đăng nhập tài khoản và đồng bộ kết quả. Lượt đang chơi giữ trong IndexedDB riêng theo tài khoản/hồ sơ; nút **Tiếp tục** khôi phục đúng câu, thứ tự thẻ, bộ 12 câu và trạng thái ghi nhớ. Xóa dữ liệu trình duyệt sẽ xóa các lượt chưa đồng bộ. Chưa hỗ trợ chuyển lượt đang chơi dở sang thiết bị khác.

## Nội dung và cách chơi

- Đúng **1.000 câu**, mã `GB-0001`–`GB-1000`; 10 mức khó, mỗi mức 100 câu; 25 dạng câu và 5 mechanic trong gói nguồn.
- Profile khám phá: `1, 2, 2, 3, 4, 5, 6, 7, 8, 8, 9, 10`. Bộ chọn tránh lặp câu, tránh ba dạng giống nhau liên tiếp, tránh hai câu ghi nhớ liền nhau và tránh lặp từ nội dung trong hai câu gần nhất. Mỗi bốn vị trí ưu tiên một câu từ cần ôn khi có câu phù hợp; điều chỉnh câu chưa chơi theo chuỗi đúng/sai.
- Các dạng bao gồm nghe–chọn hình, màu sắc, hình khối, đếm, so sánh, vị trí đồ vật, hai thuộc tính, chức năng, ghép cặp, chữ thiếu, hoàn thành/xếp câu, truyện ngắn, ghi nhớ và suy luận.
- Đáp án hình không in sẵn tên từ. Câu nhìn hình đoán chữ và câu ghép chữ hiển thị chữ theo đúng mục tiêu học.
- Ghi nhớ: xem cảnh 4,5 giây → che cảnh → trả lời; gợi ý cho xem lại tối đa một lần. Hai bước yêu cầu thực hiện đúng hai lựa chọn theo thứ tự. Xếp câu hỗ trợ cả chạm và kéo, kể cả các thẻ có cùng một từ. Chữ thiếu xử lý vị trí không theo thứ tự và từ có chữ lặp.
- Sai không mất mạng hoặc mất lượt; được thử lại trong thời gian còn lại. Gợi ý theo ngưỡng sai của câu hoặc sau 60% thời gian (tối đa 14–18 giây); sai từ lần thứ ba mới chỉ rõ lựa chọn. Nghe lại không đặt lại đồng hồ.
- Câu 4/8 có checkpoint 2,2 giây. Đi qua đủ 12 câu (đúng hoặc hết giờ) mới mở chuông; bé phải chạm chuông, nút rung hoặc kéo dây. Ghi thưởng bền vững trước hiệu ứng; một lượt chỉ nhận một Bell Token. Câu hết giờ không được tính là câu trả lời đúng hay nhận Word Star.
- Phím `1–4` chọn đáp án. Xếp câu dùng `1–7`, `Enter` để ghép, `Backspace` bỏ thẻ cuối. Có pause, mute, tự pause khi ẩn tab và giảm chuyển động theo thiết bị.

## Điểm và thời gian từng câu — cập nhật 23/09/2026

Lượt mới dùng `scoringVersion: 1`. Thời gian câu = `ceil((estimatedSeconds + difficulty) / 5) * 5` giây, giới hạn trong 15–60 giây. Đồng hồ hiện thời gian còn lại/tổng thời gian, thanh tiến độ và cảnh báo màu cam khi còn 5 giây. Đếm thời gian trả lời sau phần nghe ban đầu; câu ghi nhớ đếm sau khi che hình và đọc câu hỏi. Pause, tab ẩn và lúc xem lại cảnh ghi nhớ theo gợi ý tạm dừng đồng hồ. Nghe lại thông thường, chọn sai, chuyển thứ tự thẻ hoặc dùng gợi ý không đặt lại thời gian.

- Đúng trước hạn: **100 điểm**, cộng `floor(50 * thời_gian_còn / thời_gian_câu)` điểm tốc độ.
- Mỗi lựa chọn sai trước khi trả lời đúng giảm **25 điểm** của câu. Dùng gợi ý giảm **20 điểm** một lần/câu. Câu đã trả lời đúng nhận ít nhất **20 điểm**.
- Hết thời gian: khóa đáp án, **0 điểm**, hiện đáp án đúng trong 3,2 giây rồi chuyển câu. Câu hết giờ được đưa vào nội dung cần ôn và không được tính là đã trả lời đúng.
- Tối đa **150 điểm/câu, 1.800 điểm/lượt**. Kết quả hiển thị tổng điểm, điểm từng câu, số đúng và số hết giờ; lobby hiển thị kỷ lục.
- Thời gian trả lời và điểm được lưu cùng lượt, lưu thời gian mỗi giây và khi pause/thoát. Quay lại tiếp tục thời gian còn lại. Khi đóng tab đột ngột, tối đa khoảng một giây chưa kịp ghi có thể bị mất.
- Các lượt được lưu từ phiên bản trước giữ luật cũ và vẫn đồng bộ được; không tính điểm ngược cho lượt đã chơi. **Bắt đầu lượt mới** để dùng đồng hồ và tính điểm.

Frontend và backend cùng công thức trong `model.ts` và `GoldenBellScoring.cs`. API tự tính điểm từ đáp án, số lần sai, hint và thời gian trả lời đã ghi; không nhận điểm tự khai từ client. Input `{type:"timeout",value:null}` chỉ được chấp nhận khi đạt giới hạn thời gian. Retry cùng ID không cộng điểm/thưởng lần nữa. Migration `AddGoldenBellScoring` thêm `ScoringVersion`, `Score`, `TimedOutCount` cho session và `AnswerMs`, `Points`, `TimedOut` cho attempt; dữ liệu cũ mặc định bằng 0/false.

Migration `20260923095701_AddGoldenBellScoring` đã được áp dụng thành công trên database local `WordyWings` (`localhost:5433`) ngày 23/09/2026. Frontend và backend đã build thành công; chưa chạy test hoặc kiểm thử trình duyệt theo chỉ dẫn dự án. Tải lại trang và mở lượt mới; nếu đang chạy API, khởi động lại backend để nạp phần chấm điểm mới.

## Ngân hàng câu hỏi

```powershell
cd E:\HungNT\WoodyWings\react
node scripts/import-golden-bell.mjs
```

Import cũng tự chạy trước `npm run dev` và build. Script đọc gói canonical, kiểm tra đầy đủ mã, phân phối độ khó, dạng câu, tham chiếu đáp án, chữ thiếu, hai bước và multiset của các thẻ xếp câu, rồi mới xuất dữ liệu. Trường authoring `_correct` bị bỏ. File không đổi không bị ghi lại.

- Frontend: `react/public/content/golden-bell/v1/manifest.json` và 1.000 payload riêng. Lobby chỉ đọc manifest; khi bắt đầu mới tải 12 payload và các hình của lượt đó.
- Backend: `content/wordy-wings/golden-bell/questions.v1.json`, được nhúng trong Domain assembly. Phiên bản hiện tại: `1.0-a20392c7eef58a63`.
- Registry renderer: `react/src/game/golden-bell/renderers.ts`; model/Zod: `model.ts`; bộ chọn: `bank.ts`; persistence/sync: `store.ts`.
- Nội dung và đáp án giữ theo gói tác giả. Một số câu nguồn có cách diễn đạt như “a apple”; cần sửa trong canonical và import lại nếu biên tập ngôn ngữ, không sửa rời file đã xuất.

## Backend và migration

Migration `20260922115044_AddGoldenBell` thêm ba bảng `wordy.GoldenBellQuestions`, `GoldenBellSessions`, `GoldenBellAttempts`. Không thay schema của các mechanic trước.

Ngày 22/09/2026 đã áp dụng migration và seed thành công đủ 1.000 câu trên database phát triển `WordyWings` ở `localhost:5433`. Các môi trường khác chạy lệnh bên dưới với cấu hình database của môi trường đó.

```powershell
cd E:\HungNT\WoodyWings\src\WebHoanTien.DbMigrator
dotnet run
```

Seeder kiểm tra toàn bộ 1.000 câu trước khi ghi, chỉ thêm mã còn thiếu khi khởi tạo. Admin import cập nhật theo mã trong một transaction. Phiên chơi giữ bản chụp câu hỏi để lần import sau không đổi đáp án của lượt đang lưu.

```powershell
cd E:\HungNT\WoodyWings\react
npm run build:host
cd E:\HungNT\WoodyWings\src\WebHoanTien.Web
dotnet run
```

API cùng origin, dùng cookie/CSRF hiện có:

| Endpoint dưới `/api/game/golden-bell` | Chức năng |
|---|---|
| `POST session/start` | Tạo/khôi phục idempotent, chụp 12 câu canonical theo mã hoặc chọn theo seed |
| `GET session/{id}` | Đọc bộ câu và tiến trình đã gửi lên máy chủ |
| `POST session/{id}/answer` | Chấm input theo snapshot, ghi lần thử/hint/thời lượng; retry cùng ID không ghi trùng |
| `POST session/{id}/complete` | Kiểm tra đủ 12 câu và hành động rung chuông; cập nhật mastery một lần |
| `GET children/{childId}/history` | Câu hoàn thành, token, số lượt, phút đã đồng bộ |
| `GET questions/{code}` | Xem payload; chỉ role `admin` |
| `POST admin/import` | Nhận toàn bộ JSON ngân hàng đã chuẩn hóa; chỉ role `admin` |

Mọi endpoint của bé đều kiểm tra chủ sở hữu hồ sơ. Máy chủ tự chấm lại đáp án, không tin trường `correct` từ client. Ràng buộc unique + concurrency của session chống ghi trùng. Frontend lưu ngay trên máy; sau khi rung chuông gửi lần lượt session/attempt/complete và thử lại khi trở về lobby hoặc có mạng. Bộ câu đã điều chỉnh được gửi sau khi hoàn thành để snapshot server khớp đúng lượt thực tế. Token và danh sách câu đã hoàn thành được hợp nhất với lịch sử tài khoản.

## Đồ họa

Nền Đảo Chuông, chuông vàng khung gỗ, sân khấu đá kem, thẻ gỗ bo tròn, Pip/Momo/Lulu/Poki từ atlas hiện có; thêm Foxy và tranh đồ vật/hành động. Hình vector tự vẽ kế thừa hệ minh họa của game, giữ màu/khối và vị trí có ý nghĩa cho đáp án. Bộ xuất tranh đã render được **434 tổ hợp asset/màu** sử dụng trong 1.000 câu, không có asset thiếu hoặc placeholder dấu tick.

Asset nền: [`golden-bell-island-v1.png`](../../react/public/art/golden-bell-island-v1.png). Được tạo bằng **image_gen tích hợp**, sau đó chép nguyên ảnh vào workspace; không dùng CLI/API. Chuông và tranh từ vựng là SVG trong `art.ts` / `vocabularyArt.ts`.

Prompt tạo nền:

> Create an original polished children's English adventure game background for Wordy Wings, Golden Bell Island. Wide 16:9 landscape. Sunny soft painterly 2.5D illustration, luminous turquoise sky, soft cream clouds, lush mint and sage plants, warm honey wood, peach and lilac flowers, distant floating green islands and tiny stone paths. A charming open-air garden amphitheater: elegant curved wooden terrace across the lower quarter, low cream stone balustrades curving at the extreme edges, soft flowering trees only at the far left and right corners, a tiny whimsical cream tower on a distant island at the far upper right. Gentle golden morning light, rich appealing tactile materials and rounded forms, picture-book quality consistent with a polished family learning game. IMPORTANT GAMEPLAY COMPOSITION: center 75 percent is very open and low-detail sky and distant pastel scenery, nothing large in center; foreground platform smooth and uncluttered. NO characters, NO bell, NO text, NO letters, NO UI, NO buttons, NO watermark. Opaque full-bleed background, clean high-quality illustration, not a screenshot or mockup.

## Xác minh

Đã build TypeScript/Vite và .NET 8; import xác nhận đủ 1.000 câu/25 dạng/100 câu mỗi mức. Đã render và xem sheet tranh vector tĩnh. Chưa chạy test, Playwright hoặc kiểm thử trình duyệt tự động theo `AGENTS.md`. Hành vi tương tác thực tế trên chuột/cảm ứng, audio từng trình duyệt và đồng bộ tài khoản vẫn cần kiểm thử khi được yêu cầu.
