# Prompt tách/tạo asset runtime theo ảnh đã chốt

Đây là hướng dẫn cho Codex phối hợp với họa sĩ/công cụ tạo ảnh có sẵn trong môi trường. Gói hiện tại là reference crops; không tự nhận chúng là alpha sprite.

## Quy trình

Đọc `design/runtime-asset-checklist.json` và mở ảnh `references/01_selected_scene.png`, `references/02_interaction_board.png`. Giữ Momo rồng nhỏ xanh, phong cách 2D/2.5D hoạt hình tròn mềm, chất liệu lá/gỗ/đá và ánh sáng ban ngày tương đồng.

Ưu tiên asset đã có trong repo. Chỉ tạo/xuất những slot còn thiếu. Mỗi file chỉ chứa một asset/layer rõ ràng; không tạo cả UI hoàn chỉnh rồi dùng như background.

## Background sạch

Tạo nền rừng ngang 1600×900 trở lên: cây lớn bên trái, khoảng trống cho Momo, khu giữa đặt bụi/đá/cỏ và khoảng dưới cho toolbar. Không có mèo, chó, hổ, kéo hay bất kỳ đối tượng bài hỏi nào trong nền. Không chữ, logo, nút, sao, popup hoặc hướng dẫn. Không vẽ hình con vật bằng bóng/cành cây vô tình gợi đáp án.

## Cover

Mỗi tree_hollow / bush / rock / tall_grass / hollow_log cần closed và open, cùng kích thước/pivot. Closed phải che kín object area. Open có chỗ để object xuất hiện rõ. Không vẽ con vật vào cover. Foliage có thể tách trái/phải cho drag. Rock tách 3–4 mảnh mềm, không sắc.

## Object

Mèo, chó, hổ, kéo: bám D31–D34, nhưng xuất đầy đủ silhouette và nền trong. Apple/orange/ball/rabbit dùng library từ vựng hiện có hoặc tạo đồng bộ, không dùng một ảnh cho hai từ. Không label viết trong ảnh.

## Momo

D05 và D35 là chuẩn hình dáng; cần idle, encourage, thinking, celebrate. Nếu chưa đủ frame animation thật, dùng một sprite sạch + tween nhẹ và ghi rõ giới hạn. Không crop các pose khác nhau từ concept làm một walk cycle thiếu nhất quán.

## Tools và VFX

Hand/leaves, net, foam berry: icon nền trong, readable ở khoảng 56 CSS px. Net chỉ nâng lớp lá, không trói động vật. Berry là vật phẩm phép thuật mềm, không kíp nổ hoặc mô hình vũ khí thật. VFX puff/lá/sparkle vừa đủ, không che nhận diện object quá lâu.

## Xuất và kiểm tra

Dùng PNG RGBA hoặc định dạng dự án đang hỗ trợ, padding 8–16 px khi pack atlas, pivot ghi rõ, naming theo asset key. Kiểm tra viền alpha trên nền sáng/tối, không có checkerboard bake vào ảnh, không halo trắng quá rõ. File chữ và font không nằm trong texture; UI text do runtime render.

Khi giao asset, cập nhật checklist chỉ khi file đã tồn tại thật. Không đổi `runtimeFileIncluded` thành true chỉ vì có đường dẫn dự kiến hoặc ảnh tham chiếu. Không tạo base64 giả/sprite rỗng để làm validator pass.
