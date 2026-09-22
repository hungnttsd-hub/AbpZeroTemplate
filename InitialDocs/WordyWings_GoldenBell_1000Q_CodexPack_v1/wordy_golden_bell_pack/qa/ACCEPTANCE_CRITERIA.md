# Acceptance Criteria — Golden Bell Challenge

1. Start session trả đúng 12 câu theo profile tăng khó.
2. Không questionType nào lặp >2 lần liên tiếp.
3. Renderer hoàn toàn data-driven.
4. Mouse/touch đều chơi được.
5. Replay audio hoạt động ở mọi câu có audio.
6. Sai không mất session/progress.
7. Hint chỉ xuất hiện theo rule.
8. Memory question có show/hide phase và replay hint tối đa theo config.
9. Sequence question validate đúng order.
10. Missing-letter support từ có chữ lặp.
11. Q4/Q8 checkpoint không làm mất state.
12. Q12 mở Final Bell, không tự complete.
13. Bell interaction tạo final payoff và gửi complete một lần.
14. Resume session giữ nguyên 12 câu và index.
15. Resize desktop/mobile không crop content chính.
16. Không duplicate listener/object sau 3 lần restart.
17. 1000-question import idempotent.
18. JSON invalid bị reject với message rõ.
19. Analytics attempt/duration/hint chính xác.
20. Existing Wordy Wings modes không regression.
