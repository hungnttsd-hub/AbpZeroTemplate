# Wordy Wings — giao diện và đồ họa v1

Cập nhật: 22/09/2026.

## Căn cứ thiết kế

- `InitialDocs/wordy_wings_gdd/README.md` và `01_GDD.md`: toy-like 2.5D, khối bo tròn, ánh sáng tươi, chất liệu mềm; Pip, Poki, Lulu và Momo có hình dáng riêng.
- `InitialDocs/WordyWings_BalloonDart_DesignPack_v1/WordyWings_BalloonDart_DesignPack_v1/references/selected-balloon-dart-concept.png`: vườn bóng sáng, bóng có độ bóng, bảng kem, súng đồ chơi và nhân vật hỗ trợ.
- Giữ yêu cầu hình ảnh trên bóng, không có nhãn từ; Word Builder có pha săn chữ và pha xếp/xây, ô đích không in sẵn đáp án.

Đây là bộ đồ họa triển khai theo art direction của tài liệu, không phải bản sao từng pixel của ảnh concept. Phạm vi bản đồ chính hiện tại là W01–W03 theo repository. Các nhiệm vụ thử nghiệm dùng chung bộ đồ họa và vật thể này; chưa có bối cảnh độc lập cho đủ 10 thế giới tương lai.

## Phạm vi cập nhật

- Trang chào, menu, hồ sơ, bản đồ ba đảo, đường đi, thẻ nhiệm vụ, khu phụ huynh, popup, pause và kết quả dùng màu kem/xanh/gỗ/vàng đồng nhất.
- Bốn nhân vật dùng chung atlas giữa React và Phaser.
- Cảnh chơi đổi theo W01 (đồng cỏ), W02 (rừng tre, sông, thác), W03 (vườn nhà, gỗ ấm). Chỉ nạp một cảnh nền cho mỗi màn; nền scale kiểu cover, không kéo méo khi đổi tỉ lệ.
- Bóng dùng SVG gradient theo đúng màu ngữ nghĩa; hình trên bóng vẫn là vector rõ nét. Súng, đạn đồ chơi, bảng sao và Poki đồng bộ cùng phong cách.
- Word Shot có Pip mới, gỗ/đá có mặt sáng/tối và khung chọn đạn mới. Bản nâng cấp tiếp theo bổ sung sáu loại đạn và hiệu ứng; xem `word-shot.md` để biết quy tắc băng, thiên thạch, độ bền đá và phân bổ đạn.
- Word Builder dùng thùng, bụi cây, spring và pulley mới; chữ thu được nằm trong thẻ gỗ. Cầu có ván riêng, nước, trụ cầu; các hoạt cảnh cửa, lồng, tàu, rương, nồi được bổ sung chất liệu. Momo vẫn phản ứng và di chuyển trong payoff.
- Các hình từ vựng SVG được bổ sung ánh sáng theo từng màu và bóng mềm; màu ngữ nghĩa được thay trước khi tạo gradient.
- Khoảng cách tiêu đề/vật thể/khay được điều chỉnh; vùng chạm của bóng săn chữ bao gồm cả phần bóng. Chữ hướng dẫn tự xuống dòng.

## Tài nguyên

Tất cả nằm tại `react/public/art/`, được Vite sao chép vào build và `src/WebHoanTien.Web/wwwroot/wordy-wings/art/` khi chạy `npm run build:host`.

| File | Kích thước | Sử dụng |
| --- | --- | --- |
| meadow-v1.png | 1672 × 941 | Đồng cỏ, vườn bóng |
| forest-v1.png | 1672 × 941 | Đảo Muông Thú |
| home-v1.png | 1672 × 940 | Ngôi Nhà Vui Vẻ |
| friends-v1.png | 1254 × 1254, alpha | Pip, Poki, Lulu, Momo |
| islands-v1.png | 2172 × 724, alpha | Ba đảo trên bản đồ, từng ô 724 × 724 |
| props-v1.png | 1254 × 1254, alpha | Crate, bush, spring, pulley, từng ô 627 × 627 |

`react/src/game/artManifest.ts` giữ đường dẫn và frame nhân vật; `theme.ts` nạp asset, tạo frame, bóng và khung gỗ. URL dùng `import.meta.env.BASE_URL` nên dùng được dưới `/wordy-wings/`. Asset ảnh được tạo bằng **image_gen tích hợp (built-in)**; bản gốc vẫn được giữ trong thư mục generated_images của Codex. Không cần API key để chạy game.

## Bộ brief/prompt tạo ảnh

Các yêu cầu chung: original children's English game Wordy Wings; soft sunny toy-like 2.5D painted art; rounded forms and gentle light; production-ready game asset; no words, labels, logos, UI or watermarks. Không dùng nhân vật hoặc tài nguyên Angry Birds.

1. **Meadow:** wide 16:9 sunny meadow background, blue sky, distant mountains and lake, waterfall and cottages at far edges, grass, fence and daisies in lower corners. Keep the center 70% open and low detail for gameplay. No characters or targets.
2. **Friends:** transparent 2 × 2 character atlas. Top left: Pip, yellow chick explorer with safari hat, teal scarf and backpack. Top right: Poki, friendly panda explorer with backpack and scarf. Bottom left: Lulu, white inventor rabbit with goggles and lavender scarf. Bottom right: Momo, green baby dragon with cream belly, small horns, peach wings and curling tail. Full bodies, consistent material and light, no background or text.
3. **Islands:** transparent wide 3:1 atlas, three equal cells, rounded isometric floating islands. Rainbow meadow with rainbow/path; Animal Island with palms, waterfall and wooden hut; cozy cottage island with cream house, orange roof, teal door and garden. No characters, labels or UI.
4. **Props:** transparent 2 × 2 atlas. Honey-wood crate with brass bolts (no X); rounded leafy green bush; teal/gold spring pad; wood/teal pulley with rope and handle. Toy-like materials, full objects with padding, no characters or letters.
5. **Forest:** use meadow as style reference. Distinct friendly jungle setting with bamboo and broad tropical leaves at outer edges, waterfall at distant left, turquoise river behind an open grassy clearing, mossy stepping rocks at bottom corners. Pale mint-blue sky fills upper half; keep central 70% empty for play. Luminous green, turquoise, warm golden light. Front-on side-view landscape 16:9, opaque full bleed, no targets or characters.
6. **Home:** use meadow as style reference. Cozy cottage garden: cream cottage with rounded terracotta roof and teal door at far left, warm wood porch and pastel cushions at far right, distant picket fence and hills. Open honey-colored wooden terrace merges into grass at lower quarter. Central 70% empty, pale warm sky above. Peach, cream, mint, honey wood and blue palette. Side-view landscape 16:9, opaque full bleed, no characters or UI.

## Xác nhận và giới hạn

- Đã xem trực tiếp các ảnh tạo ra và kiểm tra kích thước/alpha của atlas; cấu hình frame dùng kích thước thực tế.
- `npm run build:host`: TypeScript và Vite build thành công, đã sao chép sang ABP web host. Cảnh báo kích thước chunk Phaser vẫn còn; Phaser đã là chunk riêng.
- Không chạy unit test, Playwright hoặc kiểm thử trình duyệt tự động theo `AGENTS.md`. Chưa xác nhận trực tiếp toàn bộ bố cục trong trình duyệt hoặc đo FPS trên thiết bị.
- Không thay schema, database hay migration. Chơi trên thiết bị vẫn dùng chế độ local hiện có.
- Tổng sáu PNG khoảng 10 MB; một màn chỉ tải một nền cùng atlas nhân vật/vật thể. Nếu thay ảnh trong tương lai, tăng hậu tố phiên bản để tránh cache cũ.


