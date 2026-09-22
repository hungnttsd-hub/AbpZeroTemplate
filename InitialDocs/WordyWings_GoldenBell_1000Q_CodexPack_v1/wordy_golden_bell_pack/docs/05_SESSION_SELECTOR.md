# 05 — Golden Bell Session Selector

## Mục tiêu
Mỗi lượt chơi có 12 câu, khó dần nhưng không tạo cảm giác thi căng thẳng. Session phải vừa ôn từ đã học, vừa đưa lại các từ bé hay sai.

## Difficulty profile mặc định

```text
Q01  D1
Q02  D2
Q03  D2
Q04  D3   -> checkpoint 1
Q05  D4
Q06  D5
Q07  D6
Q08  D7   -> checkpoint 2
Q09  D8
Q10  D8
Q11  D9
Q12  D10  -> Final Bell
```

Có thể dùng profile nhẹ hơn cho trẻ mới chơi: `1,1,2,2,3,4,5,6,7,8,9,10`.

## Candidate filtering

1. Chỉ chọn vocabulary/world đã unlock.
2. Ưu tiên 70–80% câu bình thường + 20–30% câu Review Queue.
3. Không lặp cùng `questionType` quá 2 lần liên tiếp.
4. Không dùng cùng target word trong 2 câu liền nhau.
5. Không cho 2 `memory_scene` liền nhau.
6. Q12 ưu tiên `final_reasoning`, `multi_clue_scene`, `memory_scene`, hoặc `short_story` Difficulty 10.

## Adaptive trong session

- Correct streak 4: câu kế tiếp có thể tăng +1 difficulty, nhưng không vượt mức unlock.
- Sai 2/3 câu gần nhất: câu kế tiếp giảm 1 difficulty và chọn skill vừa sai.
- Hint không bị tính như fail; chỉ lưu `hintUsed=true`.
- Không hỏi lại đúng câu vừa sai. Đưa target vào Review Queue và quay lại ở session sau hoặc sau ít nhất 3 câu.

## Deterministic resume

Khi start session, server lưu `Seed` và danh sách 12 QuestionCode. Resume phải dùng lại chính danh sách đó; không random lại.
