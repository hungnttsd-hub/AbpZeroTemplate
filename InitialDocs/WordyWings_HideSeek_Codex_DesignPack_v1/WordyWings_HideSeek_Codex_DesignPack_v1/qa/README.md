# Chạy kiểm tra của bộ bàn giao

## 1. Kiểm tra dữ liệu và ảnh

Tại thư mục gốc đã giải nén:

```bash
python -m pip install -r tools/requirements.txt
python tools/validate_pack.py
```

Dependency pin trong requirements là phiên bản đã dùng để kiểm tra gói, không phải yêu cầu thay version của repository app. Có thể dùng virtual environment riêng.

Script kiểm tra JSON Schema, target duy nhất, entity/asset reference, chỗ mở được, grammar đã viết cho scissors/apple/orange, ảnh và crop bounds. Muốn cập nhật báo cáo local, dùng `--write-report`; việc đó sẽ làm checksum ban đầu không còn khớp với file báo cáo mới.

## 2. Kiểm tra TypeScript rules

Tại thư mục gốc, dùng `tsc` có sẵn trong môi trường dev:

```bash
tsc --strict --target ES2022 --module commonjs --lib ES2022,DOM --outDir qa/.test-build engineering/contracts.ts engineering/rules.ts qa/rules.test.ts
node qa/.test-build/qa/rules.test.js
```

Đã chạy trong môi trường tạo gói với Node 22.16.0 và TypeScript 5.8.3. Kết quả ở `RULE_TEST_RESULTS.txt`: 40/40 ca pass. Không cần Phaser hoặc backend để chạy bộ test thuần này.

## 3. Tái tạo crop gốc

```bash
python tools/rebuild_crops.py
```

Script đọc tọa độ thật từ image-map và tái tạo D01–D35. D36 có sửa câu chữ, các tấm chú thích A00–A15 và gallery giữ nguyên. Script không tạo thêm artwork, không xóa nền và không biến crop thành sprite.

## 4. Những kiểm tra vẫn cần thực hiện trong app

Làm theo `ACCEPTANCE_CRITERIA.md`: ảnh nền không lộ đáp án, touch thật, net/berry effects, pause trong reveal, scene restart, API ownership, offline sync, performance và audio trên browser/điện thoại. Pack không có source game hiện tại nên chưa chạy các bài kiểm tra này.
