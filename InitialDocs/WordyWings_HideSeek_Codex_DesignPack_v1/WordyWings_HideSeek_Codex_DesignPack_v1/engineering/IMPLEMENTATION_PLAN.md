# Kế hoạch triển khai cho Codex

## Phạm vi và nền tảng

Gói không chứa repository hiện tại. Vì vậy mọi tên thư mục, scene, API trong tài liệu là gợi ý; phải map sang dự án thật. Tái sử dụng ABP backend, React app shell, Phaser engine, asset/audio manager và progress pipeline. Không nâng version framework hoặc đổi template chỉ để thêm màn này.

Trước hết đọc package lock, project files .NET và scene registry. Tài liệu Phaser online có nhiều phiên bản; API mask/lifecycle phải theo phiên bản đang cài, không chép ví dụ của bản khác. Xem `engineering/SOURCES.md`.

## Chặng 1 — Khảo sát và ranh giới

Tìm entry point game, route vào level, content loader, hệ thống từ vựng, lưu checkpoint, API service và test runner. Lập bảng reuse/refactor/new. Không sửa gameplay khác ngoài thay đổi chung thật sự cần; bảo vệ bằng regression test.

Đầu ra: ghi chú integration thực tế, danh sách file và các dependency đang có. Không hỏi người dùng những điều có thể đọc từ repository.

## Chặng 2 — Dữ liệu và rules độc lập

Validate level JSON với schema và kiểm tra cross-field. Nạp vocabulary qua adapter để không nhân bản dữ liệu đã tồn tại trong game. Giữ authored sentences đầy đủ; không hard-code grammar.

`contracts.ts` và `rules.ts` là mẫu chạy được ở lớp logic thuần, không import Phaser hoặc ABP. Chạy test kèm theo trước khi tích hợp, sau đó port vào conventions của repo. Không tạo một nguồn rule khác trong UI với cách tính sao riêng.

MVP phải nhận được HS-01 từ dữ liệu, tạo seeded mapping, start state với ba sao và mọi occupant invisible.

## Chặng 3 — Một vertical slice chạy thật

Tạo một hiding spot có leaf cover, cho reveal → quiz → NO quay lại tìm hoặc YES hoàn thành. Kết nối một controller state machine duy nhất. Kiểm tra xử lý double tap trước khi thêm trang trí.

Dùng nền rừng sạch. Nếu chưa có asset layer, tạo placeholder rõ trong dev và báo pending art; tuyệt đối không dùng ảnh concept có mắt mèo/mặt chó như backdrop để “trông giống” nhưng làm sai gameplay.

## Chặng 4 — Hoàn thiện ba công cụ

Thêm net và berry bằng cùng `RevealInteraction` interface. Nó nhận spot, emit started/finished/cancelled, và có dispose/pause/resume. Hiệu ứng không sở hữu scoring hoặc completion. Thay đổi VFX không được thay câu trả lời đúng.

Mỗi interaction có hitbox đủ lớn, cancel sạch, unlock đúng sau khi xong. Hỗ trợ reduced motion từ đầu để không phải viết logic thứ hai sau này.

## Chặng 5 — UI, sao và âm thanh

Dựng component theo D01–D18, D36 và annotation A01–A15. Text render runtime. Gỡ X của panel chưa trả lời. Dùng 3 trạng thái nút: default / pressed / disabled; YES/NO không leak correctness.

Dùng AudioManager hiện có. Thứ tự: quest → reveal neutral → question → feedback identification. Khi audio lỗi vẫn có text. Không voice name occupant trước trả lời.

Scoring áp dụng trong reducer, UI chỉ phản ánh state. Reward animation không được quyết định việc ghi điểm.

## Chặng 6 — Responsive và content mở rộng

Chạy sáu demo, kiểm tra đổi target từ cat sang dog/scissors/apple để phát hiện hard-code. Reflow landscape nhỏ; portrait pause; đo kích thước CSS thực của button. Các spot không đè lên toolbar hoặc unreachable sau crop.

Random occupant theo seed nhưng không đổi mapping giữa một phiên. Ghi seed cùng checkpoint để reproduce test và resume.

## Chặng 7 — Persistence và backend

Tạo adapter quanh progress service hiện có. Không gọi network từ `pointermove` hoặc mỗi frame. Queue event/checkpoint khi answer được chấp nhận; flush theo chính sách app. Completion idempotent theo event/session.

Server kiểm tra quyền profile, version level và event trùng; recompute stars nếu authoritative scoring được yêu cầu. Frontend offline không có bí mật đáp án thực sự: đừng hứa chống reverse engineering. Đây là game học, không gắn phần thưởng tiền thật.

## Chặng 8 — Nghiệm thu

Chạy tests của repo và tests rules. Capture SEARCH, ASKING(dog), FEEDBACK(wrong), SUCCESS(cat) ở desktop và ít nhất một viewport ngang nhỏ. Đối chiếu từng source ảnh và rule đã sửa, đặc biệt target leakage.

Kết quả bàn giao: file thay đổi, entry route/demo command, API mappings thực, build/test log, ảnh chụp chạy thật nếu khả dụng, asset còn thiếu. Không gọi một tập ảnh reference hoặc static gallery là prototype game đã chạy.

## Cấu trúc gợi ý

```text
src/game/mechanics/hide-seek/
  HideSeekController.ts
  HideSeekContentAdapter.ts
  HideSeekProgressAdapter.ts
  HideSeekAudioAdapter.ts
  rules/
  views/HidingSpotView.ts
  views/HideSeekHud.ts
  views/QuizPresenter.ts
  interactions/RevealInteraction.ts
  interactions/SwipeLeaves.ts
  interactions/ThrowNet.ts
  interactions/BlastBerry.ts
```

Tách scene hoặc plugin theo kiến trúc sẵn có. Mục tiêu là trách nhiệm rõ, không bắt buộc một file cho mỗi tên nếu dự án đang tổ chức khác.
