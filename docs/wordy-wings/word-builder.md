# Word Builder Adventure

## Mở và chơi

Chạy frontend bằng `cd react` rồi `npm run dev`. Mở `/wordy-wings/`, chọn hồ sơ và chọn **Xưởng chữ phiêu lưu → Khám phá 8 nhiệm vụ** trên bản đồ. Chế độ chơi trên thiết bị không cần API hay migration. Chế độ tài khoản vẫn cần backend như trước.

Các màn `letter_puzzle` hiện có tự dùng engine mới qua cấu hình thế giới. Không thay đổi engine bắn mục tiêu. Khu thử có CAT, DOG, SUN, APPLE, BOOK, TRAIN, FISH, STAR; sao được lưu riêng theo thiết bị và hồ sơ, không tác động mở khóa chiến dịch.

Chạm vật thể để mở chữ. Với **collect_letters**, chữ đúng tự bay đến ô phù hợp. Các chế độ **order_letters**, **missing_letter**, **build_to_unlock** cho kéo chữ hoặc chạm chữ rồi chạm vị trí. Xếp sai giữ nguyên những chữ đã đúng; sau hai lần sai có gợi ý, sau ba lần có đường dẫn minh họa. Khi xong, hoạt cảnh chạy khoảng ba giây, đọc chữ → từ → câu ví dụ; nút nhận sao xuất hiện khi cả hoạt cảnh và lời đọc kết thúc.

## Kiến trúc và tệp

- `react/src/game/word-builder/WordBuilderSceneController.ts`: adapter GameMechanic, dùng lại GameContainer và vòng đời scene.
- `WordBuilderManager.ts`: tổ chức thu thập, kéo/thả, chạm, gợi ý, bố cục và teardown.
- `LetterSequenceManager.ts`: logic thuần xác nhận vị trí, ID từng viên và chữ lặp; không phụ thuộc Phaser.
- `LetterToken.ts`, `LetterSlot.ts`, `LetterCollector.ts`: vật thể thế giới, vị trí đích và thu thập. Chỉ chữ rơi tham gia Matter; các chữ giao diện dùng tween.
- `WordBuilderEffects.ts`: chuyển động, particle tái sử dụng, timer và hủy tài nguyên.
- `WordCompletionController.ts`, `CompletionActions.ts`: hoàn thành một lần và tám hành động độc lập.
- `config.ts`, `worlds.json`: schema JSON và profile thế giới. Adapter giữ nguyên dữ liệu chiến dịch cũ.
- `events.ts`: bus nội bộ; cũng phát sự kiện trên scene, không dùng global singleton.
- `react/src/content/word-builder-demos.json`, `react/src/features/word-builder/WordBuilderDemoGallery.tsx`: tám màn và khu thử.
- Các tệp tích hợp được cập nhật: `App.tsx`, `types.ts`, `styles.css`, `game/mechanics.ts`, `game/GameContainer.tsx`, `game/services.ts`, `game/art.ts`.
- `react/tests/word-builder.test.mjs`, `react/tsconfig.word-builder-tests.json`, script package và ignore output: unit test theo yêu cầu tài liệu.

AudioService được bổ sung hàng đợi và đọc tuần tự; các cơ chế cũ giữ giao diện gọi. Campaign tiếp tục dùng AttemptTracker, sao, offline sync và hợp đồng API hiện tại, không thêm endpoint progress song song. Một lượt hoàn thành được gửi qua callback hiện có sau nút nhận sao. Gallery lưu Attempt riêng vào storage.

## Thêm màn bằng JSON

Với một LevelDefinition hiện có, giữ metadata, targetVocabulary và các trường API; chọn `mechanic: "letter_puzzle"` để tương thích admin/backend hiện tại, rồi thêm trường `wordBuilder` dưới đây. Engine frontend cũng nhận alias `word_builder`; không cần dùng alias này cho dữ liệu quản trị hiện tại. `targetVocabulary[0]` phải cùng từ với `targetWord` để thống kê đúng. Có thể thêm cấu hình tương tự vào file demo cùng `id`, `worldId`, `title`.

```json
{
  "mode": "build_to_unlock",
  "targetWord": "APPLE",
  "difficulty": 4,
  "instruction": { "text": "Build the word APPLE." },
  "letters": [
    { "id": "a1", "char": "A", "spawn": "balloon" },
    { "id": "p1", "char": "P", "spawn": "crate" },
    { "id": "p2", "char": "P", "spawn": "platform" },
    { "id": "l1", "char": "L", "spawn": "creature" },
    { "id": "e1", "char": "E", "spawn": "obstacle" }
  ],
  "slots": 5,
  "distractors": ["T"],
  "completionAction": { "type": "cook_food", "target": "apple_pot" },
  "mission": "Tìm chữ để nấu táo cho Momo!",
  "example": "It is an apple!",
  "theme": "kitchen"
}
```

Mỗi viên có ID duy nhất; hai chữ P dùng `p1`, `p2`. ID `distractor_N` dành riêng cho engine. `missing_letter` cần `missingIndices` đếm từ 0; các vị trí còn lại điền sẵn. `ghostLetters` bật chữ gợi ý. Difficulty 5 ẩn hình sau 2,2 giây; có thể đổi `hideMeaningAfterMs`. `meaningAsset: {"type":"image","src":"images/apple.png"}` chọn hình riêng qua AssetResolver. Hiện giọng đọc builder dùng TTS; trường instruction.audio được giữ trong schema để mở rộng nhưng chưa tải clip riêng.

## Mở rộng hành động và sự kiện

Các action hiện có: `open_door`, `build_bridge`, `release_animal`, `start_vehicle`, `grow_plant`, `cook_food`, `power_machine`, `reveal_treasure`.

Thêm tên vào `actionTypes`, viết class theo CompletionAction, đăng ký constructor trong `completionActions`, rồi tham chiếu từ JSON. `target` là định danh semantic gắn vào view của action; mỗi màn hiện có một action view. Nội dung từ không được kiểm tra bằng if/else trong gameplay.

Bus phát: WORD_BUILDER_STARTED, LETTER_COLLECTED, LETTER_PLACED, LETTER_WRONG, WORD_COMPLETED, COMPLETION_ACTION_STARTED, COMPLETION_ACTION_FINISHED, LEVEL_COMPLETED. Payload gồm levelId, word, tokenId, letter, slot, attempt, timestamp.

## Kiểm tra và giới hạn

- `cd react; npm run test:word-builder`: 11 unit test đã đạt, gồm APPLE, BOOK, GREEN, FOOD, thiếu chữ, chữ sai, token đã dùng, ô đầy và sentence units.
- `npm run build:host`: kiểm tra TypeScript, build Vite và copy frontend vào ABP host.
- Không chạy Playwright/kiểm thử trình duyệt tự động. Cần chơi thử thao tác thực tế trên desktop và điện thoại: kéo/thả, chạm hai bước, resize, pause/resume, bật/tắt tiếng, rời màn rồi vào lại, lưu sao và reduced motion.
- Art hiện là SVG/hình vẽ Phaser riêng, chưa có bộ sprite/voice thu âm hoàn chỉnh. Các vật thể môi trường là tương tác chạm, không phải hệ vật lý phá hủy tự do.
- Profile có W01–W10 nhưng không thêm toàn bộ nội dung mười thế giới. Difficulty 6 có nền logic `unit: "word"`; chưa có bộ màn câu hoàn chỉnh.
- Bố cục điện thoại dùng stage dọc và vùng chữ lớn; chưa đo FPS hoặc xác nhận UX trên thiết bị thật. Theme hiện là metadata cho mở rộng art; không phải mỗi theme một bộ scenery riêng.

## Letter Adventure + Bridge Builder

Các màn ghép chữ hiện chia thành hai pha: săn đủ chữ cần thiết → xếp từ để xây/kích hoạt thế giới. Chữ thu được bay vào khay theo thứ tự thu thập, không tự điền vào ô. Ô đích không còn ghost letter; riêng missing_letter vẫn giữ các chữ điền sẵn của dạng bài. Đặt đúng mới hiện chữ; chữ trùng vẫn quản lý bằng ID riêng.

Profile W01 dùng tuần tự balloon, crate, platform, bush, pulley, spring. Vì ORANGE có 6 chữ, O nằm ở bóng, R trong thùng, A trên platform, N trong bụi, G trên pulley, E trên spring. Vị trí các vật thể được xáo trộn, loại tương tác gắn vào ID chữ. Balloon chạm để phóng phi tiêu; crate/obstacle gõ ba lần; platform di chuyển và cần chạm bắt; bush vạch lá bằng hai chạm; pulley kéo xuống hoặc chạm ba lần; spring chạm nén rồi chạm bật. Các spawn cũ vẫn được hỗ trợ.

Bridge có số ván bằng số ô của từ, mỗi vị trí đặt đúng làm hiện một ván. Momo phản ứng trong lúc thu thập/xếp, ra hiệu khi còn một chữ, sau cùng nghe đánh vần → từ hoàn chỉnh rồi chạy qua cầu lấy vật và nhảy vui. ORANGE dùng hình quả cam từ meanings.json, tách khỏi tranh màu cam của màn học màu. Các action khác dùng lại quá trình săn/xếp và phản hồi Momo; không bổ sung bộ màn đường ray/rocket riêng trong lần sửa này.

Bố cục tách tiêu đề/pha, cảnh và đồ vật, khay chữ, ô đích, nút nhận sao. Khi vào pha xếp, các vật thể chưa thu được ẩn đi và cảnh chính về giữa. Chữ dùng texture độ phân giải 2 để giảm nhòe khi phóng to. Mục tiêu nhịp 60–90 giây chưa được đo bằng lượt chơi thực tế; không ép thời gian hoặc giới hạn mạng.
