# Wordy Wings — Golden Bell Challenge Pack

Bộ này chứa **1000 câu hỏi** tăng dần từ Difficulty 1 đến 10, cùng specification để Codex thêm game mode Golden Bell Challenge vào dự án Wordy Wings hiện tại.

## File quan trọng

- `data/golden_bell_questions_1000.json` — nguồn dữ liệu canonical cho code/seed.
- `data/golden_bell_questions_1000.csv` — dễ review/filter.
- `data/golden_bell_questions_1000.xlsx` — bản review bằng Excel.
- `data/question_schema.json` — schema dữ liệu.
- `data/sample_sessions_30.json` — 30 lượt chơi mẫu, mỗi lượt 12 câu tăng khó.
- `docs/01_GAME_MODE_GDD.md` — cốt truyện, gameplay loop, progression.
- `docs/02_DIFFICULTY_CONTENT_SYSTEM.md` — hệ độ khó 1–10.
- `docs/03_QUESTION_RENDERERS.md` — cách render từng dạng câu hỏi.
- `backend/ABP_BACKEND_SPEC.md` — entity/API/seed/import.
- `frontend/PHASER_IMPLEMENTATION_SPEC.md` — scene/state/event architecture.
- `prompts/CODEX_MASTER_PROMPT.md` — prompt chính để đưa cho Codex.
- `qa/ACCEPTANCE_CRITERIA.md` — tiêu chí nghiệm thu.
- `qa/VALIDATION_REPORT.md` — kiểm tra dữ liệu.

## Nguyên tắc

- Không sao chép branding/chương trình truyền hình. “Golden Bell” chỉ là mechanic quiz theo chặng với payoff rung chuông.
- Không loại trẻ khỏi game khi sai. Sai = feedback nhẹ + second chance + hint.
- Câu hỏi cuối khó hơn nhờ **nhiều điều kiện**, không nhờ từ vựng vượt quá lứa tuổi.
- Nội dung tăng dần từ recognition → 2 clues → sentence comprehension → memory → multi-step reasoning.
