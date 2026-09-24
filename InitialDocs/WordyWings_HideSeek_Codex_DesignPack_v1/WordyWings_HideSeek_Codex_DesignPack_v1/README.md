# Wordy Wings — Bộ thiết kế màn Trốn tìm

**Hide & Seek · Find a cat · Handoff v1.0**

Đây là bộ bàn giao hình ảnh và hướng dẫn triển khai cho Codex, dựa trên hai ảnh concept đã có trong cuộc hội thoại. Không phải một game đã lập trình xong.

## Mở nhanh

1. Mở `OPEN_DESIGN_GALLERY.html` bằng trình duyệt để xem toàn bộ ảnh nhỏ. Không cần mạng, npm hoặc server.
2. Đọc `START_HERE_CODEX.md`, rồi đưa cả thư mục này vào workspace của dự án Wordy Wings. Dán nội dung file đó cho Codex.
3. Đọc `design/02_GAMEPLAY_RULES.md` trước khi làm logic; dùng `qa/ACCEPTANCE_CRITERIA.md` để nghiệm thu.

## Bên trong có gì?

| Thành phần | Nội dung |
|---|---|
| `references/` | Hai ảnh gốc nguyên vẹn để đối chiếu phong cách |
| `images/details/` | 36 ảnh nhỏ: tiêu đề, sao, Momo, hốc cây, bụi cây, đá, cỏ, thân cây, ba công cụ, các bước hé lộ, bảng Yes/No, hoàn thành |
| `images/annotated/` | 16 ảnh hướng dẫn có đánh dấu và ghi chú kỹ thuật; bao gồm một overlay tổng thể |
| `images/INDEX_*.jpg` | Ba ảnh mục lục để xem nhanh |
| `design/` | Thông số hình ảnh, logic, chuyển động, responsive, danh mục asset, tọa độ và nguồn crop |
| `engineering/` | Kế hoạch tích hợp, TypeScript contracts và bộ xử lý quy tắc độc lập với engine |
| `data/` | Sáu màn demo, tám đối tượng, JSON Schema, 37 lời thoại cần thu âm, ví dụ progress |
| `qa/` | Test logic, tiêu chí nghiệm thu và báo cáo kiểm tra |
| `tools/` | Script kiểm tra JSON, tham chiếu ảnh và tính toàn vẹn của gói |
| `prompts/` | Prompt làm asset còn thiếu và kiểm tra giao diện sau khi code |

## Quy tắc đã chốt

Đầu màn chỉ có chữ **Find a cat.** và nút nghe lại. Không có ảnh mèo mẫu; mọi đối tượng đều được che kín. Bé vạch lá, tung lưới nâng tán lá hoặc dùng quả mọng phép thuật làm mở lớp đá để tìm đồ vật/con vật.

Sau khi hé lộ, hỏi **Is this a cat?** với hai lựa chọn YES / NO. Gặp chó chọn NO là đúng nhưng vẫn phải tìm tiếp. Gặp mèo chọn YES mới hoàn thành màn.

Mỗi màn bắt đầu với ba sao. Trả lời sai trừ một sao; thao tác hụt hoặc mở nhầm chỗ không trừ sao. Điểm không xuống dưới 0. Sau khi đã bị trừ sao vì một đối tượng, phần trả lời lại có trợ giúp không tiếp tục trừ sao hoặc tạo thành tích độc lập.

## Ba lỗi trong ảnh concept phải sửa khi triển khai

- Ảnh chính có mắt mèo, mặt chó, hổ và kéo lộ ra. Đây chỉ là ảnh minh họa tổng hợp; **không được dùng làm background ban đầu**.
- Dòng `This is a cat?` phải đổi thành **Is this a cat?**. File D36 đã sửa riêng dòng này để đối chiếu; các chi tiết X và cảnh báo còn lại vẫn cần làm theo spec.
- Storyboard có chỗ ghi tìm đúng mèo thì “continue searching”. Đây là lỗi: **mèo + YES → hoàn thành**. Sao ở màn kết quả phải là số sao còn lại, không tự trở về ba.

## Giới hạn của bộ ảnh

Các ảnh nhỏ là **ảnh cắt và ảnh chú thích từ concept**, không phải sprite PNG nền trong, atlas hoạt hình hay file 3D có rig. Ảnh được giữ ở độ phân giải crop gốc; ảnh chú thích có thể phóng lớn để dễ nhìn, không đồng nghĩa có thêm chi tiết mới.

Codex dùng chúng làm chuẩn bố cục, màu sắc, trạng thái và tương tác. Khi làm game thật, cần dùng asset hiện có của dự án hoặc tạo/xuất layer theo `design/runtime-asset-checklist.json`. File checklist ghi rõ từng asset runtime chưa có. Gói không có file giọng nói MP3; chỉ có kịch bản audio. Không kèm file font.

Tài liệu hướng dẫn chính viết tiếng Việt. Chú thích trên ảnh và tên dữ liệu dùng tiếng Anh để đối chiếu trực tiếp với mã nguồn.

## Kiểm tra lại gói

```bash
python tools/validate_pack.py
```

Script yêu cầu Python và `jsonschema`. Cách chạy bộ test TypeScript nằm trong `qa/README.md`. Các kiểm tra này không thay thế việc chạy thử Phaser trên thiết bị thật.
