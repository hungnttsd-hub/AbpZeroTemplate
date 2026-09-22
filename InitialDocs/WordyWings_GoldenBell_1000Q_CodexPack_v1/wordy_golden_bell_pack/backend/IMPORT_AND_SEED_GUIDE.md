# Import & Seed Guide

## Source
`data/golden_bell_questions_1000.json` là nguồn canonical.

## Import pipeline

1. Parse JSON.
2. Validate root count và từng question theo `question_schema.json`.
3. Validate business rules:
   - Difficulty 1..10.
   - ID unique.
   - `option` answer phải trỏ tới option tồn tại.
   - `sequence` answer không rỗng.
4. Upsert theo `Code`/`id`.
5. Transaction toàn bộ batch.
6. Log số create/update/reject.

## ABP permission

Tạo permission admin, ví dụ:

```text
GoldenBell.Questions.Import
GoldenBell.Questions.Manage
```

Không expose import endpoint cho child/parent role.

## Versioning
Có thể thêm `ContentVersion` ở bảng question và `QuestionBankVersion` ở session để session cũ không thay đổi nếu bank được cập nhật giữa chừng.
