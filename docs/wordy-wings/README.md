# Wordy Wings

Game học tiếng Anh cho trẻ 5–7 tuổi, triển khai theo `InitialDocs/wordy_wings_gdd/README.md` và đặc tả 30 màn. Giữ nguyên ABP 8.3.4 / .NET 8 / PostgreSQL của repository; bổ sung React, TypeScript strict và Phaser trong `react/`.

## Chạy game ngay, chưa cần database

Yêu cầu Node.js 18.18+ (khuyến nghị Node 22).

```powershell
cd E:\HungNT\WoodyWings\react
npm ci
npm run dev
```

Mở **http://127.0.0.1:5173/wordy-wings/**, chọn **Bắt đầu khám phá**, tạo biệt danh rồi chọn màn 1. Chế độ này lưu hồ sơ, sao, lượt chơi và báo cáo trong IndexedDB của trình duyệt. Không giả lập việc lưu lên máy chủ. Xóa dữ liệu site sẽ xóa tiến trình; tiến trình khách không tự chuyển sang tài khoản khác.

Chế độ local không gọi `/api/game/session` khi khởi động. API chỉ cần cho tài khoản và đồng bộ. Liên kết đăng nhập/đăng ký trả về `index.html?account=1` để yêu cầu kiểm tra session; nếu truy cập trực tiếp bằng tài khoản, có thể dùng URL này.

Nếu chỉ thấy nền trống với dòng “Find the red”, cập nhật code rồi tải lại trang: loader SVG của Phaser yêu cầu Base64 cho `data:` URL, khác với ảnh HTML có thể dùng URL-encoded SVG. Đã sửa riêng texture Phaser sang Base64 và thêm trạng thái tải/lỗi có nút mở lại màn.

## Chạy với tài khoản và backend

1. Cấu hình PostgreSQL / `ConnectionStrings:Default` cho Web và DbMigrator theo README gốc. Dùng database phát triển riêng. Không đưa mật khẩu vào source control.
2. Chạy DbMigrator để tạo bảng `wordy.*`, seed 3 world, 30 level, vocabulary và 1.000 câu Chuông Sao. Contributor chỉ thêm dữ liệu còn thiếu, không ghi đè nội dung admin đã chỉnh.
3. Build frontend vào static files của backend rồi chạy Web.

```powershell
cd E:\HungNT\WoodyWings\src\WebHoanTien.DbMigrator
dotnet run
```

```powershell
cd E:\HungNT\WoodyWings\react
npm ci
npm run build:host
```

```powershell
cd E:\HungNT\WoodyWings\src\WebHoanTien.Web
dotnet run
```

Mở **https://localhost:44433/wordy-wings/index.html**. Đăng ký/đăng nhập sử dụng ABP Identity/OpenIddict và trang Account hiện có. Cookie cùng origin, CSRF token lấy từ `/api/game/session`. Giao diện Account vẫn là của ứng dụng gốc; game không tạo cơ chế mật khẩu riêng. Nếu tài khoản gốc yêu cầu xác nhận điều khoản hoặc đổi mật khẩu admin, hoàn tất bước đó trước.

Dev frontend đã proxy `/api`, `/Account`, `/Legal`, `/connect`, `/Abp`, `/libs`, `/__bundles` tới `https://localhost:44433`. Có thể đổi bằng biến môi trường `WORDY_API_URL` trước khi chạy Vite. Cách chạy cùng host bên trên là đường tích hợp auth chuẩn; cấu hình reverse proxy/asset của giao diện Account gốc có thể cần bổ sung khi phát triển trên hai origin.

## Đã triển khai

- **Đuổi hình bắt chữ · Chuông Sao**: đủ 1.000 câu, 25 dạng, 10 mức khó; 12 câu/lượt, ghi nhớ, xếp câu, hai bước và rung chuông nhận thưởng. Mở từ bản đồ; chơi trên thiết bị không cần backend. Chi tiết và migration ở [golden-bell.md](golden-bell.md).
- Hồ sơ bé với nickname, avatar Pip/Poki/Lulu/Momo và nhóm tuổi.
- Bản đồ Rainbow Valley, Animal Island, Happy Home, mỗi world 10 màn.
- Word Shot: kéo ná chỉnh góc/lực, xem quỹ đạo vật lý rồi thả; di chuyển Pip và chọn 4 loại đạn. Phải giải cứu toàn bộ mục tiêu trước khi hết đạn; có thể thử lại ngay. Chi tiết ở `word-shot.md`.
- Balloon Pop: chạm các bóng nổi; Drag & Sort: kéo vật hoặc chạm vật rồi chạm giỏ.
- Letter Puzzle: ghép các chữ được xáo trộn, có mẫu từ và hình.
- Boss: 4 micro-rounds dùng lại Word Shot, Balloon Pop và Drag & Sort; sai không reset vòng trước.
- Với Word Shot: 1–4 chọn đạn, A/D di chuyển, trái/phải chỉnh góc, lên/xuống chỉnh lực, Space bắn. Các mechanic khác dùng phím 1–6 chọn mục tiêu/chữ từ trái sang phải. Khu vực phụ huynh có câu hỏi xác nhận người lớn.
- Audio dùng asset CDN khi cấu hình; nếu thiếu thì fallback SpeechSynthesis `en-US`. Có nút nghe lại, mute được lưu.
- Sai có lời nhắc nhẹ; từ lần sai thứ hai viền vàng chỉ mục tiêu. Không mạng, energy hay đếm ngược thất bại. Word Shot giới hạn đạn theo yêu cầu gameplay mới; hết đạn cho phép thử lại cùng bố cục.
- 1 sao khi hoàn thành; 2 sao nếu sai không quá một lần; 3 sao khi không sai và không dùng hint. Backend tính sao; chơi lại chỉ cải thiện best stars.
- Màn trước mở màn kế tiếp; L10 mở world tiếp theo. Một sao vẫn mở được hành trình.
- Pause/resume/restart/exit; tự pause khi tab bị ẩn. Nhắc nghỉ nhẹ sau khoảng 15 phút.
- Ghi attempt vào IndexedDB trước khi hiện kết quả. Cloud sync theo thứ tự màn, retry khi online và định kỳ 30 giây. ID do client sinh giúp server xử lý idempotent. Queue được tách theo parent user; không gửi queue sang tài khoản khác.
- Dashboard: màn hoàn thành, sao tốt nhất, thời gian, các từ đã gặp và danh sách cần ôn. Khi offline có bản chụp gần nhất; số liệu cloud bao gồm lượt đã đồng bộ.
- Admin role `admin`: lọc 30 màn theo world, sửa instruction/difficulty/mechanic/publish flag. Sửa nội dung tăng `contentVersion`; audio instruction cũ được vô hiệu hóa.
- Hai mechanic tương lai chỉ có placeholder registry: `rescue_mission`, `adventure_commands`.

## Các thư mục chính

| Đường dẫn | Vai trò |
|---|---|
| `react/src/App.tsx` | Hồ sơ, bản đồ, phụ huynh, kết quả, admin |
| `react/src/game/GameContainer.tsx` | Vòng đời Phaser, pause và React bridge |
| `react/src/game/mechanics.ts` | 5 mechanic và registry |
| `react/src/game/word-shot/` | Vật lý, địa hình, loại đạn, luật độ khó và tương tác Word Shot |
| `react/src/game/art.ts` | Vector minh họa từ vựng tự tạo, thay thế qua asset key |
| `react/src/services/` | HTTP, IndexedDB, queue và đồng bộ |
| `react/src/types.ts` | Zod runtime validation, types, sao và unlock |
| `src/WebHoanTien.Domain/WordyWings/` | Entities và content seeder |
| `src/WebHoanTien.Application/WordyWings/` | Ownership, validation, tiến trình và mastery |
| `src/WebHoanTien.HttpApi/Controllers/WordyGameController.cs` | REST API |
| `src/WebHoanTien.EntityFrameworkCore/Migrations/*AddWordyWings*` | Migration schema PostgreSQL |

`react/src/content/*.json` được đồng bộ từ bộ GDD trước mỗi dev/build. Nội dung cloud đọc API và cache theo account, không bị bản JSON bundle ghi đè. Level được validate trước khi tạo Phaser. Asset vector được preload theo từ trong level; ảnh CDN tải lỗi vẫn giữ ảnh vector. Không upload file vào filesystem Render.

## Biến môi trường frontend

`VITE_ASSET_BASE_URL`: URL CDN tùy chọn, ví dụ `https://assets.example.com`. Không đặt secret trong biến `VITE_*`.

## Build và trạng thái xác minh

```powershell
cd react
npm run build
```

```powershell
dotnet build src/WebHoanTien.Web/WebHoanTien.Web.csproj --no-restore -p:SkipClientAssets=true
```

Đã biên dịch frontend production và backend. Ngày 21/09/2026 đã sửa thiếu DbSet để ABP đăng ký repository Wordy Wings; đã chạy migrator và seed thành công trên database local `WordyWings`, cổng `5433`. Chưa chạy test, Playwright hoặc tương tác trình duyệt tự động, theo chỉ dẫn dự án. Acceptance runtime/auth/save/sync vẫn cần xác nhận khi bạn yêu cầu kiểm thử. Bản Phaser được lazy-load riêng; Vite có cảnh báo chunk engine lớn (~1.48 MB trước gzip), không phải lỗi build.

Âm thanh hiện dựa vào voice của thiết bị khi không có CDN. Trước khi phát hành cho trẻ, nên bổ sung bộ audio đã duyệt, kiểm tra cảm ứng/landscape và xác minh luồng auth/save/sync trên database thực. App shell chưa có service worker riêng: reload khi hoàn toàn không có mạng cần trình duyệt đã giữ được tài nguyên hoặc bản đóng gói local; hàng đợi bảo vệ các lượt hoàn thành trong phiên đang mở.

Deploy: xem `deployment.md`. Không có deployment nào được thực hiện tự động trong lần triển khai này.

## Word Builder Adventure

Các màn ghép chữ đã dùng engine phiêu lưu. Trên bản đồ, mở **Xưởng chữ phiêu lưu** để chơi 8 màn mẫu. Xem [cơ chế, JSON, tệp triển khai và kiểm tra](word-builder.md).

## Balloon Dart

Màn bóng bay độc lập đã chuyển sang bóng di chuyển và phi tiêu đồ chơi. Mở **Vườn bóng phi tiêu** trên bản đồ để chơi 8 màn mẫu. Xem [triển khai, JSON và kiểm tra](balloon-dart.md).
