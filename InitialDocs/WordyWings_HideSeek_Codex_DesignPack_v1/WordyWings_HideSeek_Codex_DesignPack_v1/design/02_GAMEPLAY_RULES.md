# 02 — Quy tắc màn Trốn tìm

## 1. Điều kiện mở màn

Tải và validate level trước khi enable công cụ. Mỗi level có đúng một target, ít nhất một distractor, hai đến sáu hiding spot, công cụ đủ để mở từng spot. Khi dữ liệu lỗi, đưa về màn tải lại/thoát an toàn; không để bé chơi một màn không thể thắng.

Session tạo `sessionId`, `contentVersion`, seed và một mapping occupant → spot cố định. Shuffle chỉ khi tạo session mới. Resume không xáo lại. HS-01 có bốn đối tượng: cat, dog, tiger, scissors. Ba sao là đánh giá **trong phiên chơi**, không phải số dư vật phẩm của tài khoản.

Mục tiêu: Find a cat. Quest word luôn được phép nhìn thấy; không bắt trẻ phải nhớ audio. Điều bị cấm là ảnh/tiếng con vật mẫu hoặc clue gợi sẵn cách hiểu từ, chứ không phải chữ cat.

## 2. Cốt truyện ngắn

Momo và bạn bè đang chơi trốn tìm trong khu rừng. Các bạn giấu những con vật và đồ vật sau tán lá, thân cây và đá phép thuật. Momo cần bé giúp tìm đúng thứ được gọi tên. Không phải cuộc săn bắt; đồ vật và con vật đều an toàn.

Opening voice: “Find a cat.” Momo có thể nói “Let's find it together!” nhưng không có mũi tên chỉ vị trí đúng. Sau một lựa chọn NO đúng: “Keep looking!” Sau YES đúng trên mục tiêu: “You found it!” Không cần cắt cảnh dài hoặc đọc một đoạn tiếng Việt trên màn chính.

## 3. State machine

```text
SEARCHING
  -> REVEALING     [valid tool + selected spot]
  -> ASKING        [cover animation finished]
  -> FEEDBACK      [one YES/NO decision accepted]
     -> SEARCHING       [any distractor; after feedback]
     -> ASSISTED_RETRY  [target + wrong NO]
     -> COMPLETED       [target + correct YES]

ASSISTED_RETRY
  -> FEEDBACK [YES -> completed; NO -> assisted_retry, no extra star charge]

PAUSE is an orthogonal flag preserving the current phase and pending reveal.
```

`revealing` khóa tool khác và spot khác. `asking` khóa world, cho dùng YES/NO, nghe lại câu hỏi và pause. `feedback` khóa YES/NO. Có thể có nút Continue trong feedback; không đồng thời cho callback audio và nút tạo hai lần chuyển state. `completed` không nhận thêm reveal/answer.

Không mở quiz ngay khi pointerdown lên lá, khi chưa có gì được hé lộ. Tạo `revealId` trước animation, nhưng chỉ render object và cho hỏi sau `finishReveal`.

## 4. Bảng chân trị

| Revealed == Target? | Answer | Correct? | Sao | Sau feedback |
|---|---|---|---|---|
| true | YES | true | giữ | COMPLETED |
| true | NO | false | -1 lần đầu | ASSISTED_RETRY |
| false | NO | true | giữ | SEARCHING |
| false | YES | false | -1 lần đầu | SEARCHING |

Luật không phụ thuộc string “cat”, loại tool, thứ tự khám phá hoặc số spot đã mở. Không kết luận thắng vì một câu bất kỳ trả lời đúng.

## 5. Feedback tiếng Anh

Trước trả lời, chỉ có câu hỏi target: `Is this a cat?` Không phát “dog” và không gắn label DOG lên object.

Sau khi thấy chó và chọn NO: `No. It is a dog.` Sau khi thấy chó và chọn YES sai, dùng chính câu giải thích đó; animation sao khác nhưng không mắng bé. Với kéo: `No. These are scissors.` Với mèo: `Yes! It is a cat.`

Nếu sai NO trên mèo, cho nghe câu đúng và mở assisted retry, kèm gợi ý chọn YES. Không di chuyển mèo sang spot khác để bắt tìm lại. Đây là sửa lỗi có hướng dẫn; lần YES sau đó không tính như nhận diện độc lập.

Đối tượng nhiễu sau khi đã xử lý được đánh dấu tìm rồi và vẫn có thể ngồi trong scene như bạn đồng hành. Tắt trigger quiz của spot đã xử lý; không lặp cùng câu để kiếm điểm hoặc trừ sao vô hạn.

## 6. Luật sao chi tiết

- `initialStars = 3`.
- `starsRemaining = max(0, initialStars - số reveal bị trả lời sai lần đầu)`.
- Mỗi reveal chỉ charge một lần; charge gắn `revealId`, không gắn tọa độ pointer hoặc label đối tượng.
- Không cộng lại sao khi trả lời assisted đúng. Không charge thêm vì gọi lại request, double click, animation restart hoặc reload checkpoint.
- Chọn công cụ không hợp, vuốt thiếu ngưỡng, chạm trúng lá trang trí, tìm được vật nhiễu, bấm pause hoặc nghe lại: **không phải sai kiến thức**.
- Ở 0 sao: chơi tiếp, gợi ý thao tác; mục tiêu vẫn phải được tìm và xác nhận. Không tự thắng, không âm sao, không reset phiên.
- Số sao ở kết quả, progress DTO và parent report phải trùng. “Best stars” nếu có là số cao nhất giữa các lần chơi; không cộng vào tài khoản sau mỗi lần replay.

## 7. Hint

Mặc định không có target outline, không tự mở lá hoặc nhìn chằm chằm của Momo về target. Sau 10–15 giây không thao tác, gợi ý **cách dùng công cụ** tại một spot chưa tìm chọn độc lập với target. Đây là thông số khởi đầu để thử với bé, không phải ngưỡng khoa học cố định.

Sau một câu trả lời sai, giải thích đúng tên đối tượng rồi cho tiếp tục. Ở assisted retry có thể làm nổi YES vì lúc này đáp án đã được dạy; phải đánh dấu trợ giúp để không làm sai thống kê.

Không dùng hint “meow” trước khi tìm mèo nếu mục tiêu của level là tự hiểu cat; có thể thêm vào mode tập làm quen khác nhưng không bật trong bản này.

## 8. Nội dung nâng dần

Sáu demo dùng cùng một mechanic và schema:

| Level | Nhiệm vụ | Số chỗ | Điểm kiểm thử |
|---|---|---:|---|
| HS-01 | Find a cat. | 4 | Toàn bộ flow cơ bản + ba công cụ |
| HS-02 | Find a dog. | 5 | Không hard-code mèo |
| HS-03 | Find a tiger. | 5 | Đổi target/distractor giữa các lần |
| HS-04 | Find the scissors. | 4 | Câu hỏi số nhiều Are these scissors? |
| HS-05 | Find an apple. | 5 | Mạo từ an, nhiễu apple/orange |
| HS-06 | Find an orange. | 6 | Nhiều chỗ hơn, phân biệt hai loại quả |

Difficulty 1–3 trong demo là mức tải của mechanic, không phải chứng nhận trình độ ngôn ngữ. Chỉ đưa vào lượt chơi các vocabulary bé được phép tiếp cận theo curriculum của app.

## 9. Các tình huống phải xử lý

Audio chưa unlock: đợi chạm hoặc hiện replay; không khóa toàn game. Mất mạng: vẫn dùng level đã tải và queue progress. Không có asset bắt buộc: hiển thị lỗi rõ cho người lớn hoặc dùng fallback được đánh dấu trong build dev, không lén thay mèo bằng chó.

Ứng dụng xuống nền hoặc portrait: pause mọi animation/input/audio liên quan, không phạt. Khi trở lại, hiển thị đúng phase đã lưu. Một callback VFX cũ sau scene destroy không được gọi tiến độ hay trừ sao.

## 10. Không nằm trong MVP

Không multiplayer, chat, quảng cáo, mua vật phẩm, nhận diện giọng nói, backend mới hoặc tạo câu bằng AI realtime. Không thêm chấm điểm tốc độ. Không yêu cầu camera/microphone. Màn mới là phần mở rộng nhỏ của hệ thống đang có.
