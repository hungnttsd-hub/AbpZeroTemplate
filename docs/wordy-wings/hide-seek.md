# Hide & Seek — Trốn tìm cùng Momo

Triển khai theo `InitialDocs/WordyWings_HideSeek_Codex_DesignPack_v1/WordyWings_HideSeek_Codex_DesignPack_v1` và master prompt của người dùng. Dùng React 18.3.1, TypeScript 5.7.3, Phaser 3.90.0 và backend ABP 8.3.4/.NET 8 hiện có.

## Mở chơi

```powershell
cd E:\HungNT\WoodyWings\react
npm run dev
```

Mở `http://127.0.0.1:5173/wordy-wings/`, chọn hồ sơ bé → bản đồ → **Trốn tìm cùng Momo · Hide & Seek** → **Vào khu rừng**. Sáu màn HS-01…HS-06 lần lượt tìm cat, dog, tiger, scissors, apple, orange. Chế độ trên thiết bị không cần API hoặc database. Route `#/hide-seek/{childId}` chỉ mở hồ sơ hiện có.

`npm run build:host` build frontend và copy vào `src/WebHoanTien.Web/wwwroot/wordy-wings/`. Khi chạy Web có thể mở `/wordy-wings/index.html`. Tài khoản và đồng bộ cần backend/PostgreSQL theo README chính. Mechanic này tái sử dụng bảng hiện có, **không thêm migration**.

## Gameplay và hình ảnh

- Bắt đầu với 3 sao. Vạch lá, tung lưới, quả mọng mở đá; có lựa chọn chạm thay cho kéo. Sai công cụ, kéo ngắn, đổi công cụ, thời gian và mở vật nhiễu đều không trừ sao.
- Cover và entity là các sprite riêng. Entity tắt hiển thị và không nhận input trước khi VFX kết thúc. Background mới không chứa sẵn vật cần tìm. Cover đóng giống nhau bất kể entity ở phía sau.
- Lưới tác động lên cover, biến mất trước khi entity xuất hiện. Quả mọng tạo hiệu ứng mềm và các mảnh tròn. Cover mở có atlas riêng, không bóp dẹt thân cây/đá để giả trạng thái mở.
- Panel hỏi về **mục tiêu**, dùng duy nhất ảnh entity vừa mở. Chỉ đọc/hiện tên vật nhiễu sau khi trả lời. YES/NO cùng kích thước; khóa thế giới và công cụ, không có nút bỏ qua câu hỏi.
- Mục tiêu + YES hoàn thành; mục tiêu + NO giảm một sao rồi cho trợ giúp; vật nhiễu + NO tiếp tục tìm; vật nhiễu + YES giảm một sao rồi tiếp tục. Mỗi reveal chỉ bị phạt tối đa một lần. 0 sao vẫn hoàn thành được. Lượt YES sau trợ giúp không tính là nhận diện độc lập.
- Momo có chuyển động thở, phản ứng với câu trả lời và đến cạnh mục tiêu khi hoàn thành. Có khoảng chờ cho payoff trước bảng kết quả.
- HUD reflow trên màn ngang nhỏ, nút chính ít nhất 48 CSS px; portrait tự pause. Reduced motion dùng fade. Tên tiếng Anh lấy nguyên câu từ JSON, gồm `Are these scissors?` và `an apple`/`an orange`.
- Giọng đọc dùng `AudioService` hiện có/SpeechSynthesis en-US, nút nghe lại và mute. Không có âm thanh gợi vị trí mục tiêu. Nếu giọng không khả dụng, chữ/hình và gameplay vẫn hoạt động.

## Tệp và trách nhiệm

| Tệp/thư mục | Chức năng |
|---|---|
| `content/wordy-wings/hide-seek/levels.v1.json`, `schema.v1.json` | Nguồn 6 màn, 8 entity và schema |
| `react/scripts/sync-content.mjs`, `react/vite.config.ts` | Đồng bộ và validate trước dev/build |
| `react/src/game/hide-seek/contracts.ts`, `rules.ts` | Hợp đồng dữ liệu và reducer thuần từ pack |
| `content.ts`, `bank.ts` trong cùng thư mục | Zod validation, seeded shuffle và dữ liệu bundle |
| `controller.ts`, `store.ts` | Một nguồn state, checkpoint IndexedDB, queue hoàn thành, đồng bộ |
| `HideSeekScene.ts`, `layout.ts`, `art.ts` | Phaser world, gesture, VFX, audio, sprite atlas, bố cục |
| `react/src/features/hide-seek/` | Lobby, HUD/quiz accessible, pause, kết quả và CSS |
| `react/src/App.tsx`, `react/src/services/repository.ts` | Route, nút trên bản đồ, progress và thống kê phụ huynh |
| `src/WebHoanTien.Domain/WordyWings/HideSeekContent.cs` | Embedded content, shuffle, tính điểm phía server và seed |
| `src/WebHoanTien.Application.Contracts/WordyWings/HideSeekContracts.cs` | DTO và input validation |
| `src/WebHoanTien.Application/WordyWings/HideSeekAppService.cs` | Quyền hồ sơ, completion idempotent, progress và mastery |
| `src/WebHoanTien.HttpApi/Controllers/HideSeekController.cs` | Hai endpoint REST có auth |
| `react/tests/hide-seek*`, `test/WebHoanTien.Domain.Tests/WordyWings/HideSeekScoringTests.cs` | Luật, lưu/khôi phục, audio fallback, layout và tính điểm server |

HUD/quiz dùng React để có focus, bàn phím và semantic controls. Scene Phaser cùng đọc `HideSeekController` qua subscription; không có state luật thứ hai. Cover, entity, tool VFX và Momo được vẽ bằng Phaser. Lifecycle dọn listener ở cả `shutdown` và `destroy`, bao gồm khi React StrictMode/HMR tạo lại scene.

## Lưu và API

Archive IndexedDB theo `owner:hide-seek:childId` (`device` cho local), giữ một phiên đang chơi và các lượt hoàn thành. Checkpoint gồm seed, phase, pending reveal, answer event IDs, sao, thời lượng hoạt động và công cụ. Resize/resume không shuffle. Pause/focus loss khóa input, tween và voice; thời gian pause không cộng duration. Lỗi ghi local giữ state và yêu cầu thử lưu lại trước khi chơi tiếp.

- `POST /api/game/hide-seek/complete`: schema/content version, sessionId, childId, levelId, seed, thời điểm/thời lượng và chuỗi decisions. Không gửi điểm để server tin theo.
- `GET /api/game/hide-seek/children/{childId}/history`: bestStars và completedCount của hồ sơ thuộc người đăng nhập.

Server kiểm tra ownership, grammar/content version qua bank, event/reveal IDs và thứ tự câu trả lời; tự tính sao và độ độc lập. Session ID dùng làm `LevelAttempt.Id` nên retry không nhân số lượt. Các cập nhật tiến trình dùng cùng Unit of Work/child concurrency stamp của ABP. History của HS nằm trong world H01 chưa publish, tách khỏi thứ tự mở khóa 30 màn gốc. Dashboard phụ huynh tính cả HS qua progress/mastery/attempt hiện có; bản đồ 30 màn chỉ tổng hợp campaign.

Queue gửi sau khi đã lưu local, khi online và mỗi 30 giây. Phiên chưa hoàn thành chỉ checkpoint trên thiết bị; không đồng bộ phiên dở giữa các máy. Chưa thêm service worker: tải lại ứng dụng khi mất mạng hoàn toàn vẫn phụ thuộc cache trình duyệt; queue bảo vệ kết quả trong phiên đã tải.

## Thêm màn bằng JSON

1. Sửa **nguồn canonical** `content/wordy-wings/hide-seek/levels.v1.json`, không sửa bản sinh `react/src/content/hide-seek.json`.
2. Sao chép một level; đặt ID HS-07…HS-99 duy nhất, `mechanic: "hide_seek"`, version nội dung, `initialStars: 3`, difficulty 1–3 và 2–6 spots.
3. `targetEntityId` phải có trong entities và xuất hiện đúng một lần trong spots. Mỗi spot có ID riêng, kind được hỗ trợ, ít nhất một allowedTool thuộc tools của level; anchor phải nằm trong design bounds 1600×900.
4. Câu `quest`, `question`, `affirmative`, `negativeIdentification` viết đầy đủ trong entity. Không tự ghép mạo từ.
5. Với 8 entity hiện có, chỉ cần thêm JSON. Entity mới cần thêm frame sprite và tên vào `objectFrames`/atlas; không dùng nhãn chữ làm ảnh thay thế. Đổi `contentVersion` nếu sửa mapping/luật để không chấm sai lượt cũ. Hiện app từ chối resume phiên có version không khớp, không tự xóa dữ liệu đó.
6. Chạy dev/build để sync và validate; khi yêu cầu kiểm thử có thể chạy `npm run test:hide-seek`. Server nhúng cùng JSON vào Domain khi build. Seed chỉ thêm level còn thiếu; rebuild/restart Web để cập nhật bank chấm điểm.

Shuffle dùng xorshift32 + Fisher–Yates cùng thứ tự trên TS/C#. Seed 731 của HS-01 có fixture `cat,dog,scissors,tiger` ở hai bộ test. Bộ demo có footprint chung nên mọi entity tương thích các spot; nếu bổ sung entity lớn cần mở rộng điều kiện tương thích và version ở cả hai phía.

## Runtime art

`react/public/art/hide-seek-forest-v1.png` là background sạch; `hide-seek-covers-v1.png` và `hide-seek-open-covers-v1.png` là atlas 3×2, `hide-seek-objects-v1.png` là atlas 4×2. Các atlas đã kiểm tra alpha thực, không dùng ảnh checkerboard giả hoặc crop concept có nền. Momo tái sử dụng `friends-v1.png`; tool icon/leaf particle là SVG vẽ bằng code trong `art.ts`.

Hình mới tạo bằng **builtin image_gen**. Prompt đầy đủ, đường dẫn nguồn và đích nằm ở [hide-seek-art-prompts.json](hide-seek-art-prompts.json). Những lần sinh ra nền đục bị loại, không đưa vào game. Đây là bộ art mới bám bố cục, chất liệu và màu của pack, không phải bản sao pixel của concept. Chưa có bộ animation skeletal/sprite-sheet Momo riêng hay audio diễn viên thu sẵn; chuyển động dùng tween và giọng hệ thống.

## Kiểm tra

Xem [hide-seek-qa.md](hide-seek-qa.md) để biết lệnh đã chạy, kết quả và phạm vi còn cần kiểm trên thiết bị/tài khoản thật. Không tự commit, push, deploy hoặc chạy migration trong lần triển khai này.
