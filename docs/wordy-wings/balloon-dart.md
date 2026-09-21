# Balloon Dart — Vườn bóng phi tiêu

Triển khai theo `InitialDocs/WordyWings_BalloonDart_DesignPack_v1/WordyWings_BalloonDart_DesignPack_v1`. Các màn chiến dịch `balloon_pop` độc lập (gồm W01-L08) tự dùng engine mới; giữ ID, mở khóa và dữ liệu đã lưu. Màn bắn ná và ghép chữ giữ nguyên. Minigame thẻ bóng trong boss cũ vẫn giữ cơ chế cũ để không đổi chuỗi boss.

## Chạy và chơi

1. Trong `react`, chạy `npm run dev` rồi mở `/wordy-wings/` ở địa chỉ Vite in ra.
2. Chọn hồ sơ → bản đồ → **Vườn bóng phi tiêu → Khám phá 8 màn bóng**. Tám cấu hình BD-01–BD-08 của Design Pack đã được đưa vào gallery.
3. Hoặc vào một màn bóng bay đã mở khóa trong chiến dịch.
4. Di chuột/kéo để ngắm, thả để bắn. Chạm bóng để launcher xoay rồi bắn khi inputMode cho phép. Bàn phím: trái/phải chỉnh góc, Space bắn; 1–5 chọn bóng theo thứ tự trái sang phải ở chế độ tap/both.
5. Điện thoại cần nằm ngang. Xoay dọc sẽ tạm dừng và hiện hướng dẫn; xoay ngang rồi chọn Chơi tiếp.

Chơi trên thiết bị không cần API. Tài khoản online cần khởi động lại backend đã build để nhận trường tổng kết và quy tắc 3 sao mới. Không cần migration: summary lưu trong PayloadJson đã có. Gallery lưu riêng trên thiết bị, không gửi tám màn thử vào API chiến dịch.

## Cơ chế

- Ba lượt mỗi màn, 3–5 bóng mỗi lượt. Ba lượt đúng luôn được ba sao; lỗi không làm giảm sao.
- Một phi tiêu hoạt động, tốc độ 960 px/s, cooldown 280–340 ms, không giới hạn đạn/thời gian/mạng.
- Bóng sai bật lại và đọc tên; bóng vẫn còn. Bắn trượt không chuyển lượt. Chỉ đáp án khớp toàn bộ thuộc tính mới nổ và chuyển lượt một lần.
- Lỗi 2: đọc lại đề. Lỗi 3: halo 1,2 giây mỗi chu kỳ 2,5 giây. Lỗi 5: đường gợi ý hướng chung. Không có halo lúc đầu, không tự chọn đáp án.
- Không bắn 8 giây: đọc lại một lần. Sau 15 giây: lời hướng dẫn nhẹ và mascot ra hiệu.
- Reduced motion giảm biên độ đường bay 35%, bỏ burst lớn và dùng fade. Không rung màn hình.
- Giọng đọc qua AudioService, có hàng đợi và fallback TTS; SFX nhẹ tổng hợp bằng Web Audio. Key `instruction.foo` ánh xạ tới `audio/voice/instructions/foo.mp3`; `word.foo` tới `audio/voice/words/foo.mp3` khi cấu hình asset base. Chưa có nhạc nền hoặc bộ thu âm production trong repo.

## Kiến trúc và tệp

- `react/src/game/balloon-dart/model.ts`: Zod schema, khớp semantic, tiến trình ba lượt và adapter JSON chiến dịch cũ.
- `motion.ts`: năm kiểu đường bay xác định theo seed, clamp góc và kiểm tra đoạn chuyển động cắt ellipse để tránh phi tiêu đi xuyên bóng ở frame chậm.
- `visuals.ts`: BalloonTarget, BalloonManager, Launcher, DartPool, phong cảnh nhiều lớp. Matter không trọng lực; body sensor bóng và phi tiêu. Vị trí do đường bay quản lý; sweep tương đối bổ sung collision cho vật thể di chuyển nhanh. Pool một phi tiêu và năm hạt trail.
- `BalloonDartController.ts`: input, HUD sao, vòng chơi, hint, phản hồi và event scene. Cleanup listener/timer/tween/body khi rời màn. Tái sử dụng bộ timer/tween/particle giới hạn của WordBuilderEffects, không thay gameplay ghép chữ.
- `react/src/content/balloon-dart-demos.json`: tám màn từ Design Pack.
- `react/src/features/balloon-dart/BalloonDartGallery.tsx`: khu chơi thử và storage riêng.
- Tệp tích hợp: `types.ts`, `App.tsx`, `styles.css`, `game/GameContainer.tsx`, `game/mechanics.ts`, `game/services.ts`, `game/art.ts`, `vite.config.ts`, `package.json`, `.gitignore`.
- Backend: `GameContracts.cs`, `WordyGameAppService.cs` thêm summary tùy chọn, xác nhận 3 lượt, đối chiếu shots/wrongHits/misses và chấm 3 sao. Các attempt cũ không có summary vẫn theo quy tắc cũ.

React giữ shell, profile, pause, replay, loading/error và storage. Phaser giữ toàn bộ vật thể chuyển động. Không gửi API theo từng lần bắn. AttemptTracker đính kèm `balloonDart` vào một lượt hoàn thành; repository ghi bền vững trên thiết bị trước rồi sync qua endpoint hiện có.

Event trên scene có prefix `BALLOON_DART_`: LEVEL_STARTED, ROUND_STARTED, DART_FIRED, WRONG_HIT, MISS, HINT_SHOWN, CORRECT_HIT, ROUND_COMPLETED, LEVEL_COMPLETED. Payload chứa levelId/roundId, thời điểm, semantic/instance, số lượt bắn và tổng kết tùy loại. Không dùng bus global.

## Thêm màn JSON

Copy một object trong `balloon-dart-demos.json`, đổi id/title và nội dung ba rounds. Gallery tự tạo card, không phải sửa logic. Mỗi round phải có 3–5 instanceId duy nhất và **đúng một** semantic khớp target. Vite xác thực tám demo và adapter chiến dịch khi dev/build; runtime cũng xác thực nội dung API.

Ví dụ một round màu + hình (đặt trong `rounds` của config, đủ tổng cộng ba rounds):

```json
{
  "id": "MY-LEVEL-R1",
  "instructionText": "Pop the blue circle.",
  "instructionAudioKey": "instruction.pop_the_blue_circle",
  "target": { "requiredShape": "circle", "requiredColor": "blue" },
  "aimAssist": "long",
  "seed": 42,
  "balloons": [
    { "instanceId": "a", "semantic": { "id": "circle", "word": "circle", "shape": "circle", "color": "blue" }, "movement": { "pattern": "gentleBob" }, "initialPosition": { "x": 0.25, "y": 0.4 }, "labelMode": "hidden" },
    { "instanceId": "b", "semantic": { "id": "circle", "word": "circle", "shape": "circle", "color": "red" }, "movement": { "pattern": "driftVertical" }, "initialPosition": { "x": 0.5, "y": 0.4 }, "labelMode": "hidden" },
    { "instanceId": "c", "semantic": { "id": "star", "word": "star", "shape": "star", "color": "blue" }, "movement": { "pattern": "driftHorizontal" }, "initialPosition": { "x": 0.75, "y": 0.4 }, "labelMode": "hidden" }
  ]
}
```

Level config có `mechanic: "balloon_dart"`, `difficulty: 1..5`, `learningMode`, `inputMode: "drag"|"tap"|"both"`, `rounds`, `reward: {"stars":3}`. Các learningMode được hỗ trợ: pictureListening, wordRecognition, colorRecognition, compoundListening, sizeAndShape.

Đưa vào chiến dịch: giữ cấu trúc LevelDefinition hiện có, thêm config đầy đủ dưới `balloonDart`, chọn mechanic balloon_dart (hoặc alias balloon_pop), giữ targetVocabulary phù hợp với các từ đúng; đưa từ còn lại vào reviewVocabulary. Outer difficulty vẫn 1–4 theo hợp đồng chiến dịch, inner difficulty hỗ trợ 5. Nội dung campaign nguồn nằm ở `InitialDocs/wordy_wings_gdd/data/mvp_levels_30.json`, được sync vào frontend khi build. Không sửa riêng file generated levels.json. Backend content mới cần quy trình seed/publish của dự án; JSON nhập từ tài khoản được validate khi mở màn.

## Mở rộng

- Đường bay mới: thêm tên vào movementSchema, thêm chiến lược trong positionAt. Giữ tọa độ giới hạn playfield và hàm thuần xác định theo seed; BalloonManager áp dụng khoảng cách chống chồng lấn. Các kiểu hiện tại: gentleBob, driftHorizontal, driftVertical, figureEight, crossLane. CrossLane dùng ping-pong liền mạch thay vì teleport ở biên.
- Từ/hình mới: thêm semantic dữ liệu, `imageKey` là đường dẫn asset hoặc bổ sung SVG trong art.ts. Không cần nhánh theo từ trong engine.
- Thuộc tính semantic mới: mở rộng semanticSchema/ruleSchema và matches, sau đó thêm cách hiển thị thuộc tính nếu cần. Tất cả trường target hoạt động theo phép AND.

## Kiểm tra và giới hạn

Frontend `npm run build:host` và backend `dotnet build src/WebHoanTien.Application/WebHoanTien.Application.csproj --no-restore` đã build thành công. Có cảnh báo kích thước chunk Phaser lớn của Vite.

Đã thêm `tests/balloon-dart.test.mjs`, `tsconfig.balloon-dart-tests.json` và script `npm run test:balloon-dart` cho validation, compound target, trạng thái lượt/sao, sweep, góc bắn và motion theo seed. **Chưa chạy unit test**, tuân theo AGENTS.md. Không chạy Playwright/kiểm thử trình duyệt tự động.

Chơi thử thủ công theo `qa/test_matrix.md`: 1440×810, 1280×720, 844×390; bắn đúng/sai/trượt, nhiều ngón, thả ngoài canvas, restart 10 lần, pause/đổi tab, xoay máy, tắt tiếng, replay, reduced motion, offline rồi sync. Chưa xác nhận touch thật, cleanup qua profiler hoặc FPS desktop/mobile.

Art là SVG và hình vẽ Phaser gốc, không phải bản bitmap hoàn thiện của concept. Path an toàn và spacing có thể thu hẹp amplitude cấu hình để tránh che đáp án. Gallery dùng TTS khi chưa có asset voice; không phụ thuộc mạng để chơi sau khi tải nội dung. Prototype không có ambient/music hoặc bộ animation mascot nhiều sprite.

## Cập nhật hiển thị bóng

Theo yêu cầu mới, bóng chỉ hiển thị hình minh họa, không có nhãn chữ hoặc chữ thay thế. Hình được căn giữa bóng. Các trường labelMode và learningMode=wordRecognition vẫn được nhận để tương thích JSON cũ, nhưng không bật nhãn chữ; wordRecognition dùng hình. Bóng nhận diện màu giữ màu thuần. Hướng dẫn và giọng đọc vẫn hoạt động.
