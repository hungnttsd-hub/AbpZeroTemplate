# Prompt nghiệm thu hình ảnh sau khi Codex code

Hãy kiểm tra màn Hide & Seek đã tích hợp với bộ ảnh và spec trong thư mục này. Không redesign mới và không đổi concept đã chốt.

Đầu tiên đọc `design/01_VISUAL_SPEC.md`, `design/02_GAMEPLAY_RULES.md`, `qa/ACCEPTANCE_CRITERIA.md`. Sau đó mở ảnh reference/crops trực tiếp và capture app thật ở các trạng thái sau nếu môi trường cho phép:

1. SEARCH ngay lúc vào màn: tất cả đối tượng che kín, chỉ có text Find a cat.
2. ASKING sau khi thấy chó: Is this a cat?, ảnh chó vừa lộ, YES/NO.
3. FEEDBACK sau chó + YES sai: mất đúng một sao.
4. SEARCH sau chó + NO đúng: chưa qua màn, spot chó resolved.
5. SUCCESS sau mèo + YES: Momo chào mèo, sao còn lại đúng.
6. ASSISTED_RETRY sau mèo + NO: có hỗ trợ, không bị penalty nhiều lần.

Chụp ít nhất desktop 1600×900 và mobile landscape 844×390. Kiểm tra safe area, min touch size thực sau scale, lỗi clip chữ, độ tương phản, vị trí tool và kích thước portrait.

So sánh fidelity về bố cục/màu/nhân vật, nhưng không lấy các lỗi trong ảnh concept làm yêu cầu: ảnh chính lộ con vật, câu This is a cat?, X trên câu chưa trả lời, caption mèo đúng vẫn tìm tiếp đều đã bị override trong spec.

Xuất báo cáo theo từng finding: severity, ảnh/file thực tế, expected, actual, cách sửa nhỏ nhất. Giữ các phần đã đúng; không rewrite toàn scene. Phân biệt lỗi gameplay với thiếu art. Nếu không có browser/device để capture, ghi rõ chưa kiểm chứng chứ không tạo ảnh giả như screenshot app.
