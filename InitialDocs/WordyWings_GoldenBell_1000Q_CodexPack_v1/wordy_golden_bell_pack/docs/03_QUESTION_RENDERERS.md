# 03 — Question Renderers

## Renderer contract
Mỗi question phải được render từ dữ liệu, không hard-code theo ID. `questionType` chọn renderer; `mechanic` chọn kiểu tương tác.

### listen_find_picture / picture_to_word
4 đáp án hình hoặc chữ lớn; target >= 88px; audio replay bằng nút loa.

### color_object / two_attribute_object
Tất cả đáp án cùng nhóm để distractor có ý nghĩa. Không highlight đáp án đúng trước.

### preposition_scene / multi_clue_scene
Render 3–4 mini-scene có cùng asset nhưng khác relation/attribute.

### missing_letter
Word lớn giữa màn. Một hoặc hai blank. Sau đúng, đọc từng chữ rồi cả từ.

### two_step_instruction
Đáp án được xử lý theo sequence. UI giữ dấu check nhỏ cho step 1 rồi chờ step 2.

### short_story
Story audio 1 lần tự động; replay tối đa tự do. Question xuất hiện sau story.

### memory_scene
Show scene 4–5 giây → fade → hỏi. Nếu sai, cho xem lại 1 lần như hint.

### sentence_order
Tile chữ lớn. Hỗ trợ drag và tap-to-slot. Không cần exact drag.

### word_picture_mismatch
4 cặp hình + chữ. Bé tìm 1 cặp sai.

## Feedback
- Correct: bounce + sparkle + voice “Great!” + answer audio.
- Wrong: soft shake 250ms + “Almost!”; không đỏ chói.
- Hint: sau 2 sai hoặc timeout mềm 12–18s tùy difficulty.
