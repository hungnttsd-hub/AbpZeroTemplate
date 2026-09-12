# CatBack Glossy Dashboard — 12/09/2026

## Reference và phạm vi

Đọc README, PROMPT_CODEX, spec, blueprint, toàn bộ crops/SVG, token và checklist trong `ScreenPlan/CatBack_Glossy_Dashboard_Template_Codex_v1/CatBack_Glossy_Dashboard_Template_Codex_v1`. Visual chính: `reference/01_final_template.png`.

Cập nhật dashboard trên template Razor hiện tại. Sidebar, topbar và token dùng chung tiếp tục áp dụng cho trang khách; các trang quản trị được loại trừ theo `_CustomerLayout.cshtml`. Ngưỡng 1024px của yêu cầu giữ mobile được ưu tiên hơn ngưỡng 992px trong handoff.

## Thay đổi

- `wwwroot/customer-desktop.css`: màu navy/cyan, card 18px, shadow mềm, chữ Montserrat đậm rõ; toàn bộ nằm trong media query từ 1024px và scope `body.cb-desktop-enabled`.
- Sidebar 224px, từ 1440px dùng 244px; topbar 70px; padding 24px/26px và nội dung tối đa 1600px. Menu active có viền cyan sáng, thêm tagline và khối mascot theo reference.
- Hero thống nhất nền mint/cyan và lớp highlight trong suốt, headline lớn, mascot hiện hữu, nhóm thông tin đơn giản/an toàn/uy tín và thẻ 80% màu teal. Giữ một nút cài đặt PWA với ID cũ.
- Khung chính hai cột 1.92/1. Ở 1024–1199px, phần tạo link/danh sách chiếm cả hàng, ví và lưu ý chuyển xuống hàng tiếp theo. Không cố thu nhỏ toàn bộ màn hình bằng transform.
- Form tạo link gọn lại, icon có nền glossy và CTA navy/teal; sửa độ ưu tiên CSS của input chung để không tạo viền kép trong input shell. Không đổi handler, ID, validation, kết quả hoặc template AJAX.
- Thẻ ví dùng gradient navy–teal–cyan và illustration SVG hiện hữu; giữ binding số dư. Khách chưa đăng nhập vẫn chỉ thấy lời mời đăng nhập.
- Dải lợi ích có icon nền pastel; bảng 5 đơn hàng dùng ảnh sản phẩm có viền, header rõ hơn, tiền hoàn dương màu xanh lá, giá trị 0 màu teal. Vẫn dùng `CustomerOrderUi` cho giá trị và nhãn trạng thái.
- Giữ nguyên văn 5 lưu ý ghi nhận hoa hồng. Lưu ý cuối đặt trên nền mint; không thay bằng nội dung mẫu hoặc rút xuống 4 bước.
- Giữ Link đã tạo và mọi thao tác hiện có. Không thêm API, đổi PageModel/backend, asset nhận diện, JavaScript hay framework UI trong lượt chỉnh glossy này.

## File sửa trong lượt glossy

Các đường dẫn dưới đây thuộc `src/WebHoanTien.Web`:

- `wwwroot/customer-desktop.css`
- `Pages/Index.cshtml`
- `Pages/Shared/_CustomerDesktopSidebar.cshtml`
- `Pages/Shared/_CustomerHeroTrust.cshtml` (mới, có `hidden` trên mobile)
- `Pages/Shared/_CustomerRecentOrders.cshtml`

## Xác minh

- Rà soát tĩnh: cấu trúc ngoặc CSS và phạm vi media 1024px; asset nội bộ không thiếu; 7 ID chính của form/PWA/kết quả xuất hiện một lần; danh sách hướng dẫn đủ 5 mục.
- `git diff --check` không phát hiện lỗi whitespace.
- Build Razor thành công, **0 warning, 0 lỗi**, bằng lệnh dưới đây; output riêng tránh ghi đè DLL của phiên debug đang chạy:

```powershell
dotnet build src/WebHoanTien.Web/WebHoanTien.Web.csproj --no-restore -m:1 -p:UseSharedCompilation=false -p:OutDir=E:/HungNT/WebHoanTien/artifacts/glossy-desktop-build/ -v:quiet
```

- Không chạy test, Playwright hoặc kiểm thử trình duyệt tự động, theo AGENTS.md và yêu cầu người dùng. Chưa có screenshot của bản chạy sau chỉnh sửa; không coi build/static review là xác nhận độ khớp trực quan.

## Phần cần đối chiếu trực quan khi được yêu cầu

- Desktop 1024/1280/1366/1440/1672/1920px: sidebar, hero, wrap chữ, chiều cao card và cuộn bảng chỉ trong card.
- Mobile 390/640/768/1023px: header, bottom nav, drawer, form, modal và thứ tự nội dung như trước.
- Chưa đăng nhập, tài khoản thường/ẩn danh/quản trị; dữ liệu rỗng/dài/lớn; validation, xử lý, kết quả sản phẩm/shop; sao chép, mua hàng và ẩn/hiện link.
- Turbo, back/forward, notification badge và cài đặt PWA.

Các khác biệt có chủ ý so với ảnh: dùng mascot/illustration thật của CatBack thay vì hình render trong reference, giữ nút cài đặt, đủ 5 đơn hàng và 5 lưu ý. Chiều cao khối dữ liệu được phép tăng theo nội dung thật, không cắt để ép bằng screenshot mẫu.

## Điều chỉnh điều hướng theo phản hồi

- Gỡ ô tìm kiếm chưa có chức năng khỏi topbar chung, gồm trang chủ và các trang khách khác.
- Trên template desktop từ 1024px, ẩn drawer tài khoản và các nút mở drawer. Avatar liên kết trực tiếp tới trang tài khoản; Đăng xuất dùng route cũ ở topbar.
- Các liên kết quản trị chuyển sang nhóm có thể mở/thu gọn ở sidebar trái, dùng chính các kết quả kiểm tra quyền hiện có trong layout. Giữ `data-turbo="false"` cho Identity, Hangfire và đăng xuất như trước.
- Khi đổi từ mobile sang desktop trong lúc drawer đang mở, đóng drawer ngay, bỏ khóa cuộn và chuyển focus tới liên kết tài khoản. Dưới 1024px vẫn dùng drawer cũ; layout quản trị không đổi.
- Kiểm tra cú pháp `customer-navigation.js` bằng `node --check` và rà soát `git diff --check` thành công. Không chạy test hay kiểm thử trình duyệt tự động.

## Banner và surface v2 theo phản hồi

- Banner desktop chuyển sang ba cột thực: nội dung, mascot, thẻ 80%; giữ mốc chiều cao tối thiểu 278px và co giãn ở 1024–1199 / 1200–1439 / từ 1440px.
- Nền trang trí `wwwroot/catback/hero-glossy-background-v2.png` được tạo bằng ImageGen từ crop banner reference: sóng cyan, mây, túi mua sắm và xu vàng. File 2048×768, khoảng 1.37 MiB, chỉ được tham chiếu trong CSS desktop. Đây là ảnh nền trang trí không chứa chữ, control hoặc mascot; không dùng screenshot toàn bộ banner làm UI.
- Mascot vẫn là `catback-mascot-reference.png`, không tạo lại nhân vật. Chữ, nhóm thông tin, khối 80% và nút cài đặt vẫn là HTML. Bổ sung bubble nội dung và icon vương miện từ Font Awesome hiện có.
- Nút cài đặt PWA giữ một ID và handler, hiển thị dạng mũi tên tròn trên desktop như reference; tên truy cập và tooltip vẫn giải thích chức năng cài đặt. Mobile giữ nhãn chữ của nút.
- Token chung bổ sung highlight, bóng viền và gradient trên CTA, card, box, nút phụ, bộ lọc và modal của các trang khách. Không thay màu trạng thái unread/validation, dữ liệu hay hành vi xử lý.
- Build Razor thành công: 0 warning, 0 lỗi. Rà soát tĩnh CSS/media query/asset/ID, cú pháp PWA và whitespace thành công. Không chạy test, Playwright hoặc kiểm thử trình duyệt tự động; chưa có đối chiếu screenshot bản chạy tại các viewport.
- Khác biệt so với reference: dùng nhân vật gốc của dự án (tư thế hiện hữu); nền là artwork dựng lại theo mẫu, không phải sao chép pixel từ screenshot. Chưa xác nhận khớp pixel trên trình duyệt.

## Banner gốc theo yêu cầu mới nhất — thay thế cách dựng v2

Người dùng yêu cầu giữ nguyên thiết kế, không thay mèo bằng mascot của CatBack. Banner desktop hiện dùng trực tiếp `reference/crops/03_hero_banner.png`, sao chép nguyên file sang `wwwroot/catback/hero-approved-reference.png`; SHA-256 của hai file giống nhau. Không tạo lại bằng AI và không ghép mascot, chữ hoặc bubble lên hình.

- Ảnh banner giữ tỷ lệ 1365×278, rộng 100% và chiều cao tự nhiên. Chỉ hiển thị từ 1024px; phần hero mobile hiện hữu giữ nguyên.
- Banner dẫn tới form tạo link. Một nút PWA hiện hữu đặt trong suốt tại vị trí mũi tên của artwork, giữ handler, ID, tooltip và tên truy cập. Khi nút PWA bị ẩn theo logic hiện có, banner vẫn dẫn tới form tạo link.
- Gỡ các rule dựng lại hero và các breakpoint tương ứng để không còn chồng hình mèo, chữ hoặc gradient cũ lên banner. Các nút/card glossy và điều hướng desktop của trang khác không đổi.
- Rà soát tĩnh xác nhận CSS chỉ áp dụng từ 1024px, cấu trúc ngoặc hợp lệ, ID form/PWA không trùng và không còn tham chiếu ảnh nền v2. Build Razor thành công, 0 warning, 0 lỗi. Không chạy test hoặc kiểm thử trình duyệt tự động.

Banner là hình marketing giữ nguyên theo yêu cầu mới nhất, nên chữ bên trong là raster và thu nhỏ cùng hình; phần giải thích HTML/alt và thao tác thật vẫn được giữ. Yêu cầu này thay thế cách dựng banner bằng HTML và mascot cũ được mô tả ở các mục trước.
