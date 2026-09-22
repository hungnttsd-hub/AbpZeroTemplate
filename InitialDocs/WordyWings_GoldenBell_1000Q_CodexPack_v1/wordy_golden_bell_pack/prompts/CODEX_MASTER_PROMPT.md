# CODEX MASTER PROMPT — Add Golden Bell Challenge

Bạn đang làm project Wordy Wings hiện tại với ABP.io + React + TypeScript + Phaser + PostgreSQL. Hãy thêm game mode **Golden Bell Challenge** mà không rewrite architecture hiện tại.

## Ground truth files
Đọc trước:
- `README.md`
- `docs/01_GAME_MODE_GDD.md`
- `docs/02_DIFFICULTY_CONTENT_SYSTEM.md`
- `docs/03_QUESTION_RENDERERS.md`
- `backend/ABP_BACKEND_SPEC.md`
- `frontend/PHASER_IMPLEMENTATION_SPEC.md`
- `data/question_schema.json`
- `data/golden_bell_questions_1000.json`
- `qa/ACCEPTANCE_CRITERIA.md`

## Goal
Tạo một boss/review stage 12 câu, difficulty tăng dần, nhiều renderer khác nhau, kết thúc bằng việc bé trực tiếp rung Star Bell. Sai không loại khỏi game.

## Trước khi code
1. Inspect codebase hiện tại.
2. Tìm GameContainer, Phaser bootstrap, SceneManager, AudioManager, progress API, ABP application services, localization và asset loader hiện tại.
3. Liệt kê file sẽ reuse/refactor/new.
4. Không đổi route/auth/progress hiện tại nếu không cần.

## Backend
Implement entity + application service tối thiểu theo `ABP_BACKEND_SPEC.md`. Tạo importer/upsert từ JSON. Thêm selector 12 câu với deterministic seed, difficulty profile và anti-repeat rules. Nếu MVP chưa cần lưu toàn bộ questions trong DB, vẫn tạo interface repository để sau này đổi từ JSON sang DB mà không thay frontend contract.

## Frontend/Phaser
Implement `GoldenBellArenaScene` + renderer registry. Bắt đầu với các renderer tối thiểu:
- listen_find_picture / picture_to_word
- color_object / two_attribute_object
- count_objects
- preposition_scene / multi_clue_scene
- missing_letter
- two_step_instruction
- short_story
- memory_scene
- sentence_order
- word_picture_mismatch

Không tạo scene riêng cho từng question.

## UX
- 16:9 landscape, responsive mobile.
- Wordy Wings visual language hiện có.
- Q1–Q12 tăng khó rõ ràng.
- Đúng: star bay vào bell.
- Sai: shake nhẹ, `Almost!`, không Game Over.
- Hint sau 2 lần sai hoặc timeout mềm.
- Q4/Q8 có checkpoint animation <= 3s.
- Q12 xong: user phải kéo dây/chạm búa để rung chuông; không auto-finish.

## Data
Canonical bank là `data/golden_bell_questions_1000.json`. Không hard-code question text trong renderer. Validate duplicate IDs, difficulty 1..10, valid answers.

## Audio
Reuse AudioManager. Queue audio để prompt/story/feedback không chồng nhau. Nút replay luôn khả dụng. Negative questions phải emphasize `NOT/CANNOT` bằng clip hoặc prosody metadata nếu asset có.

## Analytics/progress
Mỗi câu log attempt count, correct, hint used, duration. Chỉ sync server theo answer/checkpoint hoặc session complete; không gửi event cosmetic.

## Tests
- unit: selector progression, anti-repeat, answer evaluator, duplicate-letter spelling, sequence answers.
- integration: start -> answer 12 -> bell -> complete.
- scene: no listener leaks after 30 questions; restart/resume stable.

## Delivery
Cuối cùng báo:
1. File changed.
2. Architecture summary.
3. Cách import 1000 questions.
4. Cách thêm questionType mới.
5. Cách tạo một question mới chỉ bằng JSON.
6. Test commands và kết quả.
7. Limitations còn lại.
