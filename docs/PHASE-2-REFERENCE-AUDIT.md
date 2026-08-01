# Giai đoạn 2 — Khảo sát website tham chiếu IZONE

> Trạng thái: đang khảo sát. Tài liệu này ghi nhận bằng chứng đã thu thập được trước khi tạo bất kỳ domain model hoặc UI mới nào.

## 1. Design system quan sát được

| Thành phần | Bằng chứng |
|---|---|
| Màu chính | `#db0829` (đỏ IZONE); màu phụ `#174266` (xanh đậm); nền trắng và xám nhạt. |
| Typography | Website dùng `Geologica, sans`; heading lớn đậm, body khoảng 16px/24px. |
| Header | Header trắng sticky, logo trái, menu nhiều cấp, ô tìm kiếm, nút Đăng nhập/Đăng ký; trên mobile giữ logo, search và hamburger. |
| Giao diện | Container rộng, card bo góc lớn, ảnh/đỏ thương hiệu làm trọng tâm, CTA đỏ viền/pill, nhiều carousel/slider. |
| Responsive | Desktop 1280 dùng menu ngang; mobile 390 dùng menu thu gọn; section xếp một cột. |
| Thành phần cố định | Rail “Lịch khai giảng”, số điện thoại/chat/Zalo bên phải; CTA tư vấn và kiểm tra trình độ rõ ràng. |

## 2. Sitemap public đã xác nhận

| Nhóm | URL đại diện | Mục đích/flow |
|---|---|---|
| Trang chủ | `/` | Hero, thông tin giá trị, khóa học, giảng viên, học viên, video, tài liệu, CTA. |
| Giới thiệu | `/gioi-thieu-ve-izone/` | Tầm nhìn, sứ mệnh, giá trị, timeline, học bổng và sự kiện. |
| Khóa học | `/cac-khoa-hoc-ielts` | Danh sách khóa theo trình độ, CTA lịch khai giảng/tư vấn. |
| Chi tiết khóa | `/course/khoa-hoc-ielts-4-0/` | Nội dung khóa, học viên phù hợp, lộ trình, giảng viên và CTA. |
| Lộ trình | `/lo-trinh-hoc-ielts-izone/` | Lộ trình từ mất gốc đến 7.0+. |
| Lịch khai giảng | `/lich-khai-giang-cac-lop-ielts/` | Tra cứu lịch học/lớp. |
| Giảng viên | `/teacher/` | Danh sách giảng viên; dẫn tới hồ sơ. |
| Học viên | `/student` | Thành tích, chứng thực và phản hồi. |
| Luyện thi | `/luyen-thi-ielts/` và `/luyen-thi-ielts/{skill}/` | Bài học/bài tập theo kỹ năng và trình độ. |
| Kho tài liệu | `/document` | Tài liệu theo Grammar, Vocabulary, Pronunciation và IELTS. |
| Blog | `/blog/` | Nội dung kiến thức; có taxonomy skill/level/topic. |
| Liên hệ | `/lienhe-izone` | Flow nhận tư vấn. |
| Sách/landing | `/dang-ky-sach/` | Landing nhận thông tin/đăng ký sách. |
| Tài khoản | `/login`, `/register-premium` | Đăng nhập và đăng ký premium. |

## 3. Homepage — thứ tự section đã quan sát

1. Header/menu/search/auth và hero carousel.
2. CTA tư vấn khóa học + kiểm tra trình độ.
3. Video/đơn vị tin cậy, ba giá trị (đội ngũ, thiết kế khóa học, học phí).
4. 8 khóa học: 5 khóa chính, 3 khóa bổ trợ.
5. Phương pháp giảng dạy độc quyền.
6. Carousel giảng viên.
7. Học viên nói gì về IZONE.
8. Luyện thi IELTS miễn phí và kho tài liệu.
9. CTA cuối trang, footer, rail lịch khai giảng và contact actions.

## 4. Component & interaction map

| Component | Trạng thái/behavior cần tái tạo |
|---|---|
| Mega menu | Điều hướng nhiều cấp theo giới thiệu, giảng viên, khóa học, luyện thi và tài liệu; keyboard/focus required. |
| Search | Form GET với tham số `s`; desktop/mobile đều có. |
| Hero carousel | Nhiều banner, CTA từng slide, desktop/mobile asset riêng. |
| Popup campaign | Nội dung khuyến mại theo thời hạn, đóng được; cần do admin điều khiển và không hard-code. |
| CTA registration | Chuyển đến form tư vấn; cần attribution/UTM, validation, rate limit và chống gửi trùng. |
| Schedule lookup | Bộ lọc và trạng thái lớp; không cho đăng ký khi lớp đã đầy/hủy/kết thúc. |
| Content pages | Taxonomy, search, pagination, tài liệu/download có quyền. |
| Test flow | Cần tách thành service/module riêng: profile → hướng dẫn → test/timer/autosave → chấm điểm → xếp lớp → tư vấn. |

## 5. Dữ liệu cần quản trị

- Site settings, navigation, footer links, campaign popup, banners, homepage sections.
- Course, learning path, class/schedule, location, teacher, student outcome/testimonial.
- Article/knowledge content/document attachment; taxonomy theo skill, level và topic.
- Lead, consultation request, interaction, assignment và enrollment.
- Placement test, section, question, answer, attempt, result và level mapping.

## 6. Rủi ro và điểm cần xác nhận

- Popup/banners/khuyến mại trên source thay đổi theo thời gian, nên chỉ dùng làm mẫu component chứ không seed nội dung thời vụ.
- Một số flow kiểm tra trình độ/dịch vụ premium có thể nằm ở hệ thống khác; cần API, SSO hoặc quy tắc nghiệp vụ do chủ sở hữu cung cấp.
- Asset hiện tải từ WordPress/source site sẽ không được hotlink vào hệ thống mới; chỉ dùng bộ asset đã có quyền hoặc asset do bạn cung cấp.
- Trước khi kết thúc giai đoạn 2 vẫn cần ghi nhận đầy đủ các trạng thái mở menu, filter lịch khai giảng, form validation và các trang chi tiết đại diện ở desktop/mobile.

## 7. Kế hoạch sau khi hoàn tất giai đoạn 2

1. Chốt page map, component inventory và dữ liệu động.
2. Thiết kế bounded context `Education`, ERD, permissions, migration plan và chiến lược giữ/chuyển module Store.
3. Chỉ sau đó mới viết Design System, public layout và từng module nghiệp vụ theo thứ tự prompt.
