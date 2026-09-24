# Ghi chú tích hợp ABP + React + Phaser

## 1. Không giả định repository

Chưa có source repository trong gói này. Không coi tên `/api/game/...`, `HideSeekScene` hoặc đường dẫn `src/game` là cấu trúc đã kiểm tra. Đầu tiên tìm service và pattern thực tế, rồi áp dụng contracts qua adapter. Gói chỉ bổ sung một mechanic và dữ liệu.

Giữ version dependency đang được lock. Tài liệu Phaser có API theo version; ví dụ mask ở bản 3 và bản 4 không được tráo qua lại. Lớp `rules.ts` không phụ thuộc engine nên tránh được vấn đề này; view/animation mới cần map sang engine hiện có. Nguồn đối chiếu: [P1]–[P4] trong `SOURCES.md`.

## 2. Input và scene lifecycle

Phaser cung cấp input cho mouse/touch qua pointer events; dùng cơ chế của engine thay vì tự đặt listener DOM riêng cho từng cover [P1]. View có activePointerId; clip/hit area đủ lớn và cập nhật sau resize. Không dùng pixel-perfect hit test cho toàn bộ lá nếu không cần.

Scene shutdown/destroy cần dừng tween, hủy timer, tháo event bus, hủy request hoặc ignore callback cũ. Các phase cần quản lý tương ứng với scene lifecycle thực tế của version đang dùng [P3]. Pause nên giữ state, không đồng nghĩa destroy controller.

Nếu dùng mask ở Phaser 3, đọc đúng tài liệu pinned [P2]. Mask chỉ ảnh hưởng render, không tự vô hiệu input/physics. Bitmap mask trong tài liệu bản 3 yêu cầu WebGL. Cách đơn giản hơn cho MVP: closed cover kín + object invisible cho đến lúc reveal kết thúc; không cần phụ thuộc một shader mask mới.

## 3. Một nguồn state

Phaser view lắng nghe controller; React shell chỉ nhận event cần thiết như exit/completed/paused. Không có hai state `stars` ở React và Phaser tự cập nhật song song. UI animation phản ánh reducer, không tự thay giá trị score.

Event gợi ý:

```text
HIDE_SEEK_STARTED
TOOL_SELECTED
REVEAL_STARTED
REVEAL_FINISHED
ANSWER_ACCEPTED
FEEDBACK_ACKNOWLEDGED
LEVEL_COMPLETED
CHECKPOINT_QUEUED
```

`TOOL_SELECTED` và animation frame không cần gửi server. AnswerAccepted phải có eventId, sessionId, revealId, spotId, chosenAnswer, assisted flag, contentVersion. Tên occupant trong dữ liệu client phục vụ render không phải nguồn xác thực score server.

## 4. Seed, shuffle và checkpoint

Mỗi phiên chỉ shuffle mapping một lần. Dùng seeded PRNG sẵn có của repo hoặc một hàm đã được test; lưu seed hoặc mapping đã materialize. Tập spot tương thích cần được kiểm tra: mọi occupant có sprite vừa vùng reveal; mỗi spot có công cụ để mở; không có hai target.

Checkpoint gồm level/version/seed, phase, stars, resolvedSpotIds, revealedSpotIds, pendingReveal, processedAnswerIds và attempts. Active time không bao gồm pause. Có thể lưu tối thiểu sau mỗi accepted answer và khi app xuống nền; không ghi DB ở mỗi pointermove.

Resume ở `revealing`: nếu không lưu timeline, quay lại cover closed cùng pending interaction hoặc hoàn tất reveal một lần theo policy rõ ràng; không tự nộp câu trả lời. Resume ở `feedback` giữ việc đã trừ sao, không chạy lại penalty. Resume ở `assisted_retry` giữ trạng thái có trợ giúp.

## 5. Backend ABP: adapter, không viết lại auth

ABP có hệ thống authorization cho application services [A1]; ngoài quyền gọi service, phải kiểm tra người dùng được phép truy cập child profile cụ thể. Một attribute authorize chung không đủ để ngăn đổi childId xem tiến độ người khác.

Ưu tiên thêm mechanic discriminator vào content/progress pipeline hiện có. Nếu chưa có điểm tích hợp, API đề xuất sau là ví dụ để thảo luận, **không phải endpoint đã tồn tại**:

```http
GET  /api/app/game-content/hide-seek/{levelId}
POST /api/app/game-progress/hide-seek-checkpoint
POST /api/app/game-progress/hide-seek-complete
```

Server nên xác định: current user/profile ownership; level version; session seed/mapping; tập answerEventId đã nhận. Score authoritative được tính lại từ first answer của từng reveal và target identification, không chấp nhận tùy ý starsRemaining từ client.

Duplicate eventId trả kết quả đã lưu hoặc no-op có kiểm soát. Unique constraint hợp lý có thể theo `(ProfileId, SessionId, EventId)` và completion theo session/version, tùy schema dự án. Tránh request retry cộng reward hai lần. Replay level là session mới nhưng best-stars không cộng dồn.

## 6. Offline và private data

Client có thể chơi level/assets đã cache; tiến độ queue local rồi sync. Không tạo user/password/auth mới. Không lưu PII trong file level, log debug hoặc asset path. Không thu microphone/camera cho mode này.

Đáp án và hình cần nằm ở client để offline; không gọi đây là cơ chế chống gian lận bảo mật. Không gắn star của mode này với tiền thật hoặc leaderboard có giải thưởng nếu chưa có thiết kế xác thực riêng.

## 7. Deploy

Không thêm dịch vụ chỉ để chạy công cụ vạch lá. Web build và backend deploy qua pipeline hiện có. Asset upload phát sinh về sau cần storage bền vững của dự án; không mặc định ghi file runtime vào container backend. Cấu hình Render, CDN, auth và migrations nằm ngoài phạm vi bàn giao này trừ khi repo yêu cầu sửa tương thích rõ ràng.

## 8. Tách QA logic và QA giao diện

40 unit tests của pack chỉ kiểm tra pure rules: state, chân trị, sao, duplicate, assisted retry, pause và data-driven target. Chúng không chứng minh animation, input thật, storage, ABP API hoặc Capacitor đã chạy. Chỉ báo những lệnh/test/device thực sự được thực hiện khi tích hợp.
