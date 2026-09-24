# Tiêu chí nghiệm thu Hide & Seek

## A. Bắt buộc về nội dung

- [ ] Nhiệm vụ đầu HS-01 là Find a cat. Có replay audio và text fallback.
- [ ] Không có ảnh mèo mẫu ở header, loading screen, button gợi ý hoặc popup trước khi mở chỗ.
- [ ] Không lộ mắt/tai/đuôi/bóng mèo; không lộ chó/hổ/kéo trong trạng thái SEARCH.
- [ ] Không tiếng meo hoặc hướng nhìn/outline target-specific trước lần trả lời đầu.
- [ ] Prompt là Is this a cat?, không phải This is a cat?.
- [ ] Đổi target sang apple/scissors thì mạo từ và số nhiều vẫn đúng theo data.

## B. Luật thắng và sao

- [ ] Chó + NO → đúng, giữ sao, tiếp tục tìm; không qua màn.
- [ ] Hổ + NO và kéo + NO có cùng behavior ở màn tìm mèo.
- [ ] Mèo + YES → hoàn thành một lần, báo progress đúng.
- [ ] Mèo + NO → trừ một sao, giải thích, assisted retry; không biến mèo mất đi.
- [ ] Chó + YES → trừ một sao, giải thích là dog, rồi tiếp tục tìm.
- [ ] Sai trong cùng reveal không bị charge lại vì double tap/retry/assisted retry.
- [ ] Tìm sai chỗ không trừ sao; gesture hụt không trừ sao.
- [ ] 0 sao vẫn tìm và xác nhận target được; không âm sao hoặc force restart.
- [ ] Kết quả cuối giữ đúng sao còn lại, không tự hiện ba sao theo ảnh concept.
- [ ] Assisted YES không tăng chỉ số hiểu độc lập.

## C. Tương tác và state

- [ ] Ba tool đều dùng được bằng mouse và touch; có tap alternative cho kéo lá.
- [ ] Tool không hợp spot chỉ bounce/hint, không crash hoặc penalty.
- [ ] Net nâng tán lá và tan trước khi object hiện; không siết động vật.
- [ ] Berry chỉ mở rock cover, không bắn vào động vật đã lộ.
- [ ] Object chỉ visible sau reveal hoàn tất; mỗi lần chỉ có một reveal active.
- [ ] Mở quiz thì world/tool input bị khóa; click không xuyên.
- [ ] Xóa X khỏi câu hỏi đang chờ, nhưng pause/thoát theo flow app vẫn hoạt động.
- [ ] Đang feedback thì không nhận answer lần hai.
- [ ] Spot resolved không hỏi lại để farm sao/penalty.
- [ ] Lặp scene 20 lần không tăng listener, không giữ timer cũ hoặc duplicate game object.

## D. Hình ảnh

- [ ] Background runtime sạch, không phải concept bitmap có sẵn đáp án.
- [ ] Dùng layer riêng cho cover/object; text runtime thay vì chữ trong ảnh.
- [ ] Momo là nhân vật xanh theo reference, không đổi IP hoặc model tùy ý.
- [ ] Không dùng vòng sáng hoặc nét viền để đánh dấu đối tượng đúng trước trả lời.
- [ ] Hai nút YES/NO cùng kích thước; trạng thái pressed/disabled rõ.
- [ ] Font tiếng Anh rõ; UI tiếng Việt không lỗi dấu, clipping hoặc tofu.
- [ ] Chi tiết lá/đá không che hit target chính và không chồng toolbar.

## E. Responsive và hỗ trợ

- [ ] 1600×900, 1280×720, 1024×768, 844×390 và 667×375 có thể chơi.
- [ ] Hit target quan trọng đạt ít nhất 48 CSS px sau scaling.
- [ ] Safe area không che pause hoặc tool; resize giữ mapping và sao.
- [ ] Portrait pause không mất tiến độ; quay ngang resume đúng phase.
- [ ] Pointercancel và touch thứ hai không tạo reveal trùng.
- [ ] Reduced motion hoạt động; mute vẫn chơi bằng text được.
- [ ] Audio bị chặn hoặc mất asset có fallback; không tự lộ ảnh target để thay.

## F. Progress và backend

- [ ] Không request trên từng frame hoặc pointermove.
- [ ] Checkpoint sau answer giữ processed event IDs và penalty đã áp dụng.
- [ ] Offline queue retry không cộng/trừ điểm trùng.
- [ ] Server kiểm tra profile ownership, level/version và session theo hệ thống thực tế.
- [ ] Nếu score authoritative: server tính lại, không tin starsRemaining client.
- [ ] Không đưa PII của trẻ vào log debug hoặc analytics không cần thiết.
- [ ] Các mechanic cũ và routes vẫn chạy; test/build hiện có không bị phá.

## G. Những ca kiểm thử bằng tay quan trọng

| Ca | Chuỗi thao tác | Kỳ vọng |
|---|---|---|
| H01 | Mở chó, NO, mở mèo, YES | 3 sao, hoàn thành |
| H02 | Mở chó, YES, mở mèo, YES | 2 sao, hoàn thành |
| H03 | Mở mèo, NO, assisted YES | 2 sao, hoàn thành có hỗ trợ |
| H04 | Mở mèo, NO, NO, NO, YES | Chỉ trừ một lần cho reveal mèo; không tính assisted là độc lập |
| H05 | Sai ba distractor rồi tìm mèo | 0 sao nhưng vẫn hoàn thành được |
| H06 | Double tap YES trên chó | Một penalty, một event |
| H07 | Pause trong reveal rồi resume | Không lộ sớm, không reveal/câu hỏi trùng |
| H08 | Resize khi quiz đang mở | Giữ object/câu hỏi/sao, không click xuyên |
| H09 | Audio bị chặn | Text còn; không tự reveal đáp án |
| H10 | Đổi target thành scissors | Are these scissors? và feedback số nhiều |

Bảng này là yêu cầu cho app sau tích hợp. Những mục chưa chạy thật phải được ghi pending, không tự đánh dấu pass vì test thuần đã pass.
