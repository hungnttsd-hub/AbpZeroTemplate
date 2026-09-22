# 01 — Golden Bell Challenge: Game Design Document

## 1. Vai trò trong Wordy Wings
Golden Bell Challenge là **màn ôn tập/boss stage** xuất hiện sau mỗi 1–2 World. Bé cùng Poki, Lulu, Foxy, Pip và Momo đi tới Bell Island để đánh thức **Star Bell**. Mỗi câu đúng nạp một Word Star vào chuông. Khi đủ năng lượng, bé trực tiếp kéo dây hoặc chạm búa để rung chuông.

## 2. Cốt truyện ngắn
Mr. Mumble làm Star Bell mất tiếng. Lulu phát hiện chuông chỉ thức dậy khi thu đủ Word Stars. Cả đội bước vào thử thách gồm 12 câu. Sau mỗi checkpoint, một đoạn đường lên tháp chuông sáng lên. Câu 12 là Final Bell.

## 3. Session chuẩn — 12 câu
- Q1–2: Difficulty 1–2 — warm up.
- Q3–4: Difficulty 2–3 — recognition + 1 thuộc tính.
- Q5–6: Difficulty 4–5 — 2 clues / sentence.
- Q7–8: Difficulty 6–7 — two-step / mismatch / short story.
- Q9–10: Difficulty 8 — negative/detail/spelling 2 blanks.
- Q11: Difficulty 9 — memory hoặc 3 facts.
- Q12: Difficulty 10 — Final Bell, nhiều điều kiện hoặc reasoning nhẹ.

## 4. Gameplay loop
1. Camera pan tới Bell Arena.
2. Pip/Lulu giới thiệu câu bằng audio.
3. Stimulus xuất hiện.
4. Bé trả lời bằng mechanic phù hợp.
5. Đúng: Word Star bay vào chuông, đường sáng thêm một đoạn, nhân vật phản ứng.
6. Sai: bounce/shake nhẹ, “Almost!”, không mất mạng. Sau 2 sai hoặc chờ lâu, hint.
7. Checkpoint ở Q4 và Q8: mini celebration 2–3 giây.
8. Q12 xong: chuông đầy năng lượng → kéo dây/chạm búa → big payoff.

## 5. Mechanics sử dụng
- `answer_zone`: chạm/vào khu vực A/B/C/D.
- `raise_board`: chọn đáp án rồi nhân vật giơ bảng.
- `listen_run`: nghe lệnh, nhân vật chạy tới mục tiêu.
- `quick_match`: ghép cặp / xếp từ nhanh.
- `bell_choice`: lựa chọn 2–4 đáp án lớn.

## 6. Không Game Over
Mục tiêu là kiểm tra và củng cố. Không dùng lives, red X lớn, loại khỏi sàn, hay timer gây áp lực. Sau sai lần 1 chỉ feedback. Sai lần 2: hint nhẹ. Sai lần 3: visual guide rõ hơn nhưng vẫn để trẻ tự chạm đáp án.

## 7. Reward
- 1 Star Energy cho mỗi câu hoàn thành.
- 3 cosmetic sparkles cho streak 3/6/9 câu.
- Bell Token khi hoàn thành session.
- Từ hay nhầm được đưa vào `Review Queue`.

## 8. Thời lượng
Một session: 5–8 phút. Q1–6 khoảng 8–14 giây/câu, Q7–12 khoảng 14–25 giây/câu.
